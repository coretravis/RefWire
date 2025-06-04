using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text.Json;

namespace ListServDB.Security.File;

/// <summary>
/// File-based implementation of the IApiKeyRepository interface with encryption.
/// </summary>
public class FileApiKeyRepository : IApiKeyRepository, IDisposable
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<FileApiKeyRepository> _logger;
    private readonly ApiKeyFileStorageOptions _options;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private readonly string _cacheKey = "ApiKeys_All";
    private bool _isInitialized = false;
    private bool _disposedValue;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        IncludeFields = true
    };

    public FileApiKeyRepository(
        IMemoryCache memoryCache,
        IOptions<ApiKeyFileStorageOptions> options,
        ILogger<FileApiKeyRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(memoryCache, nameof(memoryCache));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));
        ArgumentNullException.ThrowIfNull(options, nameof(options));

        _cache = memoryCache;
        _logger = logger;
        _options = options.Value;

        if (string.IsNullOrEmpty(_options.FilePath))
        {
            throw new ArgumentException("File path cannot be null or empty", nameof(options));
        }

        // Check if encryption key and IV have been properly set
        if (_options.EncryptionKey.All(b => b == 0))
        {
            throw new ArgumentException("Encryption key must be set", nameof(options));
        }

        if (_options.EncryptionIV.All(b => b == 0))
        {
            throw new ArgumentException("Encryption IV must be set", nameof(options));
        }
    }

    /// <summary>
    /// Initializes the repository by creating the file if it doesn't exist and loading keys into cache.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        await _semaphore.WaitAsync();

        try
        {
            if (_isInitialized)
            {
                return;
            }

            _logger.LogInformation("Initializing encrypted API key file repository with file {FilePath}", _options.FilePath);

            // Create directory if it doesn't exist
            string? directory = Path.GetDirectoryName(_options.FilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Load all keys into cache
            await RefreshCacheAsync();

            _isInitialized = true;
            _logger.LogInformation("Encrypted API key file repository initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize encrypted API key file repository");
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task EnsureInitializedAsync()
    {
        if (!_isInitialized)
        {
            await InitializeAsync();
        }
    }

    /// <summary>
    /// Encrypts data using AES encryption.
    /// </summary>
    /// <param name="plainText">The plain text to encrypt.</param>
    /// <returns>The encrypted data as a byte array.</returns>
    private byte[] Encrypt(string plainText)
    {
        using Aes aes = Aes.Create();
        aes.Key = _options.EncryptionKey;
        aes.IV = _options.EncryptionIV;

        ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

        using MemoryStream memoryStream = new MemoryStream();
        using CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
        using (StreamWriter streamWriter = new StreamWriter(cryptoStream))
        {
            streamWriter.Write(plainText);
        }

        return memoryStream.ToArray();
    }

    /// <summary>
    /// Decrypts data using AES decryption.
    /// </summary>
    /// <param name="cipherText">The encrypted data to decrypt.</param>
    /// <returns>The decrypted plain text.</returns>
    private string Decrypt(byte[] cipherText)
    {
        using Aes aes = Aes.Create();
        aes.Key = _options.EncryptionKey;
        aes.IV = _options.EncryptionIV;

        ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

        using MemoryStream memoryStream = new MemoryStream(cipherText);
        using CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
        using StreamReader streamReader = new StreamReader(cryptoStream);

        return streamReader.ReadToEnd();
    }

    /// <summary>
    /// Refreshes the cache by loading API keys from the encrypted file.
    /// </summary>
    public async Task RefreshCacheAsync()
    {
        _logger.LogDebug("Refreshing API keys cache from encrypted file");
        List<ApiKey> keys = new List<ApiKey>();

        try
        {
            if (System.IO.File.Exists(_options.FilePath))
            {
                byte[] encryptedData = await System.IO.File.ReadAllBytesAsync(_options.FilePath);

                try
                {
                    string json = Decrypt(encryptedData);
                    var storageKeys = JsonSerializer.Deserialize<List<ApiKeyStorageModel>>(json, _jsonSerializerOptions) ?? new List<ApiKeyStorageModel>();
                    keys = storageKeys.Select(k => k.ToApiKey()).ToList();
                }
                catch (CryptographicException ex)
                {
                    _logger.LogError(ex, "Error decrypting API keys file. The file may be corrupted or the encryption key/IV may be incorrect.");
                    keys = new List<ApiKey>();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deserializing API keys from decrypted content");
                    keys = new List<ApiKey>();
                }
            }
            else
            {
                _logger.LogInformation("Encrypted API keys file does not exist yet. Creating an empty list.");
                keys = new List<ApiKey>();

                // Create an empty file
                await SaveKeysToFileAsync(keys);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading API keys from encrypted file");
            keys = new List<ApiKey>();
        }

        // Set the cache with all keys
        _cache.Set(_cacheKey, keys, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _options.CacheExpirationTime,
            Priority = CacheItemPriority.High
        });

        _logger.LogInformation("Loaded {Count} API keys into cache", keys.Count);
    }

    private async Task SaveKeysToFileAsync(List<ApiKey> keys)
    {
        var storageModels = keys.Select(k => new ApiKeyStorageModel
        {
            Id = k.Id,
            Name = k.Name,
            Description = k.Description,
            ApiKeyHash = k.ApiKeyHash,
            IsRevoked = k.IsRevoked,
            DateCreated = k.DateCreated,
            ExpiresAt = k.ExpiresAt,
            Scopes = k.Scopes
        }).ToList();

        string json = JsonSerializer.Serialize(storageModels, _jsonSerializerOptions);

        // Encrypt the JSON
        byte[] encryptedData = Encrypt(json);

        // Create a temporary file and then replace the original to avoid file corruption
        string tempFile = _options.FilePath + ".tmp";
        await System.IO.File.WriteAllBytesAsync(tempFile, encryptedData);
        System.IO.File.Move(tempFile, _options.FilePath, true);

        _logger.LogDebug("Saved {Count} encrypted API keys to file", keys.Count);
    }

    /// <summary>
    /// Gets all API keys from the cache or file.
    /// </summary>
    public async Task<List<ApiKey>> GetAllAsync()
    {
        await EnsureInitializedAsync();

        if (_cache.TryGetValue(_cacheKey, out List<ApiKey>? keys) && keys != null)
        {
            return keys.ToList();
        }

        // If cache miss, refresh cache and try again
        await RefreshCacheAsync();

        if (_cache.TryGetValue(_cacheKey, out keys) && keys != null)
        {
            return keys.ToList();
        }

        return new List<ApiKey>();
    }

    /// <summary>
    /// Gets an API key by its ID.
    /// </summary>
    public async Task<ApiKey?> GetByIdAsync(Guid id)
    {
        await EnsureInitializedAsync();

        var keys = await GetAllAsync();
        return keys.FirstOrDefault(k => k.Id == id);
    }

    /// <summary>
    /// Creates a new API key.
    /// </summary>
    public async Task<ApiKey> CreateAsync(ApiKey apiKey)
    {
        ArgumentNullException.ThrowIfNull(apiKey, nameof(apiKey));

        await EnsureInitializedAsync();

        await _semaphore.WaitAsync();
        try
        {
            // Get the current list of keys
            var keys = await GetAllAsync();

            // Remove existing key with same ID if it exists
            keys.RemoveAll(k => k.Id == apiKey.Id);

            // Add the new key
            keys.Add(apiKey);

            // Save all keys to file
            await SaveKeysToFileAsync(keys);

            // Update cache
            _cache.Set(_cacheKey, keys, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _options.CacheExpirationTime,
                Priority = CacheItemPriority.Normal
            });

            _logger.LogInformation("Added API key with ID {ApiKeyId}", apiKey.Id);
            return apiKey;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Updates an existing API key.
    /// </summary>
    public async Task UpdateAsync(ApiKey apiKey)
    {
        ArgumentNullException.ThrowIfNull(apiKey, nameof(apiKey));

        await EnsureInitializedAsync();

        // Check if the API key exists
        _ = await GetByIdAsync(apiKey.Id) ??
            throw new KeyNotFoundException($"API key with ID {apiKey.Id} not found");

        // Use the same implementation as CreateAsync
        await CreateAsync(apiKey);
    }

    /// <summary>
    /// Deletes an API key by its ID.
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        await EnsureInitializedAsync();

        await _semaphore.WaitAsync();
        try
        {
            // Get the current list of keys
            var keys = await GetAllAsync();

            // Check if the key exists
            if (!keys.Any(k => k.Id == id))
            {
                _logger.LogWarning("API key with ID {ApiKeyId} not found for deletion", id);
                return false;
            }

            // Remove the key
            keys.RemoveAll(k => k.Id == id);

            // Save the updated list to file
            await SaveKeysToFileAsync(keys);

            // Update cache
            _cache.Set(_cacheKey, keys, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _options.CacheExpirationTime,
                Priority = CacheItemPriority.Normal
            });

            _logger.LogInformation("Deleted API key with ID {ApiKeyId}", id);
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Revokes an API key by its ID.
    /// </summary>
    public async Task<bool> RevokeAsync(Guid id)
    {
        await EnsureInitializedAsync();

        await _semaphore.WaitAsync();
        try
        {
            // Get the current list of keys
            var keys = await GetAllAsync();

            // Find the key
            var key = keys.FirstOrDefault(k => k.Id == id);
            if (key is null)
            {
                _logger.LogWarning("API key with ID {ApiKeyId} not found for revoke", id);
                return false;
            }

            // Mark as revoked
            var updatedKey = key.Revoke();

            // Update the key in the list
            keys.RemoveAll(k => k.Id == id);
            keys.Add(updatedKey);

            // Save changes
            await SaveKeysToFileAsync(keys);

            // Update cache
            _cache.Set(_cacheKey, keys, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _options.CacheExpirationTime,
                Priority = CacheItemPriority.Normal
            });

            _logger.LogInformation("Revoked API key with ID {ApiKeyId}", id);
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _semaphore.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
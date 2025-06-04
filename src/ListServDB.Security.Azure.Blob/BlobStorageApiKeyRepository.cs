using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace ListServDB.Security.Azure.Blob;

public class BlobStorageApiKeyRepository : IApiKeyRepository, IDisposable
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<BlobStorageApiKeyRepository> _logger;
    private readonly BlobContainerClient _containerClient;
    private readonly BlobClient _blobClient;
    private readonly ApiKeyBlobStorageOptions _options;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private readonly string _cacheKey = "ApiKeys_All";
    private readonly string _apiKeysBlobName = "api-keys.json";
    private bool _isInitialized = false;
    private bool _disposedValue;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        IncludeFields = true
    };
    public BlobStorageApiKeyRepository(
        IMemoryCache memoryCache,
        IOptions<ApiKeyBlobStorageOptions> options,
        ILogger<BlobStorageApiKeyRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(memoryCache, nameof(memoryCache));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));
        ArgumentNullException.ThrowIfNull(options, nameof(options));

        _cache = memoryCache;
        _logger = logger;
        _options = options.Value;

        if (string.IsNullOrEmpty(_options.ConnectionString))
        {
            throw new ArgumentException("Azure Blob Storage connection string cannot be null or empty", nameof(options));
        }

        // Create blob container client
        _containerClient = new BlobContainerClient(_options.ConnectionString, _options.ContainerName);
        _blobClient = _containerClient.GetBlobClient(_apiKeysBlobName);
    }

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

            _logger.LogInformation("Initializing API key blob storage repository with container {ContainerName}", _options.ContainerName);

            // Create the container if it doesn't exist
            await _containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            // Load all keys into cache
            await RefreshCacheAsync();

            _isInitialized = true;
            _logger.LogInformation("API key blob storage repository initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize API key blob storage repository");
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

    public async Task RefreshCacheAsync()
    {
        _logger.LogDebug("Refreshing API keys cache from blob storage");
        List<ApiKey> keys = new List<ApiKey>();

        try
        {
            if (await _blobClient.ExistsAsync())
            {
                var response = await _blobClient.DownloadAsync();

                using var streamReader = new StreamReader(response.Value.Content);
                var json = await streamReader.ReadToEndAsync();

                try
                {
                    var storageKeys = JsonSerializer.Deserialize<List<ApiKeyStorageModel>>(json, _jsonSerializerOptions) ?? new List<ApiKeyStorageModel>();
                    keys = storageKeys.Select(k => k.ToApiKey()).ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deserializing API keys. JSON content: {JsonContent}", json);
                    keys = new List<ApiKey>();
                }
            }
            else
            {
                _logger.LogInformation("API keys blob does not exist yet. Creating an empty list.");
                keys = new List<ApiKey>();

                // Create an empty file
                await SaveKeysToStorageAsync(keys);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading API keys from blob");
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

    private async Task SaveKeysToStorageAsync(List<ApiKey> keys)
    {
        string json = JsonSerializer.Serialize(keys, _jsonSerializerOptions);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        await _blobClient.UploadAsync(stream, overwrite: true);

        _logger.LogDebug("Saved {Count} API keys to blob storage", keys.Count);
    }

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

    public async Task<ApiKey?> GetByIdAsync(Guid id)
    {
        await EnsureInitializedAsync();

        var keys = await GetAllAsync();
        return keys.FirstOrDefault(k => k.Id == id);
    }

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

            // Save all keys to blob storage
            await SaveKeysToStorageAsync(keys);

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

    public async Task UpdateAsync(ApiKey apiKey)
    {
        ArgumentNullException.ThrowIfNull(apiKey, nameof(apiKey));

        await EnsureInitializedAsync();

        // Check if the API key exists
        _ = await GetByIdAsync(apiKey.Id)
            ??
            throw new KeyNotFoundException($"API key with ID {apiKey.Id} not found");

        // Use the same implementation as CreateAsync
        await CreateAsync(apiKey);
    }

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

            // Save the updated list to blob storage
            await SaveKeysToStorageAsync(keys);

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

    public async Task<bool> RevokeAsync(Guid id)
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
                _logger.LogWarning("API key with ID {ApiKeyId} not found for revoke", id);
                return false;
            }

            // Remove the key
            keys.RemoveAll(k => k.Id == id);

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

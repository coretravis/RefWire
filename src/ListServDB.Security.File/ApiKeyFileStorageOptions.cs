namespace ListServDB.Security.File;

/// <summary>
/// Represents the configuration options for File-based API Key Storage.
/// </summary>
public class ApiKeyFileStorageOptions
{
    /// <summary>
    /// Gets or sets the file path where the encrypted API keys are stored.
    /// </summary>
    public string FilePath { get; set; } = "api-keys.json.enc";

    /// <summary>
    /// Gets or sets the cache expiration duration for the API keys.
    /// </summary>
    public TimeSpan CacheExpirationTime { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Gets or sets the encryption key used to encrypt/decrypt the API keys file.
    /// Should be a 32-byte key (256 bits) - stored in a secure location like Azure Key Vault in production.
    /// </summary>
    public byte[] EncryptionKey { get; set; } = new byte[32];

    /// <summary>
    /// Gets or sets the initialization vector (IV) used for encryption/decryption.
    /// Should be a 16-byte IV (128 bits) - stored in a secure location like Azure Key Vault in production.
    /// </summary>
    public byte[] EncryptionIV { get; set; } = new byte[16];
}

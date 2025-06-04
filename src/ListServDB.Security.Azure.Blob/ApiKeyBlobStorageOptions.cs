namespace ListServDB.Security.Azure.Blob;

/// <summary>
/// Represents the configuration options for API Key Blob Storage.
/// </summary>
public class ApiKeyBlobStorageOptions
{
    /// <summary>
    /// Gets or sets the connection string used to connect to the Azure Blob Storage.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the container name where the API keys are stored.
    /// </summary>
    public string ContainerName { get; set; } = "api-keys";

    /// <summary>
    /// Gets or sets the cache expiration duration for the API keys.
    /// </summary>
    public TimeSpan CacheExpirationTime { get; set; } = TimeSpan.FromMinutes(30);
}

namespace ListServDB.Security;

/// <summary>
/// Represents the storage model for an API key.
/// </summary>
public class ApiKeyStorageModel
{
    /// <summary>
    /// Gets or sets the primary identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the API key.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the API key.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the hash of the API key.
    /// </summary>
    public string ApiKeyHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the API key is revoked.
    /// </summary>
    public bool IsRevoked { get; set; }

    /// <summary>
    /// Gets or sets the date the API key was created.
    /// </summary>
    public DateTime DateCreated { get; set; }

    /// <summary>
    /// Gets or sets the expiration date of the API key.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the scopes associated with the API key.
    /// </summary>
    public IReadOnlyCollection<string> Scopes { get; set; } = new List<string>();

    /// <summary>
    /// Converts the storage model to an <see cref="ApiKey"/> instance.
    /// </summary>
    /// <returns>An <see cref="ApiKey"/> instance.</returns>
    public ApiKey ToApiKey() => new ApiKey(Id, Name, Description, ApiKeyHash, DateCreated, ExpiresAt, Scopes, IsRevoked);
}

namespace ListServDB.Security;

/// <summary>
/// Interface for API key repository to manage API keys.
/// </summary>
public interface IApiKeyRepository
{
    /// <summary>
    /// Gets an API key by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the API key.</param>
    /// <returns>The API key if found; otherwise, null.</returns>
    Task<ApiKey?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets all API keys.
    /// </summary>
    /// <returns>A list of all API keys.</returns>
    Task<List<ApiKey>> GetAllAsync();

    /// <summary>
    /// Creates a new API key.
    /// </summary>
    /// <param name="apiKey">The API key to create.</param>
    /// <returns>The created API key.</returns>
    Task<ApiKey> CreateAsync(ApiKey apiKey);

    /// <summary>
    /// Updates an existing API key.
    /// </summary>
    /// <param name="apiKey">The API key to update.</param>
    Task UpdateAsync(ApiKey apiKey);

    /// <summary>
    /// Deletes an API key by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the API key to delete.</param>
    /// <returns>True if the API key was deleted; otherwise, false.</returns>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// Revokes an API key by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the API key to revoke.</param>
    /// <returns>True if the API key was revoked; otherwise, false.</returns>
    Task<bool> RevokeAsync(Guid id);

    /// <summary>
    /// Reloads the cache of API keys.
    /// </summary>    
    Task RefreshCacheAsync();
}

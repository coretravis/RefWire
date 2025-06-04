namespace ListServDB.Security;

/// <summary>
/// Service interface for managing API keys.
/// </summary>
public interface IListServApiKeyService
{
    /// <summary>
    /// Creates a new API key.
    /// </summary>
    /// <param name="name">The name of the API key.</param>
    /// <param name="description">The description of the API key.</param>
    /// <param name="expiresAt">The expiration date of the API key. Optional.</param>
    /// <param name="scopes">The scopes associated with the API key. Optional.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the API key creation response.</returns>
    Task<ApiKeyCreationResponse> CreateApiKey(string name, string description, DateTime? expiresAt = null, IEnumerable<string>? scopes = null);

    /// <summary>
    /// Updates an existing API key.
    /// </summary>
    /// <param name="id">The identifier of the API key.</param>
    /// <param name="name">The new name of the API key.</param>
    /// <param name="description">The new description of the API key.</param>
    /// <param name="scopes">The new scopes associated with the API key. Optional.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the updated API key.</returns>
    Task<ApiKey> UpdateApiKey(Guid id, string name, string description, IEnumerable<string>? scopes = null);

    /// <summary>
    /// Retrieves all API keys.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of API keys.</returns>
    Task<List<ApiKey>> GetApiKeys();

    /// <summary>
    /// Retrieves an API key by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the API key.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the API key.</returns>
    Task<ApiKey?> GetApiKey(Guid id);

    /// <summary>
    /// Revokes an API key.
    /// </summary>
    /// <param name="id">The identifier of the API key.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether the API key was successfully revoked.</returns>
    Task<bool> RevokeApiKey(Guid id);

    /// <summary>
    /// Deletes an API key.
    /// </summary>
    /// <param name="id">The identifier of the API key.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether the API key was successfully deleted.</returns>
    Task<bool> DeleteApiKey(Guid id);

    /// <summary>
    /// Retrieves an API key by its key string.
    /// </summary>
    /// <param name="apiKey">The API key string.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the API key.</returns>
    Task<ApiKey?> GetByApiKey(string apiKey);
}

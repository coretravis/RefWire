// Null implementation for when API security is disabled
namespace ListServDB.Security;

/// <summary>
/// Null implementation of the API key service.
/// </summary>
public class NullApiKeyService : IListServApiKeyService
{
    /// <inheritdoc />
    public Task<ApiKeyCreationResponse> CreateApiKey(string name, string description, DateTime? expiresAt = null, IEnumerable<string>? scopes = null)
    {
        throw new NotImplementedException();
    }
    /// <inheritdoc />
    public Task<bool> DeleteApiKey(Guid id)
    {
        throw new NotImplementedException();
    }
    /// <inheritdoc />
    public Task<ApiKey?> GetApiKey(Guid id)
    {
        throw new NotImplementedException();
    }
    /// <inheritdoc />
    public Task<List<ApiKey>> GetApiKeys()
    {
        throw new NotImplementedException();
    }
    /// <inheritdoc />
    public Task<ApiKey?> GetByApiKey(string apiKey)
    {
        throw new NotImplementedException();
    }
    /// <inheritdoc />
    public Task<bool> RevokeApiKey(Guid id)
    {
        throw new NotImplementedException();
    }
    /// <inheritdoc />
    public Task<ApiKey> UpdateApiKey(Guid id, string name, string description, IEnumerable<string>? scopes = null)
    {
        throw new NotImplementedException();
    }
}
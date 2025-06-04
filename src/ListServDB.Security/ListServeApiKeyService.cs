namespace ListServDB.Security;

public class ListServeApiKeyService(IApiKeyRepository repository) : IListServApiKeyService
{
    private readonly IApiKeyRepository _repository = repository;

    public async Task<ApiKeyCreationResponse> CreateApiKey(string name,
                                                           string description,
                                                           DateTime? expiresAt = null,
                                                           IEnumerable<string>? scopes = null)
    {
        // Generate a secure API key
        var (apiKeyValue, apiKeyHash) = ApiKey.GenerateApiKey();

        // Create the API key entity
        var apiKey = new ApiKey(
            Guid.NewGuid(),
            name,
            description,
            apiKeyHash,
            DateTime.UtcNow,
            expiresAt,
            scopes
        );

        // Save to repository
        await _repository.CreateAsync(apiKey);

        // Return the actual key value alongside the entity
        return new ApiKeyCreationResponse(apiKey.Id, apiKey.Name, apiKeyValue);
    }

    public async Task<List<ApiKey>> GetApiKeys()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<ApiKey?> GetApiKey(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<bool> RevokeApiKey(Guid id)
    {
        await _repository.RevokeAsync(id);
        return true;
    }

    public async Task<bool> DeleteApiKey(Guid id)
    {
        return await _repository.DeleteAsync(id);
    }

    public async Task<ApiKey> UpdateApiKey(Guid id, string name, string description, IEnumerable<string>? scopes = null)
    {
        // Get the API key
        var apiKey = await _repository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("API key not found");

        // Update the API key
        apiKey.Update(name, description, scopes ?? Enumerable.Empty<string>());
        await _repository.UpdateAsync(apiKey);
        return apiKey;
    }

    public async Task<ApiKey?> GetByApiKey(string apiKey)
    {
        // Get all keys
        var keys = await _repository.GetAllAsync();

        // Find the key with the matching hash
        var query = keys.Where(k => k.ApiKeyHash == ApiKey.HashApiKey(apiKey));

        if (query.Any())
        {
            return query.First();
        }
        else
        {
            return null;
        }
    }
}

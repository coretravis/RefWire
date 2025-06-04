using Microsoft.Extensions.DependencyInjection;

namespace ListServDB.Security.Azure.Blob;

/// <summary>
/// Extension methods for configuring API key repository services.
/// </summary>
public static class ApiKeyRepositoryServiceExtensions
{
    public static IServiceCollection AddBlobStorageApiKeyRepository(
        this IServiceCollection services,
        Action<ApiKeyBlobStorageOptions> configureOptions)
    {
        services.Configure(configureOptions);

        // Register the API key repository and related services.
        services.AddSingleton<IApiKeyRepository, BlobStorageApiKeyRepository>();
        services.AddSingleton<IListServApiKeyService, ListServeApiKeyService>();
        services.AddHostedService<ApiKeyRepositoryInitializer>();

        return services;
    }
}

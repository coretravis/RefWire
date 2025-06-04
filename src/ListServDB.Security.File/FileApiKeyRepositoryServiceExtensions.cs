using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;

namespace ListServDB.Security.File;

/// <summary>
/// Extension methods for configuring encrypted file-based API key repository services.
/// </summary>
public static class FileApiKeyRepositoryServiceExtensions
{
    /// <summary>
    /// Adds an encrypted file-based API key repository to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure the file storage options.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddEncryptedFileApiKeyRepository(
        this IServiceCollection services,
        Action<ApiKeyFileStorageOptions> configureOptions)
    {
        services.Configure(configureOptions);

        // Register the API key repository and related services
        services.AddSingleton<IApiKeyRepository, FileApiKeyRepository>();
        services.AddSingleton<IListServApiKeyService, ListServeApiKeyService>();
        services.AddHostedService<FileApiKeyRepositoryInitializer>();

        return services;
    }

    /// <summary>
    /// Adds an encrypted file-based API key repository to the service collection with automatically generated keys.
    /// WARNING: This is only for development or testing. In production, use a secure key management solution.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="filePath">The file path for storing API keys.</param>
    /// <param name="cacheExpirationMinutes">The cache expiration time in minutes.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddEncryptedFileApiKeyRepositoryWithGeneratedKeys(
        this IServiceCollection services,
        string filePath,
        int cacheExpirationMinutes = 30)
    {
        services.Configure<ApiKeyFileStorageOptions>(options =>
        {
            options.FilePath = filePath;
            options.CacheExpirationTime = TimeSpan.FromMinutes(cacheExpirationMinutes);

            // Generate random key and IV - todo: THIS SHOULD BE REPLACED WITH A SECURE KEY MANAGEMENT SOLUTION IN PRODUCTION
            using (var aes = Aes.Create())
            {
                aes.GenerateKey();
                aes.GenerateIV();
                options.EncryptionKey = aes.Key;
                options.EncryptionIV = aes.IV;
            }
        });

        // Register the API key repository and related services
        services.AddSingleton<IApiKeyRepository, FileApiKeyRepository>();
        services.AddSingleton<IListServApiKeyService, ListServeApiKeyService>();
        services.AddHostedService<FileApiKeyRepositoryInitializer>();

        return services;
    }
}
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ListServDB.Security.Azure.Blob;

/// <summary>
/// Initializes the API key repository on application startup.
/// </summary>
public class ApiKeyRepositoryInitializer : IHostedService
{
    private readonly BlobStorageApiKeyRepository _repository;
    private readonly ILogger<ApiKeyRepositoryInitializer> _logger;

    public ApiKeyRepositoryInitializer(
        IApiKeyRepository repository,
        ILogger<ApiKeyRepositoryInitializer> logger)
    {
        if (repository is not BlobStorageApiKeyRepository blobRepository)
        {
            throw new ArgumentException("Repository must be of type BlobStorageApiKeyRepository", nameof(repository));
        }

        _repository = blobRepository;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing API key repository on application startup");
        await _repository.InitializeAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
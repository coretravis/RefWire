using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ListServDB.Security.File;

/// <summary>
/// Initializes the file-based API key repository on application startup.
/// </summary>
public class FileApiKeyRepositoryInitializer : IHostedService
{
    private readonly FileApiKeyRepository _repository;
    private readonly ILogger<FileApiKeyRepositoryInitializer> _logger;

    public FileApiKeyRepositoryInitializer(
        IApiKeyRepository repository,
        ILogger<FileApiKeyRepositoryInitializer> logger)
    {
        /// Ensure the repository is of the correct type
        if (repository is not FileApiKeyRepository fileRepository)
        {
            throw new ArgumentException("Repository must be of type FileApiKeyRepository", nameof(repository));
        }

        _repository = fileRepository;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing file-based API key repository on application startup");
        await _repository.InitializeAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
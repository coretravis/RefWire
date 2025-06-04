using ListServDB.Core.Interfaces;
using ListServDB.Core.Models;
using ListServDB.Persistence.Retry;
using ListServDB.Persistence.Serializers;
using System.Collections.Concurrent;
using System.Text;

namespace ListServDB.Persistence.FileSystem;

public class FilePersistenceManager : IDatasetPersistenceManager, IDisposable
{
    private readonly string _datasetsDirectory;
    private readonly string _backupsDirectory;
    private readonly int _maxBackupsPerDataset;
    private readonly long _maxDatasetSizeBytes;
    private readonly SemaphoreSlim _directoryLock = new SemaphoreSlim(1, 1);

    // Dictionary to hold a semaphore for each dataset to ensure per-dataset concurrency control.
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _datasetLocks = new();

    // Track which datasets are currently in use
    private readonly ConcurrentDictionary<string, int> _datasetUsageCounter = new();

    /// <summary>
    /// Initializes a new instance of the FilePersistenceManager.
    /// </summary>
    /// <param name="basePath">The base directory where datasets and backups are stored.</param>
    /// <param name="maxBackupsPerDataset">Maximum number of backups to keep per dataset.</param>
    /// <param name="maxDatasetSizeMB">Maximum size of a dataset in megabytes.</param>
    public FilePersistenceManager(
        string basePath,
        int maxBackupsPerDataset = 10,
        int maxDatasetSizeMB = 100)
    {
        if (string.IsNullOrWhiteSpace(basePath))
        {
            throw new ArgumentException("Base path must be provided.", nameof(basePath));
        }

        _datasetsDirectory = Path.Combine(basePath, "datasets");
        _backupsDirectory = Path.Combine(basePath, "backups");
        _maxBackupsPerDataset = maxBackupsPerDataset > 0 ? maxBackupsPerDataset : 10;
        _maxDatasetSizeBytes = maxDatasetSizeMB > 0 ? maxDatasetSizeMB * 1024 * 1024 : 104857600; // Default 100MB

        // Use the directory lock to ensure thread safety when creating directories
        _directoryLock.Wait();
        try
        {
            // Ensure that the necessary directories exist.
            Directory.CreateDirectory(_datasetsDirectory);
            Directory.CreateDirectory(_backupsDirectory);
        }
        finally
        {
            _directoryLock.Release();
        }
    }

    /// <summary>
    /// Validates and sanitizes a dataset ID to prevent directory traversal or other security issues.
    /// </summary>
    private static string SanitizeDatasetId(string datasetId)
    {
        if (string.IsNullOrWhiteSpace(datasetId))
        {
            throw new ArgumentException("Dataset ID must be provided.", nameof(datasetId));
        }

        // Remove any characters that could be used for directory traversal or are invalid in file names
        var invalidChars = Path.GetInvalidFileNameChars();
        if (datasetId.Any(c => invalidChars.Contains(c)))
        {
            throw new ArgumentException("Dataset ID contains invalid characters.", nameof(datasetId));
        }

        return datasetId;
    }

    /// <summary>
    /// Validates and sanitizes a backup file name to prevent security issues.
    /// </summary>
    private static string SanitizeBackupFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("Backup file name must be provided.", nameof(fileName));
        }

        // Remove any characters that could be used for directory traversal or are invalid in file names
        var invalidChars = Path.GetInvalidFileNameChars();
        if (fileName.Any(c => invalidChars.Contains(c)))
        {
            throw new ArgumentException("Backup file name contains invalid characters.", nameof(fileName));
        }

        // Make sure the file name has a .json extension
        if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Backup file must have a .json extension.", nameof(fileName));
        }

        return fileName;
    }

    /// <summary>
    /// Acquires a lock for the specified dataset.
    /// </summary>
    private async Task<SemaphoreSlim> AcquireDatasetLockAsync(string datasetId)
    {
        // Validate and sanitize the dataset ID
        var sanitizedId = SanitizeDatasetId(datasetId);

        // Get or create a semaphore for this dataset
        var semaphore = _datasetLocks.GetOrAdd(sanitizedId, _ => new SemaphoreSlim(1, 1));

        // Increment usage counter
        _datasetUsageCounter.AddOrUpdate(sanitizedId, 1, (_, count) => count + 1);

        await semaphore.WaitAsync();
        return semaphore;
    }

    /// <summary>
    /// Releases a lock for the specified dataset and cleans up if no longer in use.
    /// </summary>
    private void ReleaseDatasetLock(string datasetId, SemaphoreSlim semaphore)
    {
        semaphore.Release();

        // Decrement usage counter and remove lock if no longer in use
        if (_datasetUsageCounter.TryGetValue(datasetId, out int count))
        {
            if (count <= 1)
            {
                _datasetUsageCounter.TryRemove(datasetId, out _);
                _datasetLocks.TryRemove(datasetId, out _);
                semaphore.Dispose();
            }
            else
            {
                _datasetUsageCounter.TryUpdate(datasetId, count - 1, count);
            }
        }
    }

    public async Task<Dataset?> LoadDatasetAsync(string datasetId)
    {
        // Validate and sanitize the dataset ID
        var sanitizedId = SanitizeDatasetId(datasetId);
        var semaphore = await AcquireDatasetLockAsync(sanitizedId);

        try
        {
            string filePath = Path.Combine(_datasetsDirectory, $"{sanitizedId}.json");
            if (!File.Exists(filePath))
            {
                return null;
            }

            // Check file size before loading
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length > _maxDatasetSizeBytes)
            {
                throw new InvalidOperationException($"Dataset file exceeds maximum allowed size of {_maxDatasetSizeBytes / (1024 * 1024)} MB.");
            }

            return await AsyncRetryHelper.ExecuteWithRetryAsync(async () =>
            {
                string json = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
                return string.IsNullOrWhiteSpace(json)
                    ? null
                    : JsonSerializerHelper.Deserialize<Dataset>(json);
            });
        }
        finally
        {
            ReleaseDatasetLock(sanitizedId, semaphore);
        }
    }

    public async Task SaveDatasetAsync(Dataset dataset)
    {
        // Validate and sanitize the dataset ID
        ArgumentNullException.ThrowIfNull(dataset);

        if (string.IsNullOrWhiteSpace(dataset.Id))
        {
            throw new ArgumentException("Dataset must have a valid ID.", nameof(dataset));
        }

        var sanitizedId = SanitizeDatasetId(dataset.Id);
        var semaphore = await AcquireDatasetLockAsync(sanitizedId);

        try
        {
            string filePath = Path.Combine(_datasetsDirectory, $"{sanitizedId}.json");
            string tempFilePath = Path.Combine(_datasetsDirectory, $"{sanitizedId}.tmp");

            await AsyncRetryHelper.ExecuteWithRetryAsync(async () =>
            {
                // First, serialize to check the size
                string json = JsonSerializerHelper.Serialize(dataset);
                byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

                // Check if the serialized data exceeds the maximum allowed size
                if (jsonBytes.Length > _maxDatasetSizeBytes)
                {
                    throw new InvalidOperationException($"Dataset exceeds maximum allowed size of {_maxDatasetSizeBytes / (1024 * 1024)} MB.");
                }

                // Backup the existing dataset file if it exists
                if (File.Exists(filePath))
                {
                    await CreateBackupFileAsync(sanitizedId, filePath);
                }

                // Write to temp file first
                await File.WriteAllTextAsync(tempFilePath, json, Encoding.UTF8);

                // Then move the temp file to the final location
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                File.Move(tempFilePath, filePath);

                // Clean up old backups
                await CleanupOldBackupsAsync(sanitizedId);

                return true;
            });
        }
        catch (Exception)
        {
            // Clean up temp file if it exists
            string tempFilePath = Path.Combine(_datasetsDirectory, $"{sanitizedId}.tmp");
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
            throw;
        }
        finally
        {
            ReleaseDatasetLock(sanitizedId, semaphore);
        }
    }

    public async Task<IEnumerable<string>> ListDatasetIdsAsync()
    {
        // Use the directory lock to ensure consistency
        await _directoryLock.WaitAsync();
        try
        {
            if (!Directory.Exists(_datasetsDirectory))
            {
                return Enumerable.Empty<string>();
            }

            return Directory.EnumerateFiles(_datasetsDirectory, "*.json")
                .Select(file => Path.GetFileNameWithoutExtension(file))
                .Where(id => !string.IsNullOrEmpty(id))
                .ToList(); // Materialize the list while we still have the lock
        }
        finally
        {
            _directoryLock.Release();
        }
    }

    public async Task BackupDatasetAsync(string datasetId)
    {
        var sanitizedId = SanitizeDatasetId(datasetId);
        var semaphore = await AcquireDatasetLockAsync(sanitizedId);

        try
        {
            string sourceFilePath = Path.Combine(_datasetsDirectory, $"{sanitizedId}.json");
            if (!File.Exists(sourceFilePath))
            {
                throw new FileNotFoundException("Dataset file not found.", sourceFilePath);
            }

            await CreateBackupFileAsync(sanitizedId, sourceFilePath);
        }
        finally
        {
            ReleaseDatasetLock(sanitizedId, semaphore);
        }
    }

    private async Task CreateBackupFileAsync(string sanitizedId, string sourceFilePath)
    {
        // Create a dedicated backup directory for the dataset
        await _directoryLock.WaitAsync();
        try
        {
            string dBackupDir = Path.Combine(_backupsDirectory, sanitizedId);
            Directory.CreateDirectory(dBackupDir);
        }
        finally
        {
            _directoryLock.Release();
        }

        string datasetBackupDir = Path.Combine(_backupsDirectory, sanitizedId);
        string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
        string backupFileName = $"{sanitizedId}_backup_{timestamp}.json";
        string backupFilePath = Path.Combine(datasetBackupDir, backupFileName);

        await AsyncRetryHelper.ExecuteWithRetryAsync(async () =>
        {
            // File.Copy is synchronous; wrap it in Task.Run to avoid blocking
            await Task.Run(() => File.Copy(sourceFilePath, backupFilePath, overwrite: true));
            return true;
        });
    }

    public async Task<IEnumerable<string>> ListBackupsAsync(string datasetId)
    {
        var sanitizedId = SanitizeDatasetId(datasetId);

        // Use the directory lock to ensure consistency
        await _directoryLock.WaitAsync();
        try
        {
            string datasetBackupDir = Path.Combine(_backupsDirectory, sanitizedId);
            if (!Directory.Exists(datasetBackupDir))
            {
                return Enumerable.Empty<string>();
            }

            return Directory.EnumerateFiles(datasetBackupDir, "*.json")
                .Select(Path.GetFileName)
                .Where(name => !string.IsNullOrEmpty(name))
                .OrderByDescending(n => n)
                .ToList()!; // Materialize the list while we still have the lock
        }
        finally
        {
            _directoryLock.Release();
        }
    }

    public async Task RestoreDatasetBackupAsync(string datasetId, string backupFileName)
    {
        var sanitizedId = SanitizeDatasetId(datasetId);
        var sanitizedBackupFileName = SanitizeBackupFileName(backupFileName);
        var semaphore = await AcquireDatasetLockAsync(sanitizedId);

        try
        {
            string datasetBackupDir = Path.Combine(_backupsDirectory, sanitizedId);
            string backupFilePath = Path.Combine(datasetBackupDir, sanitizedBackupFileName);

            if (!File.Exists(backupFilePath))
            {
                throw new FileNotFoundException("Backup file not found.", backupFilePath);
            }

            string targetFilePath = Path.Combine(_datasetsDirectory, $"{sanitizedId}.json");
            string tempFilePath = Path.Combine(_datasetsDirectory, $"{sanitizedId}.tmp");

            await AsyncRetryHelper.ExecuteWithRetryAsync(async () =>
            {
                string json = await File.ReadAllTextAsync(backupFilePath, Encoding.UTF8);

                // Check size
                if (Encoding.UTF8.GetByteCount(json) > _maxDatasetSizeBytes)
                {
                    throw new InvalidOperationException($"Backup exceeds maximum allowed size of {_maxDatasetSizeBytes / (1024 * 1024)} MB.");
                }

                // Write to temp file first
                await File.WriteAllTextAsync(tempFilePath, json, Encoding.UTF8);

                // Then move the temp file to the final location (more atomic)
                if (File.Exists(targetFilePath))
                {
                    File.Delete(targetFilePath);
                }
                File.Move(tempFilePath, targetFilePath);

                return true;
            });
        }
        catch (Exception)
        {
            // Clean up temp file if it exists
            string tempFilePath = Path.Combine(_datasetsDirectory, $"{sanitizedId}.tmp");
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
            throw;
        }
        finally
        {
            ReleaseDatasetLock(sanitizedId, semaphore);
        }
    }

    /// <summary>
    /// Removes old backups to keep the total number of backups under the specified limit.
    /// </summary>
    private async Task CleanupOldBackupsAsync(string datasetId)
    {
        var sanitizedId = SanitizeDatasetId(datasetId);

        // Use the directory lock to ensure consistency
        await _directoryLock.WaitAsync();
        try
        {
            string datasetBackupDir = Path.Combine(_backupsDirectory, sanitizedId);
            if (!Directory.Exists(datasetBackupDir))
            {
                return;
            }

            var backupFiles = Directory.EnumerateFiles(datasetBackupDir, "*.json")
                .OrderByDescending(f => f) // Most recent first
                .Skip(_maxBackupsPerDataset) // Skip the ones we want to keep
                .ToList(); // Materialize the list while we still have the lock

            // Delete old backups
            foreach (var backupFile in backupFiles)
            {
                try
                {
                    File.Delete(backupFile);
                }
                catch (IOException)
                {
                    // log the exception and continue

                }
            }
        }
        finally
        {
            _directoryLock.Release();
        }
    }

    /// <summary>
    /// Disposes resources used by the FilePersistenceManager.
    /// </summary>
    public void Dispose()
    {
        // Dispose all semaphores
        _directoryLock.Dispose();

        foreach (var semaphore in _datasetLocks.Values)
        {
            semaphore.Dispose();
        }

        _datasetLocks.Clear();
        _datasetUsageCounter.Clear();

        GC.SuppressFinalize(this);
    }

    public async Task DeleteDatasetAsync(string datasetId)
    {
        // Validate and sanitize the dataset ID
        var sanitizedId = SanitizeDatasetId(datasetId);
        var semaphore = await AcquireDatasetLockAsync(sanitizedId);

        try
        {
            string datasetFilePath = Path.Combine(_datasetsDirectory, $"{sanitizedId}.json");
            string datasetBackupDir = Path.Combine(_backupsDirectory, sanitizedId);

            // Delete the dataset file if it exists
            if (File.Exists(datasetFilePath))
            {
                File.Delete(datasetFilePath);
            }

            // Delete the backup directory if it exists
            if (Directory.Exists(datasetBackupDir))
            {
                Directory.Delete(datasetBackupDir, recursive: true);
            }
        }
        finally
        {
            ReleaseDatasetLock(sanitizedId, semaphore);
        }
    }
}
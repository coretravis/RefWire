using ListServDB.Core.Models;

namespace ListServDB.Core.Interfaces;

/// <summary>
/// Defines the contract for managing dataset persistence.
/// </summary>
public interface IDatasetPersistenceManager
{
    /// <summary>
    /// Asynchronously loads the dataset identified by the given datasetId from its storage.
    /// </summary>
    Task<Dataset?> LoadDatasetAsync(string datasetId);

    /// <summary>
    /// Asynchronously saves the provided dataset to its corresponding storage.
    /// </summary>
    Task SaveDatasetAsync(Dataset dataset);

    /// <summary>
    /// Asynchronously returns a list of dataset IDs available in the storage.
    /// </summary>
    Task<IEnumerable<string>> ListDatasetIdsAsync();

    /// <summary>
    /// Asynchronously creates a backup for the dataset identified by datasetId.
    /// </summary>
    Task BackupDatasetAsync(string datasetId);

    /// <summary>
    /// Asynchronously lists all backup file paths for a given dataset.
    /// </summary>
    Task<IEnumerable<string>> ListBackupsAsync(string datasetId);

    /// <summary>
    /// Asynchronously restores the dataset from the specified backup.
    /// </summary>
    Task RestoreDatasetBackupAsync(string datasetId, string backupFilePath);

    /// <summary>
    /// Asynchronously deletes the dataset identified by the given datasetId from its storage.
    /// </summary>
    /// <param name="datasetId">The dataset id</param>
    /// <returns></returns>
    Task DeleteDatasetAsync(string datasetId);
}
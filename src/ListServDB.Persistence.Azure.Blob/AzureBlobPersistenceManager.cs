using Azure.Storage.Blobs;
using ListServDB.Core.Interfaces;
using ListServDB.Core.Models;
using ListServDB.Persistence.Serializers;
using ListServDB.Persistence.Retry;
using System.Text;

namespace ListServDB.Persistence.Azure.Blob;

public class AzureBlobPersistenceManager : IDatasetPersistenceManager
{
    private readonly BlobContainerClient _datasetsContainer;
    private readonly BlobContainerClient _backupsContainer;

    /// <summary>
    /// Initializes a new instance of the AzureBlobPersistenceManager.
    /// </summary>
    public AzureBlobPersistenceManager(string connectionString,
                                         string datasetsContainerName = "datasets",
                                         string backupsContainerName = "backups")
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string must be provided.", nameof(connectionString));
        }

        _datasetsContainer = new BlobContainerClient(connectionString, datasetsContainerName);
        _datasetsContainer.CreateIfNotExists();

        _backupsContainer = new BlobContainerClient(connectionString, backupsContainerName);
        _backupsContainer.CreateIfNotExists();
    }

    public async Task<Dataset?> LoadDatasetAsync(string datasetId)
    {
        // Check if the dataset exists
        string blobName = $"{datasetId}.json";
        var blobClient = _datasetsContainer.GetBlobClient(blobName);
        if (!(await blobClient.ExistsAsync()).Value)
        {
            return null;
        }

        // Attempt to download the dataset
        return await AsyncRetryHelper.ExecuteWithRetryAsync(async () =>
        {
            var downloadResult = await blobClient.DownloadContentAsync();
            string json = downloadResult.Value.Content.ToString();
            return string.IsNullOrWhiteSpace(json) ? null : JsonSerializerHelper.Deserialize<Dataset>(json);
        });
    }

    public async Task SaveDatasetAsync(Dataset dataset)
    {
        string blobName = $"{dataset.Id}.json";
        var blobClient = _datasetsContainer.GetBlobClient(blobName);

        await AsyncRetryHelper.ExecuteWithRetryAsync(async () =>
        {
            if ((await blobClient.ExistsAsync()).Value)
            {
                await BackupDatasetAsync(dataset.Id);
            }

            // Serialize the dataset to JSON and upload it
            string json = JsonSerializerHelper.Serialize(dataset);
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                await blobClient.UploadAsync(stream, overwrite: true);
            }
            return true;
        });
    }

    public async Task<IEnumerable<string>> ListDatasetIdsAsync()
    {
        var ids = new List<string>();

        // List all blobs in the datasets container
        await foreach (var blobItem in _datasetsContainer.GetBlobsAsync())
        {
            if (blobItem.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                ids.Add(Path.GetFileNameWithoutExtension(blobItem.Name));
            }
        }
        return ids;
    }

    public async Task BackupDatasetAsync(string datasetId)
    {
        // Check if the dataset exists
        string sourceBlobName = $"{datasetId}.json";
        var sourceBlob = _datasetsContainer.GetBlobClient(sourceBlobName);
        if (!(await sourceBlob.ExistsAsync()).Value)
        {
            throw new FileNotFoundException("Dataset blob not found.", sourceBlobName);
        }

        // Create a backup blob name with a timestamp
        string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
        string backupBlobName = $"{datasetId}/{datasetId}_backup_{timestamp}.json";
        var backupBlob = _backupsContainer.GetBlobClient(backupBlobName);

        // Attempt to copy the dataset blob to the backup blob
        await AsyncRetryHelper.ExecuteWithRetryAsync(async () =>
        {
            await backupBlob.StartCopyFromUriAsync(sourceBlob.Uri);
            return true;
        });
    }

    public async Task<IEnumerable<string>> ListBackupsAsync(string datasetId)
    {
        string prefix = $"{datasetId}/";
        var results = new List<string>();
        await foreach (var blobItem in _backupsContainer.GetBlobsAsync(prefix: prefix))
        {
            results.Add(blobItem.Name);
        }
        return results.OrderByDescending(n => n);
    }

    public async Task RestoreDatasetBackupAsync(string datasetId, string backupBlobName)
    {
        // Check if the backup blob exists
        string targetBlobName = $"{datasetId}.json";
        var targetBlob = _datasetsContainer.GetBlobClient(targetBlobName);
        var backupBlob = _backupsContainer.GetBlobClient(backupBlobName);
        if (!(await backupBlob.ExistsAsync()).Value)
        {
            throw new FileNotFoundException("Backup blob not found.", backupBlobName);
        }

        // Attempt to download the backup blob and upload it as the dataset blob
        await AsyncRetryHelper.ExecuteWithRetryAsync(async () =>
        {
            var downloadResult = await backupBlob.DownloadContentAsync();
            string json = downloadResult.Value.Content.ToString();
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                await targetBlob.UploadAsync(stream, overwrite: true);
            }
            return true;
        });
    }

    public async Task DeleteDatasetAsync(string datasetId)
    {
        // Check if the dataset exists
        string blobName = $"{datasetId}.json";
        var blobClient = _datasetsContainer.GetBlobClient(blobName);

        if (!(await blobClient.ExistsAsync()).Value)
        {
            throw new FileNotFoundException("Dataset blob not found.", blobName);
        }

        // Attempt to delete the dataset blob
        await AsyncRetryHelper.ExecuteWithRetryAsync(async () =>
        {
            await BackupDatasetAsync(datasetId);
            await blobClient.DeleteAsync();
            return true;
        });
    }
}

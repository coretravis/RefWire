namespace ListServDB.Persistence.Azure.Blob;

public class AzureBlobSettings
{
    /// <summary>
    /// The connection string for the Azure Storage account.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// The name of the container that stores the state and backup blobs.
    /// </summary>
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>
    /// The name of the main state blob. Defaults to "listserv_state.json".
    /// </summary>
    public string StateBlobName { get; set; } = "listserv_state.json";

    /// <summary>
    /// The blob prefix (virtual directory) used for storing backup files.
    /// Defaults to "backups/".
    /// </summary>
    public string BackupPrefix { get; set; } = "backups/";
}

namespace ListServDB.WebApi.Configuration;

/// <summary>
/// Configuration for data persistence.
/// </summary>
public class PersistenceConfig
{
    /// <summary>
    /// Indicates if Azure storage should be used.
    /// </summary>
    public bool UseAzure { get; set; } = false;

    /// <summary>
    /// Indicates if ListServStor should be used.
    /// </summary>
    public bool UseListServStor { get; set; } = false;

    /// <summary>
    /// Directory path for datasets.
    /// </summary>
    public string DatasetsDirectory { get; set; } = "datasets";

    /// <summary>
    /// Directory path for backups.
    /// </summary>
    public string BackupsDirectory { get; set; } = "backups";
    /// <summary>
    /// Directory path for API keys.
    /// </summary>
    public string ApiKeysDirectory { get; set; } = "apikeys";
}

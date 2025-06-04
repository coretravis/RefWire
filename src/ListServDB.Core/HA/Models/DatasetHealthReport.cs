namespace ListServDB.Core.HA.Models;

/// <summary>
/// Represents health details for a single dataset.
/// </summary>
public class DatasetHealthReport
{
    /// <summary>
    /// The name of the dataset.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The identifier of the dataset.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The count of active (non-archived) items in the dataset.
    /// </summary>
    public int ActiveItems { get; set; }
}

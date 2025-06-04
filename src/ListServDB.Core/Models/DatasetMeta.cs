namespace ListServDB.Core.Models;

public class DatasetMeta
{
    /// <summary>
    /// Unique identifier for the dataset (e.g. "countries", "languages").
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// A friendly name for the dataset.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// A description of the dataset.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The property name on dataset items used as the unique key.
    /// Immutable after creation.
    /// </summary>
    public string IdField { get; set; } = string.Empty;

    /// <summary>
    /// The property name on dataset items used as the display name.
    /// Immutable after creation.
    /// </summary>
    public string NameField { get; set; } = string.Empty;

    /// <summary>
    /// Defines the schema for dataset items.
    /// </summary>
    public List<DatasetField> Fields { get; set; } = new List<DatasetField>();
}
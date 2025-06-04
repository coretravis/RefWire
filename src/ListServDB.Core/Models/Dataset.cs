namespace ListServDB.Core.Models;

/// <summary>
/// Represents a dataset containing a list of items.
/// </summary>
public class Dataset
{
    /// <summary>
    /// Unique identifier for the dataset (e.g. "countries", "languages").
    /// Only special character allowed is '_', just makes life easier for everyone
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

    /// <summary>
    /// Collection of dataset items.
    /// Keyed by the item's unique identifier (e.g. ISO code).
    /// </summary>
    public Dictionary<string, DatasetItem> Items { get; set; } = new Dictionary<string, DatasetItem>();

    public override bool Equals(object? obj)
    {
        if (obj is DatasetItem other)
        {
            return this.Id.Equals(other.Id, StringComparison.Ordinal);
        }

        return false;
    }

    public override int GetHashCode() => Id.GetHashCode();
}

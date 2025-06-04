namespace ListServDB.Core.Models;

/// <summary>
/// Represents an item within a dataset.
/// </summary>
public class DatasetItem
{
    /// <summary>
    /// Unique identifier for the dataset item (e.g. ISO code).
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the item (e.g. Country Name).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Contains key-value pairs representing the item’s data.
    /// The keys should match the dataset’s defined Fields.
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Indicates if the item is archived (soft-deleted).
    /// </summary>
    public bool IsArchived { get; set; }

    public DatasetItem()
    {
        IsArchived = false;
    }

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
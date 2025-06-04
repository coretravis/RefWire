using ListServDB.Core.Indexing.SuffixTree;
using ListServDB.Core.Models;

namespace ListServDB.Core.Indexing;

/// <summary>
/// Provides functionality to rebuild in-memory indexes from persisted datasets.
/// </summary>
public static class IndexRebuilder
{
    /// <summary>
    /// Rebuilds the index for a single dataset using its NameField.
    /// </summary>
    public static SuffixTreeIndex RebuildIndex(Dataset dataset)
    {
        var index = new SuffixTreeIndex();
        foreach (var item in dataset.Items.Values)
        {
            // Only include non-archived items.
            if (!item.IsArchived && item.Data.TryGetValue(dataset.NameField, out var nameValue))
            {
                string? nameStr = nameValue?.ToString();
                if (!string.IsNullOrWhiteSpace(nameStr))
                {
                    index.Add(nameStr, item);
                }
            }
        }
        return index;
    }

    /// <summary>
    /// Rebuilds indexes for all provided datasets.
    /// Returns a dictionary mapping dataset ID to its rebuilt index.
    /// </summary>
    public static Dictionary<string, SuffixTreeIndex> RebuildAllIndexes(Dictionary<string, Dataset> datasets)
    {
        var indexes = new Dictionary<string, SuffixTreeIndex>();

        // Iterate through each dataset and rebuild its index.
        foreach (var kvp in datasets)
        {
            indexes[kvp.Key] = RebuildIndex(kvp.Value);
        }
        return indexes;
    }
}

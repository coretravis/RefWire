using ListServDB.Core.Interfaces;
using ListServDB.Core.Models;

namespace ListServDB.Core.Indexing.SuffixTree;

/// <summary>
/// Implements IIndex using a compressed suffix tree for partial, case-insensitive matching.
/// </summary>
public class SuffixTreeIndex : IIndex<string, DatasetItem>
{
    private readonly CompressedSuffixTree _suffixTree;

    public SuffixTreeIndex()
    {
        _suffixTree = new CompressedSuffixTree();
    }

    public void Add(string key, DatasetItem item)
    {
        if (string.IsNullOrEmpty(key))
        {
            return;
        }
        // Normalize key to lower-case.
        string lowerKey = key.ToLowerInvariant();
        _suffixTree.Insert(lowerKey, item);
    }

    public bool Remove(string key, DatasetItem item)
    {
        if (string.IsNullOrEmpty(key))
        {
            return false;
        }

        string lowerKey = key.ToLowerInvariant();
        // Remove the item from all suffix entries.
        _suffixTree.Remove(lowerKey, item);
        // Since our Remove method does not report removals per se, let's simply return true.
        return true;
    }

    public IEnumerable<DatasetItem> Find(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return new List<DatasetItem>();
        }

        return _suffixTree.Search(key);
    }

    public void Clear()
    {
        _suffixTree.Clear();
    }
}

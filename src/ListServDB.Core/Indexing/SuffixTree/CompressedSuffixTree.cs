using ListServDB.Core.Models;

namespace ListServDB.Core.Indexing.SuffixTree;

/// <summary>
/// A compressed suffix tree for efficient, case-insensitive substring search.
/// </summary>
public class CompressedSuffixTree
{
    private readonly CompressedSuffixTreeNode _root;

    public CompressedSuffixTree()
    {
        _root = new CompressedSuffixTreeNode(""); // Root label is empty.
    }

    /// <summary>
    /// Inserts all suffixes of the given text (assumed already lower-cased) associated with the dataset item.
    /// </summary>
    /// <param name="text">Lower-cased text to index (typically the item's name).</param>
    /// <param name="item">The dataset item to index.</param>
    public void Insert(string text, DatasetItem item)
    {
        // For each suffix, insert into the tree.
        for (int i = 0; i < text.Length; i++)
        {
            InsertSuffix(text[i..], item);
        }
    }

    /// <summary>
    /// Searches for dataset items whose names contain the given substring.
    /// </summary>
    /// <param name="query">The substring to search for.</param>
    /// <returns>A collection of matching dataset items.</returns>
    public IEnumerable<DatasetItem> Search(string query)
    {
        // Normalize query.
        query = query.ToLowerInvariant();
        var current = _root;
        int queryIndex = 0;
        while (queryIndex < query.Length)
        {
            char c = query[queryIndex];
            if (!current.Children.TryGetValue(c, out var child))
            {
                return Enumerable.Empty<DatasetItem>();
            }
            string label = child.EdgeLabel;
            int i = 0;
            // Walk through the edge label.
            while (queryIndex + i < query.Length &&
                   i < label.Length &&
                   query[queryIndex + i] == label[i])
            {
                i++;
            }
            if (queryIndex + i < query.Length && i < label.Length)
            {
                // The query does not match fully.
                return Enumerable.Empty<DatasetItem>();
            }
            queryIndex += i;
            current = child;
        }
        return current.Items;
    }

    /// <summary>
    /// Removes the dataset item from all nodes corresponding to all suffixes of the given text.
    /// </summary>
    /// <param name="text">The text whose suffix entries should be removed (case-insensitive).</param>
    /// <param name="item">The dataset item to remove.</param>
    public void Remove(string text, DatasetItem item)
    {
        string lowerText = text.ToLowerInvariant();
        // For each suffix, traverse the tree and remove the item.
        for (int i = 0; i < lowerText.Length; i++)
        {
            RemoveSuffix(_root, lowerText[i..], item);
        }
    }

    private void RemoveSuffix(CompressedSuffixTreeNode current, string s, DatasetItem item)
    {
        // Remove the item from the current node.
        current.Items.Remove(item);
        if (s.Length == 0)
        {
            return;
        }

        char firstChar = s[0];
        if (!current.Children.TryGetValue(firstChar, out var child))
        {
            return;
        }

        string label = child.EdgeLabel;
        int commonPrefixLength = 0;

        // Compute common prefix length.
        while (commonPrefixLength < s.Length &&
               commonPrefixLength < label.Length &&
               s[commonPrefixLength] == label[commonPrefixLength])
        {
            commonPrefixLength++;
        }
        if (commonPrefixLength > 0)
        {
            RemoveSuffix(child, s[commonPrefixLength..], item);
        }
    }

    private void InsertSuffix(string s, DatasetItem item)
    {
        var current = _root;
        // Always add the item to the current node.
        current.Items.Add(item);

        while (s.Length > 0)
        {
            char firstChar = s[0];
            if (!current.Children.TryGetValue(firstChar, out var child))
            {
                // No child starting with this char; create a new node for the entire suffix.
                var newChild = new CompressedSuffixTreeNode(s);
                newChild.Items.Add(item);
                current.Children[firstChar] = newChild;
                return;
            }

            string label = child.EdgeLabel;
            int commonPrefixLength = 0;
            // Compute common prefix length.
            while (commonPrefixLength < s.Length &&
                   commonPrefixLength < label.Length &&
                   s[commonPrefixLength] == label[commonPrefixLength])
            {
                commonPrefixLength++;
            }

            if (commonPrefixLength == label.Length)
            {
                // Full match on child label; add item and continue with the remainder.
                child.Items.Add(item);
                s = s[commonPrefixLength..];
                current = child;
                continue;
            }

            // Partial match: split the edge.
            string commonPrefix = s[..commonPrefixLength];
            string labelSuffix = label[commonPrefixLength..];
            var intermediate = new CompressedSuffixTreeNode(commonPrefix);

            // Reassign existing child.
            child.EdgeLabel = labelSuffix;
            intermediate.Children[labelSuffix[0]] = child;
            foreach (var it in child.Items)
            {
                intermediate.Items.Add(it);
            }
            current.Children[firstChar] = intermediate;

            // Add new node for the remainder of s.
            string sSuffix = s[commonPrefixLength..];
            if (sSuffix.Length > 0)
            {
                var newChild = new CompressedSuffixTreeNode(sSuffix);
                newChild.Items.Add(item);
                intermediate.Children[sSuffix[0]] = newChild;
                intermediate.Items.Add(item);
            }
            else
            {
                intermediate.Items.Add(item);
            }
            return;
        }
    }

    public void Clear()
    {
        _root.Children.Clear();
        _root.Items.Clear();
    }
}

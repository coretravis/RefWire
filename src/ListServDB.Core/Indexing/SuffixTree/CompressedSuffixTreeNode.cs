using ListServDB.Core.Models;

namespace ListServDB.Core.Indexing.SuffixTree;

/// <summary>
/// A node in the compressed suffix tree. Each node stores an edge label and a set of dataset items
/// whose lower-cased names contain the substring corresponding to the path from the root to this node.
/// Children are keyed by the first character of their edge label.
/// </summary>
public class CompressedSuffixTreeNode(string edgeLabel)
{
    /// <summary>
    /// The edge label for this node. This is the substring from the parent node to this node.
    /// </summary>
    public string EdgeLabel { get; set; } = edgeLabel;

    /// <summary>
    /// A set of dataset items whose names contain the substring represented by this node.
    /// </summary>
    public HashSet<DatasetItem> Items { get; set; } = new HashSet<DatasetItem>();

    /// <summary>
    /// Children of this node, keyed by the first character of their edge label.
    /// </summary>
    public Dictionary<char, CompressedSuffixTreeNode> Children { get; set; } = new Dictionary<char, CompressedSuffixTreeNode>();
}

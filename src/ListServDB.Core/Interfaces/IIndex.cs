namespace ListServDB.Core.Interfaces;

/// <summary>
/// Generic interface for an index that maps keys to items.
/// </summary>
/// <typeparam name="TKey">The type of key (e.g. string for item names).</typeparam>
/// <typeparam name="TItem">The type of item to index.</typeparam>
public interface IIndex<TKey, TItem>
{
    /// <summary>
    /// Adds an item to the index under the specified key.
    /// </summary>
    void Add(TKey key, TItem item);

    /// <summary>
    /// Removes an item from the index for the specified key.
    /// </summary>
    /// <returns>True if the item was removed; otherwise, false.</returns>
    bool Remove(TKey key, TItem item);

    /// <summary>
    /// Finds all items associated with the given key.
    /// </summary>
    IEnumerable<TItem> Find(TKey key);

    /// <summary>
    /// Clears the entire index.
    /// </summary>
    void Clear();
}

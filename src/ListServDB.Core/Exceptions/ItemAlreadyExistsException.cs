namespace ListServDB.Core.Exceptions;

/// <summary>
/// Represents an exception that is thrown when an attempt is made to add an item to a dataset, but the item already
/// exists.
/// </summary>
/// <remarks>This exception is typically used to indicate a violation of uniqueness constraints within a dataset.
/// It provides information about the conflicting item and the dataset where the conflict occurred.</remarks>
public class ItemAlreadyExistsException : Exception
{
    public ItemAlreadyExistsException(string itemId, string datasetId)
        : base($"Item with id '{itemId}' already exists in dataset '{datasetId}'.") { }
}

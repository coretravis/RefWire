namespace ListServDB.Core.Exceptions;

/// <summary>
/// Represents an exception that is thrown when a specified item cannot be found in a dataset.
/// </summary>
/// <remarks>This exception is typically used to indicate that an operation failed because the requested item does
/// not exist within the specified dataset. The exception message includes the item ID and dataset ID to provide
/// additional context.</remarks>
public class ItemNotFoundException : Exception
{
    public ItemNotFoundException(string itemId, string datasetId)
        : base($"Item '{itemId}' not found in dataset '{datasetId}'.") { }
}

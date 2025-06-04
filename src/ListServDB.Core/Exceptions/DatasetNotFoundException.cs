namespace ListServDB.Core.Exceptions;

/// <summary>
/// Represents an exception that is thrown when a specified dataset cannot be found or is unavailable.
/// </summary>
/// <remarks>This exception is typically used to indicate that an operation requiring access to a dataset has
/// failed because the dataset does not exist or is inaccessible.</remarks>
public class DatasetNotFoundException : Exception
{
    public DatasetNotFoundException(string datasetId)
        : base($"Dataset '{datasetId}' was not found or is not available.") { }
}

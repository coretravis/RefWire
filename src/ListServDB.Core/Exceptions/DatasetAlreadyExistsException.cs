namespace ListServDB.Core.Exceptions;

/// <summary>
/// Represents an exception that is thrown when attempting to create a dataset that already exists.
/// </summary>
/// <remarks>This exception is typically used to indicate a conflict when a dataset with the specified identifier
/// already exists in the system. The exception message includes the conflicting dataset identifier.</remarks>
public class DatasetAlreadyExistsException : Exception
{
    public DatasetAlreadyExistsException(string datasetId)
        : base($"Dataset with id '{datasetId}' already exists.") { }
}

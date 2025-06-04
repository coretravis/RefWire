using ListServDB.Core.Models;

namespace ListServDB.Core.Caching;

/// <summary>
/// Interface for dataset caching operations
/// </summary>
public interface IDatasetCache : IDisposable
{
    /// <summary>
    /// Initializes the cache with available dataset IDs
    /// </summary>
    Task InitializeAsync(IEnumerable<string> availableDatasetIds);

    /// <summary>
    /// Gets a cached dataset, loading it if not already cached
    /// </summary>
    Task<CachedDataset> GetCachedDatasetAsync(string datasetId);

    /// <summary>
    /// Adds a new dataset to the cache
    /// </summary>
    void AddDatasetToCache(string datasetId, Dataset dataset);

    /// <summary>
    /// Removes a dataset from the cache
    /// </summary>
    void RemoveDatasetFromCache(string datasetId);

    /// <summary>
    /// Invalidates a specific dataset's cache entry
    /// </summary>
    void InvalidateDataset(string datasetId);

    /// <summary>
    /// Checks if a dataset exists in the available datasets
    /// </summary>
    bool IsDatasetAvailable(string datasetId);

    /// <summary>
    /// Gets all available dataset IDs
    /// </summary>
    IEnumerable<string> GetAvailableDatasetIds();

    /// <summary>
    /// Gets all currently cached datasets
    /// </summary>
    IEnumerable<Dataset> GetCachedDatasets();

    /// <summary>
    /// Adds a dataset ID to the available datasets collection
    /// </summary>
    void AddAvailableDatasetId(string datasetId);

    /// <summary>
    /// Removes a dataset ID from the available datasets collection
    /// </summary>
    void RemoveAvailableDatasetId(string datasetId);
}

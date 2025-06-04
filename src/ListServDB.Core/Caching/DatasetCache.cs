using ListServDB.Core.Exceptions;
using ListServDB.Core.Indexing;
using ListServDB.Core.Interfaces;
using ListServDB.Core.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace ListServDB.Core.Caching;

/// <summary>
/// Handles caching of datasets for performance and availability.
/// </summary>
public class DatasetCache : IDatasetCache
{
    // Activity source used for distributed tracing
    private static readonly ActivitySource ActivitySource = new("ListServDB.Core.API.DatasetCache");

    private readonly MemoryCache _cache;
    private readonly IDatasetPersistenceManager _persistenceManager;
    private readonly ILogger<DatasetCache> _logger;
    private readonly DatasetCacheOptions _options;
    private readonly ConcurrentDictionary<string, bool> _availableDatasetIds;
    private volatile bool _isInitialized;

    public DatasetCache(
        IDatasetPersistenceManager persistenceManager,
        ILogger<DatasetCache> logger,
        IOptions<DatasetCacheOptions>? options = null)
    {
        _persistenceManager = persistenceManager ?? throw new ArgumentNullException(nameof(persistenceManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new DatasetCacheOptions();

        // Configure memory cache with size limit
        _cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = _options.MaxCacheSize
        });

        // Dictionary to keep track of available dataset IDs
        _availableDatasetIds = new ConcurrentDictionary<string, bool>();
    }

    // Initializes the cache with the list of available datasets
    public Task InitializeAsync(IEnumerable<string> availableDatasetIds)
    {
        ArgumentNullException.ThrowIfNull(availableDatasetIds);

        try
        {
            foreach (var id in availableDatasetIds)
            {
                _availableDatasetIds.TryAdd(id, true);
            }

            _isInitialized = true;
            _logger.LogInformation("DatasetCache initialized with {Count} available datasets.", availableDatasetIds.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize DatasetCache.");
            throw;
        }

        return Task.CompletedTask;
    }

    // Retrieves a dataset from cache or loads it from persistence if not cached
    public async Task<CachedDataset> GetCachedDatasetAsync(string datasetId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(datasetId);
        EnsureInitialized();

        if (!_availableDatasetIds.ContainsKey(datasetId))
        {
            throw new DatasetNotFoundException(datasetId);
        }

        // Try to return from cache if available
        if (_cache.TryGetValue(datasetId, out CachedDataset? cachedDataset) && cachedDataset != null)
        {
            return cachedDataset;
        }

        // If not in cache, load it from persistence and cache it
        return await _cache.GetOrCreateAsync(datasetId, async entry =>
        {
            // Start tracing activity
            using var activity = ActivitySource.StartActivity("LoadDataset");
            activity?.SetTag("dataset.id", datasetId);

            try
            {
                var dataset = await _persistenceManager.LoadDatasetAsync(datasetId).ConfigureAwait(false)
                    ?? throw new DatasetNotFoundException(datasetId);

                // Build search index for the dataset
                var index = IndexRebuilder.RebuildIndex(dataset);

                // Configure cache entry expiration and size
                entry.SetSlidingExpiration(_options.CacheExpiration);
                entry.SetSize(1);

                _logger.LogInformation("Dataset '{DatasetId}' loaded and cached from storage.", datasetId);
                return new CachedDataset { Dataset = dataset, Index = index };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load dataset '{DatasetId}' from storage.", datasetId);
                throw;
            }
        }).ConfigureAwait(false) ?? throw new DatasetNotFoundException(datasetId);
    }

    // Adds a dataset directly to the cache
    public void AddDatasetToCache(string datasetId, Dataset dataset)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(datasetId);
        ArgumentNullException.ThrowIfNull(dataset);

        var index = IndexRebuilder.RebuildIndex(dataset);
        var cachedDataset = new CachedDataset { Dataset = dataset, Index = index };

        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(_options.CacheExpiration)
            .SetSize(1);

        _cache.Set(datasetId, cachedDataset, cacheEntryOptions);
        _logger.LogDebug("Dataset '{DatasetId}' added to cache.", datasetId);
    }

    // Removes a dataset from the cache
    public void RemoveDatasetFromCache(string datasetId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(datasetId);
        _cache.Remove(datasetId);
        _logger.LogDebug("Dataset '{DatasetId}' removed from cache.", datasetId);
    }

    // Invalidates (removes) a cached dataset if it exists
    public void InvalidateDataset(string datasetId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(datasetId);

        if (_cache.TryGetValue(datasetId, out CachedDataset? cachedDataset) && cachedDataset != null)
        {
            _cache.Remove(datasetId);
            _logger.LogInformation("Cache for dataset '{DatasetId}' invalidated.", datasetId);
        }
    }

    // Checks if a dataset ID is marked as available
    public bool IsDatasetAvailable(string datasetId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(datasetId);
        EnsureInitialized();
        return _availableDatasetIds.ContainsKey(datasetId);
    }

    // Returns all available dataset IDs
    public IEnumerable<string> GetAvailableDatasetIds()
    {
        EnsureInitialized();
        return _availableDatasetIds.Keys.ToList();
    }

    // Returns all currently cached datasets
    public IEnumerable<Dataset> GetCachedDatasets()
    {
        EnsureInitialized();

        var cachedDatasets = new List<Dataset>();
        foreach (var datasetId in _availableDatasetIds.Keys)
        {
            if (_cache.TryGetValue(datasetId, out CachedDataset? cached) && cached != null)
            {
                cachedDatasets.Add(cached.Dataset);
            }
        }
        return cachedDatasets;
    }

    // Marks a dataset ID as available
    public void AddAvailableDatasetId(string datasetId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(datasetId);
        _availableDatasetIds.TryAdd(datasetId, true);
        _logger.LogDebug("Dataset ID '{DatasetId}' added to available datasets.", datasetId);
    }

    // Removes a dataset ID from the available list
    public void RemoveAvailableDatasetId(string datasetId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(datasetId);
        _availableDatasetIds.TryRemove(datasetId, out _);
        _logger.LogDebug("Dataset ID '{DatasetId}' removed from available datasets.", datasetId);
    }

    // Ensures the cache is initialized before use
    private void EnsureInitialized()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("DatasetCache has not been initialized. Call InitializeAsync first.");
        }
    }

    // Cleanup
    public void Dispose()
    {
        _cache?.Dispose();
        ActivitySource?.Dispose();
        GC.SuppressFinalize(this);
    }
}

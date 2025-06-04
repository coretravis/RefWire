using ListServDB.Core.Caching;
using ListServDB.Core.Concurrency;
using ListServDB.Core.Exceptions;
using ListServDB.Core.Interfaces;
using ListServDB.Core.Links;
using ListServDB.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace ListServDB.Core.API;

/// <summary>
/// Represents the ListServ API for managing datasets.
/// </summary>
public class ListServApi : IDisposable, IListServApi
{
    // Constants
    private static readonly ActivitySource ActivitySource = new("ListServDB.Core.API");

    // Dependencies
    private readonly IDatasetCache _datasetCache;
    private readonly IConcurrencyManager _concurrency;
    private readonly ILogger<ListServApi> _logger;
    private readonly IDatasetPersistenceManager _persistenceManager;
    private readonly ILinkEnricher _linkEnricher;
    private readonly ListServOpts _options;

    // State management
    private readonly Lazy<Task> _initializationTask;
    private volatile bool _isInitialized;

    public ListServApi(
        IDatasetCache datasetCache,
        IDatasetPersistenceManager persistenceManager,
        IConcurrencyManager concurrencyManager,
        ILinkEnricher linkEnricher,
        ILogger<ListServApi> logger,
        IOptions<ListServOpts>? options = null)
    {
        _datasetCache = datasetCache ?? throw new ArgumentNullException(nameof(datasetCache));
        _persistenceManager = persistenceManager ?? throw new ArgumentNullException(nameof(persistenceManager));
        _concurrency = concurrencyManager ?? throw new ArgumentNullException(nameof(concurrencyManager));
        _linkEnricher = linkEnricher ?? throw new ArgumentNullException(nameof(linkEnricher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new ListServOpts();
        _initializationTask = new Lazy<Task>(InitializeAsync);
    }

    /// <summary>
    /// Ensures the API is initialized before use
    /// </summary>
    private async Task EnsureInitializedAsync()
    {
        if (!_isInitialized)
        {
            await _initializationTask.Value.ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Initializes the API and dataset cache asynchronously
    /// </summary>
    private async Task InitializeAsync()
    {
        try
        {
            var datasetIds = await _persistenceManager.ListDatasetIdsAsync().ConfigureAwait(false);
            await _datasetCache.InitializeAsync(datasetIds).ConfigureAwait(false);

            _isInitialized = true;
            _logger.LogInformation("ListServApi initialized successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize ListServApi.");
            throw;
        }
    }

    /// <summary>
    /// Filters data by specified fields with proper null handling
    /// </summary>
    private Dictionary<string, object> FilterDataByFields(
        Dictionary<string, object> data,
        HashSet<string>? includeFields)
    {
        if (includeFields == null || includeFields.Count == 0)
        {
            return new Dictionary<string, object>();
        }

        return data
            .Where(kv => includeFields.Contains(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    /// <summary>
    /// Converts DatasetItem to DTO with field filtering
    /// </summary>
    private DatasetItemDto ToDto(DatasetItem item, HashSet<string>? includeFields)
    {
        return new DatasetItemDto
        {
            Id = item.Id,
            Name = item.Name,
            IsArchived = item.IsArchived,
            Data = FilterDataByFields(item.Data, includeFields)
        };
    }

    // Public API Methods
    public async Task<IEnumerable<string>> ListDatasetIdsAsync()
    {
        await EnsureInitializedAsync().ConfigureAwait(false);
        return _datasetCache.GetAvailableDatasetIds();
    }

    public async Task<IEnumerable<Dataset>> GetAllDatasetsAsync()
    {
        await EnsureInitializedAsync().ConfigureAwait(false);

        _concurrency.EnterReadLock();
        try
        {
            return _datasetCache.GetCachedDatasets();
        }
        finally
        {
            _concurrency.ExitReadLock();
        }
    }

    public async Task<Dataset> CreateDatasetAsync(
        string id,
        string name,
        string description,
        string idField,
        string nameField,
        List<DatasetField> fields,
        Dictionary<string, DatasetItem> items)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(items);

        await EnsureInitializedAsync().ConfigureAwait(false);

        if (_datasetCache.IsDatasetAvailable(id))
        {
            throw new DatasetAlreadyExistsException(id);
        }

        var dataset = new Dataset
        {
            Id = id,
            Name = name,
            Description = description ?? string.Empty,
            IdField = idField,
            NameField = nameField,
            Fields = new List<DatasetField>(fields),
            Items = new Dictionary<string, DatasetItem>(items)
        };

        _concurrency.EnterWriteLock();
        try
        {
            // Double-check after acquiring lock
            if (_datasetCache.IsDatasetAvailable(id))
            {
                throw new DatasetAlreadyExistsException(id);
            }

            _datasetCache.AddAvailableDatasetId(id);
            _datasetCache.AddDatasetToCache(id, dataset);

            _logger.LogInformation("User created dataset '{DatasetId}'.", id);
        }
        finally
        {
            _concurrency.ExitWriteLock();
        }

        await _persistenceManager.SaveDatasetAsync(dataset).ConfigureAwait(false);
        return dataset;
    }

    public async Task<Dataset> GetDatasetByIdAsync(
           string datasetId,
           IEnumerable<string>? includeFields = null,
           IEnumerable<string>? links = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(datasetId);
        await EnsureInitializedAsync().ConfigureAwait(false);

        var cached = await _datasetCache.GetCachedDatasetAsync(datasetId).ConfigureAwait(false);

        // no filtering or linking so return raw
        if ((includeFields == null || !includeFields.Any()) &&
            (links == null || !links.Any()))
        {
            return cached.Dataset;
        }

        var includeFieldsSet = includeFields?.Any() == true
            ? new HashSet<string>(includeFields)
            : null;

        var linkedItemsCache = await _linkEnricher
            .PrepareLinkedDatasetsAsync(links, null)
            .ConfigureAwait(false);

        Dataset filtered;
        _concurrency.EnterReadLock();
        try
        {
            var processed = cached.Dataset.Items.Values
                .Where(i => !i.IsArchived)
                .Select(i => ToDto(i, includeFieldsSet))
                .Select(dto => _linkEnricher.EnrichWithLinkedData(dto, links, linkedItemsCache))
                .ToDictionary(dto => dto.Id, dto => new DatasetItem
                {
                    Id = dto.Id,
                    Name = dto.Name,
                    IsArchived = dto.IsArchived,
                    Data = dto.Data
                });

            filtered = new Dataset
            {
                Id = cached.Dataset.Id,
                Name = cached.Dataset.Name,
                Description = cached.Dataset.Description,
                IdField = cached.Dataset.IdField,
                NameField = cached.Dataset.NameField,
                Fields = new List<DatasetField>(cached.Dataset.Fields),
                Items = processed
            };
        }
        finally
        {
            _concurrency.ExitReadLock();
        }

        return filtered;
    }

    public async Task<DatasetMeta> GetDatasetMetaAsync(string datasetId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(datasetId);

        await EnsureInitializedAsync().ConfigureAwait(false);

        var cached = await _datasetCache.GetCachedDatasetAsync(datasetId).ConfigureAwait(false);
        return new DatasetMeta
        {
            Id = datasetId,
            Name = cached.Dataset.Name,
            IdField = cached.Dataset.IdField,
            Description = cached.Dataset.Description,
            NameField = cached.Dataset.NameField,
            Fields = cached.Dataset.Fields
        };
    }

    public async Task<IEnumerable<DatasetItem>> ListItemsAsync(
            string datasetId,
            int skip,
            int take,
            IEnumerable<string>? includeFields = null,
            IEnumerable<string>? links = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(datasetId);
        ArgumentOutOfRangeException.ThrowIfNegative(skip);
        ArgumentOutOfRangeException.ThrowIfLessThan(take, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(take, _options.MaxSearchResults);

        await EnsureInitializedAsync().ConfigureAwait(false);

        var cached = await _datasetCache.GetCachedDatasetAsync(datasetId).ConfigureAwait(false);

        List<DatasetItem> snapshot;
        _concurrency.EnterReadLock();
        try
        {
            snapshot = cached.Dataset.Items.Values
                .Where(i => !i.IsArchived)
                .OrderBy(i => i.Id)
                .Skip(skip)
                .Take(take)
                .ToList();
        }
        finally
        {
            _concurrency.ExitReadLock();
        }

        var includeFieldsSet = includeFields?.Any() == true
            ? new HashSet<string>(includeFields)
            : null;

        var itemIds = snapshot.Select(i => i.Id);
        var linkedItemsCache = await _linkEnricher
            .PrepareLinkedDatasetsAsync(links, itemIds)
            .ConfigureAwait(false);

        return snapshot
            .Select(i => ToDto(i, includeFieldsSet))
            .Select(dto => _linkEnricher.EnrichWithLinkedData(dto, links, linkedItemsCache))
            .Select(dto => new DatasetItem
            {
                Id = dto.Id,
                Name = dto.Name,
                IsArchived = dto.IsArchived,
                Data = dto.Data
            });
    }

    public async Task<IEnumerable<DatasetItem>> SearchItemsByIdsAsync(
          string datasetId,
          IEnumerable<string> itemIds,
          IEnumerable<string>? includeFields = null,
          IEnumerable<string>? links = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(datasetId);
        ArgumentNullException.ThrowIfNull(itemIds);

        await EnsureInitializedAsync().ConfigureAwait(false);

        var idList = itemIds.ToList();
        if (idList.Count == 0)
        {
            return Enumerable.Empty<DatasetItem>();
        }

        var cached = await _datasetCache.GetCachedDatasetAsync(datasetId).ConfigureAwait(false);
        var includeFieldsSet = includeFields?.Any() == true
            ? new HashSet<string>(includeFields)
            : null;

        List<DatasetItem> snapshot;
        _concurrency.EnterReadLock();
        try
        {
            snapshot = idList
                .Where(id => cached.Dataset.Items.TryGetValue(id, out var item) && !item.IsArchived)
                .Select(id => cached.Dataset.Items[id])
                .ToList();
        }
        finally
        {
            _concurrency.ExitReadLock();
        }

        var linkedItemsCache = await _linkEnricher
            .PrepareLinkedDatasetsAsync(links, idList)
            .ConfigureAwait(false);

        return snapshot
            .Select(i => ToDto(i, includeFieldsSet))
            .Select(dto => _linkEnricher.EnrichWithLinkedData(dto, links, linkedItemsCache))
            .Select(dto => new DatasetItem
            {
                Id = dto.Id,
                Name = dto.Name,
                IsArchived = dto.IsArchived,
                Data = dto.Data
            });
    }

    public async Task<IEnumerable<DatasetItem>> SearchItemsAsync(
           string datasetId,
           string searchTerm,
           int skip,
           int take,
           IEnumerable<string>? includeFields = null,
           IEnumerable<string>? links = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(datasetId);
        ArgumentException.ThrowIfNullOrWhiteSpace(searchTerm);
        ArgumentOutOfRangeException.ThrowIfNegative(skip);
        ArgumentOutOfRangeException.ThrowIfLessThan(take, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(take, _options.MaxSearchResults);

        await EnsureInitializedAsync().ConfigureAwait(false);

        using var activity = ActivitySource.StartActivity("SearchItems");
        activity?.SetTag("dataset.id", datasetId);
        activity?.SetTag("search.term", searchTerm);

        var cached = await _datasetCache.GetCachedDatasetAsync(datasetId).ConfigureAwait(false);
        var includeFieldsSet = includeFields?.Any() == true
            ? new HashSet<string>(includeFields)
            : null;

        List<DatasetItem> snapshot;
        _concurrency.EnterReadLock();
        try
        {
            snapshot = cached.Index
                .Find(searchTerm)
                .Where(i => !i.IsArchived)
                .Skip(skip)
                .Take(take)
                .ToList();
        }
        finally
        {
            _concurrency.ExitReadLock();
        }

        var itemIds = snapshot.Select(i => i.Id);
        var linkedItemsCache = await _linkEnricher
            .PrepareLinkedDatasetsAsync(links, itemIds)
            .ConfigureAwait(false);

        return snapshot
            .Select(i => ToDto(i, includeFieldsSet))
            .Select(dto => _linkEnricher.EnrichWithLinkedData(dto, links, linkedItemsCache))
            .Select(dto => new DatasetItem
            {
                Id = dto.Id,
                Name = dto.Name,
                IsArchived = dto.IsArchived,
                Data = dto.Data
            });
    }

    public async Task AddItemAsync(string datasetId, DatasetItem item)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(datasetId);
        ArgumentNullException.ThrowIfNull(item);
        ArgumentException.ThrowIfNullOrWhiteSpace(item.Id);

        await EnsureInitializedAsync().ConfigureAwait(false);

        var cached = await _datasetCache.GetCachedDatasetAsync(datasetId).ConfigureAwait(false);

        _concurrency.EnterWriteLock();
        try
        {
            if (cached.Dataset.Items.ContainsKey(item.Id))
            {
                throw new ItemAlreadyExistsException(item.Id, datasetId);
            }

            cached.Dataset.Items.Add(item.Id, item);

            if (item.Data.TryGetValue(cached.Dataset.NameField, out var nameValue))
            {
                var nameStr = nameValue?.ToString();
                if (!string.IsNullOrEmpty(nameStr))
                {
                    cached.Index.Add(nameStr, item);
                }
            }

            _logger.LogInformation("User added item '{ItemId}' to dataset '{DatasetId}'.", item.Id, datasetId);
        }
        finally
        {
            _concurrency.ExitWriteLock();
        }

        await _persistenceManager.SaveDatasetAsync(cached.Dataset).ConfigureAwait(false);
    }

    public async Task UpdateItemAsync(string datasetId, DatasetItem updatedItem)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(datasetId);
        ArgumentNullException.ThrowIfNull(updatedItem);
        ArgumentException.ThrowIfNullOrWhiteSpace(updatedItem.Id);

        await EnsureInitializedAsync().ConfigureAwait(false);

        var cached = await _datasetCache.GetCachedDatasetAsync(datasetId).ConfigureAwait(false);

        _concurrency.EnterWriteLock();
        try
        {
            if (!cached.Dataset.Items.TryGetValue(updatedItem.Id, out var existing))
            {
                throw new ItemNotFoundException(updatedItem.Id, datasetId);
            }

            // Remove old index entry
            if (existing.Data.TryGetValue(cached.Dataset.NameField, out var oldName))
            {
                var oldNameStr = oldName?.ToString();
                if (!string.IsNullOrEmpty(oldNameStr))
                {
                    cached.Index.Remove(oldNameStr, existing);
                }
            }

            cached.Dataset.Items[updatedItem.Id] = updatedItem;

            // Add new index entry
            if (updatedItem.Data.TryGetValue(cached.Dataset.NameField, out var newName))
            {
                var newNameStr = newName?.ToString();
                if (!string.IsNullOrEmpty(newNameStr))
                {
                    cached.Index.Add(newNameStr, updatedItem);
                }
            }

            _logger.LogInformation("User updated item '{ItemId}' in dataset '{DatasetId}'.", updatedItem.Id, datasetId);
        }
        finally
        {
            _concurrency.ExitWriteLock();
        }

        await _persistenceManager.SaveDatasetAsync(cached.Dataset).ConfigureAwait(false);
    }

    public async Task ArchiveItemAsync(string datasetId, string itemId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(datasetId);
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);

        await EnsureInitializedAsync().ConfigureAwait(false);

        var cached = await _datasetCache.GetCachedDatasetAsync(datasetId).ConfigureAwait(false);

        _concurrency.EnterWriteLock();
        try
        {
            if (!cached.Dataset.Items.TryGetValue(itemId, out var item))
            {
                throw new ItemNotFoundException(itemId, datasetId);
            }

            item.IsArchived = true;

            if (item.Data.TryGetValue(cached.Dataset.NameField, out var nameValue))
            {
                var nameStr = nameValue?.ToString();
                if (!string.IsNullOrEmpty(nameStr))
                {
                    cached.Index.Remove(nameStr, item);
                }
            }

            _logger.LogInformation("User archived item '{ItemId}' in dataset '{DatasetId}'.", itemId, datasetId);
        }
        finally
        {
            _concurrency.ExitWriteLock();
        }

        await _persistenceManager.SaveDatasetAsync(cached.Dataset).ConfigureAwait(false);
    }

    public async Task UpdateDatasetAsync(Dataset updatedDataset)
    {
        ArgumentNullException.ThrowIfNull(updatedDataset);
        ArgumentException.ThrowIfNullOrWhiteSpace(updatedDataset.Id);

        await EnsureInitializedAsync().ConfigureAwait(false);

        var cached = await _datasetCache.GetCachedDatasetAsync(updatedDataset.Id).ConfigureAwait(false);

        _concurrency.EnterWriteLock();
        try
        {
            cached.Dataset.Name = updatedDataset.Name;
            cached.Dataset.Description = updatedDataset.Description;
            cached.Dataset.Fields = new List<DatasetField>(updatedDataset.Fields);

            _logger.LogInformation("User updated dataset '{DatasetId}'.", updatedDataset.Id);
        }
        finally
        {
            _concurrency.ExitWriteLock();
        }

        await _persistenceManager.SaveDatasetAsync(cached.Dataset).ConfigureAwait(false);
    }

    public async Task DeleteDatasetAsync(string datasetId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(datasetId);
        await EnsureInitializedAsync().ConfigureAwait(false);

        if (!_datasetCache.IsDatasetAvailable(datasetId))
        {
            throw new DatasetNotFoundException(datasetId);
        }

        _concurrency.EnterWriteLock();
        try
        {
            _datasetCache.RemoveDatasetFromCache(datasetId);
            _datasetCache.RemoveAvailableDatasetId(datasetId);
            _logger.LogInformation("User deleted dataset '{DatasetId}'.", datasetId);
        }
        finally
        {
            _concurrency.ExitWriteLock();
        }

        await _persistenceManager.DeleteDatasetAsync(datasetId).ConfigureAwait(false);
    }

    public async Task DeleteDataset(string datasetId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(datasetId);
        await EnsureInitializedAsync().ConfigureAwait(false);

        if (!_datasetCache.IsDatasetAvailable(datasetId))
        {
            throw new DatasetNotFoundException(datasetId);
        }

        _concurrency.EnterWriteLock();
        try
        {
            _datasetCache.RemoveDatasetFromCache(datasetId);
            _datasetCache.RemoveAvailableDatasetId(datasetId);
            _logger.LogInformation("User deleted dataset '{DatasetId}'.", datasetId);
        }
        finally
        {
            _concurrency.ExitWriteLock();
        }

        await _persistenceManager.DeleteDatasetAsync(datasetId).ConfigureAwait(false);
    }

    public async Task InvalidateDatasetCacheAsync(string datasetId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(datasetId);
        await EnsureInitializedAsync().ConfigureAwait(false);

        _concurrency.EnterWriteLock();
        try
        {
            _datasetCache.InvalidateDataset(datasetId);
            _logger.LogInformation("Cache for dataset '{DatasetId}' invalidated.", datasetId);
        }
        finally
        {
            _concurrency.ExitWriteLock();
        }

        await Task.CompletedTask;
    }

    public async Task<ListServState> GetStateAsync()
    {
        await EnsureInitializedAsync().ConfigureAwait(false);

        _concurrency.EnterReadLock();
        try
        {
            var datasets = _datasetCache.GetCachedDatasets();
            var stateDict = datasets.ToDictionary(d => d.Id, d => d);
            return new ListServState { Datasets = stateDict };
        }
        finally
        {
            _concurrency.ExitReadLock();
        }
    }

    public async Task AddItemsAsync(string datasetId, List<DatasetItem> items)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(datasetId);
        ArgumentNullException.ThrowIfNull(items);

        await EnsureInitializedAsync().ConfigureAwait(false);

        if (items.Count == 0)
        {
            return; // Nothing to add
        }

        var cached = await _datasetCache.GetCachedDatasetAsync(datasetId).ConfigureAwait(false);

        _concurrency.EnterWriteLock();
        try
        {
            var duplicateIds = new List<string>();
            var itemsToAdd = new List<DatasetItem>();

            // First pass: validate all items and check for duplicates
            foreach (var item in items)
            {
                if (item == null)
                {
                    continue; // Skip null items
                }

                if (string.IsNullOrWhiteSpace(item.Id))
                {
                    throw new ArgumentException($"Item with null or empty Id found in the collection.");
                }

                if (cached.Dataset.Items.ContainsKey(item.Id))
                {
                    duplicateIds.Add(item.Id);
                }
                else
                {
                    itemsToAdd.Add(item);
                }
            }

            // If any duplicates found, throw exception with details
            if (duplicateIds.Count > 0)
            {
                var duplicateList = string.Join(", ", duplicateIds.Take(5)); // Show first 5 duplicates
                var message = duplicateIds.Count > 5
                    ? $"Items with ids '{duplicateList}' and {duplicateIds.Count - 5} others already exist in dataset '{datasetId}'."
                    : $"Items with ids '{duplicateList}' already exist in dataset '{datasetId}'.";
                throw new ItemAlreadyExistsException(duplicateList, datasetId);
            }

            // Second pass: add all valid items to dataset and index
            foreach (var item in itemsToAdd)
            {
                cached.Dataset.Items.Add(item.Id, item);

                // Add to search index if item has a name field value
                if (item.Data.TryGetValue(cached.Dataset.NameField, out var nameValue))
                {
                    var nameStr = nameValue?.ToString();
                    if (!string.IsNullOrEmpty(nameStr))
                    {
                        cached.Index.Add(nameStr, item);
                    }
                }
            }

            _logger.LogInformation("User added {ItemCount} items to dataset '{DatasetId}'.", itemsToAdd.Count, datasetId);
        }
        finally
        {
            _concurrency.ExitWriteLock();
        }

        // Save the updated dataset to persistence
        await _persistenceManager.SaveDatasetAsync(cached.Dataset).ConfigureAwait(false);
    }

    public async Task<DatasetItem> GetItemByIdAsync(string datasetId, string itemId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(datasetId);
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);

        await EnsureInitializedAsync().ConfigureAwait(false);

        var cached = await _datasetCache.GetCachedDatasetAsync(datasetId).ConfigureAwait(false);

        DatasetItem? item;
        _concurrency.EnterReadLock();
        try
        {
            if (!cached.Dataset.Items.TryGetValue(itemId, out item))
            {
                throw new ItemNotFoundException(itemId, datasetId);
            }

            // Check if item is archived
            if (item.IsArchived)
            {
                throw new ItemNotFoundException(itemId, datasetId);
            }
        }
        finally
        {
            _concurrency.ExitReadLock();
        }

        return item;
    }

    public void Dispose()
    {
        // Suppress finalization.
        GC.SuppressFinalize(this);
    }
}
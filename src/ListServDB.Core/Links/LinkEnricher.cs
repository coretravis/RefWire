using ListServDB.Core.API;
using ListServDB.Core.Caching;
using ListServDB.Core.Concurrency;
using ListServDB.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ListServDB.Core.Links;

/// <summary>
/// Prepares and enriches dataset items with linked data from other datasets
/// </summary>
public class LinkEnricher : ILinkEnricher
{
    private readonly IConcurrencyManager _concurrency;
    private readonly ILogger<LinkEnricher> _logger;
    private readonly ListServOpts _options;
    private readonly IDatasetCache _datasetCache;

    public LinkEnricher(
        IConcurrencyManager concurrency,
        ILogger<LinkEnricher> logger,
        IOptions<ListServOpts> opts,
        IDatasetCache datasetCache)
    {
        _concurrency = concurrency ?? throw new ArgumentNullException(nameof(concurrency));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = opts?.Value ?? throw new ArgumentNullException(nameof(opts));
        _datasetCache = datasetCache ?? throw new ArgumentNullException(nameof(datasetCache));
    }

    public async Task<Dictionary<string, Dictionary<string, List<DatasetItem>>>> PrepareLinkedDatasetsAsync(
        IEnumerable<string>? links,
        IEnumerable<string>? requiredItemIds = null)
    {
        var linkedItemsCache = new Dictionary<string, Dictionary<string, List<DatasetItem>>>();

        // Return empty if no links are provided
        if (links == null || !links.Any())
        {
            return linkedItemsCache;
        }

        // Limit number of links to process based on configuration
        var linksList = links.Take(_options.MaxLinkedDatasets).ToList();

        // Parse and validate each link, extracting datasetId and field name
        var validLinks = linksList
            .Select(link => link.Split('-', 2))
            .Where(parts => parts.Length == 2 && _datasetCache.IsDatasetAvailable(parts[0]))
            .Select(parts => (link: string.Join('-', parts), datasetId: parts[0], field: parts[1]))
            .ToList();

        // Load all valid datasets asynchronously from the cache
        var tasks = validLinks.Select(async info =>
        {
            try
            {
                var cached = await _datasetCache.GetCachedDatasetAsync(info.datasetId).ConfigureAwait(false);
                return (info.link, cached, info.field);
            }
            catch (Exception ex)
            {
                // Log warning if a dataset fails to load
                _logger.LogWarning(ex, "Failed to load linked dataset '{DatasetId}'", info.datasetId);
                return (info.link, (CachedDataset?)null, info.field);
            }
        });

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);

        // Process each successfully loaded dataset
        foreach (var (link, cachedDs, field) in results)
        {
            if (cachedDs == null)
            {
                continue;
            }

            var lookup = new Dictionary<string, List<DatasetItem>>();

            // Ensure thread-safe access to dataset items
            _concurrency.EnterReadLock();
            try
            {
                // Filter items that are not archived and have a value for the specified field
                var items = cachedDs.Dataset.Items.Values
                    .Where(i => !i.IsArchived && i.Data.TryGetValue(field, out var v) && v != null);

                foreach (var it in items)
                {
                    var key = it.Data[field].ToString();

                    // Skip items with no key or if not in the required ID list
                    if (string.IsNullOrEmpty(key) || (requiredItemIds != null && !requiredItemIds.Contains(key)))
                    {
                        continue;
                    }

                    // Add item to lookup table
                    if (!lookup.TryGetValue(key, out var list))
                    {
                        lookup[key] = list = new List<DatasetItem>();
                    }

                    list.Add(it);
                }
            }
            finally
            {
                _concurrency.ExitReadLock(); // Always release lock
            }

            // Store lookup for the current link
            linkedItemsCache[link] = lookup;
        }

        return linkedItemsCache;
    }

    public DatasetItemDto EnrichWithLinkedData(
        DatasetItemDto item,
        IEnumerable<string>? links,
        Dictionary<string, Dictionary<string, List<DatasetItem>>> linkedItemsCache)
    {
        if (links == null || !links.Any())
        {
            return item; // No enrichment if no links provided
        }

        // Clone existing data for enrichment
        var enriched = new Dictionary<string, object>(item.Data);

        foreach (var link in links)
        {
            // Check if data exists in the cache for this link
            if (!linkedItemsCache.TryGetValue(link, out var lookup))
            {
                continue;
            }

            var id = item.Id;

            // Extract data from the lookup for this item's ID
            enriched[link.Split('-')[0]] = lookup.TryGetValue(id, out var list)
                ? list.Select(li => new Dictionary<string, object>(li.Data)).ToList()
                : new List<Dictionary<string, object>>();
        }

        // Return the enriched item
        return item with { Data = enriched };
    }
}

using ListServDB.Core.Models;

namespace ListServDB.Core.Links;

/// <summary>
/// Interface for link enrichment services
/// </summary>
public interface ILinkEnricher
{
    /// <summary>
    /// Prepares a cache of dataset items from linked datasets based on specified field values.
    /// </summary>
    /// <param name="links">List of links in format "datasetId-fieldName"</param>
    /// <param name="requiredItemIds">Optional filter to include only items with matching IDs</param>
    Task<Dictionary<string, Dictionary<string, List<DatasetItem>>>> PrepareLinkedDatasetsAsync(
        IEnumerable<string>? links,
        IEnumerable<string>? requiredItemIds = null);
    /// <summary>
    /// Enriches a dataset item by attaching linked data from other datasets.
    /// </summary>
    /// <param name="item">The item to enrich</param>
    /// <param name="links">The links to resolve and attach</param>
    /// <param name="linkedItemsCache">Pre-computed lookup of linked items</param>
    DatasetItemDto EnrichWithLinkedData(
        DatasetItemDto item,
        IEnumerable<string>? links,
        Dictionary<string, Dictionary<string, List<DatasetItem>>> linkedItemsCache);
}

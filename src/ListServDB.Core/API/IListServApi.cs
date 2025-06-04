using ListServDB.Core.Models;

namespace ListServDB.Core.API;

/// <summary>
/// Interface for ListServ API operations.
/// </summary>
public interface IListServApi
{

    /// <summary>
    /// Adds a new item to the specified dataset.
    /// </summary>
    /// <remarks>Use this method to add a new item to an existing dataset. Ensure that the dataset ID is valid
    /// and that the item conforms to the dataset's schema.</remarks>
    /// <param name="datasetId">The unique identifier of the dataset to which the item will be added. Cannot be null or empty.</param>
    /// <param name="item">The item to add to the dataset. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task AddItemAsync(string datasetId, DatasetItem item);
    /// <summary>
    /// Adds multiple items to the specified dataset.
    /// </summary>
    /// <param name="datasetId"> The ID of the dataset.</param>
    /// <param name="item"> The items to add.</param>
    /// <returns></returns>
    Task AddItemsAsync(string datasetId, List<DatasetItem> item);
    /// <summary>
    /// Archives the specified item within the given dataset.
    /// </summary>
    /// <remarks>Use this method to archive an item, making it inactive or hidden without permanently deleting
    /// it. Ensure that the dataset and item identifiers are valid and that the item is in a state that allows
    /// archiving.</remarks>
    /// <param name="datasetId">The unique identifier of the dataset containing the item to be archived. Cannot be null or empty.</param>
    /// <param name="itemId">The unique identifier of the item to archive. Cannot be null or empty.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ArchiveItemAsync(string datasetId, string itemId);
    /// <summary>
    /// Creates a new dataset.
    /// </summary>
    /// <param name="id">The ID of the dataset.</param>
    /// <param name="name">The name of the dataset.</param>
    /// <param name="idField">The field used as the unique identifier for items.</param>
    /// <param name="nameField">The field used as the display name for items.</param>
    /// <param name="fields">The list of fields defining the schema of the dataset.</param>
    Task<Dataset> CreateDatasetAsync(string id,
                                     string name,
                                     string description,
                                     string idField,
                                     string nameField,
                                     List<DatasetField> fields,
                                     Dictionary<string, DatasetItem> items);
    /// <summary>
    /// Retrieves all datasets available in the system.
    /// </summary>
    /// <remarks>This method does not filter or paginate the datasets. It retrieves all datasets  currently
    /// available. Ensure that the caller handles large collections appropriately.</remarks>
    /// <returns>A task that represents the asynchronous operation. The task result contains an  IEnumerable{T} of Dataset
    /// objects representing the datasets. If no datasets are available, the result will be an empty collection.</returns>
    Task<IEnumerable<Dataset>> GetAllDatasetsAsync();
    /// <summary>
    /// Retrieves a dataset by its unique identifier.
    /// </summary>
    /// <remarks>Use this method to retrieve a dataset and optionally include specific fields or related
    /// links. This method performs an asynchronous operation and should be awaited.</remarks>
    /// <param name="datasetId">The unique identifier of the dataset to retrieve. This value cannot be null or empty.</param>
    /// <param name="includeFields">An optional collection of field names to include in the result. If null, all fields are included.</param>
    /// <param name="links">An optional collection of related resource links to include in the result. If null, no related links are
    /// included.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="Dataset"/> object
    /// corresponding to the specified identifier, or null if no dataset is found.</returns>
    Task<Dataset> GetDatasetByIdAsync(
    string datasetId,
    IEnumerable<string>? includeFields = null,
    IEnumerable<string>? links = null);
    /// <summary>
    /// Asynchronously retrieves metadata for the specified dataset.
    /// </summary>
    /// <remarks>Use this method to retrieve detailed information about a dataset, such as its  schema,
    /// creation date, or other descriptive properties. Ensure that the  <paramref name="datasetId"/> provided is valid
    /// and corresponds to an existing dataset.</remarks>
    /// <param name="datasetId">The unique identifier of the dataset for which metadata is being retrieved.  This value cannot be null or empty.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains  a <see cref="DatasetMeta"/> object
    /// with metadata about the specified dataset.</returns>
    Task<DatasetMeta> GetDatasetMetaAsync(string datasetId);
    /// <summary>
    /// Retrieves a collection of dataset items by their unique identifiers.
    /// </summary>
    /// <remarks>This method performs an asynchronous search for items within a specified dataset. The caller
    /// can optionally  specify which fields and links to include in the result to optimize performance and reduce
    /// payload size.</remarks>
    /// <param name="datasetId">The unique identifier of the dataset containing the items.</param>
    /// <param name="itemIds">A collection of item IDs to search for within the dataset. Cannot be null or empty.</param>
    /// <param name="includeFields">An optional collection of field names to include in the result. If null, all fields are included.</param>
    /// <param name="links">An optional collection of link types to include in the result. If null, no links are included.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an enumerable collection of  <see
    /// cref="DatasetItem"/> objects corresponding to the specified item IDs. If no items are found, the collection will
    /// be empty.</returns>
    Task<IEnumerable<DatasetItem>> SearchItemsByIdsAsync(string datasetId,
                                                         IEnumerable<string> itemIds,
                                                         IEnumerable<string>? includeFields = null, 
                                                         IEnumerable<string>? links = null);    
    /// <summary>
    /// Retrieves the current state of the list service.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the current <see
    /// cref="ListServState"/> of the list service.</returns>
    Task<ListServState> GetStateAsync();    
    /// <summary>
    /// Asynchronously retrieves a collection of dataset identifiers.
    /// </summary>
    /// <remarks>The returned collection may be empty if no datasets are available. This method does not
    /// guarantee  the order of the dataset identifiers in the result.</remarks>
    /// <returns>A task that represents the asynchronous operation. The task result contains an  <IEnumerable{T}/> of strings,
    /// where each string is the unique identifier of a dataset.</returns>
    Task<IEnumerable<string>> ListDatasetIdsAsync();
    /// <summary>
    /// Retrieves a paginated list of items from the specified dataset.
    /// </summary>
    /// <remarks>Use this method to retrieve a subset of items from a dataset, with optional filtering of
    /// fields and links.  This method is useful for implementing pagination in scenarios where datasets contain a large
    /// number of items.</remarks>
    /// <param name="datasetId">The unique identifier of the dataset to retrieve items from. Cannot be null or empty.</param>
    /// <param name="skip">The number of items to skip before starting to retrieve results. Must be zero or greater.</param>
    /// <param name="take">The maximum number of items to retrieve. Must be greater than zero.</param>
    /// <param name="includeFields">An optional collection of field names to include in the result. If null, all fields are included.</param>
    /// <param name="links">An optional collection of link names to include in the result. If null, no links are included.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an enumerable collection of  <see
    /// cref="DatasetItem"/> objects representing the retrieved items.</returns>
    Task<IEnumerable<DatasetItem>> ListItemsAsync(string datasetId,
                                                  int skip,
                                                  int take,
                                                  IEnumerable<string>? includeFields = null, 
                                                  IEnumerable<string>? links = null);    
    /// <summary>
    /// Asynchronously retrieves a dataset item by its unique identifier.
    /// </summary>
    /// <remarks>Use this method to retrieve a specific item from a dataset when both the dataset and item
    /// identifiers are known. Ensure that the provided identifiers are valid and correspond to existing
    /// resources.</remarks>
    /// <param name="datasetId">The unique identifier of the dataset containing the item. Cannot be null or empty.</param>
    /// <param name="itemId">The unique identifier of the item to retrieve. Cannot be null or empty.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="DatasetItem"/>
    /// corresponding to the specified <paramref name="itemId"/> within the dataset identified by <paramref
    /// name="datasetId"/>.</returns>
    Task<DatasetItem> GetItemByIdAsync(string datasetId, string itemId);

    /// <summary>
    /// Searches for items in the specified dataset that match the given search term.
    /// </summary>
    /// <remarks>This method performs a paginated search within the specified dataset. Use the <paramref
    /// name="skip"/> and <paramref name="take"/> parameters to control the pagination of results. The optional
    /// <paramref name="includeFields"/> and <paramref name="links"/> parameters allow customization of the returned
    /// data.</remarks>
    /// <param name="datasetId">The unique identifier of the dataset to search within. Cannot be null or empty.</param>
    /// <param name="searchTerm">The term to search for within the dataset. Cannot be null or empty.</param>
    /// <param name="skip">The number of items to skip before starting to return results. Must be zero or greater.</param>
    /// <param name="take">The maximum number of items to return. Must be greater than zero.</param>
    /// <param name="includeFields">An optional collection of field names to include in the search results. If null, all fields are included.</param>
    /// <param name="links">An optional collection of related links to include in the search results. If null, no additional links are
    /// included.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an enumerable collection of <see
    /// cref="DatasetItem"/> objects that match the search criteria. The collection will be empty if no matches are
    /// found.</returns>
    Task<IEnumerable<DatasetItem>> SearchItemsAsync(string datasetId,
                                                    string searchTerm,
                                                    int skip,
                                                    int take,
                                                    IEnumerable<string>? includeFields = null,
                                                    IEnumerable<string>? links = null);

    /// <summary>
    /// Updates the specified dataset with new information.
    /// </summary>
    /// <remarks>This method performs an asynchronous update of the dataset. Ensure that the provided
    /// <paramref name="updatedDataset"/> contains valid and complete data before calling this method.</remarks>
    /// <param name="updatedDataset">The dataset containing the updated information. Must not be <see langword="null"/>.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task UpdateDatasetAsync(Dataset updatedDataset);

    /// <summary>
    /// Updates an existing item in the specified dataset with new data.
    /// </summary>
    /// <remarks>This method performs an update operation on an existing item in the dataset. Ensure that the
    /// dataset and item exist before calling this method.</remarks>
    /// <param name="datasetId">The unique identifier of the dataset containing the item to update. Cannot be null or empty.</param>
    /// <param name="updatedItem">The updated item data to replace the existing item. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task UpdateItemAsync(string datasetId, DatasetItem updatedItem);

    /// <summary>
    /// Invalidates the cache for the specified dataset asynchronously.
    /// </summary>
    /// <remarks>This method clears any cached data associated with the specified dataset, ensuring that
    /// subsequent operations retrieve fresh data.</remarks>
    /// <param name="datasetId">The unique identifier of the dataset whose cache should be invalidated. Cannot be null or empty.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task InvalidateDatasetCacheAsync(string datasetId);

    /// <summary>
    /// Deletes the dataset with the specified identifier.
    /// </summary>
    /// <remarks>Ensure that the dataset identifier provided is valid and that the dataset is not in use or
    /// locked by other operations.</remarks>
    /// <param name="datasetId">The unique identifier of the dataset to delete. This value cannot be null or empty.</param>
    /// <returns>A task that represents the asynchronous operation. The task completes when the dataset is successfully deleted.</returns>
    Task DeleteDataset(string datasetId);
}

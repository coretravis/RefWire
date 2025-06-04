using ListServDB.Core.API;
using ListServDB.Core.Caching;
using ListServDB.Core.Concurrency;
using ListServDB.Core.Exceptions;
using ListServDB.Core.Interfaces;
using ListServDB.Core.Links;
using ListServDB.Core.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace ListServDB.Core.Tests.Api;

public class ListServApiTests
{
    private readonly Mock<IDatasetPersistenceManager> _mockPersistenceManager;
    private readonly Mock<IConcurrencyManager> _mockConcurrencyManager;
    private readonly Mock<ILinkEnricher> _mockLinkEnricher;
    private readonly Mock<IDatasetCache> _mockDatasetCache;
    private readonly Mock<ILogger<ListServApi>> _mockLogger;
    private readonly ListServApi _api;
    private readonly Dataset _sampleDataset;

    public ListServApiTests()
    {
        // Create sample dataset first since it's used by other setup code
        _sampleDataset = CreateSampleDataset();

        // Setup mock persistence manager
        _mockPersistenceManager = new Mock<IDatasetPersistenceManager>();

        // Setup mock concurrency manager
        _mockConcurrencyManager = new Mock<IConcurrencyManager>();
        _mockConcurrencyManager.Setup(m => m.EnterReadLock()).Verifiable();
        _mockConcurrencyManager.Setup(m => m.ExitReadLock()).Verifiable();
        _mockConcurrencyManager.Setup(m => m.EnterWriteLock()).Verifiable();
        _mockConcurrencyManager.Setup(m => m.ExitWriteLock()).Verifiable();

        // Setup mock link enricher
        _mockLinkEnricher = new Mock<ILinkEnricher>();
        _mockLinkEnricher.Setup(m => m.PrepareLinkedDatasetsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, Dictionary<string, List<DatasetItem>>>());
        _mockLinkEnricher.Setup(m => m.EnrichWithLinkedData(It.IsAny<DatasetItemDto>(), It.IsAny<IEnumerable<string>>(), It.IsAny<Dictionary<string, Dictionary<string, List<DatasetItem>>>>()))
            .Returns((DatasetItemDto item, IEnumerable<string> links, Dictionary<string, Dictionary<string, List<DatasetItem>>> cache) => item);

        // Setup mock dataset cache
        _mockDatasetCache = new Mock<IDatasetCache>();
        _mockDatasetCache.Setup(m => m.InitializeAsync(It.IsAny<IEnumerable<string>>()))
            .Returns(Task.CompletedTask);
        _mockDatasetCache.Setup(m => m.GetCachedDatasetAsync("test-dataset"))
            .ReturnsAsync(new CachedDataset { Dataset = _sampleDataset });
        _mockDatasetCache.Setup(m => m.IsDatasetAvailable(It.IsAny<string>()))
            .Returns((string id) => id == "test-dataset");
        _mockDatasetCache.Setup(m => m.GetAvailableDatasetIds())
            .Returns(new List<string> { "test-dataset" });
        _mockDatasetCache.Setup(m => m.GetCachedDatasets())
            .Returns(new List<Dataset> { _sampleDataset });
        _mockDatasetCache.Setup(m => m.AddDatasetToCache(It.IsAny<string>(), It.IsAny<Dataset>()));
        _mockDatasetCache.Setup(m => m.RemoveDatasetFromCache(It.IsAny<string>()));
        _mockDatasetCache.Setup(m => m.InvalidateDataset(It.IsAny<string>()));
        _mockDatasetCache.Setup(m => m.AddAvailableDatasetId(It.IsAny<string>()));
        _mockDatasetCache.Setup(m => m.RemoveAvailableDatasetId(It.IsAny<string>()));

        // Setup logger
        _mockLogger = new Mock<ILogger<ListServApi>>();

        // Setup available dataset IDs
        var availableDatasetIds = new List<string> { "test-dataset" };
        _mockPersistenceManager.Setup(m => m.ListDatasetIdsAsync())
            .ReturnsAsync(availableDatasetIds);

        // Setup dataset loading
        _mockPersistenceManager.Setup(m => m.LoadDatasetAsync("test-dataset"))
            .ReturnsAsync(_sampleDataset);

        // Create API instance
        _api = new ListServApi(
            _mockDatasetCache.Object,
            _mockPersistenceManager.Object,
            _mockConcurrencyManager.Object,
            _mockLinkEnricher.Object,
            _mockLogger.Object
        );
    }

    private Dataset CreateSampleDataset()
    {
        var fields = new List<DatasetField>
        {
            new DatasetField { Name = "id", DataType = "string", IsRequired = true },
            new DatasetField { Name = "name", DataType = "string", IsRequired = true },
            new DatasetField { Name = "age", DataType = "number", IsRequired = false },
            new DatasetField { Name = "email", DataType = "string", IsRequired = false }
        };

        var items = new Dictionary<string, DatasetItem>
        {
            ["item1"] = new DatasetItem
            {
                Id = "item1",
                Name = "John Doe",
                Data = new Dictionary<string, object>
                {
                    ["id"] = "item1",
                    ["name"] = "John Doe",
                    ["age"] = 30,
                    ["email"] = "john@example.com"
                }
            },
            ["item2"] = new DatasetItem
            {
                Id = "item2",
                Name = "Jane Smith",
                Data = new Dictionary<string, object>
                {
                    ["id"] = "item2",
                    ["name"] = "Jane Smith",
                    ["age"] = 25,
                    ["email"] = "jane@example.com"
                }
            }
        };

        return new Dataset
        {
            Id = "test-dataset",
            Name = "Test Dataset",
            Description = "A test dataset",
            IdField = "id",
            NameField = "name",
            Fields = fields,
            Items = items
        };
    }

    [Fact]
    public async Task ListDatasetIdsAsync_ReturnsAvailableDatasetIds()
    {
        // Act
        var result = await _api.ListDatasetIdsAsync();

        // Assert
        Assert.Single(result);
        Assert.Contains("test-dataset", result);
    }

    [Fact]
    public async Task GetDatasetByIdAsync_WithValidId_ReturnsDataset()
    {
        // Act
        var result = await _api.GetDatasetByIdAsync("test-dataset");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-dataset", result.Id);
        Assert.Equal("Test Dataset", result.Name);
    }

    [Fact]
    public async Task GetDatasetMetaAsync_WithValidId_ReturnsDatasetMeta()
    {
        // Act
        var result = await _api.GetDatasetMetaAsync("test-dataset");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-dataset", result.Id);
        Assert.Equal("Test Dataset", result.Name);
        Assert.Equal("id", result.IdField);
        Assert.Equal("name", result.NameField);
        Assert.Equal(4, result.Fields.Count);
    }

    [Fact]
    public async Task ListItemsAsync_ReturnsItemsWithPagination()
    {
        // Act
        var result = await _api.ListItemsAsync("test-dataset", 0, 10);

        // Assert
        Assert.Equal(2, result.Count());
        _mockConcurrencyManager.Verify(m => m.EnterReadLock(), Times.Once);
        _mockConcurrencyManager.Verify(m => m.ExitReadLock(), Times.Once);
    }

    [Fact]
    public async Task ListItemsAsync_WithSkipAndTake_ReturnsPaginatedItems()
    {
        // Act
        var result = await _api.ListItemsAsync("test-dataset", 1, 1);

        // Assert
        Assert.Single(result);
        Assert.Equal("item2", result.First().Id);
    }

    [Fact]
    public async Task ListItemsAsync_WithIncludeFields_ReturnsFilteredFields()
    {
        // Act
        var includeFields = new List<string> { "id", "name" };
        var result = await _api.ListItemsAsync("test-dataset", 0, 10, includeFields);

        // Assert
        Assert.Equal(2, result.Count());
        var firstItem = result.First();
        Assert.Equal(2, firstItem.Data.Count);
        Assert.Contains("id", firstItem.Data.Keys);
        Assert.Contains("name", firstItem.Data.Keys);
        Assert.DoesNotContain("age", firstItem.Data.Keys);
        Assert.DoesNotContain("email", firstItem.Data.Keys);
    }

    [Fact]
    public async Task SearchItemsByIdsAsync_WithValidIds_ReturnsMatchingItems()
    {
        // Act
        var itemIds = new List<string> { "item1" };
        var result = await _api.SearchItemsByIdsAsync("test-dataset", itemIds);

        // Assert
        Assert.Single(result);
        Assert.Equal("item1", result.First().Id);
        Assert.Equal("John Doe", result.First().Name);
    }

    [Fact]
    public async Task SearchItemsByIdsAsync_WithInvalidIds_ReturnsEmptyCollection()
    {
        // Act
        var itemIds = new List<string> { "non-existent-item" };
        var result = await _api.SearchItemsByIdsAsync("test-dataset", itemIds);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task CreateDatasetAsync_WithValidData_CreatesAndReturnsDataset()
    {
        // Arrange
        var newDatasetId = "new-dataset";
        var fields = new List<DatasetField>
        {
            new DatasetField { Name = "id", DataType = "string", IsRequired = true },
            new DatasetField { Name = "name", DataType = "string", IsRequired = true }
        };
        var items = new Dictionary<string, DatasetItem>();

        // Act
        var result = await _api.CreateDatasetAsync(
            newDatasetId, "New Dataset", "A new dataset", "id", "name", fields, items);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(newDatasetId, result.Id);
        Assert.Equal("New Dataset", result.Name);
        _mockPersistenceManager.Verify(m => m.SaveDatasetAsync(It.IsAny<Dataset>()), Times.Once);
    }

    [Fact]
    public async Task CreateDatasetAsync_WithExistingId_ThrowsInvalidOperationException()
    {
        // Arrange
        var fields = new List<DatasetField>();
        var items = new Dictionary<string, DatasetItem>();

        // Act & Assert
        await Assert.ThrowsAsync<DatasetAlreadyExistsException>(() =>
            _api.CreateDatasetAsync("test-dataset", "Test", "Test", "id", "name", fields, items));
    }

    [Fact]
    public async Task AddItemAsync_WithExistingItemId_ThrowsInvalidOperationException()
    {
        // Arrange
        var item = new DatasetItem
        {
            Id = "item1", // Existing ID
            Name = "Existing Item",
            Data = new Dictionary<string, object>
            {
                ["id"] = "item1",
                ["name"] = "Existing Item"
            }
        };

        // Ensure the dataset is available in the cache
        _mockDatasetCache.Setup(m => m.IsDatasetAvailable("test-dataset")).Returns(true);
        _mockDatasetCache.Setup(m => m.GetCachedDatasetAsync("test-dataset"))
            .ReturnsAsync(new CachedDataset { Dataset = _sampleDataset });

        // Act & Assert
        await Assert.ThrowsAsync<ItemAlreadyExistsException>(() =>
            _api.AddItemAsync("test-dataset", item));
    }

    [Fact]
    public async Task UpdateItemAsync_WithNonExistentItem_ThrowsKeyNotFoundException()
    {
        // Arrange
        var nonExistentItem = new DatasetItem
        {
            Id = "non-existent-item",
            Name = "Non Existent",
            Data = new Dictionary<string, object>()
        };

        // Ensure the dataset is available in the cache
        _mockDatasetCache.Setup(m => m.IsDatasetAvailable("test-dataset")).Returns(true);
        _mockDatasetCache.Setup(m => m.GetCachedDatasetAsync("test-dataset"))
            .ReturnsAsync(new CachedDataset { Dataset = _sampleDataset });

        // Act & Assert
        await Assert.ThrowsAsync<ItemNotFoundException>(() =>
            _api.UpdateItemAsync("test-dataset", nonExistentItem));
    }

    [Fact]
    public async Task ArchiveItemAsync_WithNonExistentItem_ThrowsKeyNotFoundException()
    {
        // Arrange
        // Ensure the dataset is available in the cache
        _mockDatasetCache.Setup(m => m.IsDatasetAvailable("test-dataset")).Returns(true);
        _mockDatasetCache.Setup(m => m.GetCachedDatasetAsync("test-dataset"))
            .ReturnsAsync(new CachedDataset { Dataset = CreateSampleDataset() });

        // Act & Assert
        await Assert.ThrowsAsync<ItemNotFoundException>(() =>
            _api.ArchiveItemAsync("test-dataset", "non-existent-item"));
    }

    [Fact]
    public async Task GetItemByIdAsync_WithValidId_ReturnsItem()
    {
        // Act
        var result = await _api.GetItemByIdAsync("test-dataset", "item1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("item1", result.Id);
        Assert.Equal("John Doe", result.Name);
        Assert.False(result.IsArchived);
    }

    [Fact]
    public async Task GetItemByIdAsync_WithNonExistentId_ThrowsKeyNotFoundException()
    {
        // Arrange
        // Ensure the dataset is available in the cache
        _mockDatasetCache.Setup(m => m.IsDatasetAvailable("test-dataset")).Returns(true);
        _mockDatasetCache.Setup(m => m.GetCachedDatasetAsync("test-dataset"))
            .ReturnsAsync(new CachedDataset { Dataset = CreateSampleDataset() });

        // Act & Assert
        await Assert.ThrowsAsync<ItemNotFoundException>(() =>
            _api.GetItemByIdAsync("test-dataset", "non-existent-item"));
    }
    
    [Fact]
    public async Task UpdateDatasetAsync_WithValidDataset_UpdatesDataset()
    {
        // Arrange
        var updatedDataset = new Dataset
        {
            Id = "test-dataset",
            Name = "Updated Test Dataset",
            IdField = "id",
            NameField = "name",
            Fields = new List<DatasetField>
            {
                new DatasetField { Name = "id", DataType = "string", IsRequired = true },
                new DatasetField { Name = "name", DataType = "string", IsRequired = true },
                new DatasetField { Name = "description", DataType = "string", IsRequired = false }
            },
            Items = new Dictionary<string, DatasetItem>()
        };

        // Act
        await _api.UpdateDatasetAsync(updatedDataset);

        // Assert
        _mockPersistenceManager.Verify(m => m.SaveDatasetAsync(It.IsAny<Dataset>()), Times.Once);
    }

    [Fact]
    public async Task InvalidateDatasetCacheAsync_RemovesDatasetFromCache()
    {
        // Arrange - Ensure dataset is in cache
        await _api.GetDatasetByIdAsync("test-dataset");

        // Act
        await _api.InvalidateDatasetCacheAsync("test-dataset");

        // Assert
        _mockConcurrencyManager.Verify(m => m.EnterWriteLock(), Times.AtLeastOnce);
        _mockConcurrencyManager.Verify(m => m.ExitWriteLock(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetStateAsync_ReturnsCurrentState()
    {
        // Act
        var result = await _api.GetStateAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Datasets);
        _mockConcurrencyManager.Verify(m => m.EnterReadLock(), Times.Once);
        _mockConcurrencyManager.Verify(m => m.ExitReadLock(), Times.Once);
    }
    
    [Fact]
    public async Task AddItemsAsync_WithDuplicateItem_ThrowsInvalidOperationException()
    {
        // Arrange
        var items = new List<DatasetItem>
        {
            new DatasetItem
            {
                Id = "new-item",
                Name = "New Item",
                Data = new Dictionary<string, object>
                {
                    ["id"] = "new-item",
                    ["name"] = "New Item"
                }
            },
            new DatasetItem
            {
                Id = "item1", // Existing ID
                Name = "Duplicate Item",
                Data = new Dictionary<string, object>
                {
                    ["id"] = "item1",
                    ["name"] = "Duplicate Item"
                }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ItemAlreadyExistsException>(() =>
            _api.AddItemsAsync("test-dataset", items));
    }

    [Fact]
    public async Task DeleteDataset_WithValidId_DeletesDataset()
    {
        // Arrange
        var datasetId = "test-dataset";

        // Act
        await _api.DeleteDataset(datasetId);

        // Assert
        _mockPersistenceManager.Verify(m => m.DeleteDatasetAsync(datasetId), Times.Once);
        _mockConcurrencyManager.Verify(m => m.EnterWriteLock(), Times.Once);
        _mockConcurrencyManager.Verify(m => m.ExitWriteLock(), Times.Once);
    }

    [Fact]
    public async Task DeleteDataset_WithInvalidId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var invalidDatasetId = "non-existent-dataset";

        // Act & Assert
        await Assert.ThrowsAsync<DatasetNotFoundException>(() =>
            _api.DeleteDataset(invalidDatasetId));
    }
    
    [Fact]
    public async Task AddItemsAsync_WithValidItems_AddsMultipleItems()
    {
        // Arrange
        var items = new List<DatasetItem>
        {
            new DatasetItem
            {
                Id = "new-item1",
                Name = "New Item 1",
                Data = new Dictionary<string, object>
                {
                    ["id"] = "new-item1",
                    ["name"] = "New Item 1"
                }
            },
            new DatasetItem
            {
                Id = "new-item2",
                Name = "New Item 2",
                Data = new Dictionary<string, object>
                {
                    ["id"] = "new-item2",
                    ["name"] = "New Item 2"
                }
            }
        };

        // Create a fresh dataset instance and mock index for this test
        var dataset = CreateSampleDataset();
        var mockIndex = new Mock<IIndex<string, DatasetItem>>();
        mockIndex.Setup(i => i.Add(It.IsAny<string>(), It.IsAny<DatasetItem>()));

        var cachedDataset = new CachedDataset
        {
            Dataset = dataset,
            Index = mockIndex.Object
        };

        // Ensure the dataset is available in the cache
        _mockDatasetCache.Setup(m => m.IsDatasetAvailable("test-dataset")).Returns(true);
        _mockDatasetCache.Setup(m => m.GetCachedDatasetAsync("test-dataset"))
            .ReturnsAsync(cachedDataset);

        // Act
        await _api.AddItemsAsync("test-dataset", items);

        // Assert
        _mockPersistenceManager.Verify(m => m.SaveDatasetAsync(It.IsAny<Dataset>()), Times.Once);
        _mockConcurrencyManager.Verify(m => m.EnterWriteLock(), Times.Once);
        _mockConcurrencyManager.Verify(m => m.ExitWriteLock(), Times.Once);
        Assert.True(dataset.Items.ContainsKey("new-item1"));
        Assert.True(dataset.Items.ContainsKey("new-item2"));
        mockIndex.Verify(i => i.Add("New Item 1", It.IsAny<DatasetItem>()), Times.Once);
        mockIndex.Verify(i => i.Add("New Item 2", It.IsAny<DatasetItem>()), Times.Once);
    }

    [Fact]
    public async Task GetItemByIdAsync_WithArchivedItem_ThrowsItemNotFoundException()
    {
        // Arrange
        var dataset = CreateSampleDataset();
        var mockIndex = new Mock<IIndex<string, DatasetItem>>();
        mockIndex.Setup(i => i.Remove(It.IsAny<string>(), It.IsAny<DatasetItem>()));

        var cachedDataset = new CachedDataset
        {
            Dataset = dataset,
            Index = mockIndex.Object
        };

        // Ensure the dataset is available in the cache
        _mockDatasetCache.Setup(m => m.IsDatasetAvailable("test-dataset")).Returns(true);
        _mockDatasetCache.Setup(m => m.GetCachedDatasetAsync("test-dataset"))
            .ReturnsAsync(cachedDataset);

        // First archive the item
        await _api.ArchiveItemAsync("test-dataset", "item1");

        // Act & Assert
        await Assert.ThrowsAsync<ItemNotFoundException>(() =>
            _api.GetItemByIdAsync("test-dataset", "item1"));
    }

    [Fact]
    public async Task ArchiveItemAsync_WithValidId_ArchivesItem()
    {
        // Arrange
        var dataset = CreateSampleDataset();
        var mockIndex = new Mock<IIndex<string, DatasetItem>>();
        mockIndex.Setup(i => i.Remove(It.IsAny<string>(), It.IsAny<DatasetItem>()));

        var cachedDataset = new CachedDataset
        {
            Dataset = dataset,
            Index = mockIndex.Object
        };

        // Ensure the dataset is available in the cache
        _mockDatasetCache.Setup(m => m.IsDatasetAvailable("test-dataset")).Returns(true);
        _mockDatasetCache.Setup(m => m.GetCachedDatasetAsync("test-dataset"))
            .ReturnsAsync(cachedDataset);

        // Act
        await _api.ArchiveItemAsync("test-dataset", "item1");

        // Assert
        _mockPersistenceManager.Verify(m => m.SaveDatasetAsync(It.IsAny<Dataset>()), Times.Once);
        _mockConcurrencyManager.Verify(m => m.EnterWriteLock(), Times.Once);
        _mockConcurrencyManager.Verify(m => m.ExitWriteLock(), Times.Once);

        // Verify the item is archived in the dataset
        Assert.True(dataset.Items["item1"].IsArchived);
        // Verify the item was removed from the search index
        mockIndex.Verify(i => i.Remove("John Doe", It.IsAny<DatasetItem>()), Times.Once);
    }

    [Fact]
    public async Task UpdateItemAsync_WithValidItem_UpdatesItem()
    {
        // Arrange
        var updatedItem = new DatasetItem
        {
            Id = "item1",
            Name = "Updated John Doe",
            Data = new Dictionary<string, object>
            {
                ["id"] = "item1",
                ["name"] = "Updated John Doe",
                ["age"] = 31
            }
        };

        // Create a fresh dataset instance and mock index for this test
        var dataset = CreateSampleDataset();
        var mockIndex = new Mock<IIndex<string, DatasetItem>>();
        mockIndex.Setup(i => i.Remove(It.IsAny<string>(), It.IsAny<DatasetItem>()));
        mockIndex.Setup(i => i.Add(It.IsAny<string>(), It.IsAny<DatasetItem>()));

        var cachedDataset = new CachedDataset
        {
            Dataset = dataset,
            Index = mockIndex.Object
        };

        // Ensure the dataset is available in the cache
        _mockDatasetCache.Setup(m => m.IsDatasetAvailable("test-dataset")).Returns(true);
        _mockDatasetCache.Setup(m => m.GetCachedDatasetAsync("test-dataset"))
            .ReturnsAsync(cachedDataset);

        // Act
        await _api.UpdateItemAsync("test-dataset", updatedItem);

        // Assert
        _mockPersistenceManager.Verify(m => m.SaveDatasetAsync(It.IsAny<Dataset>()), Times.Once);
        _mockConcurrencyManager.Verify(m => m.EnterWriteLock(), Times.Once);
        _mockConcurrencyManager.Verify(m => m.ExitWriteLock(), Times.Once);

        // Verify the item was updated in the dataset
        Assert.Equal("Updated John Doe", dataset.Items["item1"].Name);
        Assert.Equal(31, dataset.Items["item1"].Data["age"]);

        // Verify index operations
        mockIndex.Verify(i => i.Remove("John Doe", It.IsAny<DatasetItem>()), Times.Once);
        mockIndex.Verify(i => i.Add("Updated John Doe", It.IsAny<DatasetItem>()), Times.Once);
    }

    [Fact]
    public async Task AddItemAsync_WithValidItem_AddsItem()
    {
        // Arrange
        var item = new DatasetItem
        {
            Id = "new-item",
            Name = "New Item",
            Data = new Dictionary<string, object>
            {
                ["id"] = "new-item",
                ["name"] = "New Item"
            }
        };

        // Create a fresh dataset instance and mock index for this test
        var dataset = CreateSampleDataset();
        var mockIndex = new Mock<IIndex<string, DatasetItem>>();
        mockIndex.Setup(i => i.Add(It.IsAny<string>(), It.IsAny<DatasetItem>()));

        var cachedDataset = new CachedDataset
        {
            Dataset = dataset,
            Index = mockIndex.Object
        };

        // Ensure the dataset is available in the cache
        _mockDatasetCache.Setup(m => m.IsDatasetAvailable("test-dataset")).Returns(true);
        _mockDatasetCache.Setup(m => m.GetCachedDatasetAsync("test-dataset"))
            .ReturnsAsync(cachedDataset);

        // Act
        await _api.AddItemAsync("test-dataset", item);

        // Assert
        _mockPersistenceManager.Verify(m => m.SaveDatasetAsync(It.Is<Dataset>(d => d.Items.ContainsKey("new-item"))), Times.Once);
        _mockConcurrencyManager.Verify(m => m.EnterWriteLock(), Times.Once);
        _mockConcurrencyManager.Verify(m => m.ExitWriteLock(), Times.Once);
        Assert.True(dataset.Items.ContainsKey("new-item"));
        Assert.Equal("New Item", dataset.Items["new-item"].Name);

        // Verify the item was added to the search index
        mockIndex.Verify(i => i.Add("New Item", It.IsAny<DatasetItem>()), Times.Once);
    }

    [Fact]
    public async Task SearchItemsAsync_WithMatchingTerm_ReturnsMatchingItems()
    {
        // Arrange
        var dataset = CreateSampleDataset();
        var mockIndex = new Mock<IIndex<string, DatasetItem>>();

        // Setup the search index to return matching items
        var searchResults = new List<DatasetItem> { dataset.Items["item1"] };
        mockIndex.Setup(i => i.Find("John")).Returns(searchResults);

        var cachedDataset = new CachedDataset
        {
            Dataset = dataset,
            Index = mockIndex.Object
        };

        // Ensure the dataset is available in the cache
        _mockDatasetCache.Setup(m => m.IsDatasetAvailable("test-dataset")).Returns(true);
        _mockDatasetCache.Setup(m => m.GetCachedDatasetAsync("test-dataset"))
            .ReturnsAsync(cachedDataset);

        // Act
        var result = await _api.SearchItemsAsync("test-dataset", "John", 0, 10);

        // Assert
        _mockConcurrencyManager.Verify(m => m.EnterReadLock(), Times.Once);
        _mockConcurrencyManager.Verify(m => m.ExitReadLock(), Times.Once);

        // The result should contain items matching "John"
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("item1", result.First().Id);

        // Verify the search index was called
        mockIndex.Verify(i => i.Find("John"), Times.Once);
    }
}
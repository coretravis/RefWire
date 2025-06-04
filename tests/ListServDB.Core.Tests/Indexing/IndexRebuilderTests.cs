using ListServDB.Core.Indexing;
using ListServDB.Core.Models;

namespace ListServDB.Core.Tests.Indexing;

public class IndexRebuilderTests
{
    [Fact]
    public void RebuildIndex_ShouldCreateCorrectIndex()
    {
        // Arrange
        var dataset = new Dataset
        {
            Id = "dataset1",
            NameField = "name",
            Items = new Dictionary<string, DatasetItem>
            {
                { "1", new DatasetItem { Id = "1", IsArchived = false, Data = new Dictionary<string, object> { { "name", "apple" } } } },
                { "2", new DatasetItem { Id = "2", IsArchived = false, Data = new Dictionary<string, object> { { "name", "banana" } } } },
                { "3", new DatasetItem { Id = "3", IsArchived = true, Data = new Dictionary<string, object> { { "name", "archived" } } } },
                { "4", new DatasetItem { Id = "4", IsArchived = false, Data = new Dictionary<string, object> { { "id", "4" } } } } // No name field
            }
        };

        // Act
        var index = IndexRebuilder.RebuildIndex(dataset);
        var appleResults = index.Find("apple");
        var bananaResults = index.Find("banana");
        var archivedResults = index.Find("archived");

        // Assert
        Assert.Single(appleResults);
        Assert.Equal("1", appleResults.First().Id);
        Assert.Single(bananaResults);
        Assert.Equal("2", bananaResults.First().Id);
        Assert.Empty(archivedResults); // Should not include archived items
    }

    [Fact]
    public void RebuildAllIndexes_ShouldCreateIndexForEachDataset()
    {
        // Arrange
        var datasets = new Dictionary<string, Dataset>
        {
            {
                "dataset1",
                new Dataset
                {
                    Id = "dataset1",
                    NameField = "name",
                    Items = new Dictionary<string, DatasetItem>
                    {
                        { "1", new DatasetItem { Id = "1", IsArchived = false, Data = new Dictionary<string, object> { { "name", "apple" } } } }
                    }
                }
            },
            {
                "dataset2",
                new Dataset
                {
                    Id = "dataset2",
                    NameField = "title",
                    Items = new Dictionary<string, DatasetItem>
                    {
                        { "2", new DatasetItem { Id = "2", IsArchived = false, Data = new Dictionary<string, object> { { "title", "banana" } } } }
                    }
                }
            }
        };

        // Act
        var indexes = IndexRebuilder.RebuildAllIndexes(datasets);
        var appleResults = indexes["dataset1"].Find("apple");
        var bananaResults = indexes["dataset2"].Find("banana");

        // Assert
        Assert.Equal(2, indexes.Count);
        Assert.Single(appleResults);
        Assert.Equal("1", appleResults.First().Id);
        Assert.Single(bananaResults);
        Assert.Equal("2", bananaResults.First().Id);
    }

    [Fact]
    public void RebuildIndex_WithEmptyNameField_ShouldSkipItem()
    {
        // Arrange
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        var dataset = new Dataset
        {
            Id = "dataset1",
            NameField = "name",
            Items = new Dictionary<string, DatasetItem>
            {
                { "1", new DatasetItem { Id = "1", IsArchived = false, Data = new Dictionary<string, object> { { "name", "" } } } },
                { "2", new DatasetItem { Id = "2", IsArchived = false, Data = new Dictionary<string, object> { { "name", null } } } },
                { "3", new DatasetItem { Id = "3", IsArchived = false, Data = new Dictionary<string, object> { { "name", "   " } } } }
            }
        };
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        // Act
        var index = IndexRebuilder.RebuildIndex(dataset);
        var emptyResults = index.Find("");

        // Assert
        Assert.Empty(emptyResults);
    }
}

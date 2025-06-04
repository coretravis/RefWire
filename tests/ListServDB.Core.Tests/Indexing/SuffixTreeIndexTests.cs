using ListServDB.Core.Indexing.SuffixTree;
using ListServDB.Core.Models;

namespace ListServDB.Core.Tests.Indexing;
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
public class SuffixTreeIndexTests
{
    [Fact]
    public void Add_ShouldMakeItemFindable()
    {
        // Arrange
        var index = new SuffixTreeIndex();
        var item = new DatasetItem { Id = "1", Data = new Dictionary<string, object?> { { "name", "test" } } };

        // Act
        index.Add("Test", item); // Deliberately uppercase to test case-insensitivity
        var results = index.Find("test");

        // Assert
        Assert.Single(results);
        Assert.Contains(item, results);
    }

    [Fact]
    public void Add_WithEmptyKey_ShouldIgnore()
    {
        // Arrange
        var index = new SuffixTreeIndex();
        var item = new DatasetItem { Id = "1", Data = new Dictionary<string, object?> { { "name", "test" } } };

        // Act
        index.Add("", item);
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        index.Add(null, item);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        var results = index.Find("");

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void Find_WithSubstring_ShouldReturnMatchingItems()
    {
        // Arrange
        var index = new SuffixTreeIndex();
        var item = new DatasetItem { Id = "1", Data = new Dictionary<string, object?> { { "name", "banana" } } };
        index.Add("banana", item);

        // Act
        var results = index.Find("ana");

        // Assert
        Assert.Single(results);
        Assert.Contains(item, results);
    }

    [Fact]
    public void Find_WithEmptyQuery_ShouldReturnEmpty()
    {
        // Arrange
        var index = new SuffixTreeIndex();
        var item = new DatasetItem { Id = "1", Data = new Dictionary<string, object?> { { "name", "test" } } };
        index.Add("test", item);

        // Act
        var results1 = index.Find("");
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        var results2 = index.Find(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        // Assert
        Assert.Empty(results1);
        Assert.Empty(results2);
    }

    [Fact]
    public void Remove_WhenItemExists_ShouldRemoveItem()
    {
        // Arrange
        var index = new SuffixTreeIndex();
        var item = new DatasetItem { Id = "1", Data = new Dictionary<string, object?> { { "name", "test" } } };
        index.Add("test", item);

        // Act
        bool result = index.Remove("test", item);
        var findResults = index.Find("test");

        // Assert
        Assert.True(result);
        Assert.Empty(findResults);
    }

    [Fact]
    public void Remove_WithEmptyKey_ShouldReturnFalse()
    {
        // Arrange
        var index = new SuffixTreeIndex();
        var item = new DatasetItem { Id = "1", Data = new Dictionary<string, object?> { { "name", "test" } } };
        index.Add("test", item);

        // Act
        bool result1 = index.Remove("", item);
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        bool result2 = index.Remove(null, item);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        // Assert
        Assert.False(result1);
        Assert.False(result2);
    }

    [Fact]
    public void Clear_ShouldRemoveAllItems()
    {
        // Arrange
        var index = new SuffixTreeIndex();
        var item1 = new DatasetItem { Id = "1", Data = new Dictionary<string, object?> { { "name", "test1" } } };
        var item2 = new DatasetItem { Id = "2", Data = new Dictionary<string, object?> { { "name", "test2" } } };
        index.Add("test1", item1);
        index.Add("test2", item2);

        // Act
        index.Clear();
        var results1 = index.Find("test1");
        var results2 = index.Find("test2");

        // Assert
        Assert.Empty(results1);
        Assert.Empty(results2);
    }
}

#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.

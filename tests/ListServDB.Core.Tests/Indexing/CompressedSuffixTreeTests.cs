using ListServDB.Core.Indexing.SuffixTree;
using ListServDB.Core.Models;

namespace ListServDB.Core.Tests.Indexing;
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
public class CompressedSuffixTreeTests
{
    [Fact]
    public void Insert_WhenSingleItem_ShouldBeRetrievable()
    {
        // Arrange
        var tree = new CompressedSuffixTree();

        var item = new DatasetItem { Id = "1", Data = new Dictionary<string, object?> { { "name", "test" } } };


        // Act
        tree.Insert("test", item);
        var results = tree.Search("test");

        // Assert
        Assert.Single(results);
        Assert.Contains(item, results);
    }

    [Fact]
    public void Insert_WhenMultipleItems_ShouldBeRetrievable()
    {
        // Arrange
        var tree = new CompressedSuffixTree();
        var item1 = new DatasetItem { Id = "1", Data = new Dictionary<string, object?> { { "name", "apple" } } };
        var item2 = new DatasetItem { Id = "2", Data = new Dictionary<string, object?> { { "name", "banana" } } };

        // Act
        tree.Insert("apple", item1);
        tree.Insert("banana", item2);
        var appleResults = tree.Search("apple");
        var bananaResults = tree.Search("banana");

        // Assert
        Assert.Single(appleResults);
        Assert.Contains(item1, appleResults);
        Assert.Single(bananaResults);
        Assert.Contains(item2, bananaResults);
    }

    [Fact]
    public void Search_WithSubstring_ShouldReturnMatchingItems()
    {
        // Arrange
        var tree = new CompressedSuffixTree();
        var item = new DatasetItem { Id = "1", Data = new Dictionary<string, object?> { { "name", "banana" } } };
        tree.Insert("banana", item);

        // Act
        var results1 = tree.Search("ban");
        var results2 = tree.Search("ana");
        var results3 = tree.Search("na");
        var results4 = tree.Search("a");

        // Assert
        Assert.Contains(item, results1);
        Assert.Contains(item, results2);
        Assert.Contains(item, results3);
        Assert.Contains(item, results4);
    }

    [Fact]
    public void Search_WithNonExistentSubstring_ShouldReturnEmpty()
    {
        // Arrange
        var tree = new CompressedSuffixTree();
        var item = new DatasetItem { Id = "1", Data = new Dictionary<string, object?> { { "name", "banana" } } };
        tree.Insert("banana", item);

        // Act
        var results = tree.Search("cherry");

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void Search_WhenEmpty_ShouldReturnEmpty()
    {
        // Arrange
        var tree = new CompressedSuffixTree();

        // Act
        var results = tree.Search("test");

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void Remove_WhenItemExists_ShouldRemoveItem()
    {
        // Arrange
        var tree = new CompressedSuffixTree();
        var item = new DatasetItem { Id = "1", Data = new Dictionary<string, object?> { { "name", "test" } } };
        tree.Insert("test", item);

        // Act
        tree.Remove("test", item);
        var results = tree.Search("test");

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void Remove_WhenItemDoesNotExist_ShouldNotModifyTree()
    {
        // Arrange
        var tree = new CompressedSuffixTree();
        var item1 = new DatasetItem { Id = "1", Data = new Dictionary<string, object?> { { "name", "test" } } };
        var item2 = new DatasetItem { Id = "2", Data = new Dictionary<string, object?> { { "name", "other" } } };
        tree.Insert("test", item1);

        // Act
        tree.Remove("test", item2);
        var results = tree.Search("test");

        // Assert
        Assert.Single(results);
        Assert.Contains(item1, results);
    }

    [Fact]
    public void Clear_ShouldRemoveAllItems()
    {
        // Arrange
        var tree = new CompressedSuffixTree();
        var item1 = new DatasetItem { Id = "1", Data = new Dictionary<string, object?> { { "name", "test1" } } };
        var item2 = new DatasetItem { Id = "2", Data = new Dictionary<string, object?> { { "name", "test2" } } };
        tree.Insert("test1", item1);
        tree.Insert("test2", item2);

        // Act
        tree.Clear();
        var results1 = tree.Search("test1");
        var results2 = tree.Search("test2");

        // Assert
        Assert.Empty(results1);
        Assert.Empty(results2);
    }

    [Fact]
    public void Insert_WithEdgeSplitting_ShouldWorkCorrectly()
    {
        // Arrange
        var tree = new CompressedSuffixTree();
        var item1 = new DatasetItem { Id = "1", Data = new Dictionary<string, object?> { { "name", "abcdef" } } };
        var item2 = new DatasetItem { Id = "2", Data = new Dictionary<string, object?> { { "name", "abcxyz" } } };

        // Act
        tree.Insert("abcdef", item1);
        tree.Insert("abcxyz", item2);
        var results1 = tree.Search("abc");
        var results2 = tree.Search("def");
        var results3 = tree.Search("xyz");

        // Assert
        Assert.Equal(2, results1.Count());
        Assert.Contains(item1, results1);
        Assert.Contains(item2, results1);
        Assert.Single(results2);
        Assert.Contains(item1, results2);
        Assert.Single(results3);
        Assert.Contains(item2, results3);
    }
}
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.
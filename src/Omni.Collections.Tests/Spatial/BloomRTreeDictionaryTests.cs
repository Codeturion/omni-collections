using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Omni.Collections.Spatial;
using Omni.Collections.Spatial.BloomRTreeDictionary;
using Xunit;

namespace Omni.Collections.Tests.Spatial;

public class BloomRTreeDictionaryTests : IDisposable
{
    #region Constructor Tests

    /// <summary>
    /// Tests that a BloomRTreeDictionary can be constructed with default parameters.
    /// The dictionary should have zero count initially and valid statistics.
    /// </summary>
    [Fact]
    public void Constructor_WithDefaults_CreatesDictionaryWithCorrectProperties()
    {
        var dictionary = new BloomRTreeDictionary<string, int>();

        dictionary.Count.Should().Be(0);
        dictionary.IsEmpty.Should().BeTrue();
        
        var stats = dictionary.Statistics;
        stats.TotalEntries.Should().Be(0);
        stats.SpatialQueries.Should().Be(0);
        stats.BloomFilterHits.Should().Be(0);
        stats.DictionaryLookups.Should().Be(0);
    }

    /// <summary>
    /// Tests that a BloomRTreeDictionary can be constructed with custom parameters.
    /// The dictionary should accept valid capacity and false positive rate values.
    /// </summary>
    [Theory]
    [InlineData(1000, 0.01)]
    [InlineData(5000, 0.05)]
    [InlineData(10000, 0.1)]
    public void Constructor_WithCustomParameters_CreatesDictionaryWithValidSettings(int expectedCapacity, double falsePositiveRate)
    {
        var dictionary = new BloomRTreeDictionary<int, string>(expectedCapacity, falsePositiveRate);

        dictionary.Count.Should().Be(0);
        dictionary.IsEmpty.Should().BeTrue();
    }

    /// <summary>
    /// Tests that constructing with invalid expected capacity throws exception.
    /// The constructor should reject zero or negative capacity values.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_WithInvalidExpectedCapacity_ThrowsArgumentOutOfRangeException(int expectedCapacity)
    {
        var act = () => new BloomRTreeDictionary<string, int>(expectedCapacity);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("expectedCapacity");
    }

    /// <summary>
    /// Tests that constructing with invalid false positive rate throws exception.
    /// The constructor should reject rates outside the valid range (0, 1).
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-0.1)]
    [InlineData(1)]
    [InlineData(1.5)]
    public void Constructor_WithInvalidFalsePositiveRate_ThrowsArgumentOutOfRangeException(double falsePositiveRate)
    {
        var act = () => new BloomRTreeDictionary<string, int>(1000, falsePositiveRate);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("falsePositiveRate");
    }

    #endregion

    #region Add Tests

    /// <summary>
    /// Tests that adding a single key-value pair with bounds works correctly.
    /// The dictionary count should increase and the item should be retrievable by key.
    /// </summary>
    [Fact]
    public void Add_SingleKeyValueWithBounds_IncreasesCountAndStoresItem()
    {
        var dictionary = new BloomRTreeDictionary<string, int>();
        var bounds = new BoundingRectangle(0, 0, 10, 10);

        dictionary.Add("key1", 42, bounds);

        dictionary.Count.Should().Be(1);
        dictionary.IsEmpty.Should().BeFalse();
        dictionary["key1"].Should().Be(42);
    }

    /// <summary>
    /// Tests that adding a key-value pair with point coordinates works correctly.
    /// The dictionary should accept point coordinates and create appropriate bounds.
    /// </summary>
    [Fact]
    public void Add_KeyValueWithPointCoordinates_CreatesPointBounds()
    {
        var dictionary = new BloomRTreeDictionary<string, string>();

        dictionary.Add("location", "home", 50.0f, 50.0f);

        dictionary.Count.Should().Be(1);
        dictionary["location"].Should().Be("home");
    }

    /// <summary>
    /// Tests that adding multiple items with different keys works correctly.
    /// The dictionary should store all items and maintain correct count.
    /// </summary>
    [Fact]
    public void Add_MultipleItemsWithDifferentKeys_StoresAllItems()
    {
        var dictionary = new BloomRTreeDictionary<int, string>();

        var items = new[]
        {
            (1, "first", new BoundingRectangle(0, 0, 10, 10)),
            (2, "second", new BoundingRectangle(20, 20, 30, 30)),
            (3, "third", new BoundingRectangle(40, 40, 50, 50))
        };

        foreach (var (key, value, bounds) in items)
        {
            dictionary.Add(key, value, bounds);
        }

        dictionary.Count.Should().Be(3);
        dictionary[1].Should().Be("first");
        dictionary[2].Should().Be("second");
        dictionary[3].Should().Be("third");
    }

    /// <summary>
    /// Tests that updating an existing key with same bounds updates value only.
    /// The dictionary should update the value without changing spatial structure.
    /// </summary>
    [Fact]
    public void Add_ExistingKeyWithSameBounds_UpdatesValueOnly()
    {
        var dictionary = new BloomRTreeDictionary<string, int>();
        var bounds = new BoundingRectangle(0, 0, 10, 10);

        dictionary.Add("key1", 42, bounds);
        dictionary.Add("key1", 84, bounds);

        dictionary.Count.Should().Be(1);
        dictionary["key1"].Should().Be(84);
    }

    /// <summary>
    /// Tests that updating an existing key with different bounds updates both value and position.
    /// The dictionary should re-index the item in the spatial structure.
    /// </summary>
    [Fact]
    public void Add_ExistingKeyWithDifferentBounds_UpdatesValueAndPosition()
    {
        var dictionary = new BloomRTreeDictionary<string, string>();
        var bounds1 = new BoundingRectangle(0, 0, 10, 10);
        var bounds2 = new BoundingRectangle(20, 20, 30, 30);

        dictionary.Add("key1", "first_location", bounds1);
        dictionary.Add("key1", "second_location", bounds2);

        dictionary.Count.Should().Be(1);
        dictionary["key1"].Should().Be("second_location");
    }

    /// <summary>
    /// Tests adding items using the indexer property.
    /// The indexer should behave consistently with the Add method.
    /// </summary>
    [Fact]
    public void Indexer_SetValue_BehavesLikeAdd()
    {
        var dictionary = new BloomRTreeDictionary<string, int>();
        var bounds = new BoundingRectangle(0, 0, 10, 10);

        dictionary.Add("key1", 42, bounds);
        dictionary["key1"] = 84;

        dictionary.Count.Should().Be(1);
        dictionary["key1"].Should().Be(84);
    }

    #endregion

    #region AddRange Tests

    /// <summary>
    /// Tests that adding multiple items via AddRange works correctly.
    /// The dictionary should efficiently handle bulk insertion operations.
    /// </summary>
    [Fact]
    public void AddRange_WithMultipleItems_AddsAllItems()
    {
        var dictionary = new BloomRTreeDictionary<int, string>();

        var items = Enumerable.Range(0, 50)
            .Select(i => (i, $"item{i}", new BoundingRectangle(i, i, i + 1, i + 1)))
            .ToArray();

        dictionary.AddRange(items);

        dictionary.Count.Should().Be(50);
        for (int i = 0; i < 50; i++)
        {
            dictionary[i].Should().Be($"item{i}");
        }
    }

    /// <summary>
    /// Tests that AddRange with empty collection handles gracefully.
    /// The dictionary should remain unchanged when adding empty collections.
    /// </summary>
    [Fact]
    public void AddRange_WithEmptyCollection_LeavesUnchanged()
    {
        var dictionary = new BloomRTreeDictionary<string, int>();
        
        dictionary.Add("existing", 42, new BoundingRectangle(0, 0, 1, 1));
        dictionary.AddRange(Array.Empty<(string, int, BoundingRectangle)>());

        dictionary.Count.Should().Be(1);
        dictionary["existing"].Should().Be(42);
    }

    /// <summary>
    /// Tests that AddRange with large collection optimizes insertion.
    /// The dictionary should use bulk operations for large data sets.
    /// </summary>
    [Fact]
    public void AddRange_WithLargeCollection_OptimizesInsertion()
    {
        var dictionary = new BloomRTreeDictionary<int, string>();

        // Add initial items
        for (int i = 0; i < 200; i++)
        {
            dictionary.Add(i, $"initial{i}", new BoundingRectangle(i, i, i + 1, i + 1));
        }

        // Add large range
        var newItems = Enumerable.Range(200, 150)
            .Select(i => (i, $"item{i}", new BoundingRectangle(i, i, i + 1, i + 1)))
            .ToArray();

        dictionary.AddRange(newItems);

        dictionary.Count.Should().Be(350);
    }

    #endregion

    #region Lookup Tests

    /// <summary>
    /// Tests that TryGetValue works correctly for existing and non-existing keys.
    /// The method should return true and correct value for existing keys.
    /// </summary>
    [Fact]
    public void TryGetValue_ExistingAndNonExistingKeys_ReturnsCorrectResults()
    {
        var dictionary = new BloomRTreeDictionary<string, int>();
        var bounds = new BoundingRectangle(0, 0, 10, 10);

        dictionary.Add("existing", 42, bounds);

        // Test existing key
        var existsResult = dictionary.TryGetValue("existing", out int existingValue);
        existsResult.Should().BeTrue();
        existingValue.Should().Be(42);

        // Test non-existing key
        var nonExistResult = dictionary.TryGetValue("nonexisting", out int nonExistingValue);
        nonExistResult.Should().BeFalse();
        nonExistingValue.Should().Be(default(int));
    }

    /// <summary>
    /// Tests that ContainsKey works correctly for existing and non-existing keys.
    /// The method should return true only for keys that exist in the dictionary.
    /// </summary>
    [Fact]
    public void ContainsKey_ExistingAndNonExistingKeys_ReturnsCorrectResults()
    {
        var dictionary = new BloomRTreeDictionary<string, string>();
        var bounds = new BoundingRectangle(0, 0, 10, 10);

        dictionary.Add("existing", "value", bounds);

        dictionary.ContainsKey("existing").Should().BeTrue();
        dictionary.ContainsKey("nonexisting").Should().BeFalse();
    }

    /// <summary>
    /// Tests that indexer getter works correctly and updates statistics.
    /// The indexer should return values and track dictionary lookup statistics.
    /// </summary>
    [Fact]
    public void Indexer_GetValue_ReturnsCorrectValueAndUpdatesStats()
    {
        var dictionary = new BloomRTreeDictionary<int, string>();
        var bounds = new BoundingRectangle(0, 0, 10, 10);

        dictionary.Add(1, "test", bounds);

        var value = dictionary[1];
        value.Should().Be("test");

        var stats = dictionary.Statistics;
        stats.DictionaryLookups.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Tests that indexer getter throws exception for non-existing keys.
    /// The indexer should throw KeyNotFoundException for missing keys.
    /// </summary>
    [Fact]
    public void Indexer_GetNonExistingKey_ThrowsKeyNotFoundException()
    {
        var dictionary = new BloomRTreeDictionary<string, int>();

        var act = () => dictionary["nonexisting"];

        act.Should().Throw<KeyNotFoundException>();
    }

    #endregion

    #region Remove Tests

    /// <summary>
    /// Tests that removing an existing key works correctly.
    /// The dictionary count should decrease and the key should no longer exist.
    /// </summary>
    [Fact]
    public void Remove_ExistingKey_DecreasesCountAndRemovesKey()
    {
        var dictionary = new BloomRTreeDictionary<string, int>();
        var bounds = new BoundingRectangle(0, 0, 10, 10);

        dictionary.Add("key1", 42, bounds);
        var removed = dictionary.Remove("key1");

        removed.Should().BeTrue();
        dictionary.Count.Should().Be(0);
        dictionary.ContainsKey("key1").Should().BeFalse();
    }

    /// <summary>
    /// Tests that removing a non-existing key returns false.
    /// The dictionary should remain unchanged when removing non-existing keys.
    /// </summary>
    [Fact]
    public void Remove_NonExistingKey_ReturnsFalseAndLeavesUnchanged()
    {
        var dictionary = new BloomRTreeDictionary<string, int>();
        var bounds = new BoundingRectangle(0, 0, 10, 10);

        dictionary.Add("existing", 42, bounds);
        var removed = dictionary.Remove("nonexisting");

        removed.Should().BeFalse();
        dictionary.Count.Should().Be(1);
        dictionary.ContainsKey("existing").Should().BeTrue();
    }

    /// <summary>
    /// Tests that removing multiple keys works correctly.
    /// The dictionary should handle sequential removal operations properly.
    /// </summary>
    [Fact]
    public void Remove_MultipleKeys_RemovesAllCorrectly()
    {
        var dictionary = new BloomRTreeDictionary<int, string>();

        for (int i = 0; i < 10; i++)
        {
            dictionary.Add(i, $"item{i}", new BoundingRectangle(i, i, i + 1, i + 1));
        }

        // Remove every other item
        for (int i = 0; i < 10; i += 2)
        {
            var removed = dictionary.Remove(i);
            removed.Should().BeTrue();
        }

        dictionary.Count.Should().Be(5);
        for (int i = 1; i < 10; i += 2)
        {
            dictionary.ContainsKey(i).Should().BeTrue();
        }
    }

    #endregion

    #region Clear Tests

    /// <summary>
    /// Tests that clearing the dictionary removes all items and resets state.
    /// The dictionary should be empty after clearing with reset statistics.
    /// </summary>
    [Fact]
    public void Clear_WithItems_RemovesAllItemsAndResetsState()
    {
        var dictionary = new BloomRTreeDictionary<string, int>();

        for (int i = 0; i < 10; i++)
        {
            dictionary.Add($"key{i}", i, new BoundingRectangle(i, i, i + 1, i + 1));
        }

        dictionary.Clear();

        dictionary.Count.Should().Be(0);
        dictionary.IsEmpty.Should().BeTrue();
        
        var stats = dictionary.Statistics;
        stats.TotalEntries.Should().Be(0);
        stats.SpatialQueries.Should().Be(0);
        stats.BloomFilterHits.Should().Be(0);
        stats.DictionaryLookups.Should().Be(0);
    }

    #endregion

    #region Spatial Query Tests

    /// <summary>
    /// Tests spatial queries using the FindIntersecting method.
    /// The dictionary should return all entries whose bounds intersect the query rectangle.
    /// </summary>
    [Fact]
    public void FindIntersecting_WithOverlappingBounds_ReturnsCorrectEntries()
    {
        var dictionary = new BloomRTreeDictionary<string, string>();

        dictionary.Add("inside", "value1", new BoundingRectangle(10, 10, 20, 20));
        dictionary.Add("partial", "value2", new BoundingRectangle(15, 15, 25, 25));
        dictionary.Add("outside", "value3", new BoundingRectangle(30, 30, 40, 40));

        var queryBounds = new BoundingRectangle(5, 5, 15, 15);
        var results = dictionary.FindIntersecting(queryBounds).ToList();

        results.Should().HaveCount(2);
        results.Should().Contain(kvp => kvp.Key == "inside");
        results.Should().Contain(kvp => kvp.Key == "partial");
        results.Should().NotContain(kvp => kvp.Key == "outside");
    }

    /// <summary>
    /// Tests spatial queries using the FindContained method.
    /// The dictionary should return all entries completely contained within the query rectangle.
    /// </summary>
    [Fact]
    public void FindContained_WithBoundsInsideQuery_ReturnsContainedEntries()
    {
        var dictionary = new BloomRTreeDictionary<string, string>();

        dictionary.Add("contained", "value1", new BoundingRectangle(10, 10, 15, 15));
        dictionary.Add("partial", "value2", new BoundingRectangle(5, 5, 25, 25));
        dictionary.Add("outside", "value3", new BoundingRectangle(30, 30, 40, 40));

        var queryBounds = new BoundingRectangle(0, 0, 20, 20);
        var results = dictionary.FindContained(queryBounds).ToList();

        results.Should().ContainSingle();
        results.Single().Key.Should().Be("contained");
    }

    /// <summary>
    /// Tests spatial queries with empty results.
    /// The dictionary should return empty collections when no entries match the query.
    /// </summary>
    [Fact]
    public void SpatialQueries_WithNoMatches_ReturnEmptyCollections()
    {
        var dictionary = new BloomRTreeDictionary<string, int>();

        dictionary.Add("item", 42, new BoundingRectangle(0, 0, 10, 10));

        var queryBounds = new BoundingRectangle(50, 50, 60, 60);
        var intersecting = dictionary.FindIntersecting(queryBounds).ToList();
        var contained = dictionary.FindContained(queryBounds).ToList();

        intersecting.Should().BeEmpty();
        contained.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that spatial queries update statistics correctly.
    /// The dictionary should track spatial query performance metrics.
    /// </summary>
    [Fact]
    public void SpatialQueries_UpdateStatisticsCorrectly()
    {
        var dictionary = new BloomRTreeDictionary<string, string>();

        for (int i = 0; i < 10; i++)
        {
            dictionary.Add($"key{i}", $"value{i}", new BoundingRectangle(i, i, i + 5, i + 5));
        }

        // Perform multiple spatial queries
        var queryBounds = new BoundingRectangle(0, 0, 5, 5);
        dictionary.FindIntersecting(queryBounds).ToList();
        dictionary.FindContained(queryBounds).ToList();

        var stats = dictionary.Statistics;
        stats.SpatialQueries.Should().BeGreaterThan(0);
    }

    #endregion

    #region Enumeration Tests

    /// <summary>
    /// Tests that the dictionary can be enumerated as key-value pairs.
    /// The enumeration should return all entries in the dictionary.
    /// </summary>
    [Fact]
    public void GetEnumerator_WithItems_ReturnsAllKeyValuePairs()
    {
        var dictionary = new BloomRTreeDictionary<int, string>();

        var items = new Dictionary<int, string>
        {
            { 1, "first" },
            { 2, "second" },
            { 3, "third" }
        };

        foreach (var (key, value) in items)
        {
            dictionary.Add(key, value, new BoundingRectangle(key, key, key + 1, key + 1));
        }

        var enumeratedItems = dictionary.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        enumeratedItems.Should().BeEquivalentTo(items);
    }

    /// <summary>
    /// Tests that enumerating an empty dictionary returns no items.
    /// The enumeration should handle empty collections gracefully.
    /// </summary>
    [Fact]
    public void GetEnumerator_EmptyDictionary_ReturnsNoItems()
    {
        var dictionary = new BloomRTreeDictionary<string, int>();

        var items = dictionary.ToList();

        items.Should().BeEmpty();
    }

    #endregion

    #region Statistics Tests

    /// <summary>
    /// Tests that statistics provide accurate information about dictionary performance.
    /// The statistics should reflect the actual usage patterns and performance metrics.
    /// </summary>
    [Fact]
    public void Statistics_WithOperations_ProvidesAccurateMetrics()
    {
        var dictionary = new BloomRTreeDictionary<string, int>();

        // Add items
        for (int i = 0; i < 100; i++)
        {
            dictionary.Add($"key{i}", i, new BoundingRectangle(i, i, i + 1, i + 1));
        }

        // Perform dictionary lookups
        for (int i = 0; i < 10; i++)
        {
            dictionary.ContainsKey($"key{i}");
        }

        // Perform spatial queries
        var queryBounds = new BoundingRectangle(0, 0, 50, 50);
        dictionary.FindIntersecting(queryBounds).ToList();

        var stats = dictionary.Statistics;

        stats.TotalEntries.Should().Be(100);
        stats.DictionaryLookups.Should().BeGreaterThan(0);
        stats.SpatialQueries.Should().BeGreaterThan(0);
        stats.TreeHeight.Should().BeGreaterThan(0);
        stats.BloomFilterEffectiveness.Should().BeGreaterOrEqualTo(0);
    }

    /// <summary>
    /// Tests that tree height statistics are calculated correctly.
    /// The statistics should provide accurate tree depth information.
    /// </summary>
    [Fact]
    public void Statistics_TreeHeight_CalculatedCorrectly()
    {
        var dictionary = new BloomRTreeDictionary<int, string>();

        // Add enough items to create a multi-level tree
        for (int i = 0; i < 1000; i++)
        {
            dictionary.Add(i, $"item{i}", new BoundingRectangle(i % 100, i / 100, (i % 100) + 1, (i / 100) + 1));
        }

        var stats = dictionary.Statistics;

        stats.TreeHeight.Should().BeGreaterThan(1); // Should have multiple levels
    }

    #endregion

    #region Edge Cases and Performance Tests

    /// <summary>
    /// Tests dictionary behavior with very small bounding rectangles.
    /// The dictionary should handle point-like bounds correctly.
    /// </summary>
    [Fact]
    public void Add_WithPointBounds_HandlesCorrectly()
    {
        var dictionary = new BloomRTreeDictionary<string, string>();

        var pointBounds = new BoundingRectangle(50.0f, 50.0f); // Point bounds
        dictionary.Add("point", "location", pointBounds);

        dictionary.Count.Should().Be(1);
        dictionary["point"].Should().Be("location");

        var results = dictionary.FindIntersecting(pointBounds).ToList();
        results.Should().ContainSingle();
    }

    /// <summary>
    /// Tests dictionary behavior with very large bounding rectangles.
    /// The dictionary should handle large spatial extents without issues.
    /// </summary>
    [Fact]
    public void Add_WithLargeBounds_HandlesCorrectly()
    {
        var dictionary = new BloomRTreeDictionary<string, string>();

        var largeBounds = new BoundingRectangle(-1e6f, -1e6f, 1e6f, 1e6f);
        dictionary.Add("large", "area", largeBounds);

        dictionary.Count.Should().Be(1);
        dictionary["large"].Should().Be("area");

        var results = dictionary.FindIntersecting(new BoundingRectangle(0, 0, 100, 100)).ToList();
        results.Should().ContainSingle(); // Large bounds should intersect
    }

    /// <summary>
    /// Tests dictionary performance with large number of items.
    /// The dictionary should handle thousands of items efficiently.
    /// </summary>
    [Fact]
    public void Operations_WithLargeDataset_PerformEfficiently()
    {
        var dictionary = new BloomRTreeDictionary<int, string>();

        // Add 5000 items
        for (int i = 0; i < 5000; i++)
        {
            var bounds = new BoundingRectangle(i % 100, i / 100, (i % 100) + 1, (i / 100) + 1);
            dictionary.Add(i, $"item{i}", bounds);
        }

        dictionary.Count.Should().Be(5000);

        // Test random access performance
        for (int i = 0; i < 100; i++)
        {
            var key = i * 50;
            dictionary.ContainsKey(key).Should().BeTrue();
            dictionary[key].Should().Be($"item{key}");
        }

        // Test spatial query performance
        var queryBounds = new BoundingRectangle(25, 25, 75, 75);
        var results = dictionary.FindIntersecting(queryBounds).ToList();
        results.Should().NotBeEmpty();
    }

    /// <summary>
    /// Tests handling of null reference type values.
    /// The dictionary should support null values when T allows nulls.
    /// </summary>
    [Fact]
    public void Add_WithNullReferenceTypeValue_HandlesCorrectly()
    {
        var dictionary = new BloomRTreeDictionary<string, string?>();

        dictionary.Add("null_key", null, new BoundingRectangle(0, 0, 1, 1));

        dictionary.Count.Should().Be(1);
        dictionary["null_key"].Should().BeNull();
    }

    /// <summary>
    /// Tests behavior with overlapping bounding rectangles.
    /// The dictionary should handle spatial overlaps correctly in queries.
    /// </summary>
    [Fact]
    public void SpatialQueries_WithOverlappingBounds_HandlesCorrectly()
    {
        var dictionary = new BloomRTreeDictionary<string, int>();

        // Add overlapping rectangles
        dictionary.Add("rect1", 1, new BoundingRectangle(0, 0, 20, 20));
        dictionary.Add("rect2", 2, new BoundingRectangle(10, 10, 30, 30));
        dictionary.Add("rect3", 3, new BoundingRectangle(5, 5, 15, 15));

        // Query that intersects all three
        var queryBounds = new BoundingRectangle(8, 8, 12, 12);
        var results = dictionary.FindIntersecting(queryBounds).ToList();

        results.Should().HaveCount(3);
        results.Select(kvp => kvp.Value).Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    #endregion

    #region Disposal Tests

    /// <summary>
    /// Tests disposing the dictionary cleans up resources properly.
    /// The dictionary should release resources and be usable after disposal.
    /// </summary>
    [Fact]
    public void Dispose_WithItems_CleansUpResources()
    {
        var dictionary = new BloomRTreeDictionary<string, int>();

        dictionary.Add("test", 42, new BoundingRectangle(0, 0, 1, 1));
        dictionary.Dispose();

        // Dictionary should still be usable after disposal
        dictionary.Count.Should().Be(0);
    }

    #endregion

    public void Dispose()
    {
        // Test cleanup if needed
    }
}
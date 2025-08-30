using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Omni.Collections.Hybrid;
using Xunit;

namespace Omni.Collections.Tests.Hybrid;

public class CounterDictionaryTests
{
    /// <summary>
    /// Tests that a CounterDictionary can be constructed with default parameters.
    /// The dictionary should have default capacity and track writes enabled by default.
    /// </summary>
    [Fact]
    public void Constructor_Default_CreatesEmptyDictionaryWithWriteTracking()
    {
        var dict = new CounterDictionary<string, int>();

        dict.Count.Should().Be(0);
        dict.TrackWrites.Should().BeTrue();
        dict.TotalAccessCount.Should().Be(0);
    }

    /// <summary>
    /// Tests that a CounterDictionary can be constructed with specified capacity.
    /// The dictionary should have the specified initial settings.
    /// </summary>
    [Theory]
    [InlineData(1, true)]
    [InlineData(16, false)]
    [InlineData(100, true)]
    [InlineData(1000, false)]
    public void Constructor_WithCapacityAndTrackWrites_CreatesEmptyDictionaryWithCorrectSettings(int capacity, bool trackWrites)
    {
        var dict = new CounterDictionary<string, int>(capacity, trackWrites);

        dict.Count.Should().Be(0);
        dict.TrackWrites.Should().Be(trackWrites);
        dict.TotalAccessCount.Should().Be(0);
    }

    /// <summary>
    /// Tests that constructing a CounterDictionary with negative capacity throws exception.
    /// The constructor should reject negative capacity values.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(-10)]
    [InlineData(-100)]
    public void Constructor_WithNegativeCapacity_ThrowsArgumentOutOfRangeException(int capacity)
    {
        var act = () => new CounterDictionary<string, int>(capacity);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("capacity");
    }

    /// <summary>
    /// Tests that constructing a CounterDictionary with invalid load factor throws exception.
    /// The constructor should reject load factors outside the valid range.
    /// </summary>
    [Theory]
    [InlineData(0.0f)]
    [InlineData(-0.5f)]
    [InlineData(1.0f)]
    [InlineData(1.5f)]
    public void Constructor_WithInvalidLoadFactor_ThrowsArgumentOutOfRangeException(float loadFactor)
    {
        var act = () => new CounterDictionary<string, int>(16, true, loadFactor, null);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("loadFactor");
    }

    /// <summary>
    /// Tests that a CounterDictionary can be constructed with custom equality comparer.
    /// The dictionary should use the provided comparer for key comparisons.
    /// </summary>
    [Fact]
    public void Constructor_WithCustomComparer_UsesCustomComparerForKeys()
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        var dict = new CounterDictionary<string, int>(16, true, 0.75f, comparer);

        dict.AddOrUpdate("KEY", 1);
        dict.ContainsKey("key").Should().BeTrue();
        dict.ContainsKey("KEY").Should().BeTrue();
    }

    /// <summary>
    /// Tests that AddOrUpdate method can add new key-value pairs with correct initial counts.
    /// The dictionary should track access counts starting from 1 when incrementCount is true.
    /// </summary>
    [Fact]
    public void AddOrUpdate_NewItemWithIncrement_AddsItemWithInitialCount()
    {
        var dict = new CounterDictionary<string, int>();

        dict.AddOrUpdate("key1", 10, incrementCount: true);

        dict.Count.Should().Be(1);
        dict.ContainsKey("key1").Should().BeTrue();
        dict.GetAccessCount("key1").Should().Be(1);
        dict.TotalAccessCount.Should().Be(1);
    }

    /// <summary>
    /// Tests that AddOrUpdate method can add new items without incrementing count.
    /// The dictionary should start with zero access count when incrementCount is false.
    /// </summary>
    [Fact]
    public void AddOrUpdate_NewItemWithoutIncrement_AddsItemWithZeroCount()
    {
        var dict = new CounterDictionary<string, int>();

        dict.AddOrUpdate("key1", 10, incrementCount: false);

        dict.Count.Should().Be(1);
        dict.ContainsKey("key1").Should().BeTrue();
        dict.GetAccessCount("key1").Should().Be(0);
        dict.TotalAccessCount.Should().Be(0);
    }

    /// <summary>
    /// Tests that AddOrUpdate method updates existing items and increments count when specified.
    /// The dictionary should update values and increase access counts appropriately.
    /// </summary>
    [Fact]
    public void AddOrUpdate_ExistingItemWithIncrement_UpdatesValueAndIncreasesCount()
    {
        var dict = new CounterDictionary<string, int>();
        dict.AddOrUpdate("key1", 10, incrementCount: true);

        dict.AddOrUpdate("key1", 20, incrementCount: true);

        dict.Count.Should().Be(1);
        dict["key1"].Should().Be(20);
        dict.GetAccessCount("key1").Should().Be(3); // 1 + 1 (update) + 1 (indexer access)
        dict.TotalAccessCount.Should().Be(3);
    }

    /// <summary>
    /// Tests that AddOrUpdate method updates existing items without incrementing count when specified.
    /// The dictionary should update values without affecting access counts.
    /// </summary>
    [Fact]
    public void AddOrUpdate_ExistingItemWithoutIncrement_UpdatesValueWithoutIncrementingCount()
    {
        var dict = new CounterDictionary<string, int>();
        dict.AddOrUpdate("key1", 10, incrementCount: true);

        dict.AddOrUpdate("key1", 20, incrementCount: false);

        dict.Count.Should().Be(1);
        dict["key1"].Should().Be(20);
        dict.GetAccessCount("key1").Should().Be(2); // Gets incremented during access for indexer
        dict.TotalAccessCount.Should().Be(2);
    }

    /// <summary>
    /// Tests that the indexer getter increments access count when retrieving values.
    /// The indexer should track accesses and update total count appropriately.
    /// </summary>
    [Fact]
    public void Indexer_Get_IncrementsAccessCountAndReturnsValue()
    {
        var dict = new CounterDictionary<string, int>();
        dict.AddOrUpdate("key1", 42, incrementCount: false);

        var value = dict["key1"];

        value.Should().Be(42);
        dict.GetAccessCount("key1").Should().Be(1);
        dict.TotalAccessCount.Should().Be(1);
    }

    /// <summary>
    /// Tests that the indexer setter behavior depends on TrackWrites setting.
    /// The indexer should increment count only when TrackWrites is enabled.
    /// </summary>
    [Theory]
    [InlineData(true, 2)] // 1 for set + 1 for get (indexer access)
    [InlineData(false, 1)] // 0 for set + 1 for get (indexer access)
    public void Indexer_Set_BehaviorDependsOnTrackWritesSetting(bool trackWrites, long expectedCount)
    {
        var dict = new CounterDictionary<string, int>(16, trackWrites);

        dict["key1"] = 42;

        dict.Count.Should().Be(1);
        dict["key1"].Should().Be(42);
        dict.GetAccessCount("key1").Should().Be(expectedCount);
        dict.TotalAccessCount.Should().Be(expectedCount);
    }

    /// <summary>
    /// Tests that the indexer throws exception when accessing non-existent keys.
    /// The indexer should throw KeyNotFoundException for missing keys.
    /// </summary>
    [Fact]
    public void Indexer_NonExistentKey_ThrowsKeyNotFoundException()
    {
        var dict = new CounterDictionary<string, int>();

        var act = () => dict["nonexistent"];

        act.Should().Throw<KeyNotFoundException>();
    }

    /// <summary>
    /// Tests that TryGetValue method returns true and increments access count for existing keys.
    /// The method should track accesses while successfully retrieving values.
    /// </summary>
    [Fact]
    public void TryGetValue_ExistingKey_ReturnsTrueAndIncrementsCount()
    {
        var dict = new CounterDictionary<string, int>();
        dict.AddOrUpdate("key1", 42, incrementCount: false);

        var result = dict.TryGetValue("key1", out var value);

        result.Should().BeTrue();
        value.Should().Be(42);
        dict.GetAccessCount("key1").Should().Be(1);
        dict.TotalAccessCount.Should().Be(1);
    }

    /// <summary>
    /// Tests that TryGetValue method returns false without affecting counts for non-existent keys.
    /// The method should not track accesses for missing keys.
    /// </summary>
    [Fact]
    public void TryGetValue_NonExistentKey_ReturnsFalseWithoutAffectingCounts()
    {
        var dict = new CounterDictionary<string, int>();
        dict.AddOrUpdate("key1", 42, incrementCount: true);
        var initialCount = dict.TotalAccessCount;

        var result = dict.TryGetValue("nonexistent", out var value);

        result.Should().BeFalse();
        value.Should().Be(default(int));
        dict.TotalAccessCount.Should().Be(initialCount);
    }

    /// <summary>
    /// Tests that TryPeek method returns value and count without incrementing access count.
    /// The method should provide read-only access without affecting frequency tracking.
    /// </summary>
    [Fact]
    public void TryPeek_ExistingKey_ReturnsValueAndCountWithoutIncrementing()
    {
        var dict = new CounterDictionary<string, int>();
        dict.AddOrUpdate("key1", 42, incrementCount: true);
        var initialCount = dict.TotalAccessCount;

        var result = dict.TryPeek("key1", out var value, out var accessCount);

        result.Should().BeTrue();
        value.Should().Be(42);
        accessCount.Should().Be(1);
        dict.TotalAccessCount.Should().Be(initialCount); // Should not change
    }

    /// <summary>
    /// Tests that TryPeek method returns false for non-existent keys without affecting state.
    /// The method should handle missing keys gracefully.
    /// </summary>
    [Fact]
    public void TryPeek_NonExistentKey_ReturnsFalseWithDefaultValues()
    {
        var dict = new CounterDictionary<string, int>();

        var result = dict.TryPeek("nonexistent", out var value, out var accessCount);

        result.Should().BeFalse();
        value.Should().Be(default(int));
        accessCount.Should().Be(0);
    }

    /// <summary>
    /// Tests that GetAccessCount method returns correct count for existing keys.
    /// The method should retrieve access counts without affecting frequency tracking.
    /// </summary>
    [Fact]
    public void GetAccessCount_ExistingKey_ReturnsCorrectCount()
    {
        var dict = new CounterDictionary<string, int>();
        dict.AddOrUpdate("key1", 42, incrementCount: true);
        dict.TryGetValue("key1", out _);
        dict.TryGetValue("key1", out _);

        var count = dict.GetAccessCount("key1");

        count.Should().Be(3); // 1 from AddOrUpdate + 2 from TryGetValue calls
    }

    /// <summary>
    /// Tests that GetAccessCount method returns zero for non-existent keys.
    /// The method should handle missing keys by returning zero count.
    /// </summary>
    [Fact]
    public void GetAccessCount_NonExistentKey_ReturnsZero()
    {
        var dict = new CounterDictionary<string, int>();

        var count = dict.GetAccessCount("nonexistent");

        count.Should().Be(0);
    }

    /// <summary>
    /// Tests that ContainsKey method returns correct boolean values without affecting counts.
    /// The method should check key existence without incrementing access frequencies.
    /// </summary>
    [Theory]
    [InlineData("existing", true)]
    [InlineData("nonexistent", false)]
    public void ContainsKey_VariousKeys_ReturnsCorrectResultWithoutAffectingCounts(string key, bool expected)
    {
        var dict = new CounterDictionary<string, int>();
        dict.AddOrUpdate("existing", 42, incrementCount: true);
        var initialCount = dict.TotalAccessCount;

        var result = dict.ContainsKey(key);

        result.Should().Be(expected);
        dict.TotalAccessCount.Should().Be(initialCount); // Should not change
    }

    /// <summary>
    /// Tests that IncrementCount method increases access count for existing keys.
    /// The method should increment counts and return true for existing items.
    /// </summary>
    [Fact]
    public void IncrementCount_ExistingKey_IncrementsCountAndReturnsTrue()
    {
        var dict = new CounterDictionary<string, int>();
        dict.AddOrUpdate("key1", 42, incrementCount: true);

        var result = dict.IncrementCount("key1");

        result.Should().BeTrue();
        dict.GetAccessCount("key1").Should().Be(2);
        dict.TotalAccessCount.Should().Be(2);
    }

    /// <summary>
    /// Tests that IncrementCount method returns false for non-existent keys.
    /// The method should not affect counts when key is not found.
    /// </summary>
    [Fact]
    public void IncrementCount_NonExistentKey_ReturnsFalseWithoutAffectingCounts()
    {
        var dict = new CounterDictionary<string, int>();
        dict.AddOrUpdate("key1", 42, incrementCount: true);
        var initialCount = dict.TotalAccessCount;

        var result = dict.IncrementCount("nonexistent");

        result.Should().BeFalse();
        dict.TotalAccessCount.Should().Be(initialCount);
    }

    /// <summary>
    /// Tests that GetMostFrequent method returns items in descending frequency order.
    /// The method should identify and return the most frequently accessed items.
    /// </summary>
    [Fact]
    public void GetMostFrequent_MultipleItemsWithDifferentCounts_ReturnsInDescendingOrder()
    {
        var dict = new CounterDictionary<string, int>();
        dict.AddOrUpdate("low", 1, incrementCount: true);      // Count: 1
        dict.AddOrUpdate("high", 2, incrementCount: true);     // Count: 1
        dict.AddOrUpdate("medium", 3, incrementCount: true);   // Count: 1

        // Increase access counts to create differences
        dict.IncrementCount("high");    // Count: 2
        dict.IncrementCount("high");    // Count: 3
        dict.IncrementCount("medium");  // Count: 2

        var mostFrequent = dict.GetMostFrequent(3).ToList();

        mostFrequent.Should().HaveCount(3);
        mostFrequent[0].Key.Should().Be("high");     // Count: 3
        mostFrequent[0].Value.count.Should().Be(3);
        mostFrequent[1].Key.Should().Be("medium");   // Count: 2
        mostFrequent[1].Value.count.Should().Be(2);
        mostFrequent[2].Key.Should().Be("low");      // Count: 1
        mostFrequent[2].Value.count.Should().Be(1);
    }

    /// <summary>
    /// Tests that GetMostFrequent method limits results to requested count.
    /// The method should return only the specified number of top items.
    /// </summary>
    [Fact]
    public void GetMostFrequent_WithLimit_ReturnsRequestedNumberOfItems()
    {
        var dict = new CounterDictionary<string, int>();
        dict.AddOrUpdate("item1", 1, incrementCount: true);
        dict.AddOrUpdate("item2", 2, incrementCount: true);
        dict.AddOrUpdate("item3", 3, incrementCount: true);

        var topTwo = dict.GetMostFrequent(2).ToList();

        topTwo.Should().HaveCount(2);
    }

    /// <summary>
    /// Tests that GetMostFrequent method returns empty sequence for invalid count.
    /// The method should handle edge cases gracefully.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GetMostFrequent_WithInvalidCount_ReturnsEmptySequence(int count)
    {
        var dict = new CounterDictionary<string, int>();
        dict.AddOrUpdate("key1", 1, incrementCount: true);

        var result = dict.GetMostFrequent(count).ToList();

        result.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that GetLeastFrequent method returns items in ascending frequency order.
    /// The method should identify and return the least frequently accessed items.
    /// </summary>
    [Fact]
    public void GetLeastFrequent_MultipleItemsWithDifferentCounts_ReturnsInAscendingOrder()
    {
        var dict = new CounterDictionary<string, int>();
        dict.AddOrUpdate("low", 1, incrementCount: true);      // Count: 1
        dict.AddOrUpdate("high", 2, incrementCount: true);     // Count: 1
        dict.AddOrUpdate("medium", 3, incrementCount: true);   // Count: 1

        // Increase access counts
        dict.IncrementCount("high");    // Count: 2
        dict.IncrementCount("high");    // Count: 3
        dict.IncrementCount("medium");  // Count: 2

        var leastFrequent = dict.GetLeastFrequent(3).ToList();

        leastFrequent.Should().HaveCount(3);
        leastFrequent[0].Key.Should().Be("low");     // Count: 1
        leastFrequent[0].Value.count.Should().Be(1);
        leastFrequent[1].Key.Should().Be("medium");  // Count: 2
        leastFrequent[1].Value.count.Should().Be(2);
        leastFrequent[2].Key.Should().Be("high");    // Count: 3
        leastFrequent[2].Value.count.Should().Be(3);
    }

    /// <summary>
    /// Tests that GetItemsWithCount method returns all items with specified access count.
    /// The method should filter items based on exact count match.
    /// </summary>
    [Fact]
    public void GetItemsWithCount_SpecificCount_ReturnsItemsWithExactCount()
    {
        var dict = new CounterDictionary<string, int>();
        dict.AddOrUpdate("item1", 1, incrementCount: true);  // Count: 1
        dict.AddOrUpdate("item2", 2, incrementCount: true);  // Count: 1
        dict.AddOrUpdate("item3", 3, incrementCount: true);  // Count: 1

        dict.IncrementCount("item2"); // Count: 2
        dict.IncrementCount("item3"); // Count: 2

        var itemsWithCount2 = dict.GetItemsWithCount(2).ToList();

        itemsWithCount2.Should().HaveCount(2);
        itemsWithCount2.Should().Contain(kvp => kvp.Key == "item2" && kvp.Value == 2);
        itemsWithCount2.Should().Contain(kvp => kvp.Key == "item3" && kvp.Value == 3);
    }

    /// <summary>
    /// Tests that GetItemsWithCount method returns empty sequence for non-existent counts.
    /// The method should handle cases where no items have the specified count.
    /// </summary>
    [Fact]
    public void GetItemsWithCount_NonExistentCount_ReturnsEmptySequence()
    {
        var dict = new CounterDictionary<string, int>();
        dict.AddOrUpdate("item1", 1, incrementCount: true);

        var itemsWithCount5 = dict.GetItemsWithCount(5).ToList();

        itemsWithCount5.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that Remove method successfully removes existing items and adjusts counts.
    /// The method should return true and decrease both item count and total access count.
    /// </summary>
    [Fact]
    public void Remove_ExistingKey_ReturnsTrueAndAdjustsCounts()
    {
        var dict = new CounterDictionary<string, int>();
        dict.AddOrUpdate("key1", 10, incrementCount: true);
        dict.AddOrUpdate("key2", 20, incrementCount: true);
        dict.IncrementCount("key1"); // key1 now has count 2

        var result = dict.Remove("key1");

        result.Should().BeTrue();
        dict.Count.Should().Be(1);
        dict.ContainsKey("key1").Should().BeFalse();
        dict.ContainsKey("key2").Should().BeTrue();
        dict.TotalAccessCount.Should().Be(1); // Should decrease by 2 (key1's count)
    }

    /// <summary>
    /// Tests that Remove method returns false when trying to remove non-existent keys.
    /// The method should not modify the dictionary when key is not found.
    /// </summary>
    [Fact]
    public void Remove_NonExistentKey_ReturnsFalseWithoutChangingCollection()
    {
        var dict = new CounterDictionary<string, int>();
        dict.AddOrUpdate("key1", 10, incrementCount: true);
        var initialTotalCount = dict.TotalAccessCount;

        var result = dict.Remove("nonexistent");

        result.Should().BeFalse();
        dict.Count.Should().Be(1);
        dict.ContainsKey("key1").Should().BeTrue();
        dict.TotalAccessCount.Should().Be(initialTotalCount);
    }

    /// <summary>
    /// Tests that RemoveLeastFrequent method removes and returns the least accessed item.
    /// The method should identify and remove the item with minimum access count.
    /// </summary>
    [Fact]
    public void RemoveLeastFrequent_MultipleItems_RemovesAndReturnsLeastAccessed()
    {
        var dict = new CounterDictionary<string, int>();
        dict.AddOrUpdate("frequent", 1, incrementCount: true);
        dict.AddOrUpdate("infrequent", 2, incrementCount: true);

        dict.IncrementCount("frequent"); // frequent now has count 2, infrequent has count 1

        var removed = dict.RemoveLeastFrequent();

        removed.Key.Should().Be("infrequent");
        removed.Value.Should().Be(2);
        dict.Count.Should().Be(1);
        dict.ContainsKey("infrequent").Should().BeFalse();
        dict.ContainsKey("frequent").Should().BeTrue();
    }

    /// <summary>
    /// Tests that RemoveLeastFrequent method throws exception when dictionary is empty.
    /// The method should throw InvalidOperationException for empty collections.
    /// </summary>
    [Fact]
    public void RemoveLeastFrequent_EmptyDictionary_ThrowsInvalidOperationException()
    {
        var dict = new CounterDictionary<string, int>();

        var act = () => dict.RemoveLeastFrequent();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Dictionary is empty");
    }

    /// <summary>
    /// Tests that Clear method removes all items and resets all counters.
    /// The method should restore dictionary to initial empty state.
    /// </summary>
    [Fact]
    public void Clear_WithMultipleItems_RemovesAllItemsAndResetsCounts()
    {
        var dict = new CounterDictionary<string, int>();
        dict.AddOrUpdate("key1", 10, incrementCount: true);
        dict.AddOrUpdate("key2", 20, incrementCount: true);
        dict.IncrementCount("key1");

        dict.Clear();

        dict.Count.Should().Be(0);
        dict.TotalAccessCount.Should().Be(0);
        dict.ContainsKey("key1").Should().BeFalse();
        dict.ContainsKey("key2").Should().BeFalse();
    }

    /// <summary>
    /// Tests that GetStatistics method returns correct statistical information.
    /// The method should calculate accurate statistics based on current access counts.
    /// </summary>
    [Fact]
    public void GetStatistics_WithMultipleItems_ReturnsCorrectStatistics()
    {
        var dict = new CounterDictionary<string, int>();
        dict.AddOrUpdate("item1", 1, incrementCount: true);  // Count: 1
        dict.AddOrUpdate("item2", 2, incrementCount: true);  // Count: 1
        dict.AddOrUpdate("item3", 3, incrementCount: true);  // Count: 1

        dict.IncrementCount("item2");  // Count: 2
        dict.IncrementCount("item3");  // Count: 2
        dict.IncrementCount("item3");  // Count: 3

        var stats = dict.GetStatistics();

        stats.averageAccessCount.Should().BeApproximately(2.0, 0.001); // (1 + 2 + 3) / 3 = 2
        stats.minCount.Should().Be(1);
        stats.maxCount.Should().Be(3);
        stats.standardDeviation.Should().BeGreaterThan(0); // Should have some variance
    }

    /// <summary>
    /// Tests that GetStatistics method returns zero values for empty dictionary.
    /// The method should handle empty collections gracefully.
    /// </summary>
    [Fact]
    public void GetStatistics_EmptyDictionary_ReturnsZeroValues()
    {
        var dict = new CounterDictionary<string, int>();

        var stats = dict.GetStatistics();

        stats.averageAccessCount.Should().Be(0);
        stats.minCount.Should().Be(0);
        stats.maxCount.Should().Be(0);
        stats.standardDeviation.Should().Be(0);
    }

    /// <summary>
    /// Tests that enumeration traverses all items with their values and access counts.
    /// The foreach operation should provide complete frequency information for all items.
    /// </summary>
    [Fact]
    public void Enumeration_MultipleItems_TraversesAllItemsWithCounts()
    {
        var dict = new CounterDictionary<string, int>();
        dict.AddOrUpdate("key1", 10, incrementCount: true);
        dict.AddOrUpdate("key2", 20, incrementCount: true);
        dict.IncrementCount("key1");

        var items = dict.ToList();

        items.Should().HaveCount(2);
        items.Should().Contain(kvp => kvp.Key == "key1" && kvp.Value.value == 10 && kvp.Value.count == 2);
        items.Should().Contain(kvp => kvp.Key == "key2" && kvp.Value.value == 20 && kvp.Value.count == 1);
    }

    /// <summary>
    /// Tests that enumeration works correctly on empty dictionary.
    /// The foreach operation should handle empty collections gracefully.
    /// </summary>
    [Fact]
    public void Enumeration_EmptyDictionary_ReturnsNoItems()
    {
        var dict = new CounterDictionary<string, int>();

        var items = dict.ToList();

        items.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that enumeration throws exception when collection is modified during iteration.
    /// The enumerator should detect version changes and throw InvalidOperationException.
    /// </summary>
    [Fact]
    public void Enumeration_ModifiedDuringIteration_ThrowsInvalidOperationException()
    {
        var dict = new CounterDictionary<string, int>();
        dict.AddOrUpdate("key1", 1, incrementCount: true);
        dict.AddOrUpdate("key2", 2, incrementCount: true);

        var act = () =>
        {
            foreach (var kvp in dict)
            {
                dict.AddOrUpdate("key3", 3, incrementCount: true); // Modify during enumeration
            }
        };

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Collection was modified during enumeration");
    }

    /// <summary>
    /// Tests that Dispose method properly cleans up dictionary resources.
    /// The method should clear all items and reset internal state.
    /// </summary>
    [Fact]
    public void Dispose_WithItems_ClearsAllItemsAndResetsState()
    {
        var dict = new CounterDictionary<string, int>();
        dict.AddOrUpdate("key1", 1, incrementCount: true);
        dict.AddOrUpdate("key2", 2, incrementCount: true);

        dict.Dispose();

        dict.Count.Should().Be(0);
        dict.TotalAccessCount.Should().Be(0);
        dict.ContainsKey("key1").Should().BeFalse();
        dict.ContainsKey("key2").Should().BeFalse();
    }

    /// <summary>
    /// Tests that dictionary handles complex frequency tracking scenarios correctly.
    /// The dictionary should maintain accurate counts across mixed operations.
    /// </summary>
    [Fact]
    public void ComplexFrequencyTracking_MixedOperations_MaintainsAccurateCounts()
    {
        var dict = new CounterDictionary<string, int>();

        // Add items with different initial counts
        dict.AddOrUpdate("A", 1, incrementCount: true);   // Count: 1
        dict.AddOrUpdate("B", 2, incrementCount: false);  // Count: 0
        dict.AddOrUpdate("C", 3, incrementCount: true);   // Count: 1

        // Access items multiple times
        dict.TryGetValue("A", out _);  // Count: 2
        dict.TryGetValue("A", out _);  // Count: 3
        dict.TryGetValue("B", out _);  // Count: 1
        dict.IncrementCount("C");      // Count: 2

        // Verify final counts
        dict.GetAccessCount("A").Should().Be(3);
        dict.GetAccessCount("B").Should().Be(1);
        dict.GetAccessCount("C").Should().Be(2);
        dict.TotalAccessCount.Should().Be(6);
    }

    /// <summary>
    /// Tests that dictionary works correctly with null values.
    /// The dictionary should handle null values properly while maintaining frequency tracking.
    /// </summary>
    [Fact]
    public void NullValues_Operations_WorkCorrectlyWithFrequencyTracking()
    {
        var dict = new CounterDictionary<string, string?>();

        dict.AddOrUpdate("key1", null, incrementCount: true);
        dict.AddOrUpdate("key2", "value", incrementCount: true);

        dict["key1"].Should().BeNull();
        dict["key2"].Should().Be("value");
        dict.GetAccessCount("key1").Should().Be(2); // 1 for add + 1 for indexer access
        dict.GetAccessCount("key2").Should().Be(2); // 1 for add + 1 for indexer access
    }

    /// <summary>
    /// Tests dictionary behavior with single item operations.
    /// The dictionary should handle single-item scenarios correctly.
    /// </summary>
    [Fact]
    public void SingleItem_Operations_WorkCorrectlyWithFrequencyTracking()
    {
        var dict = new CounterDictionary<string, int>();
        dict.AddOrUpdate("only", 42, incrementCount: true);

        var mostFrequent = dict.GetMostFrequent(1).ToList();
        var leastFrequent = dict.GetLeastFrequent(1).ToList();

        mostFrequent.Should().HaveCount(1);
        mostFrequent[0].Key.Should().Be("only");
        leastFrequent.Should().HaveCount(1);
        leastFrequent[0].Key.Should().Be("only");

        dict.RemoveLeastFrequent();
        dict.Count.Should().Be(0);
    }
}
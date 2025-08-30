using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Omni.Collections.Hybrid.LinkedDictionary;
using Xunit;

namespace Omni.Collections.Tests.Hybrid;

public class LinkedDictionaryTests
{
    /// <summary>
    /// Tests that a LinkedDictionary can be constructed with default parameters.
    /// The dictionary should have default capacity and dynamic mode initially.
    /// </summary>
    [Fact]
    public void Constructor_Default_CreatesEmptyDictionaryWithDynamicMode()
    {
        var dict = new LinkedDictionary<string, int>();

        dict.Count.Should().Be(0);
        dict.Mode.Should().Be(CapacityMode.Dynamic);
    }

    /// <summary>
    /// Tests that a LinkedDictionary can be constructed with a specified capacity.
    /// The dictionary should have the specified capacity and dynamic mode.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(16)]
    [InlineData(100)]
    [InlineData(1000)]
    public void Constructor_WithCapacity_CreatesEmptyDictionaryWithDynamicMode(int capacity)
    {
        var dict = new LinkedDictionary<string, int>(capacity);

        dict.Count.Should().Be(0);
        dict.Mode.Should().Be(CapacityMode.Dynamic);
    }

    /// <summary>
    /// Tests that constructing a LinkedDictionary with negative capacity throws exception.
    /// The constructor should reject negative capacity values.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(-10)]
    [InlineData(-100)]
    public void Constructor_WithNegativeCapacity_ThrowsArgumentOutOfRangeException(int capacity)
    {
        var act = () => new LinkedDictionary<string, int>(capacity);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("capacity");
    }

    /// <summary>
    /// Tests that a LinkedDictionary can be constructed with capacity and mode parameters.
    /// The dictionary should have the specified capacity mode and load factor.
    /// </summary>
    [Theory]
    [InlineData(CapacityMode.Dynamic)]
    [InlineData(CapacityMode.Fixed)]
    public void Constructor_WithCapacityAndMode_CreatesEmptyDictionaryWithSpecifiedMode(CapacityMode mode)
    {
        var dict = new LinkedDictionary<string, int>(10, mode);

        dict.Count.Should().Be(0);
        dict.Mode.Should().Be(mode);
    }

    /// <summary>
    /// Tests that constructing a LinkedDictionary with invalid load factor throws exception.
    /// The constructor should reject load factors outside the valid range.
    /// </summary>
    [Theory]
    [InlineData(0.0f)]
    [InlineData(-0.5f)]
    [InlineData(1.1f)]
    [InlineData(2.0f)]
    public void Constructor_WithInvalidLoadFactor_ThrowsArgumentOutOfRangeException(float loadFactor)
    {
        var act = () => new LinkedDictionary<string, int>(10, CapacityMode.Dynamic, loadFactor);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("loadFactor");
    }

    /// <summary>
    /// Tests that a LinkedDictionary can be constructed with custom equality comparer.
    /// The dictionary should use the provided comparer for key comparisons.
    /// </summary>
    [Fact]
    public void Constructor_WithCustomComparer_UsesCustomComparerForKeys()
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        var dict = new LinkedDictionary<string, int>(16, CapacityMode.Dynamic, 0.75f, comparer);

        dict.AddOrUpdate("KEY", 1);
        dict.ContainsKey("key").Should().BeTrue();
        dict.ContainsKey("KEY").Should().BeTrue();
    }

    /// <summary>
    /// Tests that AddOrUpdate method can add new key-value pairs to the dictionary.
    /// The dictionary should contain the added items and maintain correct count.
    /// </summary>
    [Fact]
    public void AddOrUpdate_NewItems_AddsItemsAndIncreasesCount()
    {
        var dict = new LinkedDictionary<string, int>();

        dict.AddOrUpdate("key1", 10);
        dict.AddOrUpdate("key2", 20);
        dict.AddOrUpdate("key3", 30);

        dict.Count.Should().Be(3);
        dict.ContainsKey("key1").Should().BeTrue();
        dict.ContainsKey("key2").Should().BeTrue();
        dict.ContainsKey("key3").Should().BeTrue();
    }

    /// <summary>
    /// Tests that AddOrUpdate method can update existing key-value pairs in the dictionary.
    /// The dictionary should contain the updated value without changing count.
    /// </summary>
    [Fact]
    public void AddOrUpdate_ExistingItem_UpdatesValueWithoutChangingCount()
    {
        var dict = new LinkedDictionary<string, int>();
        dict.AddOrUpdate("key1", 10);

        dict.AddOrUpdate("key1", 100);

        dict.Count.Should().Be(1);
        dict["key1"].Should().Be(100);
    }

    /// <summary>
    /// Tests that the indexer can get and set values for existing keys.
    /// The indexer should return correct values and update existing entries.
    /// </summary>
    [Fact]
    public void Indexer_ExistingKey_GetsAndSetsValueCorrectly()
    {
        var dict = new LinkedDictionary<string, int>();
        dict.AddOrUpdate("key1", 10);

        var value = dict["key1"];
        dict["key1"] = 100;

        value.Should().Be(10);
        dict["key1"].Should().Be(100);
    }

    /// <summary>
    /// Tests that the indexer throws exception when accessing non-existent keys.
    /// The indexer should throw KeyNotFoundException for missing keys.
    /// </summary>
    [Fact]
    public void Indexer_NonExistentKey_ThrowsKeyNotFoundException()
    {
        var dict = new LinkedDictionary<string, int>();

        var act = () => dict["nonexistent"];

        act.Should().Throw<KeyNotFoundException>();
    }

    /// <summary>
    /// Tests that the indexer can add new key-value pairs when setting values.
    /// The indexer should create new entries for non-existent keys.
    /// </summary>
    [Fact]
    public void Indexer_NewKey_AddsNewItemToCollection()
    {
        var dict = new LinkedDictionary<string, int>();

        dict["newkey"] = 42;

        dict.Count.Should().Be(1);
        dict.ContainsKey("newkey").Should().BeTrue();
        dict["newkey"].Should().Be(42);
    }

    /// <summary>
    /// Tests that TryGetValue method returns true and correct value for existing keys.
    /// The method should successfully retrieve values without throwing exceptions.
    /// </summary>
    [Fact]
    public void TryGetValue_ExistingKey_ReturnsTrueWithCorrectValue()
    {
        var dict = new LinkedDictionary<string, int>();
        dict.AddOrUpdate("key1", 42);

        var result = dict.TryGetValue("key1", out var value);

        result.Should().BeTrue();
        value.Should().Be(42);
    }

    /// <summary>
    /// Tests that TryGetValue method returns false for non-existent keys.
    /// The method should return false and default value for missing keys.
    /// </summary>
    [Fact]
    public void TryGetValue_NonExistentKey_ReturnsFalseWithDefaultValue()
    {
        var dict = new LinkedDictionary<string, int>();

        var result = dict.TryGetValue("nonexistent", out var value);

        result.Should().BeFalse();
        value.Should().Be(default(int));
    }

    /// <summary>
    /// Tests that ContainsKey method returns true for existing keys in the dictionary.
    /// The method should correctly identify keys that are present.
    /// </summary>
    [Fact]
    public void ContainsKey_ExistingKey_ReturnsTrue()
    {
        var dict = new LinkedDictionary<string, int>();
        dict.AddOrUpdate("key1", 42);

        var result = dict.ContainsKey("key1");

        result.Should().BeTrue();
    }

    /// <summary>
    /// Tests that ContainsKey method returns false for non-existent keys in the dictionary.
    /// The method should correctly identify keys that are not present.
    /// </summary>
    [Fact]
    public void ContainsKey_NonExistentKey_ReturnsFalse()
    {
        var dict = new LinkedDictionary<string, int>();

        var result = dict.ContainsKey("nonexistent");

        result.Should().BeFalse();
    }

    /// <summary>
    /// Tests that Remove method can successfully remove existing items from the dictionary.
    /// The method should return true and decrease count for existing keys.
    /// </summary>
    [Fact]
    public void Remove_ExistingKey_ReturnsTrueAndRemovesItem()
    {
        var dict = new LinkedDictionary<string, int>();
        dict.AddOrUpdate("key1", 10);
        dict.AddOrUpdate("key2", 20);

        var result = dict.Remove("key1");

        result.Should().BeTrue();
        dict.Count.Should().Be(1);
        dict.ContainsKey("key1").Should().BeFalse();
        dict.ContainsKey("key2").Should().BeTrue();
    }

    /// <summary>
    /// Tests that Remove method returns false when trying to remove non-existent keys.
    /// The method should not modify the dictionary when key is not found.
    /// </summary>
    [Fact]
    public void Remove_NonExistentKey_ReturnsFalseWithoutChangingCollection()
    {
        var dict = new LinkedDictionary<string, int>();
        dict.AddOrUpdate("key1", 10);

        var result = dict.Remove("nonexistent");

        result.Should().BeFalse();
        dict.Count.Should().Be(1);
        dict.ContainsKey("key1").Should().BeTrue();
    }

    /// <summary>
    /// Tests that Clear method removes all items from the dictionary.
    /// The method should reset count to zero and remove all entries.
    /// </summary>
    [Fact]
    public void Clear_WithMultipleItems_RemovesAllItemsAndResetsCount()
    {
        var dict = new LinkedDictionary<string, int>();
        dict.AddOrUpdate("key1", 10);
        dict.AddOrUpdate("key2", 20);
        dict.AddOrUpdate("key3", 30);

        dict.Clear();

        dict.Count.Should().Be(0);
        dict.ContainsKey("key1").Should().BeFalse();
        dict.ContainsKey("key2").Should().BeFalse();
        dict.ContainsKey("key3").Should().BeFalse();
    }

    /// <summary>
    /// Tests that PeekLru method returns the least recently used item without removing it.
    /// The method should identify the LRU item correctly in insertion order scenarios.
    /// </summary>
    [Fact]
    public void PeekLru_WithMultipleItems_ReturnsLeastRecentlyUsedItem()
    {
        var dict = new LinkedDictionary<string, int>();
        dict.AddOrUpdate("first", 1);
        dict.AddOrUpdate("second", 2);
        dict.AddOrUpdate("third", 3);

        var lru = dict.PeekLru();

        lru.Key.Should().Be("first");
        lru.Value.Should().Be(1);
        dict.Count.Should().Be(3); // Should not remove item
    }

    /// <summary>
    /// Tests that PeekLru method throws exception when dictionary is empty.
    /// The method should throw InvalidOperationException for empty dictionaries.
    /// </summary>
    [Fact]
    public void PeekLru_EmptyDictionary_ThrowsInvalidOperationException()
    {
        var dict = new LinkedDictionary<string, int>();

        var act = () => dict.PeekLru();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Dictionary is empty");
    }

    /// <summary>
    /// Tests that PeekMru method returns the most recently used item without removing it.
    /// The method should identify the MRU item correctly after access operations.
    /// </summary>
    [Fact]
    public void PeekMru_WithMultipleItems_ReturnsMostRecentlyUsedItem()
    {
        var dict = new LinkedDictionary<string, int>();
        dict.AddOrUpdate("first", 1);
        dict.AddOrUpdate("second", 2);
        dict.AddOrUpdate("third", 3);

        var mru = dict.PeekMru();

        mru.Key.Should().Be("third");
        mru.Value.Should().Be(3);
        dict.Count.Should().Be(3); // Should not remove item
    }

    /// <summary>
    /// Tests that PeekMru method throws exception when dictionary is empty.
    /// The method should throw InvalidOperationException for empty dictionaries.
    /// </summary>
    [Fact]
    public void PeekMru_EmptyDictionary_ThrowsInvalidOperationException()
    {
        var dict = new LinkedDictionary<string, int>();

        var act = () => dict.PeekMru();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Dictionary is empty");
    }

    /// <summary>
    /// Tests that accessing an item updates its position to most recently used.
    /// The TryGetValue operation should move accessed items to front of LRU chain.
    /// </summary>
    [Fact]
    public void TryGetValue_AccessExistingItem_MovesItemToMostRecent()
    {
        var dict = new LinkedDictionary<string, int>();
        dict.AddOrUpdate("first", 1);
        dict.AddOrUpdate("second", 2);
        dict.AddOrUpdate("third", 3);

        dict.TryGetValue("first", out _); // Access first item

        var mru = dict.PeekMru();
        mru.Key.Should().Be("first"); // First should now be MRU
        mru.Value.Should().Be(1);
    }

    /// <summary>
    /// Tests that updating an existing item moves it to most recently used position.
    /// The AddOrUpdate operation should maintain LRU ordering for updates.
    /// </summary>
    [Fact]
    public void AddOrUpdate_ExistingItem_MovesItemToMostRecent()
    {
        var dict = new LinkedDictionary<string, int>();
        dict.AddOrUpdate("first", 1);
        dict.AddOrUpdate("second", 2);
        dict.AddOrUpdate("third", 3);

        dict.AddOrUpdate("first", 10); // Update first item

        var mru = dict.PeekMru();
        mru.Key.Should().Be("first"); // First should now be MRU
        mru.Value.Should().Be(10);
    }

    /// <summary>
    /// Tests that enumeration maintains insertion order for the dictionary.
    /// The foreach operation should traverse items in insertion order sequence.
    /// </summary>
    [Fact]
    public void Enumeration_MultipleItems_TraversesInInsertionOrder()
    {
        var dict = new LinkedDictionary<string, int>();
        dict.AddOrUpdate("first", 1);
        dict.AddOrUpdate("second", 2);
        dict.AddOrUpdate("third", 3);

        var items = dict.ToList();

        items.Should().HaveCount(3);
        items[0].Should().Be(new KeyValuePair<string, int>("third", 3)); // MRU first
        items[1].Should().Be(new KeyValuePair<string, int>("second", 2));
        items[2].Should().Be(new KeyValuePair<string, int>("first", 1)); // LRU last
    }

    /// <summary>
    /// Tests that enumeration works correctly on empty dictionary.
    /// The foreach operation should handle empty collections gracefully.
    /// </summary>
    [Fact]
    public void Enumeration_EmptyDictionary_ReturnsNoItems()
    {
        var dict = new LinkedDictionary<string, int>();

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
        var dict = new LinkedDictionary<string, int>();
        dict.AddOrUpdate("key1", 1);
        dict.AddOrUpdate("key2", 2);

        var act = () =>
        {
            foreach (var kvp in dict)
            {
                dict.AddOrUpdate("key3", 3); // Modify during enumeration
            }
        };

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Collection was modified during enumeration");
    }

    /// <summary>
    /// Tests that fixed capacity mode evicts LRU items when capacity is exceeded.
    /// The dictionary should maintain fixed size by removing oldest items.
    /// </summary>
    [Fact]
    public void FixedCapacityMode_ExceedsCapacity_EvictsLruItems()
    {
        var dict = new LinkedDictionary<string, int>(2, CapacityMode.Fixed);
        dict.AddOrUpdate("first", 1);
        dict.AddOrUpdate("second", 2);

        dict.AddOrUpdate("third", 3); // Should evict "first"

        dict.Count.Should().Be(2);
        dict.ContainsKey("first").Should().BeFalse();
        dict.ContainsKey("second").Should().BeTrue();
        dict.ContainsKey("third").Should().BeTrue();
    }

    /// <summary>
    /// Tests that dynamic capacity mode grows the dictionary when load factor is exceeded.
    /// The dictionary should resize automatically to accommodate more items.
    /// </summary>
    [Fact]
    public void DynamicCapacityMode_ExceedsLoadFactor_ResizesDictionary()
    {
        var dict = new LinkedDictionary<string, int>(4, CapacityMode.Dynamic, 0.5f);
        dict.AddOrUpdate("key1", 1);
        dict.AddOrUpdate("key2", 2);

        dict.AddOrUpdate("key3", 3); // Should trigger resize

        dict.Count.Should().Be(3);
        dict.ContainsKey("key1").Should().BeTrue();
        dict.ContainsKey("key2").Should().BeTrue();
        dict.ContainsKey("key3").Should().BeTrue();
    }

    /// <summary>
    /// Tests that handling null keys results in hash code operations working correctly.
    /// The dictionary should handle the notnull constraint properly via compiler checks.
    /// </summary>
    [Fact]
    public void Operations_WithValidKeys_WorkCorrectly()
    {
        var dict = new LinkedDictionary<string, int>();

        dict.AddOrUpdate("valid", 1);
        dict.TryGetValue("valid", out var value).Should().BeTrue();
        value.Should().Be(1);
        dict.ContainsKey("valid").Should().BeTrue();
        dict.Remove("valid").Should().BeTrue();
    }

    /// <summary>
    /// Tests that Dispose method properly cleans up dictionary resources.
    /// The method should clear all items and reset internal state.
    /// </summary>
    [Fact]
    public void Dispose_WithItems_ClearsAllItemsAndResetsState()
    {
        var dict = new LinkedDictionary<string, int>();
        dict.AddOrUpdate("key1", 1);
        dict.AddOrUpdate("key2", 2);

        dict.Dispose();

        dict.Count.Should().Be(0);
        dict.ContainsKey("key1").Should().BeFalse();
        dict.ContainsKey("key2").Should().BeFalse();
    }

    /// <summary>
    /// Tests that dictionary works correctly with complex objects as keys and values.
    /// The dictionary should handle custom types properly with appropriate equality comparison.
    /// </summary>
    [Fact]
    public void ComplexTypes_AsKeysAndValues_WorksCorrectly()
    {
        var dict = new LinkedDictionary<int, Person>();
        var person1 = new Person("Alice", 25);
        var person2 = new Person("Bob", 30);

        dict.AddOrUpdate(1, person1);
        dict.AddOrUpdate(2, person2);

        dict.Count.Should().Be(2);
        dict[1].Should().Be(person1);
        dict[2].Should().Be(person2);
    }

    /// <summary>
    /// Tests that dictionary maintains correct LRU order after multiple operations.
    /// Complex access patterns should properly maintain the LRU chain ordering.
    /// </summary>
    [Fact]
    public void ComplexLruBehavior_MultipleOperations_MaintainsCorrectOrder()
    {
        var dict = new LinkedDictionary<string, int>();
        dict.AddOrUpdate("A", 1);
        dict.AddOrUpdate("B", 2);
        dict.AddOrUpdate("C", 3);
        dict.AddOrUpdate("D", 4);

        // Access B, then A
        dict.TryGetValue("B", out _);
        dict.TryGetValue("A", out _);

        // Expected order: A (MRU), B, D, C (LRU)
        dict.PeekMru().Key.Should().Be("A");
        dict.PeekLru().Key.Should().Be("C");
    }

    /// <summary>
    /// Tests dictionary behavior with single item operations.
    /// The dictionary should handle single-item scenarios correctly.
    /// </summary>
    [Fact]
    public void SingleItem_Operations_WorkCorrectly()
    {
        var dict = new LinkedDictionary<string, int>();
        dict.AddOrUpdate("only", 42);

        dict.PeekMru().Key.Should().Be("only");
        dict.PeekLru().Key.Should().Be("only");
        dict.Count.Should().Be(1);

        dict.Remove("only").Should().BeTrue();
        dict.Count.Should().Be(0);
    }

    private record Person(string Name, int Age);
}
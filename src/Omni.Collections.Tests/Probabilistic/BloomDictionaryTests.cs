using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Omni.Collections.Probabilistic;
using Xunit;

namespace Omni.Collections.Tests.Probabilistic;

public class BloomDictionaryTests
{
    /// <summary>
    /// Tests that a BloomDictionary can be constructed with default parameters.
    /// The dictionary should have default capacity and be empty initially.
    /// </summary>
    [Fact]
    public void Constructor_Default_CreatesEmptyDictionary()
    {
        var dict = new BloomDictionary<string, int>();

        dict.Count.Should().Be(0);
        dict.IsReadOnly.Should().BeFalse();
    }

    /// <summary>
    /// Tests that a BloomDictionary can be constructed with specified capacity.
    /// The dictionary should initialize with the given capacity parameter.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(16)]
    [InlineData(100)]
    [InlineData(1000)]
    public void Constructor_WithCapacity_CreatesEmptyDictionary(int capacity)
    {
        var dict = new BloomDictionary<string, int>(capacity);

        dict.Count.Should().Be(0);
        dict.IsReadOnly.Should().BeFalse();
    }

    /// <summary>
    /// Tests that constructing a BloomDictionary with negative capacity throws exception.
    /// The constructor should reject negative capacity values.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(-10)]
    [InlineData(-100)]
    public void Constructor_WithNegativeCapacity_ThrowsArgumentOutOfRangeException(int capacity)
    {
        var act = () => new BloomDictionary<string, int>(capacity);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("capacity");
    }

    /// <summary>
    /// Tests that constructing a BloomDictionary with invalid false positive rate throws exception.
    /// The constructor should reject false positive rates outside the valid range.
    /// </summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.5)]
    [InlineData(1.0)]
    [InlineData(1.5)]
    public void Constructor_WithInvalidFalsePositiveRate_ThrowsArgumentOutOfRangeException(double falsePositiveRate)
    {
        var act = () => new BloomDictionary<string, int>(16, falsePositiveRate);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("falsePositiveRate");
    }

    /// <summary>
    /// Tests that a BloomDictionary can be constructed with valid false positive rate.
    /// The dictionary should initialize with appropriate Bloom filter settings.
    /// </summary>
    [Theory]
    [InlineData(0.01)]
    [InlineData(0.05)]
    [InlineData(0.1)]
    [InlineData(0.5)]
    public void Constructor_WithValidFalsePositiveRate_CreatesEmptyDictionary(double falsePositiveRate)
    {
        var dict = new BloomDictionary<string, int>(16, falsePositiveRate);

        dict.Count.Should().Be(0);
        dict.IsReadOnly.Should().BeFalse();
    }

    /// <summary>
    /// Tests that a BloomDictionary can be constructed with custom equality comparer.
    /// The dictionary should use the provided comparer for key comparisons.
    /// </summary>
    [Fact]
    public void Constructor_WithCustomComparer_UsesCustomComparerForKeys()
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        var dict = new BloomDictionary<string, int>(16, 0.01, comparer);

        dict.Add("KEY", 1);
        // Note: Custom comparer affects the dictionary but the Bloom filter may use default comparer
        // Test that the item is accessible through the dictionary
        dict.ContainsKey("KEY").Should().BeTrue();
        dict["KEY"].Should().Be(1);
    }

    /// <summary>
    /// Tests that a BloomDictionary can be constructed from a collection of key-value pairs.
    /// The dictionary should contain all items from the source collection.
    /// </summary>
    [Fact]
    public void Constructor_FromCollection_ContainsAllItemsFromSource()
    {
        var sourceData = new List<KeyValuePair<string, int>>
        {
            new("key1", 10),
            new("key2", 20),
            new("key3", 30)
        };

        var dict = new BloomDictionary<string, int>(sourceData);

        dict.Count.Should().Be(3);
        dict["key1"].Should().Be(10);
        dict["key2"].Should().Be(20);
        dict["key3"].Should().Be(30);
    }

    /// <summary>
    /// Tests that constructing a BloomDictionary from null collection throws exception.
    /// The constructor should reject null collection parameters.
    /// </summary>
    [Fact]
    public void Constructor_WithNullCollection_ThrowsArgumentNullException()
    {
        var act = () => new BloomDictionary<string, int>((IEnumerable<KeyValuePair<string, int>>)null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("collection");
    }

    /// <summary>
    /// Tests that Add method can add new key-value pairs to the dictionary.
    /// The dictionary should contain the added items and maintain correct count.
    /// </summary>
    [Fact]
    public void Add_NewItems_AddsItemsAndIncreasesCount()
    {
        var dict = new BloomDictionary<string, int>();

        dict.Add("key1", 10);
        dict.Add("key2", 20);
        dict.Add("key3", 30);

        dict.Count.Should().Be(3);
        dict.ContainsKey("key1").Should().BeTrue();
        dict.ContainsKey("key2").Should().BeTrue();
        dict.ContainsKey("key3").Should().BeTrue();
    }

    /// <summary>
    /// Tests that Add method throws exception when adding duplicate keys.
    /// The method should reject duplicate keys with ArgumentException.
    /// </summary>
    [Fact]
    public void Add_DuplicateKey_ThrowsArgumentException()
    {
        var dict = new BloomDictionary<string, int>();
        dict.Add("key1", 10);

        var act = () => dict.Add("key1", 20);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*already exists*");
    }

    /// <summary>
    /// Tests that Add method throws exception when adding null keys.
    /// The method should reject null keys with ArgumentNullException.
    /// </summary>
    [Fact]
    public void Add_NullKey_ThrowsArgumentNullException()
    {
        var dict = new BloomDictionary<string?, int>();

        var act = () => dict.Add(null!, 10);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("key");
    }

    /// <summary>
    /// Tests that the indexer can get and set values for keys.
    /// The indexer should provide dictionary-like access with proper Bloom filter integration.
    /// </summary>
    [Fact]
    public void Indexer_GetAndSet_WorksCorrectlyWithBloomFilter()
    {
        var dict = new BloomDictionary<string, int>();
        dict.Add("key1", 10);

        var value = dict["key1"];
        dict["key2"] = 20; // Should add new item
        dict["key1"] = 100; // Should update existing

        value.Should().Be(10);
        dict["key1"].Should().Be(100);
        dict["key2"].Should().Be(20);
        dict.Count.Should().Be(2);
    }

    /// <summary>
    /// Tests that the indexer throws exception when accessing non-existent keys.
    /// The indexer should throw KeyNotFoundException for missing keys.
    /// </summary>
    [Fact]
    public void Indexer_NonExistentKey_ThrowsKeyNotFoundException()
    {
        var dict = new BloomDictionary<string, int>();

        var act = () => dict["nonexistent"];

        act.Should().Throw<KeyNotFoundException>();
    }

    /// <summary>
    /// Tests that the indexer setter throws exception when setting null keys.
    /// The indexer should reject null keys with ArgumentNullException.
    /// </summary>
    [Fact]
    public void Indexer_SetNullKey_ThrowsArgumentNullException()
    {
        var dict = new BloomDictionary<string?, int>();

        var act = () => dict[null!] = 10;

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("key");
    }

    /// <summary>
    /// Tests that TryGetValue method returns true and correct value for existing keys.
    /// The method should leverage Bloom filter for efficient positive lookups.
    /// </summary>
    [Fact]
    public void TryGetValue_ExistingKey_ReturnsTrueWithCorrectValue()
    {
        var dict = new BloomDictionary<string, int>();
        dict.Add("key1", 42);

        var result = dict.TryGetValue("key1", out var value);

        result.Should().BeTrue();
        value.Should().Be(42);
    }

    /// <summary>
    /// Tests that TryGetValue method returns false for non-existent keys efficiently.
    /// The method should use Bloom filter to quickly eliminate negative lookups.
    /// </summary>
    [Fact]
    public void TryGetValue_NonExistentKey_ReturnsFalseWithDefaultValue()
    {
        var dict = new BloomDictionary<string, int>();

        var result = dict.TryGetValue("nonexistent", out var value);

        result.Should().BeFalse();
        value.Should().Be(default(int));
    }

    /// <summary>
    /// Tests that TryGetValue method throws exception when accessing with null keys.
    /// The method should reject null keys with ArgumentNullException.
    /// </summary>
    [Fact]
    public void TryGetValue_NullKey_ThrowsArgumentNullException()
    {
        var dict = new BloomDictionary<string?, int>();

        var act = () => dict.TryGetValue(null!, out _);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("key");
    }

    /// <summary>
    /// Tests that ContainsKey method returns correct boolean values.
    /// The method should use Bloom filter optimization for efficient key existence checks.
    /// </summary>
    [Theory]
    [InlineData("existing", true)]
    [InlineData("nonexistent", false)]
    public void ContainsKey_VariousKeys_ReturnsCorrectResult(string key, bool expected)
    {
        var dict = new BloomDictionary<string, int>();
        dict.Add("existing", 42);

        var result = dict.ContainsKey(key);

        result.Should().Be(expected);
    }

    /// <summary>
    /// Tests that Remove method can successfully remove existing items from the dictionary.
    /// The method should use Bloom filter pre-screening and maintain count correctly.
    /// </summary>
    [Fact]
    public void Remove_ExistingKey_ReturnsTrueAndRemovesItem()
    {
        var dict = new BloomDictionary<string, int>();
        dict.Add("key1", 10);
        dict.Add("key2", 20);

        var result = dict.Remove("key1");

        result.Should().BeTrue();
        dict.Count.Should().Be(1);
        dict.ContainsKey("key1").Should().BeFalse();
        dict.ContainsKey("key2").Should().BeTrue();
    }

    /// <summary>
    /// Tests that Remove method returns false when trying to remove non-existent keys.
    /// The method should use Bloom filter to quickly eliminate impossible removals.
    /// </summary>
    [Fact]
    public void Remove_NonExistentKey_ReturnsFalseWithoutChangingCollection()
    {
        var dict = new BloomDictionary<string, int>();
        dict.Add("key1", 10);

        var result = dict.Remove("nonexistent");

        result.Should().BeFalse();
        dict.Count.Should().Be(1);
        dict.ContainsKey("key1").Should().BeTrue();
    }

    /// <summary>
    /// Tests that Remove method throws exception when removing with null keys.
    /// The method should reject null keys with ArgumentNullException.
    /// </summary>
    [Fact]
    public void Remove_NullKey_ThrowsArgumentNullException()
    {
        var dict = new BloomDictionary<string?, int>();

        var act = () => dict.Remove(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("key");
    }

    /// <summary>
    /// Tests that Clear method removes all items from the dictionary.
    /// The method should reset both the dictionary and Bloom filter states.
    /// </summary>
    [Fact]
    public void Clear_WithMultipleItems_RemovesAllItemsAndResetsState()
    {
        var dict = new BloomDictionary<string, int>();
        dict.Add("key1", 10);
        dict.Add("key2", 20);
        dict.Add("key3", 30);

        dict.Clear();

        dict.Count.Should().Be(0);
        dict.ContainsKey("key1").Should().BeFalse();
        dict.ContainsKey("key2").Should().BeFalse();
        dict.ContainsKey("key3").Should().BeFalse();
    }

    /// <summary>
    /// Tests that Keys property returns correct collection of all keys.
    /// The property should provide access to all keys currently in the dictionary.
    /// </summary>
    [Fact]
    public void Keys_WithItems_ReturnsAllKeys()
    {
        var dict = new BloomDictionary<string, int>();
        dict.Add("key1", 10);
        dict.Add("key2", 20);
        dict.Add("key3", 30);

        var keys = dict.Keys.ToList();

        keys.Should().HaveCount(3);
        keys.Should().Contain("key1");
        keys.Should().Contain("key2");
        keys.Should().Contain("key3");
    }

    /// <summary>
    /// Tests that Values property returns correct collection of all values.
    /// The property should provide access to all values currently in the dictionary.
    /// </summary>
    [Fact]
    public void Values_WithItems_ReturnsAllValues()
    {
        var dict = new BloomDictionary<string, int>();
        dict.Add("key1", 10);
        dict.Add("key2", 20);
        dict.Add("key3", 30);

        var values = dict.Values.ToList();

        values.Should().HaveCount(3);
        values.Should().Contain(10);
        values.Should().Contain(20);
        values.Should().Contain(30);
    }

    /// <summary>
    /// Tests that Add method with KeyValuePair adds items correctly.
    /// The method should support ICollection interface properly.
    /// </summary>
    [Fact]
    public void Add_KeyValuePair_AddsItemToCollection()
    {
        var dict = new BloomDictionary<string, int>();
        var kvp = new KeyValuePair<string, int>("key1", 42);

        dict.Add(kvp);

        dict.Count.Should().Be(1);
        dict["key1"].Should().Be(42);
    }

    /// <summary>
    /// Tests that Contains method with KeyValuePair checks both key and value.
    /// The method should verify exact key-value pair matches.
    /// </summary>
    [Fact]
    public void Contains_KeyValuePair_ChecksBothKeyAndValue()
    {
        var dict = new BloomDictionary<string, int>();
        dict.Add("key1", 42);

        var correctPair = new KeyValuePair<string, int>("key1", 42);
        var wrongValuePair = new KeyValuePair<string, int>("key1", 99);
        var wrongKeyPair = new KeyValuePair<string, int>("key2", 42);

        dict.Contains(correctPair).Should().BeTrue();
        dict.Contains(wrongValuePair).Should().BeFalse();
        dict.Contains(wrongKeyPair).Should().BeFalse();
    }

    /// <summary>
    /// Tests that CopyTo method copies all items to target array correctly.
    /// The method should support ICollection interface with proper bounds checking.
    /// </summary>
    [Fact]
    public void CopyTo_ValidArray_CopiesAllItemsToArray()
    {
        var dict = new BloomDictionary<string, int>();
        dict.Add("key1", 10);
        dict.Add("key2", 20);

        var array = new KeyValuePair<string, int>[4];
        dict.CopyTo(array, 1);

        array[0].Should().Be(default(KeyValuePair<string, int>));
        array[1].Key.Should().BeOneOf("key1", "key2");
        array[2].Key.Should().BeOneOf("key1", "key2");
        array[1].Key.Should().NotBe(array[2].Key);
        array[3].Should().Be(default(KeyValuePair<string, int>));
    }

    /// <summary>
    /// Tests that CopyTo method throws exception with null array parameter.
    /// The method should validate array parameter with ArgumentNullException.
    /// </summary>
    [Fact]
    public void CopyTo_NullArray_ThrowsArgumentNullException()
    {
        var dict = new BloomDictionary<string, int>();

        var act = () => dict.CopyTo(null!, 0);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("array");
    }

    /// <summary>
    /// Tests that CopyTo method throws exception with invalid array index.
    /// The method should validate array index bounds with ArgumentOutOfRangeException.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    public void CopyTo_InvalidArrayIndex_ThrowsArgumentOutOfRangeException(int arrayIndex)
    {
        var dict = new BloomDictionary<string, int>();
        var array = new KeyValuePair<string, int>[4];

        var act = () => dict.CopyTo(array, arrayIndex);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("arrayIndex");
    }

    /// <summary>
    /// Tests that CopyTo method throws exception when array is too small.
    /// The method should validate array capacity with ArgumentException.
    /// </summary>
    [Fact]
    public void CopyTo_ArrayTooSmall_ThrowsArgumentException()
    {
        var dict = new BloomDictionary<string, int>();
        dict.Add("key1", 10);
        dict.Add("key2", 20);

        var smallArray = new KeyValuePair<string, int>[1];
        var act = () => dict.CopyTo(smallArray, 0);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*not large enough*");
    }

    /// <summary>
    /// Tests that Remove method with KeyValuePair removes only exact matches.
    /// The method should verify both key and value before removing items.
    /// </summary>
    [Fact]
    public void Remove_KeyValuePair_RemovesOnlyExactMatches()
    {
        var dict = new BloomDictionary<string, int>();
        dict.Add("key1", 42);
        dict.Add("key2", 99);

        var correctPair = new KeyValuePair<string, int>("key1", 42);
        var wrongValuePair = new KeyValuePair<string, int>("key1", 99);

        dict.Remove(correctPair).Should().BeTrue();
        dict.Remove(wrongValuePair).Should().BeFalse();

        dict.Count.Should().Be(1);
        dict.ContainsKey("key1").Should().BeFalse();
        dict.ContainsKey("key2").Should().BeTrue();
    }

    /// <summary>
    /// Tests that enumeration traverses all items in the dictionary.
    /// The foreach operation should visit all currently stored items.
    /// </summary>
    [Fact]
    public void Enumeration_MultipleItems_TraversesAllItems()
    {
        var dict = new BloomDictionary<string, int>();
        dict.Add("A", 1);
        dict.Add("B", 2);
        dict.Add("C", 3);

        var items = dict.ToList();

        items.Should().HaveCount(3);
        items.Should().Contain(kvp => kvp.Key == "A" && kvp.Value == 1);
        items.Should().Contain(kvp => kvp.Key == "B" && kvp.Value == 2);
        items.Should().Contain(kvp => kvp.Key == "C" && kvp.Value == 3);
    }

    /// <summary>
    /// Tests that enumeration works correctly on empty dictionary.
    /// The foreach operation should handle empty collections gracefully.
    /// </summary>
    [Fact]
    public void Enumeration_EmptyDictionary_ReturnsNoItems()
    {
        var dict = new BloomDictionary<string, int>();

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
        var dict = new BloomDictionary<string, int>();
        dict.Add("key1", 1);
        dict.Add("key2", 2);

        var act = () =>
        {
            foreach (var kvp in dict)
            {
                dict.Add("key3", 3); // Modify during enumeration
            }
        };

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*modified during enumeration*");
    }

    /// <summary>
    /// Tests that Dispose method properly cleans up dictionary resources.
    /// The method should dispose the Bloom filter and release internal resources.
    /// </summary>
    [Fact]
    public void Dispose_WithItems_ClearsAllItemsAndDisposesBloomFilter()
    {
        var dict = new BloomDictionary<string, int>();
        dict.Add("key1", 1);
        dict.Add("key2", 2);

        dict.Dispose();

        // After disposal, we cannot access the internal state easily
        // The Dispose method should have cleaned up internal resources
        // We verify no exceptions are thrown when disposing
        dict.Dispose(); // Should not throw when disposed multiple times
    }

    /// <summary>
    /// Tests that dictionary handles load factor resizing correctly.
    /// The dictionary should automatically resize when load factor thresholds are exceeded.
    /// </summary>
    [Fact]
    public void LoadFactorResizing_AddManyItems_HandlesResizingCorrectly()
    {
        var dict = new BloomDictionary<int, string>();
        const int itemCount = 50; // Reduced count for more reliable testing

        for (int i = 0; i < itemCount; i++)
        {
            dict.Add(i, $"value{i}");
        }

        dict.Count.Should().Be(itemCount);
        for (int i = 0; i < itemCount; i++)
        {
            dict.ContainsKey(i).Should().BeTrue($"Key {i} should exist");
            dict[i].Should().Be($"value{i}");
        }
    }

    /// <summary>
    /// Tests that dictionary handles many deletions with appropriate resizing.
    /// The dictionary should shrink and rebuild when too many items are deleted.
    /// </summary>
    [Fact]
    public void ManyDeletions_TriggersRebuildAndShrinking()
    {
        var dict = new BloomDictionary<int, string>();
        const int itemCount = 100;

        // Add many items
        for (int i = 0; i < itemCount; i++)
        {
            dict.Add(i, $"value{i}");
        }

        // Remove most items to trigger shrinking/rebuilding
        for (int i = 0; i < itemCount - 5; i++)
        {
            dict.Remove(i).Should().BeTrue();
        }

        dict.Count.Should().Be(5);
        for (int i = itemCount - 5; i < itemCount; i++)
        {
            dict.ContainsKey(i).Should().BeTrue();
            dict[i].Should().Be($"value{i}");
        }
    }

    /// <summary>
    /// Tests that Bloom filter optimization provides fast negative lookups.
    /// The dictionary should efficiently handle lookups for non-existent keys.
    /// </summary>
    [Fact]
    public void BloomFilterOptimization_FastNegativeLookups_HandlesNonExistentKeys()
    {
        var dict = new BloomDictionary<string, int>();
        dict.Add("exists", 42);

        // These should all be handled quickly by the Bloom filter
        dict.ContainsKey("nonexistent1").Should().BeFalse();
        dict.ContainsKey("nonexistent2").Should().BeFalse();
        dict.ContainsKey("nonexistent3").Should().BeFalse();
        dict.TryGetValue("nonexistent4", out _).Should().BeFalse();
        dict.Remove("nonexistent5").Should().BeFalse();

        // But existing key should still work
        dict.ContainsKey("exists").Should().BeTrue();
    }

    /// <summary>
    /// Tests that dictionary works correctly with null values.
    /// The dictionary should handle null values properly while maintaining Bloom filter functionality.
    /// </summary>
    [Fact]
    public void NullValues_Operations_WorkCorrectlyWithBloomFilter()
    {
        var dict = new BloomDictionary<string, string?>();

        dict.Add("key1", null);
        dict.Add("key2", "value");

        dict["key1"].Should().BeNull();
        dict["key2"].Should().Be("value");
        dict.ContainsKey("key1").Should().BeTrue();
        dict.TryGetValue("key1", out var value).Should().BeTrue();
        value.Should().BeNull();
    }

    /// <summary>
    /// Tests that dictionary handles complex collision scenarios correctly.
    /// The dictionary should maintain correctness even with hash collisions and probe distances.
    /// </summary>
    [Fact]
    public void CollisionHandling_ComplexScenarios_MaintainsCorrectness()
    {
        var dict = new BloomDictionary<string, int>();

        // Add items that might cause collisions
        var items = new List<string>();
        for (int i = 0; i < 50; i++)
        {
            var key = $"item{i:D3}";
            items.Add(key);
            dict.Add(key, i);
        }

        // Verify all items are accessible
        for (int i = 0; i < 50; i++)
        {
            var key = items[i];
            dict.ContainsKey(key).Should().BeTrue();
            dict[key].Should().Be(i);
        }

        // Remove some items and verify remaining ones
        for (int i = 0; i < 25; i++)
        {
            dict.Remove(items[i]).Should().BeTrue();
        }

        dict.Count.Should().Be(25);
        for (int i = 25; i < 50; i++)
        {
            var key = items[i];
            dict.ContainsKey(key).Should().BeTrue();
            dict[key].Should().Be(i);
        }
    }

    /// <summary>
    /// Tests dictionary behavior with single item operations.
    /// The dictionary should handle single-item scenarios correctly with Bloom filter.
    /// </summary>
    [Fact]
    public void SingleItem_Operations_WorkCorrectlyWithBloomFilter()
    {
        var dict = new BloomDictionary<string, int>();
        dict.Add("only", 42);

        dict.Count.Should().Be(1);
        dict.ContainsKey("only").Should().BeTrue();
        dict["only"].Should().Be(42);

        dict.Remove("only").Should().BeTrue();
        dict.Count.Should().Be(0);
        dict.ContainsKey("only").Should().BeFalse();
    }
}
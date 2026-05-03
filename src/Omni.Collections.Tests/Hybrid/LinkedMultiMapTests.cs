using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Omni.Collections.Hybrid;
using Omni.Collections.Hybrid.LinkedDictionary;
using Xunit;

namespace Omni.Collections.Tests.Hybrid;

public class LinkedMultiMapTests
{
    [Fact]
    public void Constructor_Default_CreatesEmptyMap()
    {
        using var map = new LinkedMultiMap<string, int>();

        map.KeyCount.Should().Be(0);
        map.TotalValueCount.Should().Be(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(16)]
    [InlineData(1000)]
    public void Constructor_WithValidCapacity_CreatesEmptyMap(int capacity)
    {
        using var map = new LinkedMultiMap<string, int>(capacity);

        map.KeyCount.Should().Be(0);
        map.TotalValueCount.Should().Be(0);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_WithNegativeCapacity_ThrowsArgumentOutOfRangeException(int capacity)
    {
        Action act = () => new LinkedMultiMap<string, int>(capacity);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("capacity");
    }

    [Theory]
    [InlineData(CapacityMode.Dynamic)]
    [InlineData(CapacityMode.Fixed)]
    public void Constructor_WithMode_CreatesEmptyMap(CapacityMode mode)
    {
        using var map = new LinkedMultiMap<string, int>(8, mode);

        map.KeyCount.Should().Be(0);
    }

    [Fact]
    public void Add_SingleKeyValue_IncrementsCounts()
    {
        using var map = new LinkedMultiMap<string, int>();

        map.Add("a", 1);

        map.KeyCount.Should().Be(1);
        map.TotalValueCount.Should().Be(1);
    }

    [Fact]
    public void Add_SameKeyMultipleValues_OneKeyManyValues()
    {
        using var map = new LinkedMultiMap<string, int>();

        map.Add("a", 1);
        map.Add("a", 2);
        map.Add("a", 3);

        map.KeyCount.Should().Be(1);
        map.TotalValueCount.Should().Be(3);
        map.GetValueCount("a").Should().Be(3);
    }

    [Fact]
    public void Add_PreservesInsertionOrderOfValues()
    {
        using var map = new LinkedMultiMap<string, int>();

        map.Add("k", 10);
        map.Add("k", 20);
        map.Add("k", 30);

        var values = map["k"].ToArray();

        values.Should().Equal(new[] { 10, 20, 30 });
    }

    [Fact]
    public void Add_MultipleKeys_IndependentValueLists()
    {
        using var map = new LinkedMultiMap<string, int>();

        map.Add("a", 1);
        map.Add("b", 2);
        map.Add("a", 3);
        map.Add("b", 4);

        map.KeyCount.Should().Be(2);
        map.TotalValueCount.Should().Be(4);
        map["a"].Should().Equal(new[] { 1, 3 });
        map["b"].Should().Equal(new[] { 2, 4 });
    }

    [Fact]
    public void Add_DuplicateValueAllowed_WhenAllowDuplicateValuesTrue()
    {
        using var map = new LinkedMultiMap<string, int>(16, CapacityMode.Dynamic, allowDuplicateValues: true);

        map.Add("k", 1);
        map.Add("k", 1);

        map.GetValueCount("k").Should().Be(2);
    }

    [Fact]
    public void Add_DuplicateValueRejected_WhenAllowDuplicateValuesFalse()
    {
        using var map = new LinkedMultiMap<string, int>(16, CapacityMode.Dynamic, allowDuplicateValues: false);

        map.Add("k", 1);
        map.Add("k", 1);
        map.Add("k", 2);

        map.GetValueCount("k").Should().Be(2);
        map["k"].Should().Equal(new[] { 1, 2 });
    }

    [Fact]
    public void Indexer_MissingKey_ReturnsEmpty()
    {
        using var map = new LinkedMultiMap<string, int>();

        var values = map["missing"];

        values.Should().NotBeNull();
        values.Count.Should().Be(0);
    }

    [Fact]
    public void TryGetValues_ExistingKey_ReturnsTrueAndValues()
    {
        using var map = new LinkedMultiMap<string, int>();
        map.Add("k", 1);
        map.Add("k", 2);

        var found = map.TryGetValues("k", out var values);

        found.Should().BeTrue();
        values.Should().Equal(new[] { 1, 2 });
    }

    [Fact]
    public void TryGetValues_MissingKey_ReturnsFalseAndEmpty()
    {
        using var map = new LinkedMultiMap<string, int>();

        var found = map.TryGetValues("missing", out var values);

        found.Should().BeFalse();
        values.Should().NotBeNull();
        values.Count.Should().Be(0);
    }

    [Fact]
    public void RemoveKey_ExistingKey_ReturnsTrueAndDropsAllValues()
    {
        using var map = new LinkedMultiMap<string, int>();
        map.Add("a", 1);
        map.Add("a", 2);
        map.Add("a", 3);
        map.Add("b", 99);

        var removed = map.RemoveKey("a");

        removed.Should().BeTrue();
        map.KeyCount.Should().Be(1);
        map.TotalValueCount.Should().Be(1);
        map.ContainsKey("a").Should().BeFalse();
        map.ContainsKey("b").Should().BeTrue();
    }

    [Fact]
    public void RemoveKey_MissingKey_ReturnsFalse()
    {
        using var map = new LinkedMultiMap<string, int>();
        map.Add("a", 1);

        var removed = map.RemoveKey("missing");

        removed.Should().BeFalse();
        map.KeyCount.Should().Be(1);
    }

    [Fact]
    public void Remove_SingleValue_KeepsOtherValuesUnderKey()
    {
        using var map = new LinkedMultiMap<string, int>();
        map.Add("k", 1);
        map.Add("k", 2);
        map.Add("k", 3);

        var removed = map.Remove("k", 2);

        removed.Should().BeTrue();
        map["k"].Should().Equal(new[] { 1, 3 });
        map.GetValueCount("k").Should().Be(2);
        map.TotalValueCount.Should().Be(2);
    }

    [Fact]
    public void Remove_LastValue_DropsKeyEntirely()
    {
        using var map = new LinkedMultiMap<string, int>();
        map.Add("k", 1);

        var removed = map.Remove("k", 1);

        removed.Should().BeTrue();
        map.ContainsKey("k").Should().BeFalse();
        map.KeyCount.Should().Be(0);
        map.TotalValueCount.Should().Be(0);
    }

    [Fact]
    public void Remove_MissingValue_ReturnsFalse()
    {
        using var map = new LinkedMultiMap<string, int>();
        map.Add("k", 1);

        var removed = map.Remove("k", 99);

        removed.Should().BeFalse();
        map.GetValueCount("k").Should().Be(1);
    }

    [Fact]
    public void Remove_MissingKey_ReturnsFalse()
    {
        using var map = new LinkedMultiMap<string, int>();

        var removed = map.Remove("ghost", 1);

        removed.Should().BeFalse();
    }

    [Fact]
    public void Remove_HeadValue_PreservesRemainingOrder()
    {
        using var map = new LinkedMultiMap<string, int>();
        map.Add("k", 1);
        map.Add("k", 2);
        map.Add("k", 3);

        map.Remove("k", 1);

        map["k"].Should().Equal(new[] { 2, 3 });
    }

    [Fact]
    public void Remove_TailValue_PreservesRemainingOrder()
    {
        using var map = new LinkedMultiMap<string, int>();
        map.Add("k", 1);
        map.Add("k", 2);
        map.Add("k", 3);

        map.Remove("k", 3);

        map["k"].Should().Equal(new[] { 1, 2 });
        // Subsequent add should append at end.
        map.Add("k", 4);
        map["k"].Should().Equal(new[] { 1, 2, 4 });
    }

    [Fact]
    public void ContainsKey_ReturnsCorrectResult()
    {
        using var map = new LinkedMultiMap<string, int>();
        map.Add("a", 1);

        map.ContainsKey("a").Should().BeTrue();
        map.ContainsKey("b").Should().BeFalse();
    }

    [Fact]
    public void Contains_KeyValuePair_ReturnsCorrectResult()
    {
        using var map = new LinkedMultiMap<string, int>();
        map.Add("a", 1);
        map.Add("a", 2);

        map.Contains("a", 1).Should().BeTrue();
        map.Contains("a", 2).Should().BeTrue();
        map.Contains("a", 3).Should().BeFalse();
        map.Contains("b", 1).Should().BeFalse();
    }

    [Fact]
    public void Clear_RemovesAllKeysAndValues()
    {
        using var map = new LinkedMultiMap<string, int>();
        map.Add("a", 1);
        map.Add("b", 2);
        map.Add("a", 3);

        map.Clear();

        map.KeyCount.Should().Be(0);
        map.TotalValueCount.Should().Be(0);
        map.ContainsKey("a").Should().BeFalse();
        map.ContainsKey("b").Should().BeFalse();
    }

    [Fact]
    public void GetValueCount_MissingKey_ReturnsZero()
    {
        using var map = new LinkedMultiMap<string, int>();

        map.GetValueCount("ghost").Should().Be(0);
    }

    [Fact]
    public void Keys_EnumeratesAllKeys()
    {
        using var map = new LinkedMultiMap<string, int>();
        map.Add("a", 1);
        map.Add("b", 2);
        map.Add("c", 3);

        var keys = map.Keys.ToHashSet();

        keys.Should().BeEquivalentTo(new[] { "a", "b", "c" });
    }

    [Fact]
    public void Enumerator_YieldsKeyValuePairs()
    {
        using var map = new LinkedMultiMap<string, int>();
        map.Add("a", 1);
        map.Add("a", 2);
        map.Add("b", 10);

        var snapshot = new Dictionary<string, int[]>();
        foreach (var kvp in map)
        {
            snapshot[kvp.Key] = kvp.Value.ToArray();
        }

        snapshot.Should().ContainKey("a");
        snapshot.Should().ContainKey("b");
        snapshot["a"].Should().Equal(new[] { 1, 2 });
        snapshot["b"].Should().Equal(new[] { 10 });
    }

    [Fact]
    public void NonGenericEnumerator_YieldsItems()
    {
        using var map = new LinkedMultiMap<string, int>();
        map.Add("a", 1);
        map.Add("b", 2);

        IEnumerable enumerable = map;
        int count = 0;
        foreach (var _ in enumerable)
            count++;

        count.Should().Be(2);
    }

    [Fact]
    public void FixedCapacity_EvictsLruKeyWhenFull()
    {
        // Fixed capacity = 3; adding a 4th distinct key should evict the LRU tail.
        using var map = new LinkedMultiMap<string, int>(3, CapacityMode.Fixed, true);

        map.Add("a", 1);
        map.Add("b", 2);
        map.Add("c", 3);
        map.KeyCount.Should().Be(3);

        map.Add("d", 4); // Should evict the least-recently-used key.

        map.KeyCount.Should().Be(3);
        map.ContainsKey("d").Should().BeTrue();
        // One of a/b/c was evicted (the LRU tail at the moment of eviction).
        var present = new[] { "a", "b", "c" }.Count(map.ContainsKey);
        present.Should().Be(2);
    }

    [Fact]
    public void DynamicCapacity_GrowsBeyondInitialCapacity()
    {
        using var map = new LinkedMultiMap<int, int>(4, CapacityMode.Dynamic);

        for (int i = 0; i < 100; i++)
            map.Add(i, i * 10);

        map.KeyCount.Should().Be(100);
        for (int i = 0; i < 100; i++)
            map[i].Should().Equal(new[] { i * 10 });
    }

    [Fact]
    public void TryGetValues_PromotesKeyInLruOrder()
    {
        // Default LRU optimization disabled — every access should promote.
        using var map = new LinkedMultiMap<string, int>(3, CapacityMode.Fixed, true);
        map.Add("a", 1);
        map.Add("b", 2);
        map.Add("c", 3);

        // Touch "a" so it becomes most-recently-used.
        map.TryGetValues("a", out _);

        // Adding a new key should now evict the LRU tail (which should NOT be "a").
        map.Add("d", 4);

        map.ContainsKey("a").Should().BeTrue();
        map.ContainsKey("d").Should().BeTrue();
    }

    [Fact]
    public void Dispose_CanBeCalledSafely()
    {
        var map = new LinkedMultiMap<string, int>();
        map.Add("a", 1);

        Action act = () => map.Dispose();

        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_AfterDispose_StateIsCleared()
    {
        var map = new LinkedMultiMap<string, int>();
        map.Add("a", 1);
        map.Add("b", 2);

        map.Dispose();

        map.KeyCount.Should().Be(0);
        map.TotalValueCount.Should().Be(0);
    }

    [Fact]
    public void CustomKeyComparer_UsedForLookups()
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        using var map = new LinkedMultiMap<string, int>(8, CapacityMode.Dynamic, true, comparer, null);

        map.Add("Hello", 1);

        map.ContainsKey("HELLO").Should().BeTrue();
        map["hello"].Should().Equal(new[] { 1 });
    }

    [Fact]
    public void CustomValueComparer_UsedForRemoveAndContains()
    {
        var valueComparer = StringComparer.OrdinalIgnoreCase;
        using var map = new LinkedMultiMap<string, string>(8, CapacityMode.Dynamic, true, null, valueComparer);
        map.Add("k", "Hello");

        map.Contains("k", "HELLO").Should().BeTrue();
        map.Remove("k", "hello").Should().BeTrue();
        map.GetValueCount("k").Should().Be(0);
    }

    [Fact]
    public void ManyValuesUnderOneKey_AllAccessible()
    {
        using var map = new LinkedMultiMap<string, int>();

        for (int i = 0; i < 50; i++)
            map.Add("bucket", i);

        map.KeyCount.Should().Be(1);
        map.GetValueCount("bucket").Should().Be(50);
        map["bucket"].Should().Equal(Enumerable.Range(0, 50));
    }

    [Fact]
    public void IndexerSubscript_AccessByPosition()
    {
        using var map = new LinkedMultiMap<string, int>();
        map.Add("k", 100);
        map.Add("k", 200);
        map.Add("k", 300);

        var values = map["k"];

        values[0].Should().Be(100);
        values[1].Should().Be(200);
        values[2].Should().Be(300);
    }

    [Fact]
    public void IndexerSubscript_OutOfRange_Throws()
    {
        using var map = new LinkedMultiMap<string, int>();
        map.Add("k", 1);

        var values = map["k"];

        Action act = () => { var _ = values[5]; };
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}

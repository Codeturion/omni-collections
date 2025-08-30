using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Omni.Collections.Hybrid.QueueDictionary;
using Xunit;

namespace Omni.Collections.Tests.Hybrid;

public class QueueDictionaryTests
{
    /// <summary>
    /// Tests that a QueueDictionary can be constructed with default parameters.
    /// The dictionary should have default capacity and be empty initially.
    /// </summary>
    [Fact]
    public void Constructor_Default_CreatesEmptyDictionary()
    {
        var dict = new QueueDictionary<string, int>();

        dict.Count.Should().Be(0);
        dict.IsEmpty.Should().BeTrue();
    }

    /// <summary>
    /// Tests that a QueueDictionary can be constructed with specified capacity.
    /// The dictionary should initialize with the given capacity parameter.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(16)]
    [InlineData(100)]
    [InlineData(1000)]
    public void Constructor_WithCapacity_CreatesEmptyDictionary(int capacity)
    {
        var dict = new QueueDictionary<string, int>(capacity);

        dict.Count.Should().Be(0);
        dict.IsEmpty.Should().BeTrue();
    }

    /// <summary>
    /// Tests that constructing a QueueDictionary with negative capacity throws exception.
    /// The constructor should reject negative capacity values.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(-10)]
    [InlineData(-100)]
    public void Constructor_WithNegativeCapacity_ThrowsArgumentOutOfRangeException(int capacity)
    {
        var act = () => new QueueDictionary<string, int>(capacity);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("capacity");
    }

    /// <summary>
    /// Tests that constructing a QueueDictionary with invalid load factor throws exception.
    /// The constructor should reject load factors outside the valid range.
    /// </summary>
    [Theory]
    [InlineData(0.0f)]
    [InlineData(-0.5f)]
    [InlineData(1.0f)]
    [InlineData(1.5f)]
    public void Constructor_WithInvalidLoadFactor_ThrowsArgumentOutOfRangeException(float loadFactor)
    {
        var act = () => new QueueDictionary<string, int>(16, loadFactor);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("loadFactor");
    }

    /// <summary>
    /// Tests that a QueueDictionary can be constructed with custom equality comparer.
    /// The dictionary should use the provided comparer for key comparisons.
    /// </summary>
    [Fact]
    public void Constructor_WithCustomComparer_UsesCustomComparerForKeys()
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        var dict = new QueueDictionary<string, int>(16, 0.75f, comparer);

        dict.Enqueue("KEY", 1);
        dict.ContainsKey("key").Should().BeTrue();
        dict.ContainsKey("KEY").Should().BeTrue();
    }

    /// <summary>
    /// Tests that Enqueue method can add new key-value pairs to the dictionary.
    /// The dictionary should maintain FIFO order and allow key-based access.
    /// </summary>
    [Fact]
    public void Enqueue_NewItems_AddsItemsInFifoOrder()
    {
        var dict = new QueueDictionary<string, int>();

        dict.Enqueue("first", 1);
        dict.Enqueue("second", 2);
        dict.Enqueue("third", 3);

        dict.Count.Should().Be(3);
        dict.IsEmpty.Should().BeFalse();
        dict.ContainsKey("first").Should().BeTrue();
        dict.ContainsKey("second").Should().BeTrue();
        dict.ContainsKey("third").Should().BeTrue();

        dict.PeekFront().Key.Should().Be("first");
        dict.PeekBack().Key.Should().Be("third");
    }

    /// <summary>
    /// Tests that Enqueue method updates existing keys and moves them to back.
    /// The dictionary should update values and maintain queue ordering.
    /// </summary>
    [Fact]
    public void Enqueue_ExistingKey_UpdatesValueAndMovesToBack()
    {
        var dict = new QueueDictionary<string, int>();
        dict.Enqueue("A", 1);
        dict.Enqueue("B", 2);
        dict.Enqueue("C", 3);

        dict.Enqueue("A", 10); // Update A and move to back

        dict.Count.Should().Be(3);
        dict["A"].Should().Be(10);
        dict.PeekFront().Key.Should().Be("B");
        dict.PeekBack().Key.Should().Be("A");
    }

    /// <summary>
    /// Tests that Dequeue method removes and returns items in FIFO order.
    /// The method should remove the front item and maintain queue ordering.
    /// </summary>
    [Fact]
    public void Dequeue_WithItems_RemovesAndReturnsFrontItem()
    {
        var dict = new QueueDictionary<string, int>();
        dict.Enqueue("first", 1);
        dict.Enqueue("second", 2);
        dict.Enqueue("third", 3);

        var result = dict.Dequeue();

        result.Key.Should().Be("first");
        result.Value.Should().Be(1);
        dict.Count.Should().Be(2);
        dict.ContainsKey("first").Should().BeFalse();
        dict.PeekFront().Key.Should().Be("second");
    }

    /// <summary>
    /// Tests that Dequeue method throws exception when queue is empty.
    /// The method should throw InvalidOperationException for empty queues.
    /// </summary>
    [Fact]
    public void Dequeue_EmptyQueue_ThrowsInvalidOperationException()
    {
        var dict = new QueueDictionary<string, int>();

        var act = () => dict.Dequeue();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Queue is empty");
    }

    /// <summary>
    /// Tests that TryDequeue method successfully dequeues items when available.
    /// The method should return true and the dequeued item when queue is not empty.
    /// </summary>
    [Fact]
    public void TryDequeue_WithItems_ReturnsTrueAndDequeuedItem()
    {
        var dict = new QueueDictionary<string, int>();
        dict.Enqueue("key1", 42);

        var success = dict.TryDequeue(out var result);

        success.Should().BeTrue();
        result.Key.Should().Be("key1");
        result.Value.Should().Be(42);
        dict.Count.Should().Be(0);
        dict.IsEmpty.Should().BeTrue();
    }

    /// <summary>
    /// Tests that TryDequeue method returns false when queue is empty.
    /// The method should return false and default value for empty queues.
    /// </summary>
    [Fact]
    public void TryDequeue_EmptyQueue_ReturnsFalseWithDefaultValue()
    {
        var dict = new QueueDictionary<string, int>();

        var success = dict.TryDequeue(out var result);

        success.Should().BeFalse();
        result.Should().Be(default(KeyValuePair<string, int>));
    }

    /// <summary>
    /// Tests that PeekFront method returns the front item without removing it.
    /// The method should provide access to the front item without modifying the queue.
    /// </summary>
    [Fact]
    public void PeekFront_WithItems_ReturnsFrontItemWithoutRemoving()
    {
        var dict = new QueueDictionary<string, int>();
        dict.Enqueue("first", 1);
        dict.Enqueue("second", 2);

        var front = dict.PeekFront();

        front.Key.Should().Be("first");
        front.Value.Should().Be(1);
        dict.Count.Should().Be(2); // Should not remove item
    }

    /// <summary>
    /// Tests that PeekFront method throws exception when queue is empty.
    /// The method should throw InvalidOperationException for empty queues.
    /// </summary>
    [Fact]
    public void PeekFront_EmptyQueue_ThrowsInvalidOperationException()
    {
        var dict = new QueueDictionary<string, int>();

        var act = () => dict.PeekFront();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Queue is empty");
    }

    /// <summary>
    /// Tests that PeekBack method returns the back item without removing it.
    /// The method should provide access to the back item without modifying the queue.
    /// </summary>
    [Fact]
    public void PeekBack_WithItems_ReturnsBackItemWithoutRemoving()
    {
        var dict = new QueueDictionary<string, int>();
        dict.Enqueue("first", 1);
        dict.Enqueue("second", 2);

        var back = dict.PeekBack();

        back.Key.Should().Be("second");
        back.Value.Should().Be(2);
        dict.Count.Should().Be(2); // Should not remove item
    }

    /// <summary>
    /// Tests that PeekBack method throws exception when queue is empty.
    /// The method should throw InvalidOperationException for empty queues.
    /// </summary>
    [Fact]
    public void PeekBack_EmptyQueue_ThrowsInvalidOperationException()
    {
        var dict = new QueueDictionary<string, int>();

        var act = () => dict.PeekBack();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Queue is empty");
    }

    /// <summary>
    /// Tests that the indexer can get and set values for keys.
    /// The indexer should provide dictionary-like access while maintaining queue semantics.
    /// </summary>
    [Fact]
    public void Indexer_GetAndSet_WorksCorrectlyWithQueueSemantics()
    {
        var dict = new QueueDictionary<string, int>();
        dict.Enqueue("key1", 10);

        var value = dict["key1"];
        dict["key2"] = 20; // Should enqueue new item
        dict["key1"] = 100; // Should update and move to back

        value.Should().Be(10);
        dict["key1"].Should().Be(100);
        dict["key2"].Should().Be(20);
        dict.Count.Should().Be(2);
        dict.PeekBack().Key.Should().Be("key1"); // key1 should be at back after update
    }

    /// <summary>
    /// Tests that the indexer throws exception when accessing non-existent keys.
    /// The indexer should throw KeyNotFoundException for missing keys.
    /// </summary>
    [Fact]
    public void Indexer_NonExistentKey_ThrowsKeyNotFoundException()
    {
        var dict = new QueueDictionary<string, int>();

        var act = () => dict["nonexistent"];

        act.Should().Throw<KeyNotFoundException>();
    }

    /// <summary>
    /// Tests that TryGetValue method returns true and correct value for existing keys.
    /// The method should provide dictionary-like lookup without affecting queue order.
    /// </summary>
    [Fact]
    public void TryGetValue_ExistingKey_ReturnsTrueWithCorrectValue()
    {
        var dict = new QueueDictionary<string, int>();
        dict.Enqueue("key1", 42);

        var result = dict.TryGetValue("key1", out var value);

        result.Should().BeTrue();
        value.Should().Be(42);
        dict.PeekFront().Key.Should().Be("key1"); // Should not affect queue order
    }

    /// <summary>
    /// Tests that TryGetValue method returns false for non-existent keys.
    /// The method should return false and default value for missing keys.
    /// </summary>
    [Fact]
    public void TryGetValue_NonExistentKey_ReturnsFalseWithDefaultValue()
    {
        var dict = new QueueDictionary<string, int>();

        var result = dict.TryGetValue("nonexistent", out var value);

        result.Should().BeFalse();
        value.Should().Be(default(int));
    }

    /// <summary>
    /// Tests that ContainsKey method returns correct boolean values.
    /// The method should check key existence without affecting queue order.
    /// </summary>
    [Theory]
    [InlineData("existing", true)]
    [InlineData("nonexistent", false)]
    public void ContainsKey_VariousKeys_ReturnsCorrectResult(string key, bool expected)
    {
        var dict = new QueueDictionary<string, int>();
        dict.Enqueue("existing", 42);

        var result = dict.ContainsKey(key);

        result.Should().Be(expected);
    }

    /// <summary>
    /// Tests that Remove method can successfully remove existing items from the dictionary.
    /// The method should return true and maintain queue ordering for remaining items.
    /// </summary>
    [Fact]
    public void Remove_ExistingKey_ReturnsTrueAndMaintainsOrder()
    {
        var dict = new QueueDictionary<string, int>();
        dict.Enqueue("A", 1);
        dict.Enqueue("B", 2);
        dict.Enqueue("C", 3);

        var result = dict.Remove("B"); // Remove middle item

        result.Should().BeTrue();
        dict.Count.Should().Be(2);
        dict.ContainsKey("B").Should().BeFalse();
        dict.PeekFront().Key.Should().Be("A");
        dict.PeekBack().Key.Should().Be("C");
    }

    /// <summary>
    /// Tests that Remove method returns false when trying to remove non-existent keys.
    /// The method should not modify the dictionary when key is not found.
    /// </summary>
    [Fact]
    public void Remove_NonExistentKey_ReturnsFalseWithoutChangingCollection()
    {
        var dict = new QueueDictionary<string, int>();
        dict.Enqueue("key1", 10);

        var result = dict.Remove("nonexistent");

        result.Should().BeFalse();
        dict.Count.Should().Be(1);
        dict.ContainsKey("key1").Should().BeTrue();
    }

    /// <summary>
    /// Tests that Clear method removes all items from the dictionary.
    /// The method should reset count and empty the queue completely.
    /// </summary>
    [Fact]
    public void Clear_WithMultipleItems_RemovesAllItemsAndResetsState()
    {
        var dict = new QueueDictionary<string, int>();
        dict.Enqueue("key1", 10);
        dict.Enqueue("key2", 20);
        dict.Enqueue("key3", 30);

        dict.Clear();

        dict.Count.Should().Be(0);
        dict.IsEmpty.Should().BeTrue();
        dict.ContainsKey("key1").Should().BeFalse();
        dict.ContainsKey("key2").Should().BeFalse();
        dict.ContainsKey("key3").Should().BeFalse();

        var act = () => dict.PeekFront();
        act.Should().Throw<InvalidOperationException>();
    }

    /// <summary>
    /// Tests that ToArray method returns items in queue order.
    /// The method should create an array with items in FIFO sequence.
    /// </summary>
    [Fact]
    public void ToArray_WithItems_ReturnsItemsInQueueOrder()
    {
        var dict = new QueueDictionary<string, int>();
        dict.Enqueue("first", 1);
        dict.Enqueue("second", 2);
        dict.Enqueue("third", 3);

        var array = dict.ToArray();

        array.Should().HaveCount(3);
        array[0].Should().Be(new KeyValuePair<string, int>("first", 1));
        array[1].Should().Be(new KeyValuePair<string, int>("second", 2));
        array[2].Should().Be(new KeyValuePair<string, int>("third", 3));
    }

    /// <summary>
    /// Tests that ToArray method returns empty array for empty dictionary.
    /// The method should handle empty collections gracefully.
    /// </summary>
    [Fact]
    public void ToArray_EmptyDictionary_ReturnsEmptyArray()
    {
        var dict = new QueueDictionary<string, int>();

        var array = dict.ToArray();

        array.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that default enumeration traverses items in queue order.
    /// The foreach operation should visit items in FIFO sequence.
    /// </summary>
    [Fact]
    public void Enumeration_DefaultMode_TraversesInQueueOrder()
    {
        var dict = new QueueDictionary<string, int>();
        dict.Enqueue("first", 1);
        dict.Enqueue("second", 2);
        dict.Enqueue("third", 3);

        var items = dict.ToList();

        items.Should().HaveCount(3);
        items[0].Should().Be(new KeyValuePair<string, int>("first", 1));
        items[1].Should().Be(new KeyValuePair<string, int>("second", 2));
        items[2].Should().Be(new KeyValuePair<string, int>("third", 3));
    }

    /// <summary>
    /// Tests that enumeration with Fast mode traverses items efficiently.
    /// The Fast enumeration mode should provide optimal performance traversal.
    /// </summary>
    [Fact]
    public void Enumeration_FastMode_TraversesItemsEfficiently()
    {
        var dict = new QueueDictionary<string, int>();
        dict.Enqueue("A", 1);
        dict.Enqueue("B", 2);
        dict.Enqueue("C", 3);

        var items = dict.GetEnumerator(EnumerationMode.Fast).AsEnumerable().ToList();

        items.Should().HaveCount(3);
        items.Should().Contain(kvp => kvp.Key == "A" && kvp.Value == 1);
        items.Should().Contain(kvp => kvp.Key == "B" && kvp.Value == 2);
        items.Should().Contain(kvp => kvp.Key == "C" && kvp.Value == 3);
    }

    /// <summary>
    /// Tests that enumeration with InsertionOrder mode maintains correct sequence.
    /// The InsertionOrder enumeration mode should preserve queue ordering.
    /// </summary>
    [Fact]
    public void Enumeration_InsertionOrderMode_MaintainsCorrectSequence()
    {
        var dict = new QueueDictionary<string, int>();
        dict.Enqueue("first", 1);
        dict.Enqueue("second", 2);
        dict.Enqueue("third", 3);

        var items = dict.GetEnumerator(EnumerationMode.InsertionOrder).AsEnumerable().ToList();

        items.Should().HaveCount(3);
        items[0].Key.Should().Be("first");
        items[1].Key.Should().Be("second");
        items[2].Key.Should().Be("third");
    }

    /// <summary>
    /// Tests that WarmEnumerationCache method improves subsequent cached enumeration performance.
    /// The method should prepare cache for optimized Cached enumeration mode.
    /// </summary>
    [Fact]
    public void WarmEnumerationCache_PreparesCacheForFasterEnumeration()
    {
        var dict = new QueueDictionary<string, int>();
        dict.Enqueue("A", 1);
        dict.Enqueue("B", 2);
        dict.Enqueue("C", 3);

        dict.WarmEnumerationCache();

        var items = dict.GetEnumerator(EnumerationMode.Cached).AsEnumerable().ToList();

        items.Should().HaveCount(3);
        items.Should().Contain(kvp => kvp.Key == "A" && kvp.Value == 1);
        items.Should().Contain(kvp => kvp.Key == "B" && kvp.Value == 2);
        items.Should().Contain(kvp => kvp.Key == "C" && kvp.Value == 3);
    }

    /// <summary>
    /// Tests that FastEnumerator provides high-performance enumeration.
    /// The FastEnumerator should efficiently traverse items without boxing.
    /// </summary>
    [Fact]
    public void FastEnumerator_TraversesItemsWithHighPerformance()
    {
        var dict = new QueueDictionary<string, int>();
        dict.Enqueue("fast1", 1);
        dict.Enqueue("fast2", 2);
        dict.Enqueue("fast3", 3);

        var items = new List<KeyValuePair<string, int>>();
        var enumerator = dict.GetFastEnumerator();
        while (enumerator.MoveNext())
        {
            items.Add(enumerator.Current);
        }

        items.Should().HaveCount(3);
        items[0].Key.Should().Be("fast1");
        items[1].Key.Should().Be("fast2");
        items[2].Key.Should().Be("fast3");
    }

    /// <summary>
    /// Tests that enumeration throws exception when collection is modified during iteration.
    /// The enumerator should detect version changes and throw InvalidOperationException.
    /// </summary>
    [Fact]
    public void Enumeration_ModifiedDuringIteration_ThrowsInvalidOperationException()
    {
        var dict = new QueueDictionary<string, int>();
        dict.Enqueue("key1", 1);
        dict.Enqueue("key2", 2);

        var act = () =>
        {
            foreach (var kvp in dict)
            {
                dict.Enqueue("key3", 3); // Modify during enumeration
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
        var dict = new QueueDictionary<string, int>();
        dict.Enqueue("key1", 1);
        dict.Enqueue("key2", 2);

        dict.Dispose();

        dict.Count.Should().Be(0);
        dict.IsEmpty.Should().BeTrue();
        dict.ContainsKey("key1").Should().BeFalse();
        dict.ContainsKey("key2").Should().BeFalse();
    }

    /// <summary>
    /// Tests that queue operations maintain FIFO order correctly across complex scenarios.
    /// The dictionary should preserve queue semantics through mixed operations.
    /// </summary>
    [Fact]
    public void ComplexQueueOperations_MaintainCorrectFifoOrder()
    {
        var dict = new QueueDictionary<string, int>();

        // Build initial queue
        dict.Enqueue("A", 1);
        dict.Enqueue("B", 2);
        dict.Enqueue("C", 3);
        dict.Enqueue("D", 4);

        // Remove middle item
        dict.Remove("B");

        // Update existing item (should move to back)
        dict.Enqueue("A", 10);

        // Add new item
        dict.Enqueue("E", 5);

        // Verify order: C, D, A (updated), E
        dict.PeekFront().Key.Should().Be("C");
        dict.Dequeue().Key.Should().Be("C");
        dict.Dequeue().Key.Should().Be("D");
        dict.Dequeue().Key.Should().Be("A");
        dict.Dequeue().Key.Should().Be("E");
        dict.IsEmpty.Should().BeTrue();
    }

    /// <summary>
    /// Tests that dictionary maintains performance characteristics under load.
    /// The dictionary should handle large numbers of items efficiently.
    /// </summary>
    [Fact]
    public void PerformanceCharacteristics_HandlesLargeDataSetsEfficiently()
    {
        var dict = new QueueDictionary<int, string>();
        const int itemCount = 1000;

        // Add many items
        for (int i = 0; i < itemCount; i++)
        {
            dict.Enqueue(i, $"value{i}");
        }

        dict.Count.Should().Be(itemCount);
        dict.ContainsKey(0).Should().BeTrue();
        dict.ContainsKey(itemCount - 1).Should().BeTrue();
        dict.PeekFront().Key.Should().Be(0);
        dict.PeekBack().Key.Should().Be(itemCount - 1);
    }

    /// <summary>
    /// Tests that dictionary works correctly with null values.
    /// The dictionary should handle null values properly while maintaining queue semantics.
    /// </summary>
    [Fact]
    public void NullValues_Operations_WorkCorrectlyWithQueueSemantics()
    {
        var dict = new QueueDictionary<string, string?>();

        dict.Enqueue("key1", null);
        dict.Enqueue("key2", "value");
        dict.Enqueue("key3", null);

        dict["key1"].Should().BeNull();
        dict["key2"].Should().Be("value");
        dict["key3"].Should().BeNull();

        var first = dict.Dequeue();
        first.Key.Should().Be("key1");
        first.Value.Should().BeNull();
    }

    /// <summary>
    /// Tests dictionary behavior with single item operations.
    /// The dictionary should handle single-item scenarios correctly.
    /// </summary>
    [Fact]
    public void SingleItem_Operations_WorkCorrectlyWithQueueSemantics()
    {
        var dict = new QueueDictionary<string, int>();
        dict.Enqueue("only", 42);

        dict.PeekFront().Key.Should().Be("only");
        dict.PeekBack().Key.Should().Be("only");
        dict.Count.Should().Be(1);

        var dequeued = dict.Dequeue();
        dequeued.Key.Should().Be("only");
        dequeued.Value.Should().Be(42);
        dict.IsEmpty.Should().BeTrue();
    }
}

public static class EnumeratorExtensions
{
    public static IEnumerable<T> AsEnumerable<T>(this IEnumerator<T> enumerator)
    {
        try
        {
            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }
        finally
        {
            enumerator?.Dispose();
        }
    }
}
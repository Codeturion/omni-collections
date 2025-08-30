using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Omni.Collections.Hybrid;
using Xunit;

namespace Omni.Collections.Tests.Hybrid;

public class DequeDictionaryTests
{
    /// <summary>
    /// Tests that a DequeDictionary can be constructed with default parameters.
    /// The dictionary should have default capacity and be empty initially.
    /// </summary>
    [Fact]
    public void Constructor_Default_CreatesEmptyDictionary()
    {
        var dict = new DequeDictionary<string, int>();

        dict.Count.Should().Be(0);
        dict.IsEmpty.Should().BeTrue();
    }

    /// <summary>
    /// Tests that a DequeDictionary can be constructed with specified capacity.
    /// The dictionary should initialize with the given capacity parameter.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(16)]
    [InlineData(100)]
    [InlineData(1000)]
    public void Constructor_WithCapacity_CreatesEmptyDictionary(int capacity)
    {
        var dict = new DequeDictionary<string, int>(capacity);

        dict.Count.Should().Be(0);
        dict.IsEmpty.Should().BeTrue();
    }

    /// <summary>
    /// Tests that constructing a DequeDictionary with negative capacity throws exception.
    /// The constructor should reject negative capacity values.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(-10)]
    [InlineData(-100)]
    public void Constructor_WithNegativeCapacity_ThrowsArgumentOutOfRangeException(int capacity)
    {
        var act = () => new DequeDictionary<string, int>(capacity);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("capacity");
    }

    /// <summary>
    /// Tests that constructing a DequeDictionary with invalid load factor throws exception.
    /// The constructor should reject load factors outside the valid range.
    /// </summary>
    [Theory]
    [InlineData(0.0f)]
    [InlineData(-0.5f)]
    [InlineData(1.0f)]
    [InlineData(1.5f)]
    public void Constructor_WithInvalidLoadFactor_ThrowsArgumentOutOfRangeException(float loadFactor)
    {
        var act = () => new DequeDictionary<string, int>(16, loadFactor);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("loadFactor");
    }

    /// <summary>
    /// Tests that a DequeDictionary can be constructed with custom equality comparer.
    /// The dictionary should use the provided comparer for key comparisons.
    /// </summary>
    [Fact]
    public void Constructor_WithCustomComparer_UsesCustomComparerForKeys()
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        var dict = new DequeDictionary<string, int>(16, 0.75f, comparer);

        dict.PushBack("KEY", 1);
        dict.ContainsKey("key").Should().BeTrue();
        dict.ContainsKey("KEY").Should().BeTrue();
    }

    /// <summary>
    /// Tests that PushFront method adds items to the front of the deque.
    /// The method should maintain deque ordering with new items at the front.
    /// </summary>
    [Fact]
    public void PushFront_NewItems_AddsItemsToFront()
    {
        var dict = new DequeDictionary<string, int>();

        dict.PushFront("first", 1);
        dict.PushFront("second", 2);
        dict.PushFront("third", 3);

        dict.Count.Should().Be(3);
        dict.IsEmpty.Should().BeFalse();
        dict.PeekFront().Key.Should().Be("third"); // Last pushed to front
        dict.PeekBack().Key.Should().Be("first");  // First pushed, now at back
    }

    /// <summary>
    /// Tests that PushFront method updates existing keys and moves them to front.
    /// The method should update values and reposition items to maintain deque semantics.
    /// </summary>
    [Fact]
    public void PushFront_ExistingKey_UpdatesValueAndMovesToFront()
    {
        var dict = new DequeDictionary<string, int>();
        dict.PushBack("A", 1);
        dict.PushBack("B", 2);
        dict.PushBack("C", 3);

        dict.PushFront("B", 20); // Update B and move to front

        dict.Count.Should().Be(3);
        dict["B"].Should().Be(20);
        dict.PeekFront().Key.Should().Be("B");
        dict.PeekBack().Key.Should().Be("C");
    }

    /// <summary>
    /// Tests that PushBack method adds items to the back of the deque.
    /// The method should maintain deque ordering with new items at the back.
    /// </summary>
    [Fact]
    public void PushBack_NewItems_AddsItemsToBack()
    {
        var dict = new DequeDictionary<string, int>();

        dict.PushBack("first", 1);
        dict.PushBack("second", 2);
        dict.PushBack("third", 3);

        dict.Count.Should().Be(3);
        dict.IsEmpty.Should().BeFalse();
        dict.PeekFront().Key.Should().Be("first");
        dict.PeekBack().Key.Should().Be("third");
    }

    /// <summary>
    /// Tests that PushBack method updates existing keys and moves them to back.
    /// The method should update values and reposition items to maintain deque semantics.
    /// </summary>
    [Fact]
    public void PushBack_ExistingKey_UpdatesValueAndMovesToBack()
    {
        var dict = new DequeDictionary<string, int>();
        dict.PushFront("A", 1);
        dict.PushFront("B", 2);
        dict.PushFront("C", 3);

        dict.PushBack("B", 20); // Update B and move to back

        dict.Count.Should().Be(3);
        dict["B"].Should().Be(20);
        dict.PeekFront().Key.Should().Be("C");
        dict.PeekBack().Key.Should().Be("B");
    }

    /// <summary>
    /// Tests that PopFront method removes and returns the front item.
    /// The method should remove from front and maintain deque ordering.
    /// </summary>
    [Fact]
    public void PopFront_WithItems_RemovesAndReturnsFrontItem()
    {
        var dict = new DequeDictionary<string, int>();
        dict.PushBack("first", 1);
        dict.PushBack("second", 2);
        dict.PushBack("third", 3);

        var result = dict.PopFront();

        result.Key.Should().Be("first");
        result.Value.Should().Be(1);
        dict.Count.Should().Be(2);
        dict.ContainsKey("first").Should().BeFalse();
        dict.PeekFront().Key.Should().Be("second");
    }

    /// <summary>
    /// Tests that PopFront method throws exception when deque is empty.
    /// The method should throw InvalidOperationException for empty deques.
    /// </summary>
    [Fact]
    public void PopFront_EmptyDeque_ThrowsInvalidOperationException()
    {
        var dict = new DequeDictionary<string, int>();

        var act = () => dict.PopFront();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Deque is empty");
    }

    /// <summary>
    /// Tests that PopBack method removes and returns the back item.
    /// The method should remove from back and maintain deque ordering.
    /// </summary>
    [Fact]
    public void PopBack_WithItems_RemovesAndReturnsBackItem()
    {
        var dict = new DequeDictionary<string, int>();
        dict.PushBack("first", 1);
        dict.PushBack("second", 2);
        dict.PushBack("third", 3);

        var result = dict.PopBack();

        result.Key.Should().Be("third");
        result.Value.Should().Be(3);
        dict.Count.Should().Be(2);
        dict.ContainsKey("third").Should().BeFalse();
        dict.PeekBack().Key.Should().Be("second");
    }

    /// <summary>
    /// Tests that PopBack method throws exception when deque is empty.
    /// The method should throw InvalidOperationException for empty deques.
    /// </summary>
    [Fact]
    public void PopBack_EmptyDeque_ThrowsInvalidOperationException()
    {
        var dict = new DequeDictionary<string, int>();

        var act = () => dict.PopBack();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Deque is empty");
    }

    /// <summary>
    /// Tests that TryPopFront method successfully pops items when available.
    /// The method should return true and the popped item when deque is not empty.
    /// </summary>
    [Fact]
    public void TryPopFront_WithItems_ReturnsTrueAndPoppedItem()
    {
        var dict = new DequeDictionary<string, int>();
        dict.PushBack("key1", 42);

        var success = dict.TryPopFront(out var result);

        success.Should().BeTrue();
        result.Key.Should().Be("key1");
        result.Value.Should().Be(42);
        dict.Count.Should().Be(0);
        dict.IsEmpty.Should().BeTrue();
    }

    /// <summary>
    /// Tests that TryPopFront method returns false when deque is empty.
    /// The method should return false and default value for empty deques.
    /// </summary>
    [Fact]
    public void TryPopFront_EmptyDeque_ReturnsFalseWithDefaultValue()
    {
        var dict = new DequeDictionary<string, int>();

        var success = dict.TryPopFront(out var result);

        success.Should().BeFalse();
        result.Should().Be(default(KeyValuePair<string, int>));
    }

    /// <summary>
    /// Tests that TryPopBack method successfully pops items when available.
    /// The method should return true and the popped item when deque is not empty.
    /// </summary>
    [Fact]
    public void TryPopBack_WithItems_ReturnsTrueAndPoppedItem()
    {
        var dict = new DequeDictionary<string, int>();
        dict.PushBack("key1", 42);

        var success = dict.TryPopBack(out var result);

        success.Should().BeTrue();
        result.Key.Should().Be("key1");
        result.Value.Should().Be(42);
        dict.Count.Should().Be(0);
        dict.IsEmpty.Should().BeTrue();
    }

    /// <summary>
    /// Tests that TryPopBack method returns false when deque is empty.
    /// The method should return false and default value for empty deques.
    /// </summary>
    [Fact]
    public void TryPopBack_EmptyDeque_ReturnsFalseWithDefaultValue()
    {
        var dict = new DequeDictionary<string, int>();

        var success = dict.TryPopBack(out var result);

        success.Should().BeFalse();
        result.Should().Be(default(KeyValuePair<string, int>));
    }

    /// <summary>
    /// Tests that PeekFront method returns the front item without removing it.
    /// The method should provide access to the front item without modifying the deque.
    /// </summary>
    [Fact]
    public void PeekFront_WithItems_ReturnsFrontItemWithoutRemoving()
    {
        var dict = new DequeDictionary<string, int>();
        dict.PushBack("first", 1);
        dict.PushBack("second", 2);

        var front = dict.PeekFront();

        front.Key.Should().Be("first");
        front.Value.Should().Be(1);
        dict.Count.Should().Be(2); // Should not remove item
    }

    /// <summary>
    /// Tests that PeekFront method throws exception when deque is empty.
    /// The method should throw InvalidOperationException for empty deques.
    /// </summary>
    [Fact]
    public void PeekFront_EmptyDeque_ThrowsInvalidOperationException()
    {
        var dict = new DequeDictionary<string, int>();

        var act = () => dict.PeekFront();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Deque is empty");
    }

    /// <summary>
    /// Tests that PeekBack method returns the back item without removing it.
    /// The method should provide access to the back item without modifying the deque.
    /// </summary>
    [Fact]
    public void PeekBack_WithItems_ReturnsBackItemWithoutRemoving()
    {
        var dict = new DequeDictionary<string, int>();
        dict.PushBack("first", 1);
        dict.PushBack("second", 2);

        var back = dict.PeekBack();

        back.Key.Should().Be("second");
        back.Value.Should().Be(2);
        dict.Count.Should().Be(2); // Should not remove item
    }

    /// <summary>
    /// Tests that PeekBack method throws exception when deque is empty.
    /// The method should throw InvalidOperationException for empty deques.
    /// </summary>
    [Fact]
    public void PeekBack_EmptyDeque_ThrowsInvalidOperationException()
    {
        var dict = new DequeDictionary<string, int>();

        var act = () => dict.PeekBack();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Deque is empty");
    }

    /// <summary>
    /// Tests that the indexer can get and set values for keys.
    /// The indexer should provide dictionary-like access while using PushBack semantics for setting.
    /// </summary>
    [Fact]
    public void Indexer_GetAndSet_WorksCorrectlyWithDequeSemantics()
    {
        var dict = new DequeDictionary<string, int>();
        dict.PushBack("key1", 10);

        var value = dict["key1"];
        dict["key2"] = 20; // Should push back new item
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
        var dict = new DequeDictionary<string, int>();

        var act = () => dict["nonexistent"];

        act.Should().Throw<KeyNotFoundException>();
    }

    /// <summary>
    /// Tests that TryGetValue method returns true and correct value for existing keys.
    /// The method should provide dictionary-like lookup without affecting deque order.
    /// </summary>
    [Fact]
    public void TryGetValue_ExistingKey_ReturnsTrueWithCorrectValue()
    {
        var dict = new DequeDictionary<string, int>();
        dict.PushBack("key1", 42);

        var result = dict.TryGetValue("key1", out var value);

        result.Should().BeTrue();
        value.Should().Be(42);
        dict.PeekFront().Key.Should().Be("key1"); // Should not affect deque order
    }

    /// <summary>
    /// Tests that TryGetValue method returns false for non-existent keys.
    /// The method should return false and default value for missing keys.
    /// </summary>
    [Fact]
    public void TryGetValue_NonExistentKey_ReturnsFalseWithDefaultValue()
    {
        var dict = new DequeDictionary<string, int>();

        var result = dict.TryGetValue("nonexistent", out var value);

        result.Should().BeFalse();
        value.Should().Be(default(int));
    }

    /// <summary>
    /// Tests that ContainsKey method returns correct boolean values.
    /// The method should check key existence without affecting deque order.
    /// </summary>
    [Theory]
    [InlineData("existing", true)]
    [InlineData("nonexistent", false)]
    public void ContainsKey_VariousKeys_ReturnsCorrectResult(string key, bool expected)
    {
        var dict = new DequeDictionary<string, int>();
        dict.PushBack("existing", 42);

        var result = dict.ContainsKey(key);

        result.Should().Be(expected);
    }

    /// <summary>
    /// Tests that Remove method can successfully remove existing items from the dictionary.
    /// The method should return true and maintain deque ordering for remaining items.
    /// </summary>
    [Fact]
    public void Remove_ExistingKey_ReturnsTrueAndMaintainsOrder()
    {
        var dict = new DequeDictionary<string, int>();
        dict.PushBack("A", 1);
        dict.PushBack("B", 2);
        dict.PushBack("C", 3);

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
        var dict = new DequeDictionary<string, int>();
        dict.PushBack("key1", 10);

        var result = dict.Remove("nonexistent");

        result.Should().BeFalse();
        dict.Count.Should().Be(1);
        dict.ContainsKey("key1").Should().BeTrue();
    }

    /// <summary>
    /// Tests that MoveToFront method successfully moves existing items to front.
    /// The method should return true and reposition the item without changing its value.
    /// </summary>
    [Fact]
    public void MoveToFront_ExistingKey_ReturnsTrueAndMovesItemToFront()
    {
        var dict = new DequeDictionary<string, int>();
        dict.PushBack("A", 1);
        dict.PushBack("B", 2);
        dict.PushBack("C", 3);

        var result = dict.MoveToFront("C");

        result.Should().BeTrue();
        dict.Count.Should().Be(3);
        dict.PeekFront().Key.Should().Be("C");
        dict.PeekBack().Key.Should().Be("B");
        dict["C"].Should().Be(3); // Value should remain unchanged
    }

    /// <summary>
    /// Tests that MoveToFront method returns false for non-existent keys.
    /// The method should not modify the deque when key is not found.
    /// </summary>
    [Fact]
    public void MoveToFront_NonExistentKey_ReturnsFalseWithoutChangingCollection()
    {
        var dict = new DequeDictionary<string, int>();
        dict.PushBack("A", 1);
        dict.PushBack("B", 2);
        var frontBefore = dict.PeekFront().Key;

        var result = dict.MoveToFront("nonexistent");

        result.Should().BeFalse();
        dict.Count.Should().Be(2);
        dict.PeekFront().Key.Should().Be(frontBefore);
    }

    /// <summary>
    /// Tests that MoveToBack method successfully moves existing items to back.
    /// The method should return true and reposition the item without changing its value.
    /// </summary>
    [Fact]
    public void MoveToBack_ExistingKey_ReturnsTrueAndMovesItemToBack()
    {
        var dict = new DequeDictionary<string, int>();
        dict.PushBack("A", 1);
        dict.PushBack("B", 2);
        dict.PushBack("C", 3);

        var result = dict.MoveToBack("A");

        result.Should().BeTrue();
        dict.Count.Should().Be(3);
        dict.PeekFront().Key.Should().Be("B");
        dict.PeekBack().Key.Should().Be("A");
        dict["A"].Should().Be(1); // Value should remain unchanged
    }

    /// <summary>
    /// Tests that MoveToBack method returns false for non-existent keys.
    /// The method should not modify the deque when key is not found.
    /// </summary>
    [Fact]
    public void MoveToBack_NonExistentKey_ReturnsFalseWithoutChangingCollection()
    {
        var dict = new DequeDictionary<string, int>();
        dict.PushBack("A", 1);
        dict.PushBack("B", 2);
        var backBefore = dict.PeekBack().Key;

        var result = dict.MoveToBack("nonexistent");

        result.Should().BeFalse();
        dict.Count.Should().Be(2);
        dict.PeekBack().Key.Should().Be(backBefore);
    }

    /// <summary>
    /// Tests that Clear method removes all items from the dictionary.
    /// The method should reset count and empty the deque completely.
    /// </summary>
    [Fact]
    public void Clear_WithMultipleItems_RemovesAllItemsAndResetsState()
    {
        var dict = new DequeDictionary<string, int>();
        dict.PushBack("key1", 10);
        dict.PushBack("key2", 20);
        dict.PushBack("key3", 30);

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
    /// Tests that ToArray method returns items in deque order.
    /// The method should create an array with items from front to back.
    /// </summary>
    [Fact]
    public void ToArray_WithItems_ReturnsItemsInDequeOrder()
    {
        var dict = new DequeDictionary<string, int>();
        dict.PushBack("first", 1);
        dict.PushBack("second", 2);
        dict.PushBack("third", 3);

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
        var dict = new DequeDictionary<string, int>();

        var array = dict.ToArray();

        array.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that Reverse method returns items in reverse deque order.
    /// The method should provide back-to-front traversal of the deque.
    /// </summary>
    [Fact]
    public void Reverse_WithItems_ReturnsItemsInReverseOrder()
    {
        var dict = new DequeDictionary<string, int>();
        dict.PushBack("first", 1);
        dict.PushBack("second", 2);
        dict.PushBack("third", 3);

        var reversed = dict.Reverse().ToList();

        reversed.Should().HaveCount(3);
        reversed[0].Should().Be(new KeyValuePair<string, int>("third", 3));
        reversed[1].Should().Be(new KeyValuePair<string, int>("second", 2));
        reversed[2].Should().Be(new KeyValuePair<string, int>("first", 1));
    }

    /// <summary>
    /// Tests that Reverse method returns empty sequence for empty dictionary.
    /// The method should handle empty collections gracefully.
    /// </summary>
    [Fact]
    public void Reverse_EmptyDictionary_ReturnsEmptySequence()
    {
        var dict = new DequeDictionary<string, int>();

        var reversed = dict.Reverse().ToList();

        reversed.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that default enumeration traverses items in deque order.
    /// The foreach operation should visit items from front to back.
    /// </summary>
    [Fact]
    public void Enumeration_DefaultOrder_TraversesFromFrontToBack()
    {
        var dict = new DequeDictionary<string, int>();
        dict.PushBack("first", 1);
        dict.PushBack("second", 2);
        dict.PushBack("third", 3);

        var items = dict.ToList();

        items.Should().HaveCount(3);
        items[0].Should().Be(new KeyValuePair<string, int>("first", 1));
        items[1].Should().Be(new KeyValuePair<string, int>("second", 2));
        items[2].Should().Be(new KeyValuePair<string, int>("third", 3));
    }

    /// <summary>
    /// Tests that enumeration works correctly on empty dictionary.
    /// The foreach operation should handle empty collections gracefully.
    /// </summary>
    [Fact]
    public void Enumeration_EmptyDictionary_ReturnsNoItems()
    {
        var dict = new DequeDictionary<string, int>();

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
        var dict = new DequeDictionary<string, int>();
        dict.PushBack("key1", 1);
        dict.PushBack("key2", 2);

        var act = () =>
        {
            foreach (var kvp in dict)
            {
                dict.PushBack("key3", 3); // Modify during enumeration
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
        var dict = new DequeDictionary<string, int>();
        dict.PushBack("key1", 1);
        dict.PushBack("key2", 2);

        dict.Dispose();

        dict.Count.Should().Be(0);
        dict.IsEmpty.Should().BeTrue();
        dict.ContainsKey("key1").Should().BeFalse();
        dict.ContainsKey("key2").Should().BeFalse();
    }

    /// <summary>
    /// Tests that complex deque operations maintain correct ordering across all scenarios.
    /// The dictionary should preserve deque semantics through mixed operations.
    /// </summary>
    [Fact]
    public void ComplexDequeOperations_MaintainCorrectOrdering()
    {
        var dict = new DequeDictionary<string, int>();

        // Build initial deque
        dict.PushBack("A", 1);   // [A]
        dict.PushFront("B", 2);  // [B, A]
        dict.PushBack("C", 3);   // [B, A, C]
        dict.PushFront("D", 4);  // [D, B, A, C]

        // Move items around
        dict.MoveToBack("B");    // [D, A, C, B]
        dict.MoveToFront("C");   // [C, D, A, B]

        // Verify final order
        var items = dict.ToArray();
        items[0].Key.Should().Be("C");
        items[1].Key.Should().Be("D");
        items[2].Key.Should().Be("A");
        items[3].Key.Should().Be("B");
    }

    /// <summary>
    /// Tests that deque maintains performance characteristics under load.
    /// The dictionary should handle large numbers of items efficiently.
    /// </summary>
    [Fact]
    public void PerformanceCharacteristics_HandlesLargeDataSetsEfficiently()
    {
        var dict = new DequeDictionary<int, string>();
        const int itemCount = 1000;

        // Add items alternating front and back
        for (int i = 0; i < itemCount; i++)
        {
            if (i % 2 == 0)
                dict.PushBack(i, $"value{i}");
            else
                dict.PushFront(i, $"value{i}");
        }

        dict.Count.Should().Be(itemCount);
        dict.ContainsKey(0).Should().BeTrue();
        dict.ContainsKey(itemCount - 1).Should().BeTrue();
        dict.IsEmpty.Should().BeFalse();
    }

    /// <summary>
    /// Tests that dictionary works correctly with null values.
    /// The dictionary should handle null values properly while maintaining deque semantics.
    /// </summary>
    [Fact]
    public void NullValues_Operations_WorkCorrectlyWithDequeSemantics()
    {
        var dict = new DequeDictionary<string, string?>();

        dict.PushBack("key1", null);
        dict.PushFront("key2", "value");
        dict.PushBack("key3", null);

        dict["key1"].Should().BeNull();
        dict["key2"].Should().Be("value");
        dict["key3"].Should().BeNull();

        var front = dict.PopFront();
        front.Key.Should().Be("key2");
        front.Value.Should().Be("value");

        var back = dict.PopBack();
        back.Key.Should().Be("key3");
        back.Value.Should().BeNull();
    }

    /// <summary>
    /// Tests dictionary behavior with single item operations.
    /// The dictionary should handle single-item scenarios correctly with deque semantics.
    /// </summary>
    [Fact]
    public void SingleItem_Operations_WorkCorrectlyWithDequeSemantics()
    {
        var dict = new DequeDictionary<string, int>();
        dict.PushBack("only", 42);

        dict.PeekFront().Key.Should().Be("only");
        dict.PeekBack().Key.Should().Be("only");
        dict.Count.Should().Be(1);

        dict.MoveToFront("only").Should().BeTrue();
        dict.PeekFront().Key.Should().Be("only");

        var popped = dict.PopFront();
        popped.Key.Should().Be("only");
        popped.Value.Should().Be(42);
        dict.IsEmpty.Should().BeTrue();
    }

    /// <summary>
    /// Tests that mixed front and back operations work correctly together.
    /// The dictionary should maintain proper ordering across all double-ended operations.
    /// </summary>
    [Fact]
    public void MixedFrontBackOperations_MaintainCorrectOrdering()
    {
        var dict = new DequeDictionary<string, int>();

        // Mix of push operations
        dict.PushBack("1", 1);    // [1]
        dict.PushFront("2", 2);   // [2, 1]
        dict.PushBack("3", 3);    // [2, 1, 3]
        dict.PushFront("4", 4);   // [4, 2, 1, 3]

        // Mix of pop operations
        var front = dict.PopFront();  // [2, 1, 3], got 4
        var back = dict.PopBack();    // [2, 1], got 3

        front.Key.Should().Be("4");
        back.Key.Should().Be("3");
        dict.PeekFront().Key.Should().Be("2");
        dict.PeekBack().Key.Should().Be("1");
        dict.Count.Should().Be(2);
    }
}
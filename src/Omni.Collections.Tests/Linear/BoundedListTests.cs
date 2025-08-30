using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Omni.Collections.Linear;
using Xunit;

namespace Omni.Collections.Tests.Linear;

public class BoundedListTests
{
    /// <summary>
    /// Tests that a BoundedList can be constructed with a positive capacity.
    /// The list should have the specified capacity and zero count initially.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void Constructor_WithValidCapacity_CreatesListWithCorrectCapacity(int capacity)
    {
        var list = new BoundedList<int>(capacity);

        list.Capacity.Should().Be(capacity);
        list.Count.Should().Be(0);
        list.IsFull.Should().BeFalse();
        list.RemainingCapacity.Should().Be(capacity);
    }

    /// <summary>
    /// Tests that constructing a BoundedList with invalid capacity throws exception.
    /// The constructor should reject zero or negative capacity values.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_WithInvalidCapacity_ThrowsArgumentOutOfRangeException(int capacity)
    {
        var act = () => new BoundedList<int>(capacity);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("capacity");
    }

    /// <summary>
    /// Tests that a BoundedList can be constructed with initial items.
    /// The list should contain all provided items in the correct order.
    /// </summary>
    [Fact]
    public void Constructor_WithInitialItems_PopulatesListCorrectly()
    {
        var items = new[] { 1, 2, 3, 4, 5 };
        var list = new BoundedList<int>(10, items);

        list.Count.Should().Be(5);
        list.Should().Equal(items);
        list.IsFull.Should().BeFalse();
        list.RemainingCapacity.Should().Be(5);
    }

    /// <summary>
    /// Tests that constructing with items exceeding capacity throws exception.
    /// The constructor should reject initial items that exceed the specified capacity.
    /// </summary>
    [Fact]
    public void Constructor_WithItemsExceedingCapacity_ThrowsInvalidOperationException()
    {
        var items = new[] { 1, 2, 3, 4, 5 };
        var act = () => new BoundedList<int>(3, items);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*exceed capacity*");
    }

    /// <summary>
    /// Tests that Add method successfully adds items within capacity.
    /// Items should be added in order and count should update correctly.
    /// </summary>
    [Fact]
    public void Add_WhenNotFull_AddsItemSuccessfully()
    {
        var list = new BoundedList<int>(5);

        list.Add(10);
        list.Add(20);
        list.Add(30);

        list.Count.Should().Be(3);
        list[0].Should().Be(10);
        list[1].Should().Be(20);
        list[2].Should().Be(30);
    }

    /// <summary>
    /// Tests that Add method throws exception when list is at capacity.
    /// The method should reject adding items when the list is full.
    /// </summary>
    [Fact]
    public void Add_WhenFull_ThrowsInvalidOperationException()
    {
        var list = new BoundedList<int>(2);
        list.Add(1);
        list.Add(2);

        var act = () => list.Add(3);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*capacity*");
    }

    /// <summary>
    /// Tests that TryAdd returns true and adds item when not full.
    /// The method should successfully add the item and return true.
    /// </summary>
    [Fact]
    public void TryAdd_WhenNotFull_ReturnsTrueAndAddsItem()
    {
        var list = new BoundedList<int>(3);

        var result1 = list.TryAdd(10);
        var result2 = list.TryAdd(20);

        result1.Should().BeTrue();
        result2.Should().BeTrue();
        list.Count.Should().Be(2);
        list.Should().Equal(10, 20);
    }

    /// <summary>
    /// Tests that TryAdd returns false when list is at capacity.
    /// The method should not add the item and return false without throwing.
    /// </summary>
    [Fact]
    public void TryAdd_WhenFull_ReturnsFalseWithoutThrowing()
    {
        var list = new BoundedList<int>(2);
        list.Add(1);
        list.Add(2);

        var result = list.TryAdd(3);

        result.Should().BeFalse();
        list.Count.Should().Be(2);
        list.Should().Equal(1, 2);
    }

    /// <summary>
    /// Tests that AddRange adds multiple items from IEnumerable.
    /// The method should add as many items as possible and return the count added.
    /// </summary>
    [Fact]
    public void AddRange_WithIEnumerable_AddsItemsUntilCapacity()
    {
        var list = new BoundedList<int>(5);
        list.Add(1);

        var itemsToAdd = new[] { 2, 3, 4, 5, 6, 7 };
        var added = list.AddRange(itemsToAdd.AsEnumerable());

        added.Should().Be(4);
        list.Count.Should().Be(5);
        list.Should().Equal(1, 2, 3, 4, 5);
    }

    /// <summary>
    /// Tests that AddRange adds multiple items from ReadOnlySpan.
    /// The method should efficiently add items using span operations.
    /// </summary>
    [Fact]
    public void AddRange_WithReadOnlySpan_AddsItemsEfficiently()
    {
        var list = new BoundedList<int>(10);
        var items = new[] { 1, 2, 3, 4, 5 };

        var added = list.AddRange(items.AsSpan());

        added.Should().Be(5);
        list.Count.Should().Be(5);
        list.Should().Equal(items);
    }

    /// <summary>
    /// Tests that Insert method adds item at specified index.
    /// The method should shift existing items and maintain order.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Insert_AtValidIndex_InsertsItemCorrectly(int index)
    {
        var list = new BoundedList<int>(5);
        list.Add(10);
        list.Add(30);

        list.Insert(index, 20);

        list.Count.Should().Be(3);
        if (index == 0)
            list.Should().Equal(20, 10, 30);
        else if (index == 1)
            list.Should().Equal(10, 20, 30);
        else
            list.Should().Equal(10, 30, 20);
    }

    /// <summary>
    /// Tests that Insert throws exception for invalid index.
    /// The method should reject negative or out-of-bounds indices.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(100)]
    public void Insert_AtInvalidIndex_ThrowsArgumentOutOfRangeException(int index)
    {
        var list = new BoundedList<int>(5);
        list.Add(1);
        list.Add(2);

        var act = () => list.Insert(index, 3);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Tests that Insert throws exception when list is full.
    /// The method should reject insertion when capacity is reached.
    /// </summary>
    [Fact]
    public void Insert_WhenFull_ThrowsInvalidOperationException()
    {
        var list = new BoundedList<int>(2);
        list.Add(1);
        list.Add(2);

        var act = () => list.Insert(1, 3);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*capacity*");
    }

    /// <summary>
    /// Tests that TryInsert returns true and inserts item at valid index.
    /// The method should successfully insert and return true.
    /// </summary>
    [Fact]
    public void TryInsert_AtValidIndexWhenNotFull_ReturnsTrueAndInsertsItem()
    {
        var list = new BoundedList<int>(5);
        list.Add(1);
        list.Add(3);

        var result = list.TryInsert(1, 2);

        result.Should().BeTrue();
        list.Should().Equal(1, 2, 3);
    }

    /// <summary>
    /// Tests that TryInsert returns false for invalid conditions.
    /// The method should return false without throwing for invalid index or full list.
    /// </summary>
    [Fact]
    public void TryInsert_WhenInvalid_ReturnsFalse()
    {
        var list = new BoundedList<int>(2);
        list.Add(1);
        list.Add(2);

        var result1 = list.TryInsert(0, 3); // Full
        var result2 = list.TryInsert(5, 3); // Invalid index

        result1.Should().BeFalse();
        result2.Should().BeFalse();
        list.Should().Equal(1, 2);
    }

    /// <summary>
    /// Tests that Remove successfully removes first occurrence of item.
    /// The method should return true and remove only the first matching item.
    /// </summary>
    [Fact]
    public void Remove_ExistingItem_RemovesFirstOccurrenceAndReturnsTrue()
    {
        var list = new BoundedList<int>(5);
        list.Add(1);
        list.Add(2);
        list.Add(3);
        list.Add(2);

        var result = list.Remove(2);

        result.Should().BeTrue();
        list.Count.Should().Be(3);
        list.Should().Equal(1, 3, 2);
    }

    /// <summary>
    /// Tests that Remove returns false for non-existent item.
    /// The method should not modify the list and return false.
    /// </summary>
    [Fact]
    public void Remove_NonExistentItem_ReturnsFalse()
    {
        var list = new BoundedList<int>(5);
        list.Add(1);
        list.Add(2);
        list.Add(3);

        var result = list.Remove(4);

        result.Should().BeFalse();
        list.Count.Should().Be(3);
        list.Should().Equal(1, 2, 3);
    }

    /// <summary>
    /// Tests that RemoveAt removes item at specified index.
    /// The method should shift remaining items to fill the gap.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void RemoveAt_ValidIndex_RemovesItemCorrectly(int index)
    {
        var list = new BoundedList<int>(5);
        list.Add(10);
        list.Add(20);
        list.Add(30);

        list.RemoveAt(index);

        list.Count.Should().Be(2);
        if (index == 0)
            list.Should().Equal(20, 30);
        else if (index == 1)
            list.Should().Equal(10, 30);
        else
            list.Should().Equal(10, 20);
    }

    /// <summary>
    /// Tests that RemoveAt throws exception for invalid index.
    /// The method should reject negative or out-of-bounds indices.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(100)]
    public void RemoveAt_InvalidIndex_ThrowsArgumentOutOfRangeException(int index)
    {
        var list = new BoundedList<int>(5);
        list.Add(1);
        list.Add(2);

        var act = () => list.RemoveAt(index);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Tests that RemoveLast removes and returns the last item.
    /// The method should return the last item and decrease count.
    /// </summary>
    [Fact]
    public void RemoveLast_WhenNotEmpty_RemovesAndReturnsLastItem()
    {
        var list = new BoundedList<int>(5);
        list.Add(1);
        list.Add(2);
        list.Add(3);

        var removed = list.RemoveLast();

        removed.Should().Be(3);
        list.Count.Should().Be(2);
        list.Should().Equal(1, 2);
    }

    /// <summary>
    /// Tests that RemoveLast throws exception when list is empty.
    /// The method should throw InvalidOperationException for empty list.
    /// </summary>
    [Fact]
    public void RemoveLast_WhenEmpty_ThrowsInvalidOperationException()
    {
        var list = new BoundedList<int>(5);

        var act = () => list.RemoveLast();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*empty*");
    }

    /// <summary>
    /// Tests that TryRemoveLast removes last item when not empty.
    /// The method should return true and output the removed item.
    /// </summary>
    [Fact]
    public void TryRemoveLast_WhenNotEmpty_ReturnsTrueAndRemovesItem()
    {
        var list = new BoundedList<int>(5);
        list.Add(1);
        list.Add(2);

        var result = list.TryRemoveLast(out var removed);

        result.Should().BeTrue();
        removed.Should().Be(2);
        list.Count.Should().Be(1);
        list.Should().Equal(1);
    }

    /// <summary>
    /// Tests that TryRemoveLast returns false when empty.
    /// The method should return false without throwing.
    /// </summary>
    [Fact]
    public void TryRemoveLast_WhenEmpty_ReturnsFalse()
    {
        var list = new BoundedList<int>(5);

        var result = list.TryRemoveLast(out var removed);

        result.Should().BeFalse();
        removed.Should().Be(0);
        list.Count.Should().Be(0);
    }

    /// <summary>
    /// Tests that RemoveAtSwap efficiently removes item by swapping with last.
    /// The method should swap target with last item and remove, changing order.
    /// </summary>
    [Fact]
    public void RemoveAtSwap_ValidIndex_SwapsAndRemoves()
    {
        var list = new BoundedList<int>(5);
        list.Add(1);
        list.Add(2);
        list.Add(3);
        list.Add(4);

        list.RemoveAtSwap(1);

        list.Count.Should().Be(3);
        list[0].Should().Be(1);
        list[1].Should().Be(4);
        list[2].Should().Be(3);
    }

    /// <summary>
    /// Tests that indexer get returns correct value.
    /// The indexer should provide direct access to elements.
    /// </summary>
    [Fact]
    public void Indexer_Get_ReturnsCorrectValue()
    {
        var list = new BoundedList<int>(5);
        list.Add(10);
        list.Add(20);
        list.Add(30);

        list[0].Should().Be(10);
        list[1].Should().Be(20);
        list[2].Should().Be(30);
    }

    /// <summary>
    /// Tests that indexer set updates value correctly.
    /// The indexer should allow updating existing elements.
    /// </summary>
    [Fact]
    public void Indexer_Set_UpdatesValue()
    {
        var list = new BoundedList<int>(5);
        list.Add(10);
        list.Add(20);
        list.Add(30);

        list[1] = 25;

        list[1].Should().Be(25);
        list.Should().Equal(10, 25, 30);
    }

    /// <summary>
    /// Tests that indexer throws exception for invalid index.
    /// The indexer should reject negative or out-of-bounds indices.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(100)]
    public void Indexer_InvalidIndex_ThrowsIndexOutOfRangeException(int index)
    {
        var list = new BoundedList<int>(5);
        list.Add(1);
        list.Add(2);

        var getAct = () => { var x = list[index]; };
        var setAct = () => list[index] = 3;

        getAct.Should().Throw<IndexOutOfRangeException>();
        setAct.Should().Throw<IndexOutOfRangeException>();
    }

    /// <summary>
    /// Tests that GetRef returns a reference to the element.
    /// The method should allow direct modification through reference.
    /// </summary>
    [Fact]
    public void GetRef_ValidIndex_ReturnsReference()
    {
        var list = new BoundedList<int>(5);
        list.Add(10);
        list.Add(20);

        ref var item = ref list.GetRef(1);
        item = 25;

        list[1].Should().Be(25);
    }

    /// <summary>
    /// Tests that IndexOf finds the correct index of an item.
    /// The method should return the index of first occurrence.
    /// </summary>
    [Fact]
    public void IndexOf_ExistingItem_ReturnsCorrectIndex()
    {
        var list = new BoundedList<int>(5);
        list.Add(10);
        list.Add(20);
        list.Add(30);
        list.Add(20);

        var index = list.IndexOf(20);

        index.Should().Be(1);
    }

    /// <summary>
    /// Tests that IndexOf returns -1 for non-existent item.
    /// The method should return -1 when item is not found.
    /// </summary>
    [Fact]
    public void IndexOf_NonExistentItem_ReturnsNegativeOne()
    {
        var list = new BoundedList<int>(5);
        list.Add(10);
        list.Add(20);

        var index = list.IndexOf(30);

        index.Should().Be(-1);
    }

    /// <summary>
    /// Tests that Contains correctly identifies existing items.
    /// The method should return true for items in the list.
    /// </summary>
    [Fact]
    public void Contains_ExistingItem_ReturnsTrue()
    {
        var list = new BoundedList<int>(5);
        list.Add(10);
        list.Add(20);
        list.Add(30);

        list.Contains(20).Should().BeTrue();
        list.Contains(30).Should().BeTrue();
    }

    /// <summary>
    /// Tests that Contains returns false for non-existent items.
    /// The method should return false for items not in the list.
    /// </summary>
    [Fact]
    public void Contains_NonExistentItem_ReturnsFalse()
    {
        var list = new BoundedList<int>(5);
        list.Add(10);
        list.Add(20);

        list.Contains(30).Should().BeFalse();
        list.Contains(0).Should().BeFalse();
    }

    /// <summary>
    /// Tests that Clear removes all items from the list.
    /// The method should reset count to zero and clear all elements.
    /// </summary>
    [Fact]
    public void Clear_RemovesAllItems()
    {
        var list = new BoundedList<int>(5);
        list.Add(1);
        list.Add(2);
        list.Add(3);

        list.Clear();

        list.Count.Should().Be(0);
        list.IsFull.Should().BeFalse();
        list.RemainingCapacity.Should().Be(5);
    }

    /// <summary>
    /// Tests that AsSpan returns a read-only span of current items.
    /// The span should contain only the active elements.
    /// </summary>
    [Fact]
    public void AsSpan_ReturnsCorrectSpan()
    {
        var list = new BoundedList<int>(5);
        list.Add(1);
        list.Add(2);
        list.Add(3);

        var span = list.AsSpan();

        span.Length.Should().Be(3);
        span.ToArray().Should().Equal(1, 2, 3);
    }

    /// <summary>
    /// Tests that AsSpanMutable returns a writable span.
    /// The span should allow modification of list elements.
    /// </summary>
    [Fact]
    public void AsSpanMutable_AllowsModification()
    {
        var list = new BoundedList<int>(5);
        list.Add(1);
        list.Add(2);
        list.Add(3);

        var span = list.AsSpanMutable();
        span[1] = 20;

        list[1].Should().Be(20);
    }

    /// <summary>
    /// Tests that CopyTo copies items to destination span.
    /// The method should copy all current items to the provided span.
    /// </summary>
    [Fact]
    public void CopyTo_Span_CopiesItemsCorrectly()
    {
        var list = new BoundedList<int>(5);
        list.Add(1);
        list.Add(2);
        list.Add(3);

        var destination = new int[5];
        list.CopyTo(destination.AsSpan());

        destination.Take(3).Should().Equal(1, 2, 3);
    }

    /// <summary>
    /// Tests that CopyTo copies items to array at specified index.
    /// The method should copy items starting at the given array index.
    /// </summary>
    [Fact]
    public void CopyTo_Array_CopiesItemsAtIndex()
    {
        var list = new BoundedList<int>(5);
        list.Add(1);
        list.Add(2);
        list.Add(3);

        var array = new int[10];
        list.CopyTo(array, 2);

        array[2].Should().Be(1);
        array[3].Should().Be(2);
        array[4].Should().Be(3);
    }

    /// <summary>
    /// Tests that CopyTo throws exception for null array.
    /// The method should validate array parameter is not null.
    /// </summary>
    [Fact]
    public void CopyTo_NullArray_ThrowsArgumentNullException()
    {
        var list = new BoundedList<int>(5);
        list.Add(1);

        var act = () => list.CopyTo(null!, 0);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("array");
    }

    /// <summary>
    /// Tests that CopyTo throws exception for invalid array index.
    /// The method should validate the destination has enough space.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(8)]
    [InlineData(10)]
    public void CopyTo_InvalidArrayIndex_ThrowsArgumentException(int index)
    {
        var list = new BoundedList<int>(5);
        list.Add(1);
        list.Add(2);
        list.Add(3);

        var array = new int[10];
        var act = () => list.CopyTo(array, index);

        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Tests that ForEachRef applies action to all items by reference.
    /// The method should allow modification of items through reference action.
    /// </summary>
    [Fact]
    public void ForEachRef_ModifiesItemsByReference()
    {
        var list = new BoundedList<int>(5);
        list.Add(1);
        list.Add(2);
        list.Add(3);

        list.ForEachRef((ref int item) => item *= 2);

        list.Should().Equal(2, 4, 6);
    }

    /// <summary>
    /// Tests that enumerator iterates through all items.
    /// The enumerator should visit each item in order.
    /// </summary>
    [Fact]
    public void GetEnumerator_IteratesAllItems()
    {
        var list = new BoundedList<int>(5);
        list.Add(1);
        list.Add(2);
        list.Add(3);

        var items = new List<int>();
        foreach (var item in list)
        {
            items.Add(item);
        }

        items.Should().Equal(1, 2, 3);
    }

    /// <summary>
    /// Tests that enumerator works with LINQ methods.
    /// The enumerator should be compatible with standard LINQ operations.
    /// </summary>
    [Fact]
    public void Enumerator_WorksWithLinq()
    {
        var list = new BoundedList<int>(5);
        list.Add(1);
        list.Add(2);
        list.Add(3);

        var sum = list.Sum();
        var doubled = list.Select(x => x * 2).ToList();

        sum.Should().Be(6);
        doubled.Should().Equal(2, 4, 6);
    }

    /// <summary>
    /// Tests that CreateWithArrayPool creates pooled instance.
    /// The method should create a list that uses array pooling.
    /// </summary>
    [Fact]
    public void CreateWithArrayPool_CreatesPooledInstance()
    {
        using var list = BoundedList<int>.CreateWithArrayPool(10);

        list.Capacity.Should().BeGreaterThanOrEqualTo(10);
        list.Count.Should().Be(0);
        list.Add(1);
        list.Add(2);
        list.Should().Equal(1, 2);
    }

    /// <summary>
    /// Tests that Rent returns a reusable instance from pool.
    /// The method should provide an instance that can be returned to pool.
    /// </summary>
    [Fact]
    public void Rent_Return_ReuseInstance()
    {
        var list1 = BoundedList<int>.Rent(10);
        list1.Add(1);
        list1.Add(2);
        list1.Return();

        var list2 = BoundedList<int>.Rent(10);
        
        list2.Count.Should().Be(0);
        list2.Capacity.Should().BeGreaterThanOrEqualTo(10);
    }

    /// <summary>
    /// Tests that IsFull property correctly indicates when list is at capacity.
    /// The property should return true only when count equals capacity.
    /// </summary>
    [Fact]
    public void IsFull_ReflectsCapacityStatus()
    {
        var list = new BoundedList<int>(2);

        list.IsFull.Should().BeFalse();
        
        list.Add(1);
        list.IsFull.Should().BeFalse();
        
        list.Add(2);
        list.IsFull.Should().BeTrue();
    }

    /// <summary>
    /// Tests that RemainingCapacity correctly calculates available space.
    /// The property should return the difference between capacity and count.
    /// </summary>
    [Fact]
    public void RemainingCapacity_ReturnsCorrectValue()
    {
        var list = new BoundedList<int>(5);

        list.RemainingCapacity.Should().Be(5);
        
        list.Add(1);
        list.RemainingCapacity.Should().Be(4);
        
        list.Add(2);
        list.Add(3);
        list.RemainingCapacity.Should().Be(2);
    }

    /// <summary>
    /// Tests that Dispose properly cleans up resources.
    /// The method should return pooled arrays when using array pooling.
    /// </summary>
    [Fact]
    public void Dispose_CleansUpResources()
    {
        var list = BoundedList<int>.CreateWithArrayPool(10);
        list.Add(1);
        list.Add(2);

        list.Dispose();

        // After disposal, the list should not throw but behavior is undefined
        // This test mainly ensures Dispose doesn't throw
        list.Dispose(); // Double dispose should not throw
    }

    /// <summary>
    /// Tests that list handles null values correctly for reference types.
    /// The list should accept and manage null values without issues.
    /// </summary>
    [Fact]
    public void NullHandling_ForReferenceTypes_WorksCorrectly()
    {
        var list = new BoundedList<string>(5);

        list.Add(null!);
        list.Add("test");
        list.Add(null!);

        list.Count.Should().Be(3);
        list[0].Should().BeNull();
        list[1].Should().Be("test");
        list[2].Should().BeNull();

        list.Contains(null!).Should().BeTrue();
        list.IndexOf(null!).Should().Be(0);
        list.Remove(null!).Should().BeTrue();
        list.Count.Should().Be(2);
    }

    /// <summary>
    /// Tests edge case of single-element list operations.
    /// The list should handle single element scenarios correctly.
    /// </summary>
    [Fact]
    public void SingleElementList_HandlesOperationsCorrectly()
    {
        var list = new BoundedList<int>(1);

        list.TryAdd(10).Should().BeTrue();
        list.IsFull.Should().BeTrue();
        list.TryAdd(20).Should().BeFalse();

        list.RemoveLast().Should().Be(10);
        list.Count.Should().Be(0);
        
        list.Add(30);
        list.RemoveAt(0);
        list.Count.Should().Be(0);
    }

    /// <summary>
    /// Tests that empty list operations behave correctly.
    /// The list should handle empty state operations appropriately.
    /// </summary>
    [Fact]
    public void EmptyList_HandlesOperationsCorrectly()
    {
        var list = new BoundedList<int>(5);

        list.Count.Should().Be(0);
        list.Contains(1).Should().BeFalse();
        list.IndexOf(1).Should().Be(-1);
        list.Remove(1).Should().BeFalse();
        
        list.TryRemoveLast(out var item).Should().BeFalse();
        
        var act = () => list.RemoveLast();
        act.Should().Throw<InvalidOperationException>();

        list.Clear(); // Should not throw on empty list
        list.Count.Should().Be(0);
    }
}
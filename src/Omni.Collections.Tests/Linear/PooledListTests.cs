using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Omni.Collections.Linear;
using Xunit;

namespace Omni.Collections.Tests.Linear;

public class PooledListTests
{
    /// <summary>
    /// Tests that a PooledList can be constructed with valid capacity.
    /// The list should have at least the specified capacity and zero count initially.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(16)]
    [InlineData(100)]
    public void Constructor_WithValidCapacity_CreatesListWithCorrectCapacity(int capacity)
    {
        using var list = new PooledList<int>(capacity);

        list.Capacity.Should().BeGreaterThanOrEqualTo(capacity);
        list.Count.Should().Be(0);
        list.IsReadOnly.Should().BeFalse();
    }

    /// <summary>
    /// Tests that constructor uses default capacity when not specified.
    /// The list should be created with a default capacity of 16.
    /// </summary>
    [Fact]
    public void Constructor_WithoutCapacity_UsesDefaultCapacity()
    {
        using var list = new PooledList<int>();

        list.Capacity.Should().BeGreaterThanOrEqualTo(16);
        list.Count.Should().Be(0);
    }

    /// <summary>
    /// Tests that constructing with invalid capacity throws exception.
    /// The constructor should reject zero or negative capacity values.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_WithInvalidCapacity_ThrowsArgumentOutOfRangeException(int capacity)
    {
        var act = () => new PooledList<int>(capacity);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("initialCapacity");
    }

    /// <summary>
    /// Tests that Add method successfully adds items to the list.
    /// Items should be added in order and count should update correctly.
    /// </summary>
    [Fact]
    public void Add_AddsItemsInOrder()
    {
        using var list = new PooledList<int>();

        list.Add(1);
        list.Add(2);
        list.Add(3);

        list.Count.Should().Be(3);
        list[0].Should().Be(1);
        list[1].Should().Be(2);
        list[2].Should().Be(3);
    }

    /// <summary>
    /// Tests that Add triggers resize when capacity is exceeded.
    /// The list should automatically grow to accommodate new items.
    /// </summary>
    [Fact]
    public void Add_WhenFull_TriggersResize()
    {
        using var list = new PooledList<int>(2);

        list.Add(1);
        list.Add(2);
        list.Add(3); // Should trigger resize
        list.Add(4);

        list.Count.Should().Be(4);
        list.Capacity.Should().BeGreaterThan(2);
        list.Should().Equal(1, 2, 3, 4);
    }

    /// <summary>
    /// Tests that AddRange adds multiple items from ReadOnlySpan.
    /// The method should efficiently add all items from the span.
    /// </summary>
    [Fact]
    public void AddRange_WithReadOnlySpan_AddsAllItems()
    {
        using var list = new PooledList<int>();
        var items = new[] { 1, 2, 3, 4, 5 };

        list.AddRange(items.AsSpan());

        list.Count.Should().Be(5);
        list.Should().Equal(items);
    }

    /// <summary>
    /// Tests that AddRange with IEnumerable adds all items.
    /// The method should add all items from the enumerable.
    /// </summary>
    [Fact]
    public void AddRange_WithIEnumerable_AddsAllItems()
    {
        using var list = new PooledList<int>();
        var items = Enumerable.Range(1, 5);

        list.AddRange(items);

        list.Count.Should().Be(5);
        list.Should().Equal(1, 2, 3, 4, 5);
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
        using var list = new PooledList<int>();
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
        using var list = new PooledList<int>();
        list.Add(1);
        list.Add(2);

        var act = () => list.Insert(index, 3);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Tests that Remove successfully removes first occurrence of item.
    /// The method should return true and remove only the first matching item.
    /// </summary>
    [Fact]
    public void Remove_ExistingItem_RemovesFirstOccurrenceAndReturnsTrue()
    {
        using var list = new PooledList<int>();
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
        using var list = new PooledList<int>();
        list.Add(1);
        list.Add(2);

        var result = list.Remove(3);

        result.Should().BeFalse();
        list.Count.Should().Be(2);
        list.Should().Equal(1, 2);
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
        using var list = new PooledList<int>();
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
        using var list = new PooledList<int>();
        list.Add(1);
        list.Add(2);

        var act = () => list.RemoveAt(index);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Tests that indexer get returns correct value.
    /// The indexer should provide direct access to elements.
    /// </summary>
    [Fact]
    public void Indexer_Get_ReturnsCorrectValue()
    {
        using var list = new PooledList<int>();
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
        using var list = new PooledList<int>();
        list.Add(10);
        list.Add(20);
        list.Add(30);

        list[1] = 25;

        list[1].Should().Be(25);
        list.Should().Equal(10, 25, 30);
    }

#if DEBUG
    /// <summary>
    /// Tests that indexer throws exception for invalid index in debug mode.
    /// The indexer should reject negative or out-of-bounds indices.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(100)]
    public void Indexer_InvalidIndex_ThrowsIndexOutOfRangeException(int index)
    {
        using var list = new PooledList<int>();
        list.Add(1);
        list.Add(2);

        var getAct = () => { var x = list[index]; };
        var setAct = () => list[index] = 3;

        getAct.Should().Throw<IndexOutOfRangeException>();
        setAct.Should().Throw<IndexOutOfRangeException>();
    }
#endif

    /// <summary>
    /// Tests that GetRef returns a reference to the element.
    /// The method should allow direct modification through reference.
    /// </summary>
    [Fact]
    public void GetRef_ValidIndex_ReturnsReference()
    {
        using var list = new PooledList<int>();
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
        using var list = new PooledList<int>();
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
        using var list = new PooledList<int>();
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
        using var list = new PooledList<int>();
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
        using var list = new PooledList<int>();
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
        using var list = new PooledList<int>();
        list.Add(1);
        list.Add(2);
        list.Add(3);

        list.Clear();

        list.Count.Should().Be(0);
        list.Contains(1).Should().BeFalse();
    }

    /// <summary>
    /// Tests that CopyTo copies items to array at specified index.
    /// The method should copy items starting at the given array index.
    /// </summary>
    [Fact]
    public void CopyTo_Array_CopiesItemsAtIndex()
    {
        using var list = new PooledList<int>();
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
        using var list = new PooledList<int>();
        list.Add(1);

        var act = () => list.CopyTo(null!, 0);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("array");
    }

    /// <summary>
    /// Tests that ToArray creates a new array with all items.
    /// The method should return a copy of all list items.
    /// </summary>
    [Fact]
    public void ToArray_CreatesArrayWithAllItems()
    {
        using var list = new PooledList<int>();
        list.Add(1);
        list.Add(2);
        list.Add(3);

        var array = list.ToArray();

        array.Should().Equal(1, 2, 3);
        array.Length.Should().Be(3);
    }

    /// <summary>
    /// Tests that AsSpan returns a span of current items.
    /// The span should contain only the active elements.
    /// </summary>
    [Fact]
    public void AsSpan_ReturnsCorrectSpan()
    {
        using var list = new PooledList<int>();
        list.Add(1);
        list.Add(2);
        list.Add(3);

        var span = list.AsSpan();

        span.Length.Should().Be(3);
        span.ToArray().Should().Equal(1, 2, 3);
    }

    /// <summary>
    /// Tests that enumerator iterates through all items.
    /// The enumerator should visit each item in order.
    /// </summary>
    [Fact]
    public void GetEnumerator_IteratesAllItems()
    {
        using var list = new PooledList<int>();
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
        using var list = new PooledList<int>();
        list.Add(1);
        list.Add(2);
        list.Add(3);

        var sum = list.Sum();
        var doubled = list.Select(x => x * 2).ToList();

        sum.Should().Be(6);
        doubled.Should().Equal(2, 4, 6);
    }

    /// <summary>
    /// Tests that CreateWithArrayPool creates a pooled instance.
    /// The method should create a list that uses array pooling.
    /// </summary>
    [Fact]
    public void CreateWithArrayPool_CreatesPooledInstance()
    {
        using var list = PooledList<int>.CreateWithArrayPool(10);

        list.Capacity.Should().BeGreaterThanOrEqualTo(10);
        list.Count.Should().Be(0);
        
        list.Add(1);
        list.Add(2);
        list.Should().Equal(1, 2);
    }

    /// <summary>
    /// Tests that Dispose properly cleans up resources.
    /// The method should return pooled arrays and prevent further operations.
    /// </summary>
    [Fact]
    public void Dispose_CleansUpResources()
    {
        var list = PooledList<int>.CreateWithArrayPool(10);
        list.Add(1);
        list.Add(2);

        list.Dispose();
        
#if DEBUG
        var act = () => list.Add(3);
        act.Should().Throw<ObjectDisposedException>();
#endif
        
        list.Dispose(); // Double dispose should not throw
    }

    /// <summary>
    /// Tests that list handles null values correctly for reference types.
    /// The list should accept and manage null values without issues.
    /// </summary>
    [Fact]
    public void NullHandling_ForReferenceTypes_WorksCorrectly()
    {
        using var list = new PooledList<string>();

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
    /// Tests empty list edge cases.
    /// The list should handle empty state operations appropriately.
    /// </summary>
    [Fact]
    public void EmptyList_HandlesOperationsCorrectly()
    {
        using var list = new PooledList<int>();

        list.Count.Should().Be(0);
        list.Contains(1).Should().BeFalse();
        list.IndexOf(1).Should().Be(-1);
        list.Remove(1).Should().BeFalse();
        
        list.Clear(); // Should not throw on empty list
        list.Count.Should().Be(0);
        
        var array = list.ToArray();
        array.Should().BeEmpty();
    }

    /// <summary>
    /// Tests list with large number of items.
    /// The list should handle many items efficiently with proper resizing.
    /// </summary>
    [Fact]
    public void LargeList_HandlesMannyItemsEfficiently()
    {
        using var list = new PooledList<int>(2);
        const int itemCount = 1000;

        for (int i = 0; i < itemCount; i++)
        {
            list.Add(i);
        }

        list.Count.Should().Be(itemCount);
        list.Capacity.Should().BeGreaterThanOrEqualTo(itemCount);

        for (int i = 0; i < itemCount; i++)
        {
            list[i].Should().Be(i);
        }
    }
}
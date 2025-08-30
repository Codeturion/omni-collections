using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Omni.Collections.Linear;
using Xunit;

namespace Omni.Collections.Tests.Linear;

public class MinHeapTests
{
    /// <summary>
    /// Tests that a MinHeap can be constructed with valid capacity.
    /// The heap should have the specified capacity and zero count initially.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(16)]
    [InlineData(100)]
    public void Constructor_WithValidCapacity_CreatesHeapWithCorrectCapacity(int capacity)
    {
        var heap = new MinHeap<int>(capacity);

        heap.Capacity.Should().BeGreaterThanOrEqualTo(capacity);
        heap.Count.Should().Be(0);
        heap.IsEmpty.Should().BeTrue();
    }

    /// <summary>
    /// Tests that constructor uses default capacity when not specified.
    /// The heap should be created with a default capacity of 16.
    /// </summary>
    [Fact]
    public void Constructor_WithoutCapacity_UsesDefaultCapacity()
    {
        var heap = new MinHeap<int>();

        heap.Capacity.Should().BeGreaterThanOrEqualTo(16);
        heap.Count.Should().Be(0);
        heap.IsEmpty.Should().BeTrue();
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
        var act = () => new MinHeap<int>(capacity);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("initialCapacity");
    }

    /// <summary>
    /// Tests that a MinHeap can be constructed with initial items.
    /// The heap should be properly heapified with all items.
    /// </summary>
    [Fact]
    public void Constructor_WithInitialItems_CreatesProperlyHeapifiedHeap()
    {
        var items = new[] { 3, 1, 4, 1, 5, 9, 2, 6 };
        var heap = new MinHeap<int>(items);

        heap.Count.Should().Be(items.Length);
        heap.PeekMin().Should().Be(1); // Minimum element should be at root
    }

    /// <summary>
    /// Tests that Insert adds items maintaining min-heap property.
    /// The minimum element should always be at the root.
    /// </summary>
    [Fact]
    public void Insert_MaintainsMinHeapProperty()
    {
        var heap = new MinHeap<int>();

        heap.Insert(5);
        heap.PeekMin().Should().Be(5);

        heap.Insert(3);
        heap.PeekMin().Should().Be(3);

        heap.Insert(7);
        heap.PeekMin().Should().Be(3);

        heap.Insert(1);
        heap.PeekMin().Should().Be(1);

        heap.Insert(10);
        heap.PeekMin().Should().Be(1);

        heap.Count.Should().Be(5);
    }

    /// <summary>
    /// Tests that Insert triggers resize when capacity is exceeded.
    /// The heap should automatically grow to accommodate new items.
    /// </summary>
    [Fact]
    public void Insert_WhenFull_TriggersResize()
    {
        var heap = new MinHeap<int>(2);

        heap.Insert(3);
        heap.Insert(2);
        heap.Insert(1); // Should trigger resize
        heap.Insert(4);

        heap.Count.Should().Be(4);
        heap.Capacity.Should().BeGreaterThan(2);
        heap.PeekMin().Should().Be(1);
    }

    /// <summary>
    /// Tests that ExtractMin removes and returns the minimum element.
    /// The method should maintain heap property after extraction.
    /// </summary>
    [Fact]
    public void ExtractMin_RemovesAndReturnsMinimum()
    {
        var heap = new MinHeap<int>();
        heap.Insert(5);
        heap.Insert(3);
        heap.Insert(7);
        heap.Insert(1);
        heap.Insert(10);

        var min1 = heap.ExtractMin();
        min1.Should().Be(1);
        heap.Count.Should().Be(4);

        var min2 = heap.ExtractMin();
        min2.Should().Be(3);
        heap.Count.Should().Be(3);

        var min3 = heap.ExtractMin();
        min3.Should().Be(5);
        heap.Count.Should().Be(2);
    }

    /// <summary>
    /// Tests that ExtractMin throws exception when heap is empty.
    /// The method should validate the heap is not empty.
    /// </summary>
    [Fact]
    public void ExtractMin_WhenEmpty_ThrowsInvalidOperationException()
    {
        var heap = new MinHeap<int>();

        var act = () => heap.ExtractMin();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*empty*");
    }

    /// <summary>
    /// Tests that TryExtractMin successfully extracts when not empty.
    /// The method should return true and output the minimum element.
    /// </summary>
    [Fact]
    public void TryExtractMin_WhenNotEmpty_ReturnsTrueAndExtractsMin()
    {
        var heap = new MinHeap<int>();
        heap.Insert(5);
        heap.Insert(1);
        heap.Insert(3);

        var result = heap.TryExtractMin(out var min);

        result.Should().BeTrue();
        min.Should().Be(1);
        heap.Count.Should().Be(2);
        heap.PeekMin().Should().Be(3);
    }

    /// <summary>
    /// Tests that TryExtractMin returns false when heap is empty.
    /// The method should return false without throwing.
    /// </summary>
    [Fact]
    public void TryExtractMin_WhenEmpty_ReturnsFalse()
    {
        var heap = new MinHeap<int>();

        var result = heap.TryExtractMin(out var min);

        result.Should().BeFalse();
        min.Should().Be(0);
        heap.Count.Should().Be(0);
    }

    /// <summary>
    /// Tests that Peek returns the minimum without removing it.
    /// The method should return the root element without modification.
    /// </summary>
    [Fact]
    public void Peek_ReturnsMinimumWithoutRemoving()
    {
        var heap = new MinHeap<int>();
        heap.Insert(5);
        heap.Insert(1);
        heap.Insert(3);

        var peeked = heap.PeekMin();

        peeked.Should().Be(1);
        heap.Count.Should().Be(3);
        heap.ExtractMin().Should().Be(1); // Verify it wasn't removed
    }

    /// <summary>
    /// Tests that Peek throws exception when heap is empty.
    /// The method should validate the heap is not empty.
    /// </summary>
    [Fact]
    public void Peek_WhenEmpty_ThrowsInvalidOperationException()
    {
        var heap = new MinHeap<int>();

        var act = () => heap.PeekMin();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*empty*");
    }

    /// <summary>
    /// Tests that TryPeek returns true and peeks when not empty.
    /// The method should return true and output the minimum without removing.
    /// </summary>
    [Fact]
    public void TryPeek_WhenNotEmpty_ReturnsTrueAndPeeksMin()
    {
        var heap = new MinHeap<int>();
        heap.Insert(5);
        heap.Insert(1);

        var result = heap.TryPeekMin(out var min);

        result.Should().BeTrue();
        min.Should().Be(1);
        heap.Count.Should().Be(2);
    }

    /// <summary>
    /// Tests that TryPeek returns false when heap is empty.
    /// The method should return false without throwing.
    /// </summary>
    [Fact]
    public void TryPeek_WhenEmpty_ReturnsFalse()
    {
        var heap = new MinHeap<int>();

        var result = heap.TryPeekMin(out var min);

        result.Should().BeFalse();
        min.Should().Be(0);
    }

    /// <summary>
    /// Tests that Clear removes all items from the heap.
    /// The method should reset the heap to empty state.
    /// </summary>
    [Fact]
    public void Clear_RemovesAllItems()
    {
        var heap = new MinHeap<int>();
        heap.Insert(5);
        heap.Insert(1);
        heap.Insert(3);

        heap.Clear();

        heap.Count.Should().Be(0);
        heap.IsEmpty.Should().BeTrue();
        heap.TryExtractMin(out _).Should().BeFalse();
    }

    /// <summary>
    /// Tests that extracting all items produces ascending order.
    /// The items should be extracted from minimum to maximum.
    /// </summary>
    [Fact]
    public void ExtractAll_ProducesAscendingOrder()
    {
        var heap = new MinHeap<int>();
        var items = new[] { 3, 1, 4, 1, 5, 9, 2, 6 };
        foreach (var item in items)
        {
            heap.Insert(item);
        }

        var extracted = new List<int>();
        while (!heap.IsEmpty)
        {
            extracted.Add(heap.ExtractMin());
        }

        extracted.Should().BeInAscendingOrder();
        extracted.Count.Should().Be(items.Length);
        extracted[0].Should().Be(1);
    }

    /// <summary>
    /// Tests that enumerator iterates through items in heap order.
    /// The enumerator should visit all items without specific order guarantee.
    /// </summary>
    [Fact]
    public void GetEnumerator_IteratesAllItems()
    {
        var heap = new MinHeap<int>();
        var items = new[] { 5, 3, 7, 1, 9 };
        foreach (var item in items)
        {
            heap.Insert(item);
        }

        var enumerated = new List<int>();
        foreach (var item in heap)
        {
            enumerated.Add(item);
        }

        enumerated.Should().HaveCount(5);
        enumerated.Should().Contain(items);
    }

    /// <summary>
    /// Tests that enumerator works with LINQ methods.
    /// The enumerator should be compatible with standard LINQ operations.
    /// </summary>
    [Fact]
    public void Enumerator_WorksWithLinq()
    {
        var heap = new MinHeap<int>();
        heap.Insert(5);
        heap.Insert(3);
        heap.Insert(7);

        var sum = heap.Sum();
        var count = heap.Count();

        sum.Should().Be(15);
        count.Should().Be(3);
    }

    /// <summary>
    /// Tests heap with duplicate values.
    /// The heap should correctly handle multiple equal values.
    /// </summary>
    [Fact]
    public void Heap_WithDuplicates_HandlesCorrectly()
    {
        var heap = new MinHeap<int>();
        heap.Insert(5);
        heap.Insert(5);
        heap.Insert(3);
        heap.Insert(5);
        heap.Insert(7);

        heap.ExtractMin().Should().Be(3);
        heap.ExtractMin().Should().Be(5);
        heap.ExtractMin().Should().Be(5);
        heap.ExtractMin().Should().Be(5);
        heap.ExtractMin().Should().Be(7);
        heap.IsEmpty.Should().BeTrue();
    }

    /// <summary>
    /// Tests that CreateWithArrayPool creates a pooled instance.
    /// The method should create a heap that uses array pooling.
    /// </summary>
    [Fact]
    public void CreateWithArrayPool_CreatesPooledInstance()
    {
        using var heap = MinHeap<int>.CreateWithArrayPool(10);

        heap.Capacity.Should().BeGreaterThanOrEqualTo(10);
        heap.Count.Should().Be(0);
        
        heap.Insert(5);
        heap.Insert(1);
        heap.PeekMin().Should().Be(1);
    }

    /// <summary>
    /// Tests that Rent returns a reusable instance from pool.
    /// The method should provide an instance that can be returned to pool.
    /// </summary>
    [Fact]
    public void Rent_Return_ReuseInstance()
    {
        var heap1 = MinHeap<int>.Rent(10);
        heap1.Insert(5);
        heap1.Insert(1);
        heap1.Return();

        var heap2 = MinHeap<int>.Rent(10);
        
        heap2.Count.Should().Be(0);
        heap2.IsEmpty.Should().BeTrue();
    }

    /// <summary>
    /// Tests that Dispose properly cleans up resources.
    /// The method should return pooled arrays when using array pooling.
    /// </summary>
    [Fact]
    public void Dispose_CleansUpResources()
    {
        var heap = MinHeap<int>.CreateWithArrayPool(10);
        heap.Insert(5);
        heap.Insert(1);

        heap.Dispose();
        heap.Dispose(); // Double dispose should not throw
    }

    /// <summary>
    /// Tests heap with custom comparable objects.
    /// The heap should correctly order objects implementing IComparable.
    /// </summary>
    [Fact]
    public void Heap_WithCustomComparable_OrdersCorrectly()
    {
        var heap = new MinHeap<TestComparable>();
        heap.Insert(new TestComparable(5));
        heap.Insert(new TestComparable(3));
        heap.Insert(new TestComparable(7));

        heap.ExtractMin().Value.Should().Be(3);
        heap.ExtractMin().Value.Should().Be(5);
        heap.ExtractMin().Value.Should().Be(7);
    }

    /// <summary>
    /// Tests empty heap edge cases.
    /// The heap should handle empty state operations appropriately.
    /// </summary>
    [Fact]
    public void EmptyHeap_HandlesOperationsCorrectly()
    {
        var heap = new MinHeap<int>();

        heap.IsEmpty.Should().BeTrue();
        heap.Count.Should().Be(0);
        heap.TryExtractMin(out _).Should().BeFalse();
        heap.TryPeekMin(out _).Should().BeFalse();
        
        heap.Clear(); // Should not throw on empty heap
        heap.IsEmpty.Should().BeTrue();
    }

    /// <summary>
    /// Tests single element heap operations.
    /// The heap should handle single element scenarios correctly.
    /// </summary>
    [Fact]
    public void SingleElementHeap_HandlesOperationsCorrectly()
    {
        var heap = new MinHeap<int>();

        heap.Insert(42);
        heap.Count.Should().Be(1);
        heap.IsEmpty.Should().BeFalse();
        heap.PeekMin().Should().Be(42);
        
        var extracted = heap.ExtractMin();
        extracted.Should().Be(42);
        heap.IsEmpty.Should().BeTrue();
    }

    /// <summary>
    /// Tests heap with large number of items.
    /// The heap should efficiently handle many items with proper resizing.
    /// </summary>
    [Fact]
    public void LargeHeap_HandlesMannyItemsEfficiently()
    {
        var heap = new MinHeap<int>(2);
        const int itemCount = 1000;
        var random = new Random(42);

        // Insert many random items
        var items = new List<int>();
        for (int i = 0; i < itemCount; i++)
        {
            var item = random.Next(0, 10000);
            items.Add(item);
            heap.Insert(item);
        }

        heap.Count.Should().Be(itemCount);

        // Extract all items and verify they come out in ascending order
        var extracted = new List<int>();
        while (!heap.IsEmpty)
        {
            extracted.Add(heap.ExtractMin());
        }

        extracted.Should().BeInAscendingOrder();
        extracted.Should().HaveCount(itemCount);
    }

    /// <summary>
    /// Tests that min heap can be used for priority queue scenarios.
    /// The heap should correctly prioritize items with lower values.
    /// </summary>
    [Fact]
    public void MinHeap_AsPriorityQueue_ProcessesInPriorityOrder()
    {
        var heap = new MinHeap<PriorityItem>();
        
        heap.Insert(new PriorityItem(3, "Low"));
        heap.Insert(new PriorityItem(1, "Critical"));
        heap.Insert(new PriorityItem(2, "High"));
        heap.Insert(new PriorityItem(4, "Normal"));

        heap.ExtractMin().Name.Should().Be("Critical");
        heap.ExtractMin().Name.Should().Be("High");
        heap.ExtractMin().Name.Should().Be("Low");
        heap.ExtractMin().Name.Should().Be("Normal");
    }

    private class TestComparable : IComparable<TestComparable>
    {
        public int Value { get; }

        public TestComparable(int value)
        {
            Value = value;
        }

        public int CompareTo(TestComparable? other)
        {
            if (other == null) return 1;
            return Value.CompareTo(other.Value);
        }
    }

    private class PriorityItem : IComparable<PriorityItem>
    {
        public int Priority { get; }
        public string Name { get; }

        public PriorityItem(int priority, string name)
        {
            Priority = priority;
            Name = name;
        }

        public int CompareTo(PriorityItem? other)
        {
            if (other == null) return 1;
            return Priority.CompareTo(other.Priority);
        }
    }
}
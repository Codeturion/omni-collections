using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Omni.Collections.Linear;
using Xunit;

namespace Omni.Collections.Tests.Linear;

public class MaxHeapTests
{
    /// <summary>
    /// Tests that a MaxHeap can be constructed with valid capacity.
    /// The heap should have the specified capacity and zero count initially.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(16)]
    [InlineData(100)]
    public void Constructor_WithValidCapacity_CreatesHeapWithCorrectCapacity(int capacity)
    {
        var heap = new MaxHeap<int>(capacity);

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
        var heap = new MaxHeap<int>();

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
        var act = () => new MaxHeap<int>(capacity);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("initialCapacity");
    }

    /// <summary>
    /// Tests that a MaxHeap can be constructed with initial items.
    /// The heap should be properly heapified with all items.
    /// </summary>
    [Fact]
    public void Constructor_WithInitialItems_CreatesProperlyHeapifiedHeap()
    {
        var items = new[] { 3, 1, 4, 1, 5, 9, 2, 6 };
        var heap = new MaxHeap<int>(items);

        heap.Count.Should().Be(items.Length);
        heap.PeekMax().Should().Be(9); // Maximum element should be at root
    }

    /// <summary>
    /// Tests that Insert adds items maintaining max-heap property.
    /// The maximum element should always be at the root.
    /// </summary>
    [Fact]
    public void Insert_MaintainsMaxHeapProperty()
    {
        var heap = new MaxHeap<int>();

        heap.Insert(5);
        heap.PeekMax().Should().Be(5);

        heap.Insert(3);
        heap.PeekMax().Should().Be(5);

        heap.Insert(7);
        heap.PeekMax().Should().Be(7);

        heap.Insert(1);
        heap.PeekMax().Should().Be(7);

        heap.Insert(10);
        heap.PeekMax().Should().Be(10);

        heap.Count.Should().Be(5);
    }

    /// <summary>
    /// Tests that Insert triggers resize when capacity is exceeded.
    /// The heap should automatically grow to accommodate new items.
    /// </summary>
    [Fact]
    public void Insert_WhenFull_TriggersResize()
    {
        var heap = new MaxHeap<int>(2);

        heap.Insert(1);
        heap.Insert(2);
        heap.Insert(3); // Should trigger resize
        heap.Insert(4);

        heap.Count.Should().Be(4);
        heap.Capacity.Should().BeGreaterThan(2);
        heap.PeekMax().Should().Be(4);
    }

    /// <summary>
    /// Tests that ExtractMax removes and returns the maximum element.
    /// The method should maintain heap property after extraction.
    /// </summary>
    [Fact]
    public void ExtractMax_RemovesAndReturnsMaximum()
    {
        var heap = new MaxHeap<int>();
        heap.Insert(5);
        heap.Insert(3);
        heap.Insert(7);
        heap.Insert(1);
        heap.Insert(10);

        var max1 = heap.ExtractMax();
        max1.Should().Be(10);
        heap.Count.Should().Be(4);

        var max2 = heap.ExtractMax();
        max2.Should().Be(7);
        heap.Count.Should().Be(3);

        var max3 = heap.ExtractMax();
        max3.Should().Be(5);
        heap.Count.Should().Be(2);
    }

    /// <summary>
    /// Tests that ExtractMax throws exception when heap is empty.
    /// The method should validate the heap is not empty.
    /// </summary>
    [Fact]
    public void ExtractMax_WhenEmpty_ThrowsInvalidOperationException()
    {
        var heap = new MaxHeap<int>();

        var act = () => heap.ExtractMax();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*empty*");
    }

    /// <summary>
    /// Tests that TryExtractMax successfully extracts when not empty.
    /// The method should return true and output the maximum element.
    /// </summary>
    [Fact]
    public void TryExtractMax_WhenNotEmpty_ReturnsTrueAndExtractsMax()
    {
        var heap = new MaxHeap<int>();
        heap.Insert(5);
        heap.Insert(10);
        heap.Insert(3);

        var result = heap.TryExtractMax(out var max);

        result.Should().BeTrue();
        max.Should().Be(10);
        heap.Count.Should().Be(2);
        heap.PeekMax().Should().Be(5);
    }

    /// <summary>
    /// Tests that TryExtractMax returns false when heap is empty.
    /// The method should return false without throwing.
    /// </summary>
    [Fact]
    public void TryExtractMax_WhenEmpty_ReturnsFalse()
    {
        var heap = new MaxHeap<int>();

        var result = heap.TryExtractMax(out var max);

        result.Should().BeFalse();
        max.Should().Be(0);
        heap.Count.Should().Be(0);
    }

    /// <summary>
    /// Tests that Peek returns the maximum without removing it.
    /// The method should return the root element without modification.
    /// </summary>
    [Fact]
    public void Peek_ReturnsMaximumWithoutRemoving()
    {
        var heap = new MaxHeap<int>();
        heap.Insert(5);
        heap.Insert(10);
        heap.Insert(3);

        var peeked = heap.PeekMax();

        peeked.Should().Be(10);
        heap.Count.Should().Be(3);
        heap.ExtractMax().Should().Be(10); // Verify it wasn't removed
    }

    /// <summary>
    /// Tests that Peek throws exception when heap is empty.
    /// The method should validate the heap is not empty.
    /// </summary>
    [Fact]
    public void Peek_WhenEmpty_ThrowsInvalidOperationException()
    {
        var heap = new MaxHeap<int>();

        var act = () => heap.PeekMax();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*empty*");
    }

    /// <summary>
    /// Tests that TryPeek returns true and peeks when not empty.
    /// The method should return true and output the maximum without removing.
    /// </summary>
    [Fact]
    public void TryPeek_WhenNotEmpty_ReturnsTrueAndPeeksMax()
    {
        var heap = new MaxHeap<int>();
        heap.Insert(5);
        heap.Insert(10);

        var result = heap.TryPeekMax(out var max);

        result.Should().BeTrue();
        max.Should().Be(10);
        heap.Count.Should().Be(2);
    }

    /// <summary>
    /// Tests that TryPeek returns false when heap is empty.
    /// The method should return false without throwing.
    /// </summary>
    [Fact]
    public void TryPeek_WhenEmpty_ReturnsFalse()
    {
        var heap = new MaxHeap<int>();

        var result = heap.TryPeekMax(out var max);

        result.Should().BeFalse();
        max.Should().Be(0);
    }

    /// <summary>
    /// Tests that Clear removes all items from the heap.
    /// The method should reset the heap to empty state.
    /// </summary>
    [Fact]
    public void Clear_RemovesAllItems()
    {
        var heap = new MaxHeap<int>();
        heap.Insert(5);
        heap.Insert(10);
        heap.Insert(3);

        heap.Clear();

        heap.Count.Should().Be(0);
        heap.IsEmpty.Should().BeTrue();
        heap.TryExtractMax(out _).Should().BeFalse();
    }

    /// <summary>
    /// Tests that extracting all items produces descending order.
    /// The items should be extracted from maximum to minimum.
    /// </summary>
    [Fact]
    public void ExtractAll_ProducesDescendingOrder()
    {
        var heap = new MaxHeap<int>();
        var items = new[] { 3, 1, 4, 1, 5, 9, 2, 6 };
        foreach (var item in items)
        {
            heap.Insert(item);
        }

        var extracted = new List<int>();
        while (!heap.IsEmpty)
        {
            extracted.Add(heap.ExtractMax());
        }

        extracted.Should().BeInDescendingOrder();
        extracted.Count.Should().Be(items.Length);
        extracted[0].Should().Be(9);
    }

    /// <summary>
    /// Tests that enumerator iterates through items in heap order.
    /// The enumerator should visit all items without specific order guarantee.
    /// </summary>
    [Fact]
    public void GetEnumerator_IteratesAllItems()
    {
        var heap = new MaxHeap<int>();
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
        var heap = new MaxHeap<int>();
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
        var heap = new MaxHeap<int>();
        heap.Insert(5);
        heap.Insert(5);
        heap.Insert(3);
        heap.Insert(5);
        heap.Insert(7);

        heap.ExtractMax().Should().Be(7);
        heap.ExtractMax().Should().Be(5);
        heap.ExtractMax().Should().Be(5);
        heap.ExtractMax().Should().Be(5);
        heap.ExtractMax().Should().Be(3);
        heap.IsEmpty.Should().BeTrue();
    }

    /// <summary>
    /// Tests that CreateWithArrayPool creates a pooled instance.
    /// The method should create a heap that uses array pooling.
    /// </summary>
    [Fact]
    public void CreateWithArrayPool_CreatesPooledInstance()
    {
        using var heap = MaxHeap<int>.CreateWithArrayPool(10);

        heap.Capacity.Should().BeGreaterThanOrEqualTo(10);
        heap.Count.Should().Be(0);
        
        heap.Insert(5);
        heap.Insert(10);
        heap.PeekMax().Should().Be(10);
    }

    /// <summary>
    /// Tests that Rent returns a reusable instance from pool.
    /// The method should provide an instance that can be returned to pool.
    /// </summary>
    [Fact]
    public void Rent_Return_ReuseInstance()
    {
        var heap1 = MaxHeap<int>.Rent(10);
        heap1.Insert(5);
        heap1.Insert(10);
        heap1.Return();

        var heap2 = MaxHeap<int>.Rent(10);
        
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
        var heap = MaxHeap<int>.CreateWithArrayPool(10);
        heap.Insert(5);
        heap.Insert(10);

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
        var heap = new MaxHeap<TestComparable>();
        heap.Insert(new TestComparable(5));
        heap.Insert(new TestComparable(3));
        heap.Insert(new TestComparable(7));

        heap.ExtractMax().Value.Should().Be(7);
        heap.ExtractMax().Value.Should().Be(5);
        heap.ExtractMax().Value.Should().Be(3);
    }

    /// <summary>
    /// Tests empty heap edge cases.
    /// The heap should handle empty state operations appropriately.
    /// </summary>
    [Fact]
    public void EmptyHeap_HandlesOperationsCorrectly()
    {
        var heap = new MaxHeap<int>();

        heap.IsEmpty.Should().BeTrue();
        heap.Count.Should().Be(0);
        heap.TryExtractMax(out _).Should().BeFalse();
        heap.TryPeekMax(out _).Should().BeFalse();
        
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
        var heap = new MaxHeap<int>();

        heap.Insert(42);
        heap.Count.Should().Be(1);
        heap.IsEmpty.Should().BeFalse();
        heap.PeekMax().Should().Be(42);
        
        var extracted = heap.ExtractMax();
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
        var heap = new MaxHeap<int>(2);
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

        // Extract all items and verify they come out in descending order
        var extracted = new List<int>();
        while (!heap.IsEmpty)
        {
            extracted.Add(heap.ExtractMax());
        }

        extracted.Should().BeInDescendingOrder();
        extracted.Should().HaveCount(itemCount);
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
}
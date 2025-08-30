using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Omni.Collections.Linear;
using Xunit;

namespace Omni.Collections.Tests.Linear;

public class FastQueueTests
{
    /// <summary>
    /// Tests that a FastQueue can be constructed with valid capacity.
    /// The queue should have at least the specified capacity rounded to power of two.
    /// </summary>
    [Theory]
    [InlineData(1, 1)]
    [InlineData(10, 16)]
    [InlineData(16, 16)]
    [InlineData(17, 32)]
    [InlineData(100, 128)]
    public void Constructor_WithValidCapacity_CreatesQueueWithPowerOfTwoCapacity(int requestedCapacity, int expectedMinCapacity)
    {
        var queue = new FastQueue<int>(requestedCapacity);

        queue.Capacity.Should().BeGreaterThanOrEqualTo(expectedMinCapacity);
        queue.Count.Should().Be(0);
    }

    /// <summary>
    /// Tests that constructor uses default capacity when not specified.
    /// The queue should be created with a default capacity of 16.
    /// </summary>
    [Fact]
    public void Constructor_WithoutCapacity_UsesDefaultCapacity()
    {
        var queue = new FastQueue<int>();

        queue.Capacity.Should().BeGreaterThanOrEqualTo(16);
        queue.Count.Should().Be(0);
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
        var act = () => new FastQueue<int>(capacity);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("capacity");
    }

    /// <summary>
    /// Tests that Enqueue adds items to the queue.
    /// Items should be added in FIFO order.
    /// </summary>
    [Fact]
    public void Enqueue_AddsItemsInOrder()
    {
        var queue = new FastQueue<int>();

        queue.Enqueue(1);
        queue.Enqueue(2);
        queue.Enqueue(3);

        queue.Count.Should().Be(3);
        queue.Should().Equal(1, 2, 3);
    }

    /// <summary>
    /// Tests that Enqueue automatically resizes when capacity is exceeded.
    /// The queue should grow to accommodate new items.
    /// </summary>
    [Fact]
    public void Enqueue_WhenFull_AutomaticallyResizes()
    {
        var queue = new FastQueue<int>(2);
        
        queue.Enqueue(1);
        queue.Enqueue(2);
        queue.Enqueue(3); // Should trigger resize
        queue.Enqueue(4);

        queue.Count.Should().Be(4);
        queue.Capacity.Should().BeGreaterThan(2);
        queue.Should().Equal(1, 2, 3, 4);
    }

    /// <summary>
    /// Tests that Dequeue removes and returns items in FIFO order.
    /// The first item enqueued should be the first dequeued.
    /// </summary>
    [Fact]
    public void Dequeue_RemovesItemsInFifoOrder()
    {
        var queue = new FastQueue<int>();
        queue.Enqueue(1);
        queue.Enqueue(2);
        queue.Enqueue(3);

        var item1 = queue.Dequeue();
        var item2 = queue.Dequeue();
        var item3 = queue.Dequeue();

        item1.Should().Be(1);
        item2.Should().Be(2);
        item3.Should().Be(3);
        queue.Count.Should().Be(0);
    }

    /// <summary>
    /// Tests that Dequeue throws exception when queue is empty in debug mode.
    /// The method should validate the queue is not empty.
    /// </summary>
    [Fact]
    public void Dequeue_WhenEmpty_ThrowsInvalidOperationException()
    {
        var queue = new FastQueue<int>();

#if DEBUG
        var act = () => queue.Dequeue();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*empty*");
#else
        // In release mode, behavior is undefined but should not crash
        // We skip this test in release mode
#endif
    }

    /// <summary>
    /// Tests that TryDequeue successfully dequeues when not empty.
    /// The method should return true and output the dequeued item.
    /// </summary>
    [Fact]
    public void TryDequeue_WhenNotEmpty_ReturnsTrueAndDequeuesItem()
    {
        var queue = new FastQueue<int>();
        queue.Enqueue(10);
        queue.Enqueue(20);

        var result1 = queue.TryDequeue(out var item1);
        var result2 = queue.TryDequeue(out var item2);

        result1.Should().BeTrue();
        item1.Should().Be(10);
        result2.Should().BeTrue();
        item2.Should().Be(20);
        queue.Count.Should().Be(0);
    }

    /// <summary>
    /// Tests that TryDequeue returns false when queue is empty.
    /// The method should return false without throwing.
    /// </summary>
    [Fact]
    public void TryDequeue_WhenEmpty_ReturnsFalse()
    {
        var queue = new FastQueue<int>();

        var result = queue.TryDequeue(out var item);

        result.Should().BeFalse();
        item.Should().Be(0);
        queue.Count.Should().Be(0);
    }

    /// <summary>
    /// Tests that Peek returns the front item without removing it.
    /// The method should return the next item to be dequeued without modification.
    /// </summary>
    [Fact]
    public void Peek_ReturnsFirstItemWithoutRemoving()
    {
        var queue = new FastQueue<int>();
        queue.Enqueue(10);
        queue.Enqueue(20);

        var peeked = queue.Peek();
        
        peeked.Should().Be(10);
        queue.Count.Should().Be(2);
        queue.Dequeue().Should().Be(10); // Verify item wasn't removed
    }

    /// <summary>
    /// Tests that Peek throws exception when queue is empty.
    /// The method should validate the queue is not empty.
    /// </summary>
    [Fact]
    public void Peek_WhenEmpty_ThrowsInvalidOperationException()
    {
        var queue = new FastQueue<int>();

        var act = () => queue.Peek();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*empty*");
    }

    /// <summary>
    /// Tests that TryPeek returns true and peeks item when not empty.
    /// The method should return true and output the front item without removing.
    /// </summary>
    [Fact]
    public void TryPeek_WhenNotEmpty_ReturnsTrueAndPeeksItem()
    {
        var queue = new FastQueue<int>();
        queue.Enqueue(10);
        queue.Enqueue(20);

        var result = queue.TryPeek(out var item);

        result.Should().BeTrue();
        item.Should().Be(10);
        queue.Count.Should().Be(2);
    }

    /// <summary>
    /// Tests that TryPeek returns false when queue is empty.
    /// The method should return false without throwing.
    /// </summary>
    [Fact]
    public void TryPeek_WhenEmpty_ReturnsFalse()
    {
        var queue = new FastQueue<int>();

        var result = queue.TryPeek(out var item);

        result.Should().BeFalse();
        item.Should().Be(0);
    }

    /// <summary>
    /// Tests that EnqueueSpan adds multiple items efficiently.
    /// The method should add all items from the span in order.
    /// </summary>
    [Fact]
    public void EnqueueSpan_AddsMultipleItems()
    {
        var queue = new FastQueue<int>();
        var items = new[] { 1, 2, 3, 4, 5 };

        queue.EnqueueSpan(items.AsSpan());

        queue.Count.Should().Be(5);
        queue.Should().Equal(items);
    }

    /// <summary>
    /// Tests that EnqueueSpan handles empty span correctly.
    /// The method should handle empty span without errors.
    /// </summary>
    [Fact]
    public void EnqueueSpan_WithEmptySpan_DoesNothing()
    {
        var queue = new FastQueue<int>();
        queue.Enqueue(1);

        queue.EnqueueSpan(ReadOnlySpan<int>.Empty);

        queue.Count.Should().Be(1);
        queue.Peek().Should().Be(1);
    }

    /// <summary>
    /// Tests that EnqueueSpan triggers resize when needed.
    /// The method should automatically grow the queue to fit all items.
    /// </summary>
    [Fact]
    public void EnqueueSpan_WhenExceedsCapacity_TriggersResize()
    {
        var queue = new FastQueue<int>(2);
        var items = new[] { 1, 2, 3, 4, 5 };

        queue.EnqueueSpan(items.AsSpan());

        queue.Count.Should().Be(5);
        queue.Capacity.Should().BeGreaterThan(2);
        queue.Should().Equal(items);
    }

    /// <summary>
    /// Tests that DequeueSpan removes multiple items efficiently.
    /// The method should dequeue specified number of items and return as span.
    /// </summary>
    [Fact]
    public void DequeueSpan_RemovesMultipleItems()
    {
        var queue = new FastQueue<int>();
        for (int i = 1; i <= 5; i++)
            queue.Enqueue(i);

        var dequeued = queue.DequeueSpan(3);

        dequeued.ToArray().Should().Equal(1, 2, 3);
        queue.Count.Should().Be(2);
        queue.Should().Equal(4, 5);
    }

    /// <summary>
    /// Tests that DequeueSpan handles count larger than size.
    /// The method should dequeue all available items when count exceeds size.
    /// </summary>
    [Fact]
    public void DequeueSpan_WithCountLargerThanSize_DequeuesAllItems()
    {
        var queue = new FastQueue<int>();
        queue.Enqueue(1);
        queue.Enqueue(2);

        var dequeued = queue.DequeueSpan(10);

        dequeued.ToArray().Should().Equal(1, 2);
        queue.Count.Should().Be(0);
    }

    /// <summary>
    /// Tests that DequeueSpan returns empty span for invalid count.
    /// The method should return empty span for zero or negative count.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void DequeueSpan_WithInvalidCount_ReturnsEmptySpan(int count)
    {
        var queue = new FastQueue<int>();
        queue.Enqueue(1);
        queue.Enqueue(2);

        var dequeued = queue.DequeueSpan(count);

        dequeued.IsEmpty.Should().BeTrue();
        queue.Count.Should().Be(2);
    }

    /// <summary>
    /// Tests that Clear removes all items from the queue.
    /// The method should reset the queue to empty state.
    /// </summary>
    [Fact]
    public void Clear_RemovesAllItems()
    {
        var queue = new FastQueue<int>();
        queue.Enqueue(1);
        queue.Enqueue(2);
        queue.Enqueue(3);

        queue.Clear();

        queue.Count.Should().Be(0);
        queue.TryDequeue(out _).Should().BeFalse();
    }

    /// <summary>
    /// Tests that Clear on empty queue doesn't throw.
    /// The method should handle empty queue gracefully.
    /// </summary>
    [Fact]
    public void Clear_OnEmptyQueue_DoesNotThrow()
    {
        var queue = new FastQueue<int>();

        var act = () => queue.Clear();

        act.Should().NotThrow();
        queue.Count.Should().Be(0);
    }

    /// <summary>
    /// Tests that enumerator iterates through all items in order.
    /// The enumerator should visit items in FIFO order.
    /// </summary>
    [Fact]
    public void GetEnumerator_IteratesInFifoOrder()
    {
        var queue = new FastQueue<int>();
        queue.Enqueue(1);
        queue.Enqueue(2);
        queue.Enqueue(3);

        var items = new List<int>();
        foreach (var item in queue)
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
        var queue = new FastQueue<int>();
        queue.Enqueue(1);
        queue.Enqueue(2);
        queue.Enqueue(3);

        var sum = queue.Sum();
        var doubled = queue.Select(x => x * 2).ToList();

        sum.Should().Be(6);
        doubled.Should().Equal(2, 4, 6);
    }

    /// <summary>
    /// Tests circular buffer behavior with enqueue and dequeue operations.
    /// The queue should efficiently reuse buffer space in circular fashion.
    /// </summary>
    [Fact]
    public void CircularBuffer_HandlesWrapAroundCorrectly()
    {
        var queue = new FastQueue<int>(4);
        
        // Fill queue
        queue.Enqueue(1);
        queue.Enqueue(2);
        queue.Enqueue(3);
        queue.Enqueue(4);
        
        // Dequeue some items
        queue.Dequeue().Should().Be(1);
        queue.Dequeue().Should().Be(2);
        
        // Enqueue more items (should wrap around)
        queue.Enqueue(5);
        queue.Enqueue(6);
        
        // Verify order is maintained
        queue.Should().Equal(3, 4, 5, 6);
    }

    /// <summary>
    /// Tests that CreateWithArrayPool creates a pooled instance.
    /// The method should create a queue that uses array pooling.
    /// </summary>
    [Fact]
    public void CreateWithArrayPool_CreatesPooledInstance()
    {
        using var queue = FastQueue<int>.CreateWithArrayPool(10);

        queue.Capacity.Should().BeGreaterThanOrEqualTo(10);
        queue.Count.Should().Be(0);
        
        queue.Enqueue(1);
        queue.Enqueue(2);
        queue.Should().Equal(1, 2);
    }

    /// <summary>
    /// Tests that Rent returns a reusable instance from pool.
    /// The method should provide an instance that can be returned to pool.
    /// </summary>
    [Fact]
    public void Rent_Return_ReuseInstance()
    {
        var queue1 = FastQueue<int>.Rent(10);
        queue1.Enqueue(1);
        queue1.Enqueue(2);
        queue1.Return();

        var queue2 = FastQueue<int>.Rent(10);
        
        queue2.Count.Should().Be(0);
        queue2.Capacity.Should().BeGreaterThanOrEqualTo(10);
    }

    /// <summary>
    /// Tests that Dispose properly cleans up resources.
    /// The method should return pooled arrays when using array pooling.
    /// </summary>
    [Fact]
    public void Dispose_CleansUpResources()
    {
        var queue = FastQueue<int>.CreateWithArrayPool(10);
        queue.Enqueue(1);
        queue.Enqueue(2);

        queue.Dispose();
        queue.Dispose(); // Double dispose should not throw
    }

    /// <summary>
    /// Tests that queue handles null values correctly for reference types.
    /// The queue should accept and manage null values without issues.
    /// </summary>
    [Fact]
    public void NullHandling_ForReferenceTypes_WorksCorrectly()
    {
        var queue = new FastQueue<string>();

        queue.Enqueue(null!);
        queue.Enqueue("test");
        queue.Enqueue(null!);

        queue.Count.Should().Be(3);
        queue.Dequeue().Should().BeNull();
        queue.Dequeue().Should().Be("test");
        queue.Dequeue().Should().BeNull();
    }

    /// <summary>
    /// Tests empty queue edge cases.
    /// The queue should handle empty state operations appropriately.
    /// </summary>
    [Fact]
    public void EmptyQueue_HandlesOperationsCorrectly()
    {
        var queue = new FastQueue<int>();

        queue.Count.Should().Be(0);
        queue.TryDequeue(out _).Should().BeFalse();
        queue.TryPeek(out _).Should().BeFalse();
        
        queue.Clear(); // Should not throw on empty queue
        queue.Count.Should().Be(0);
        
        var items = queue.ToList();
        items.Should().BeEmpty();
    }

    /// <summary>
    /// Tests single element queue operations.
    /// The queue should handle single element scenarios correctly.
    /// </summary>
    [Fact]
    public void SingleElementQueue_HandlesOperationsCorrectly()
    {
        var queue = new FastQueue<int>(1);

        queue.Enqueue(10);
        queue.Count.Should().Be(1);
        queue.Peek().Should().Be(10);
        
        var dequeued = queue.Dequeue();
        dequeued.Should().Be(10);
        queue.Count.Should().Be(0);
    }

    /// <summary>
    /// Tests queue behavior with large number of items.
    /// The queue should handle many items efficiently with proper resizing.
    /// </summary>
    [Fact]
    public void LargeQueue_HandlesMannyItemsEfficiently()
    {
        var queue = new FastQueue<int>(2);
        const int itemCount = 1000;

        // Enqueue many items
        for (int i = 0; i < itemCount; i++)
        {
            queue.Enqueue(i);
        }

        queue.Count.Should().Be(itemCount);
        queue.Capacity.Should().BeGreaterThanOrEqualTo(itemCount);

        // Dequeue all items
        for (int i = 0; i < itemCount; i++)
        {
            queue.Dequeue().Should().Be(i);
        }

        queue.Count.Should().Be(0);
    }

    /// <summary>
    /// Tests alternating enqueue and dequeue operations.
    /// The queue should maintain correct state during mixed operations.
    /// </summary>
    [Fact]
    public void AlternatingOperations_MaintainCorrectState()
    {
        var queue = new FastQueue<int>();

        for (int i = 0; i < 10; i++)
        {
            queue.Enqueue(i * 2);
            queue.Enqueue(i * 2 + 1);
            
            if (i % 2 == 0)
            {
                queue.Dequeue();
            }
        }

        queue.Count.Should().Be(15); // 20 enqueued - 5 dequeued
        
        var first = queue.Dequeue();
        first.Should().Be(5); // First remaining item
    }
}
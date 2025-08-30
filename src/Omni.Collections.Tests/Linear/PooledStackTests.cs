using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Omni.Collections.Linear;
using Xunit;

namespace Omni.Collections.Tests.Linear;

public class PooledStackTests
{
    /// <summary>
    /// Tests that a PooledStack can be constructed with valid capacity.
    /// The stack should have at least the specified capacity and zero count initially.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(16)]
    [InlineData(100)]
    public void Constructor_WithValidCapacity_CreatesStackWithCorrectCapacity(int capacity)
    {
        using var stack = new PooledStack<int>(capacity);

        stack.Capacity.Should().BeGreaterThanOrEqualTo(capacity);
        stack.Count.Should().Be(0);
        stack.IsEmpty.Should().BeTrue();
    }

    /// <summary>
    /// Tests that constructor uses default capacity when not specified.
    /// The stack should be created with a default capacity of 16.
    /// </summary>
    [Fact]
    public void Constructor_WithoutCapacity_UsesDefaultCapacity()
    {
        using var stack = new PooledStack<int>();

        stack.Capacity.Should().BeGreaterThanOrEqualTo(16);
        stack.Count.Should().Be(0);
        stack.IsEmpty.Should().BeTrue();
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
        var act = () => new PooledStack<int>(capacity);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("initialCapacity");
    }

    /// <summary>
    /// Tests that Push adds items to the stack.
    /// Items should be added in LIFO order.
    /// </summary>
    [Fact]
    public void Push_AddsItemsInLifoOrder()
    {
        using var stack = new PooledStack<int>();

        stack.Push(1);
        stack.Push(2);
        stack.Push(3);

        stack.Count.Should().Be(3);
        stack.Peek().Should().Be(3);
    }

    /// <summary>
    /// Tests that Push triggers resize when capacity is exceeded.
    /// The stack should automatically grow to accommodate new items.
    /// </summary>
    [Fact]
    public void Push_WhenFull_TriggersResize()
    {
        using var stack = new PooledStack<int>(2);

        stack.Push(1);
        stack.Push(2);
        stack.Push(3); // Should trigger resize
        stack.Push(4);

        stack.Count.Should().Be(4);
        stack.Capacity.Should().BeGreaterThan(2);
        stack.Pop().Should().Be(4);
    }

    /// <summary>
    /// Tests that Pop removes and returns items in LIFO order.
    /// The last item pushed should be the first popped.
    /// </summary>
    [Fact]
    public void Pop_RemovesItemsInLifoOrder()
    {
        using var stack = new PooledStack<int>();
        stack.Push(1);
        stack.Push(2);
        stack.Push(3);

        var item1 = stack.Pop();
        var item2 = stack.Pop();
        var item3 = stack.Pop();

        item1.Should().Be(3);
        item2.Should().Be(2);
        item3.Should().Be(1);
        stack.Count.Should().Be(0);
        stack.IsEmpty.Should().BeTrue();
    }

#if DEBUG
    /// <summary>
    /// Tests that Pop throws exception when stack is empty in debug mode.
    /// The method should validate the stack is not empty.
    /// </summary>
    [Fact]
    public void Pop_WhenEmpty_ThrowsInvalidOperationException()
    {
        using var stack = new PooledStack<int>();

        var act = () => stack.Pop();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*empty*");
    }
#endif

    /// <summary>
    /// Tests that TryPop successfully pops when not empty.
    /// The method should return true and output the popped item.
    /// </summary>
    [Fact]
    public void TryPop_WhenNotEmpty_ReturnsTrueAndPopsItem()
    {
        using var stack = new PooledStack<int>();
        stack.Push(10);
        stack.Push(20);

        var result1 = stack.TryPop(out var item1);
        var result2 = stack.TryPop(out var item2);

        result1.Should().BeTrue();
        item1.Should().Be(20);
        result2.Should().BeTrue();
        item2.Should().Be(10);
        stack.Count.Should().Be(0);
    }

    /// <summary>
    /// Tests that TryPop returns false when stack is empty.
    /// The method should return false without throwing.
    /// </summary>
    [Fact]
    public void TryPop_WhenEmpty_ReturnsFalse()
    {
        using var stack = new PooledStack<int>();

        var result = stack.TryPop(out var item);

        result.Should().BeFalse();
        item.Should().Be(0);
        stack.Count.Should().Be(0);
    }

    /// <summary>
    /// Tests that Peek returns the top item without removing it.
    /// The method should return the next item to be popped without modification.
    /// </summary>
    [Fact]
    public void Peek_ReturnsTopItemWithoutRemoving()
    {
        using var stack = new PooledStack<int>();
        stack.Push(10);
        stack.Push(20);

        var peeked = stack.Peek();

        peeked.Should().Be(20);
        stack.Count.Should().Be(2);
        stack.Pop().Should().Be(20); // Verify it wasn't removed
    }

    /// <summary>
    /// Tests that Peek throws exception when stack is empty.
    /// The method should validate the stack is not empty.
    /// </summary>
    [Fact]
    public void Peek_WhenEmpty_ThrowsInvalidOperationException()
    {
        using var stack = new PooledStack<int>();

        var act = () => stack.Peek();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*empty*");
    }

    /// <summary>
    /// Tests that TryPeek returns true and peeks item when not empty.
    /// The method should return true and output the top item without removing.
    /// </summary>
    [Fact]
    public void TryPeek_WhenNotEmpty_ReturnsTrueAndPeeksItem()
    {
        using var stack = new PooledStack<int>();
        stack.Push(10);
        stack.Push(20);

        var result = stack.TryPeek(out var item);

        result.Should().BeTrue();
        item.Should().Be(20);
        stack.Count.Should().Be(2);
    }

    /// <summary>
    /// Tests that TryPeek returns false when stack is empty.
    /// The method should return false without throwing.
    /// </summary>
    [Fact]
    public void TryPeek_WhenEmpty_ReturnsFalse()
    {
        using var stack = new PooledStack<int>();

        var result = stack.TryPeek(out var item);

        result.Should().BeFalse();
        item.Should().Be(0);
    }

    /// <summary>
    /// Tests that PushRange adds multiple items efficiently.
    /// The method should add all items from the span in order.
    /// </summary>
    [Fact]
    public void PushRange_AddsMultipleItems()
    {
        using var stack = new PooledStack<int>();
        var items = new[] { 1, 2, 3, 4, 5 };

        stack.PushRange(items);

        stack.Count.Should().Be(5);
        stack.Pop().Should().Be(5);
        stack.Pop().Should().Be(4);
    }

    /// <summary>
    /// Tests that PopSpan removes multiple items efficiently.
    /// The method should pop specified number of items and return as span.
    /// </summary>
    [Fact]
    public void PopSpan_RemovesMultipleItems()
    {
        using var stack = new PooledStack<int>();
        for (int i = 1; i <= 5; i++)
            stack.Push(i);

        var popped = stack.PopSpan(3);

        popped.ToArray().Should().Equal(5, 4, 3);
        stack.Count.Should().Be(2);
        stack.Pop().Should().Be(2);
    }

    /// <summary>
    /// Tests that Clear removes all items from the stack.
    /// The method should reset the stack to empty state.
    /// </summary>
    [Fact]
    public void Clear_RemovesAllItems()
    {
        using var stack = new PooledStack<int>();
        stack.Push(1);
        stack.Push(2);
        stack.Push(3);

        stack.Clear();

        stack.Count.Should().Be(0);
        stack.IsEmpty.Should().BeTrue();
        stack.TryPop(out _).Should().BeFalse();
    }

    /// <summary>
    /// Tests that Clear on empty stack doesn't throw.
    /// The method should handle empty stack gracefully.
    /// </summary>
    [Fact]
    public void Clear_OnEmptyStack_DoesNotThrow()
    {
        using var stack = new PooledStack<int>();

        var act = () => stack.Clear();

        act.Should().NotThrow();
        stack.Count.Should().Be(0);
    }

    /// <summary>
    /// Tests that Contains correctly identifies existing items.
    /// The method should return true for items in the stack.
    /// </summary>
    [Fact]
    public void Contains_ExistingItem_ReturnsTrue()
    {
        using var stack = new PooledStack<int>();
        stack.Push(10);
        stack.Push(20);
        stack.Push(30);

        stack.Contains(20).Should().BeTrue();
        stack.Contains(30).Should().BeTrue();
        stack.Contains(10).Should().BeTrue();
    }

    /// <summary>
    /// Tests that Contains returns false for non-existent items.
    /// The method should return false for items not in the stack.
    /// </summary>
    [Fact]
    public void Contains_NonExistentItem_ReturnsFalse()
    {
        using var stack = new PooledStack<int>();
        stack.Push(10);
        stack.Push(20);

        stack.Contains(30).Should().BeFalse();
        stack.Contains(0).Should().BeFalse();
    }

    /// <summary>
    /// Tests that stack can be converted to array via LINQ.
    /// The method should return all items in enumeration order.
    /// </summary>
    [Fact]
    public void ToArray_ConvertsStackToArray()
    {
        using var stack = new PooledStack<int>();
        stack.Push(1);
        stack.Push(2);
        stack.Push(3);

        var array = stack.ToArray();

        array.Should().Equal(3, 2, 1); // Top to bottom
        array.Length.Should().Be(3);
    }

    /// <summary>
    /// Tests that AsSpan returns correct span of current items.
    /// The method should return items from top to bottom.
    /// </summary>
    [Fact]
    public void AsSpan_ReturnsCorrectSpan()
    {
        using var stack = new PooledStack<int>();
        stack.Push(1);
        stack.Push(2);
        stack.Push(3);

        var span = stack.AsSpan();

        span.Length.Should().Be(3);
        span.ToArray().Should().Equal(1, 2, 3); // Stack order: bottom to top
    }

    /// <summary>
    /// Tests that enumerator iterates through items in stack order.
    /// The enumerator should visit items from top to bottom.
    /// </summary>
    [Fact]
    public void GetEnumerator_IteratesInStackOrder()
    {
        using var stack = new PooledStack<int>();
        stack.Push(1);
        stack.Push(2);
        stack.Push(3);

        var items = new List<int>();
        foreach (var item in stack)
        {
            items.Add(item);
        }

        items.Should().Equal(3, 2, 1); // Top to bottom
    }

    /// <summary>
    /// Tests that enumerator works with LINQ methods.
    /// The enumerator should be compatible with standard LINQ operations.
    /// </summary>
    [Fact]
    public void Enumerator_WorksWithLinq()
    {
        using var stack = new PooledStack<int>();
        stack.Push(1);
        stack.Push(2);
        stack.Push(3);

        var sum = stack.Sum();
        var doubled = stack.Select(x => x * 2).ToList();

        sum.Should().Be(6);
        doubled.Should().Equal(6, 4, 2);
    }

    /// <summary>
    /// Tests that CreateWithArrayPool creates a pooled instance.
    /// The method should create a stack that uses array pooling.
    /// </summary>
    [Fact]
    public void CreateWithArrayPool_CreatesPooledInstance()
    {
        using var stack = PooledStack<int>.CreateWithArrayPool(10);

        stack.Capacity.Should().BeGreaterThanOrEqualTo(10);
        stack.Count.Should().Be(0);
        
        stack.Push(1);
        stack.Push(2);
        stack.Pop().Should().Be(2);
    }

    /// <summary>
    /// Tests that Dispose properly cleans up resources.
    /// The method should return pooled arrays and prevent further operations.
    /// </summary>
    [Fact]
    public void Dispose_CleansUpResources()
    {
        var stack = PooledStack<int>.CreateWithArrayPool(10);
        stack.Push(1);
        stack.Push(2);

        stack.Dispose();
        
#if DEBUG
        var act = () => stack.Push(3);
        act.Should().Throw<ObjectDisposedException>();
#endif
        
        stack.Dispose(); // Double dispose should not throw
    }

    /// <summary>
    /// Tests that stack handles null values correctly for reference types.
    /// The stack should accept and manage null values without issues.
    /// </summary>
    [Fact]
    public void NullHandling_ForReferenceTypes_WorksCorrectly()
    {
        using var stack = new PooledStack<string>();

        stack.Push(null!);
        stack.Push("test");
        stack.Push(null!);

        stack.Count.Should().Be(3);
        stack.Pop().Should().BeNull();
        stack.Pop().Should().Be("test");
        stack.Pop().Should().BeNull();
    }

    /// <summary>
    /// Tests empty stack edge cases.
    /// The stack should handle empty state operations appropriately.
    /// </summary>
    [Fact]
    public void EmptyStack_HandlesOperationsCorrectly()
    {
        using var stack = new PooledStack<int>();

        stack.IsEmpty.Should().BeTrue();
        stack.Count.Should().Be(0);
        stack.TryPop(out _).Should().BeFalse();
        stack.TryPeek(out _).Should().BeFalse();
        stack.Contains(1).Should().BeFalse();
        
        stack.Clear(); // Should not throw on empty stack
        stack.IsEmpty.Should().BeTrue();
        
        var array = stack.ToArray();
        array.Should().BeEmpty();
    }

    /// <summary>
    /// Tests single element stack operations.
    /// The stack should handle single element scenarios correctly.
    /// </summary>
    [Fact]
    public void SingleElementStack_HandlesOperationsCorrectly()
    {
        using var stack = new PooledStack<int>(1);

        stack.Push(42);
        stack.Count.Should().Be(1);
        stack.IsEmpty.Should().BeFalse();
        stack.Peek().Should().Be(42);
        
        var popped = stack.Pop();
        popped.Should().Be(42);
        stack.IsEmpty.Should().BeTrue();
    }

    /// <summary>
    /// Tests stack with large number of items.
    /// The stack should handle many items efficiently with proper resizing.
    /// </summary>
    [Fact]
    public void LargeStack_HandlesMannyItemsEfficiently()
    {
        using var stack = new PooledStack<int>(2);
        const int itemCount = 1000;

        // Push many items
        for (int i = 0; i < itemCount; i++)
        {
            stack.Push(i);
        }

        stack.Count.Should().Be(itemCount);
        stack.Capacity.Should().BeGreaterThanOrEqualTo(itemCount);

        // Pop all items and verify LIFO order
        for (int i = itemCount - 1; i >= 0; i--)
        {
            stack.Pop().Should().Be(i);
        }

        stack.IsEmpty.Should().BeTrue();
    }

    /// <summary>
    /// Tests alternating push and pop operations.
    /// The stack should maintain correct state during mixed operations.
    /// </summary>
    [Fact]
    public void AlternatingOperations_MaintainCorrectState()
    {
        using var stack = new PooledStack<int>();

        for (int i = 0; i < 10; i++)
        {
            stack.Push(i * 2);
            stack.Push(i * 2 + 1);
            
            if (i % 2 == 0)
            {
                stack.Pop();
            }
        }

        stack.Count.Should().Be(15); // 20 pushed - 5 popped
        
        var top = stack.Pop();
        top.Should().Be(19); // Last item pushed
    }
}
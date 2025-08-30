using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Omni.Collections.Reactive;
using Xunit;

namespace Omni.Collections.Tests.Reactive;

public class ObservableListTests
{
    /// <summary>
    /// Tests that an ObservableList can be constructed with default parameters.
    /// The list should initialize empty with proper event handlers ready.
    /// </summary>
    [Fact]
    public void Constructor_Default_InitializesCorrectly()
    {
        var list = new ObservableList<int>();

        list.Count.Should().Be(0);
        list.Capacity.Should().BeGreaterOrEqualTo(0);
        list.IsReadOnly.Should().BeFalse();
        list.Version.Should().Be(0);
    }

    /// <summary>
    /// Tests that an ObservableList can be constructed with initial capacity.
    /// The list should initialize empty with the specified capacity.
    /// </summary>
    [Fact]
    public void Constructor_WithCapacity_InitializesCorrectly()
    {
        var list = new ObservableList<int>(100);

        list.Count.Should().Be(0);
        list.Capacity.Should().BeGreaterOrEqualTo(100);
        list.IsReadOnly.Should().BeFalse();
    }

    /// <summary>
    /// Tests that an ObservableList can be constructed with initial collection.
    /// The list should contain all items from the provided collection in order.
    /// </summary>
    [Fact]
    public void Constructor_WithCollection_InitializesWithItems()
    {
        var items = new[] { 1, 2, 3, 4, 5 };
        var list = new ObservableList<int>(items);

        list.Count.Should().Be(5);
        list.Should().Equal(items);
    }

    /// <summary>
    /// Tests that Add successfully adds items and fires appropriate events.
    /// Adding items should trigger CollectionChanged, PropertyChanged, and custom events.
    /// </summary>
    [Fact]
    public void Add_AddsItemAndFiresEvents()
    {
        var list = new ObservableList<int>();
        var collectionChangedFired = false;
        var propertyChangedFired = false;
        var itemAddedFired = false;
        var listChangedFired = false;

        list.CollectionChanged += (s, e) => 
        {
            collectionChangedFired = true;
            e.Action.Should().Be(NotifyCollectionChangedAction.Add);
            e.NewItems!.Cast<int>().Should().Contain(42);
            e.NewStartingIndex.Should().Be(0);
        };
        list.PropertyChanged += (s, e) => 
        {
            if (e.PropertyName == "Count") propertyChangedFired = true;
        };
        list.ItemAdded += item => 
        {
            itemAddedFired = true;
            item.Should().Be(42);
        };
        list.ListChanged += () => listChangedFired = true;

        list.Add(42);

        list.Count.Should().Be(1);
        list[0].Should().Be(42);
        list.Version.Should().Be(1);
        collectionChangedFired.Should().BeTrue();
        propertyChangedFired.Should().BeTrue();
        itemAddedFired.Should().BeTrue();
        listChangedFired.Should().BeTrue();
    }

    /// <summary>
    /// Tests that Insert adds items at specified index and fires appropriate events.
    /// Inserting should shift existing items and trigger position-aware events.
    /// </summary>
    [Fact]
    public void Insert_AddsItemAtIndexAndFiresEvents()
    {
        var list = new ObservableList<int> { 1, 3 };
        var collectionChangedFired = false;
        var itemInsertedFired = false;

        list.CollectionChanged += (s, e) => 
        {
            collectionChangedFired = true;
            e.Action.Should().Be(NotifyCollectionChangedAction.Add);
            e.NewItems!.Cast<int>().Should().Contain(2);
            e.NewStartingIndex.Should().Be(1);
        };
        list.ItemInserted += (item, index) => 
        {
            itemInsertedFired = true;
            item.Should().Be(2);
            index.Should().Be(1);
        };

        list.Insert(1, 2);

        list.Count.Should().Be(3);
        list.Should().Equal(new[] { 1, 2, 3 });
        collectionChangedFired.Should().BeTrue();
        itemInsertedFired.Should().BeTrue();
    }

    /// <summary>
    /// Tests that Insert throws exception for invalid indices.
    /// The method should validate index bounds before insertion.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void Insert_InvalidIndex_ThrowsArgumentOutOfRangeException(int index)
    {
        var list = new ObservableList<int> { 1, 2 };

        var act = () => list.Insert(index, 99);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Tests that RemoveAt removes items by index and fires appropriate events.
    /// Removing by index should trigger position-aware events.
    /// </summary>
    [Fact]
    public void RemoveAt_RemovesItemAndFiresEvents()
    {
        var list = new ObservableList<int> { 1, 2, 3 };
        var collectionChangedFired = false;
        var itemRemovedAtFired = false;

        list.CollectionChanged += (s, e) => 
        {
            collectionChangedFired = true;
            e.Action.Should().Be(NotifyCollectionChangedAction.Remove);
            e.OldItems!.Cast<int>().Should().Contain(2);
            e.OldStartingIndex.Should().Be(1);
        };
        list.ItemRemovedAt += (item, index) => 
        {
            itemRemovedAtFired = true;
            item.Should().Be(2);
            index.Should().Be(1);
        };

        list.RemoveAt(1);

        list.Count.Should().Be(2);
        list.Should().Equal(new[] { 1, 3 });
        collectionChangedFired.Should().BeTrue();
        itemRemovedAtFired.Should().BeTrue();
    }

    /// <summary>
    /// Tests that RemoveAt throws exception for invalid indices.
    /// The method should validate index bounds before removal.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void RemoveAt_InvalidIndex_ThrowsArgumentOutOfRangeException(int index)
    {
        var list = new ObservableList<int> { 1, 2 };

        var act = () => list.RemoveAt(index);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Tests that Remove removes first occurrence of item and fires events.
    /// Removing by value should find and remove the first matching item.
    /// </summary>
    [Fact]
    public void Remove_RemovesFirstOccurrenceAndFiresEvents()
    {
        var list = new ObservableList<int> { 1, 2, 3, 2 };
        var collectionChangedFired = false;
        var itemRemovedFired = false;

        list.CollectionChanged += (s, e) => 
        {
            collectionChangedFired = true;
            e.Action.Should().Be(NotifyCollectionChangedAction.Remove);
            e.OldItems!.Cast<int>().Should().Contain(2);
            e.OldStartingIndex.Should().Be(1);
        };
        list.ItemRemoved += item => 
        {
            itemRemovedFired = true;
            item.Should().Be(2);
        };

        var result = list.Remove(2);

        result.Should().BeTrue();
        list.Count.Should().Be(3);
        list.Should().Equal(new[] { 1, 3, 2 }); // First occurrence removed
        collectionChangedFired.Should().BeTrue();
        itemRemovedFired.Should().BeTrue();
    }

    /// <summary>
    /// Tests that Remove returns false for non-existent items without firing events.
    /// Removing non-existent items should not modify the list or trigger events.
    /// </summary>
    [Fact]
    public void Remove_NonExistentItem_ReturnsFalseWithoutEvents()
    {
        var list = new ObservableList<int> { 1, 2, 3 };
        var collectionChangedFired = false;

        list.CollectionChanged += (s, e) => collectionChangedFired = true;

        var result = list.Remove(99);

        result.Should().BeFalse();
        list.Count.Should().Be(3);
        collectionChangedFired.Should().BeFalse();
    }

    /// <summary>
    /// Tests that indexer get returns correct value for valid indices.
    /// The indexer should provide direct access to elements by position.
    /// </summary>
    [Fact]
    public void Indexer_Get_ReturnsCorrectValue()
    {
        var list = new ObservableList<int> { 10, 20, 30 };

        list[0].Should().Be(10);
        list[1].Should().Be(20);
        list[2].Should().Be(30);
    }

    /// <summary>
    /// Tests that indexer set updates value and fires appropriate events.
    /// Setting values should trigger replacement events with old and new values.
    /// </summary>
    [Fact]
    public void Indexer_Set_UpdatesValueAndFiresEvents()
    {
        var list = new ObservableList<int> { 10, 20, 30 };
        var collectionChangedFired = false;
        var itemReplacedFired = false;

        list.CollectionChanged += (s, e) => 
        {
            collectionChangedFired = true;
            e.Action.Should().Be(NotifyCollectionChangedAction.Replace);
            e.OldItems!.Cast<int>().Should().Contain(20);
            e.NewItems!.Cast<int>().Should().Contain(25);
            e.NewStartingIndex.Should().Be(1);
            e.OldStartingIndex.Should().Be(1);
        };
        list.ItemReplaced += (oldItem, newItem, index) => 
        {
            itemReplacedFired = true;
            oldItem.Should().Be(20);
            newItem.Should().Be(25);
            index.Should().Be(1);
        };

        list[1] = 25;

        list[1].Should().Be(25);
        list.Should().Equal(new[] { 10, 25, 30 });
        collectionChangedFired.Should().BeTrue();
        itemReplacedFired.Should().BeTrue();
    }

    /// <summary>
    /// Tests that indexer throws exception for invalid indices.
    /// The indexer should validate bounds for both get and set operations.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void Indexer_InvalidIndex_ThrowsIndexOutOfRangeException(int index)
    {
        var list = new ObservableList<int> { 1, 2 };

        var getAct = () => { var x = list[index]; };
        var setAct = () => list[index] = 99;

        getAct.Should().Throw<ArgumentOutOfRangeException>();
        setAct.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Tests that Clear removes all items and fires appropriate events.
    /// Clearing should reset the list and trigger reset events.
    /// </summary>
    [Fact]
    public void Clear_RemovesAllItemsAndFiresEvents()
    {
        var list = new ObservableList<int> { 1, 2, 3 };
        var collectionChangedFired = false;
        var propertyChangedFired = false;
        var listClearedFired = false;

        list.CollectionChanged += (s, e) => 
        {
            collectionChangedFired = true;
            e.Action.Should().Be(NotifyCollectionChangedAction.Reset);
        };
        list.PropertyChanged += (s, e) => 
        {
            if (e.PropertyName == "Count") propertyChangedFired = true;
        };
        list.ListCleared += () => listClearedFired = true;

        list.Clear();

        list.Count.Should().Be(0);
        collectionChangedFired.Should().BeTrue();
        propertyChangedFired.Should().BeTrue();
        listClearedFired.Should().BeTrue();
    }

    /// <summary>
    /// Tests that IndexOf finds the correct index of an item.
    /// The method should return the index of the first occurrence.
    /// </summary>
    [Fact]
    public void IndexOf_FindsCorrectIndex()
    {
        var list = new ObservableList<int> { 10, 20, 30, 20 };

        list.IndexOf(20).Should().Be(1); // First occurrence
        list.IndexOf(30).Should().Be(2);
        list.IndexOf(99).Should().Be(-1); // Not found
    }

    /// <summary>
    /// Tests that Contains correctly identifies existing and non-existing items.
    /// The method should return true for items in the list and false otherwise.
    /// </summary>
    [Fact]
    public void Contains_ChecksItemExistence()
    {
        var list = new ObservableList<int> { 1, 2, 3 };

        list.Contains(2).Should().BeTrue();
        list.Contains(4).Should().BeFalse();
    }

    /// <summary>
    /// Tests that CopyTo copies list items to array at specified index.
    /// All items should be copied in order to the target array.
    /// </summary>
    [Fact]
    public void CopyTo_CopiesItemsToArray()
    {
        var list = new ObservableList<int> { 1, 2, 3 };
        var array = new int[5];

        list.CopyTo(array, 1);

        array[0].Should().Be(0); // Unchanged
        array[1].Should().Be(1);
        array[2].Should().Be(2);
        array[3].Should().Be(3);
        array[4].Should().Be(0); // Unchanged
    }

    /// <summary>
    /// Tests that CopyTo throws exception for invalid parameters.
    /// The method should validate array and index parameters.
    /// </summary>
    [Fact]
    public void CopyTo_WithInvalidParameters_ThrowsException()
    {
        var list = new ObservableList<int> { 1, 2, 3 };

        var actNull = () => list.CopyTo(null!, 0);
        var actNegativeIndex = () => list.CopyTo(new int[5], -1);
        var actInsufficientSpace = () => list.CopyTo(new int[2], 0);

        actNull.Should().Throw<ArgumentNullException>();
        actNegativeIndex.Should().Throw<ArgumentOutOfRangeException>();
        actInsufficientSpace.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Tests that enumerator iterates through all items in correct order.
    /// The enumerator should visit each item in the order they appear in the list.
    /// </summary>
    [Fact]
    public void GetEnumerator_IteratesAllItemsInOrder()
    {
        var list = new ObservableList<int> { 1, 2, 3 };
        var visitedItems = new List<int>();

        foreach (var item in list)
        {
            visitedItems.Add(item);
        }

        visitedItems.Should().Equal(new[] { 1, 2, 3 });
    }

    /// <summary>
    /// Tests that AddRange adds multiple items efficiently and fires batch events.
    /// All items should be added in order with appropriate event notifications.
    /// </summary>
    [Fact]
    public void AddRange_AddsMultipleItemsAndFiresEvents()
    {
        var list = new ObservableList<int> { 1 };
        var collectionChangedFired = false;
        var itemAddedCount = 0;

        list.CollectionChanged += (s, e) => collectionChangedFired = true;
        list.ItemAdded += item => itemAddedCount++;

        var itemsToAdd = new[] { 2, 3, 4 };
        list.AddRange(itemsToAdd);

        list.Count.Should().Be(4);
        list.Should().Equal(new[] { 1, 2, 3, 4 });
        collectionChangedFired.Should().BeTrue();
        itemAddedCount.Should().Be(3);
    }

    /// <summary>
    /// Tests that InsertRange inserts multiple items at specified index.
    /// Items should be inserted in order at the specified position.
    /// </summary>
    [Fact]
    public void InsertRange_InsertsMultipleItemsAtIndex()
    {
        var list = new ObservableList<int> { 1, 4 };
        var collectionChangedFired = false;

        list.CollectionChanged += (s, e) => collectionChangedFired = true;

        var itemsToInsert = new[] { 2, 3 };
        list.InsertRange(1, itemsToInsert);

        list.Count.Should().Be(4);
        list.Should().Equal(new[] { 1, 2, 3, 4 });
        collectionChangedFired.Should().BeTrue();
    }

    /// <summary>
    /// Tests that Remove operations work correctly with multiple items.
    /// Removing multiple items should update the list correctly.
    /// </summary>
    [Fact]
    public void RemoveMultipleItems_WorksCorrectly()
    {
        var list = new ObservableList<int> { 1, 2, 3, 4, 5 };
        var collectionChangedCount = 0;

        list.CollectionChanged += (s, e) => collectionChangedCount++;

        list.RemoveAt(1); // Remove item at index 1 (value 2)
        list.RemoveAt(1); // Remove item at index 1 (now value 3)

        list.Count.Should().Be(3);
        list.Should().Equal(new[] { 1, 4, 5 });
        collectionChangedCount.Should().Be(2);
    }

    /// <summary>
    /// Tests that Sort reorders items and fires appropriate events.
    /// The list should be sorted according to the natural ordering or provided comparer.
    /// </summary>
    [Fact]
    public void Sort_ReordersItemsAndFiresEvents()
    {
        var list = new ObservableList<int> { 3, 1, 4, 1, 5 };
        var collectionChangedFired = false;

        list.CollectionChanged += (s, e) => collectionChangedFired = true;

        list.Sort();

        list.Should().Equal(new[] { 1, 1, 3, 4, 5 });
        collectionChangedFired.Should().BeTrue();
    }

    /// <summary>
    /// Tests that Reverse reorders items in reverse and fires events.
    /// The list order should be completely reversed.
    /// </summary>
    [Fact]
    public void Reverse_ReversesOrderAndFiresEvents()
    {
        var list = new ObservableList<int> { 1, 2, 3, 4, 5 };
        var collectionChangedFired = false;

        list.CollectionChanged += (s, e) => collectionChangedFired = true;

        list.Reverse();

        list.Should().Equal(new[] { 5, 4, 3, 2, 1 });
        collectionChangedFired.Should().BeTrue();
    }

    /// <summary>
    /// Tests that Capacity property can be read and set correctly.
    /// Setting capacity should trigger PropertyChanged events when different.
    /// </summary>
    [Fact]
    public void Capacity_CanBeSetAndTriggersEvents()
    {
        var list = new ObservableList<int>();
        var propertyChangedFired = false;

        list.PropertyChanged += (s, e) => 
        {
            if (e.PropertyName == "Capacity") propertyChangedFired = true;
        };

        list.Capacity = 100;

        list.Capacity.Should().BeGreaterOrEqualTo(100);
        propertyChangedFired.Should().BeTrue();
    }

    /// <summary>
    /// Tests that Version property increments correctly with modifications.
    /// The version should increment for each operation that modifies the list.
    /// </summary>
    [Fact]
    public void Version_IncrementsWithModifications()
    {
        var list = new ObservableList<int>();

        list.Version.Should().Be(0);

        list.Add(1);
        list.Version.Should().Be(1);

        list.Insert(0, 0);
        list.Version.Should().Be(2);

        list[0] = 10;
        list.Version.Should().Be(3);

        list.RemoveAt(0);
        list.Version.Should().Be(4);

        list.Clear();
        list.Version.Should().Be(5);
    }

    /// <summary>
    /// Tests that multiple event subscribers all receive notifications.
    /// All registered event handlers should be invoked for each modification.
    /// </summary>
    [Fact]
    public void MultipleEventSubscribers_AllReceiveNotifications()
    {
        var list = new ObservableList<int>();
        var subscriber1Fired = false;
        var subscriber2Fired = false;

        list.CollectionChanged += (s, e) => subscriber1Fired = true;
        list.CollectionChanged += (s, e) => subscriber2Fired = true;

        list.Add(42);

        subscriber1Fired.Should().BeTrue();
        subscriber2Fired.Should().BeTrue();
    }

    /// <summary>
    /// Tests that event unsubscription works correctly.
    /// Unsubscribed handlers should not receive further notifications.
    /// </summary>
    [Fact]
    public void EventUnsubscription_StopsNotifications()
    {
        var list = new ObservableList<int>();
        var fired = false;

        NotifyCollectionChangedEventHandler handler = (s, e) => fired = true;
        list.CollectionChanged += handler;
        list.Add(1);
        fired.Should().BeTrue();

        fired = false;
        list.CollectionChanged -= handler;
        list.Add(2);
        fired.Should().BeFalse();
    }

    /// <summary>
    /// Tests that ObservableList works correctly with custom object types.
    /// Custom objects should be handled properly with event notifications.
    /// </summary>
    [Fact]
    public void ObservableList_WithCustomObjects_WorksCorrectly()
    {
        var list = new ObservableList<Person>();
        var person1 = new Person("Alice", 25);
        var person2 = new Person("Bob", 30);

        var addedItems = new List<Person>();
        list.ItemAdded += p => addedItems.Add(p);

        list.Add(person1);
        list.Add(person2);

        list.Count.Should().Be(2);
        list[0].Should().Be(person1);
        list[1].Should().Be(person2);
        addedItems.Should().Contain(person1);
        addedItems.Should().Contain(person2);
        addedItems.Count.Should().Be(2);
    }

    /// <summary>
    /// Tests edge case of empty ObservableList operations.
    /// Empty list operations should behave correctly and fire appropriate events.
    /// </summary>
    [Fact]
    public void EmptyList_HandlesOperationsCorrectly()
    {
        var list = new ObservableList<int>();

        list.Count.Should().Be(0);
        list.Contains(1).Should().BeFalse();
        list.IndexOf(1).Should().Be(-1);
        list.Remove(1).Should().BeFalse();

        // Operations that should work on empty list
        list.Clear(); // Should not throw
        list.AddRange(new[] { 1, 2 });
        list.Count.Should().Be(2);
    }

    /// <summary>
    /// Tests that events are not fired when no subscribers are attached.
    /// The list should function normally without event overhead when no one is listening.
    /// </summary>
    [Fact]
    public void NoEventSubscribers_PerformsNormally()
    {
        var list = new ObservableList<int>();

        // Should not throw even without event subscribers
        list.Add(1);
        list.Add(2);
        list.Insert(1, 10);
        list[0] = 100;
        list.Remove(10);
        list.RemoveAt(0);
        list.Clear();

        list.Count.Should().Be(0);
    }

    /// <summary>
    /// Tests that large batch operations maintain performance and fire appropriate events.
    /// Large operations should be efficient and trigger proper event notifications.
    /// </summary>
    [Fact]
    public void LargeBatchOperations_MaintainPerformanceAndEvents()
    {
        var list = new ObservableList<int>();
        var eventCount = 0;

        list.CollectionChanged += (s, e) => eventCount++;

        var largeRange = Enumerable.Range(1, 1000).ToArray();
        list.AddRange(largeRange);

        list.Count.Should().Be(1000);
        list.Should().Equal(largeRange);
        eventCount.Should().BeGreaterThan(0); // Should fire events for batch operation
    }

    #region Async Tests

    /// <summary>
    /// Tests that AddRangeAsync correctly adds items and fires appropriate events.
    /// </summary>
    [Fact]
    public async Task AddRangeAsync_AddsItems_FiresEvents()
    {
        var list = new ObservableList<int>();
        var eventCount = 0;
        list.ListChanged += () => eventCount++;

        var itemsToAdd = Enumerable.Range(1, 100).ToList();
        
        await list.AddRangeAsync(itemsToAdd);

        list.Count.Should().Be(100);
        list.Should().Equal(itemsToAdd);
        eventCount.Should().Be(1); // Should fire single event for batch operation
    }

    /// <summary>
    /// Tests that AddRangeAsync respects cancellation tokens.
    /// </summary>
    [Fact]
    public async Task AddRangeAsync_WithCancellation_ThrowsOperationCancelledException()
    {
        var list = new ObservableList<int>();
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        var itemsToAdd = Enumerable.Range(1, 100).ToList();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => list.AddRangeAsync(itemsToAdd, cts.Token));
    }

    /// <summary>
    /// Tests that AddRangeAsync handles empty collections correctly.
    /// </summary>
    [Fact]
    public async Task AddRangeAsync_EmptyCollection_DoesNothing()
    {
        var list = new ObservableList<int>();
        var eventCount = 0;
        list.ListChanged += () => eventCount++;

        await list.AddRangeAsync(new List<int>());

        list.Count.Should().Be(0);
        eventCount.Should().Be(0); // No events for empty operation
    }

    /// <summary>
    /// Tests that RemoveAllAsync correctly removes matching items.
    /// </summary>
    [Fact]
    public async Task RemoveAllAsync_RemovesMatchingItems_ReturnsCount()
    {
        var list = new ObservableList<int>();
        list.AddRange(Enumerable.Range(1, 10));

        var removedCount = await list.RemoveAllAsync(x => x % 2 == 0); // Remove even numbers

        removedCount.Should().Be(5);
        list.Count.Should().Be(5);
        list.Should().Equal(new[] { 1, 3, 5, 7, 9 });
    }

    /// <summary>
    /// Tests that RemoveAllAsync respects cancellation tokens.
    /// </summary>
    [Fact]
    public async Task RemoveAllAsync_WithCancellation_ThrowsOperationCancelledException()
    {
        var list = new ObservableList<int>();
        list.AddRange(Enumerable.Range(1, 100));
        
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => list.RemoveAllAsync(x => x > 50, cts.Token));
    }

    /// <summary>
    /// Tests that RemoveAllAsync returns 0 when no items match.
    /// </summary>
    [Fact]
    public async Task RemoveAllAsync_NoMatches_ReturnsZero()
    {
        var list = new ObservableList<int>();
        list.AddRange(new[] { 1, 3, 5 });

        var removedCount = await list.RemoveAllAsync(x => x % 2 == 0);

        removedCount.Should().Be(0);
        list.Count.Should().Be(3);
        list.Should().Equal(new[] { 1, 3, 5 });
    }

    /// <summary>
    /// Tests that BatchUpdateAsync executes operations and fires events.
    /// </summary>
    [Fact]
    public async Task BatchUpdateAsync_ExecutesOperations_FiresEvents()
    {
        var list = new ObservableList<int>();
        var eventCount = 0;
        list.ListChanged += () => eventCount++;

        await list.BatchUpdateAsync(async (l, ct) =>
        {
            await l.AddRangeAsync(new[] { 1, 2, 3 }, ct);
            l.Add(4);
            await Task.Yield(); // Simulate async work
        });

        list.Count.Should().Be(4);
        list.Should().Equal(new[] { 1, 2, 3, 4 });
        eventCount.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Tests that BatchUpdateAsync respects cancellation tokens.
    /// </summary>
    [Fact]
    public async Task BatchUpdateAsync_WithCancellation_ThrowsOperationCancelledException()
    {
        var list = new ObservableList<int>();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => 
            list.BatchUpdateAsync(async (l, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                await Task.Delay(100, ct);
            }, cts.Token));
    }

    /// <summary>
    /// Tests that GetAsyncEnumerator correctly enumerates items.
    /// </summary>
    [Fact]
    public async Task GetAsyncEnumerator_EnumeratesItems_Correctly()
    {
        var list = new ObservableList<int>();
        list.AddRange(new[] { 1, 2, 3, 4, 5 });

        var results = new List<int>();
        await foreach (var item in list)
        {
            results.Add(item);
        }

        results.Should().Equal(new[] { 1, 2, 3, 4, 5 });
    }

    /// <summary>
    /// Tests that GetAsyncEnumerator respects cancellation tokens.
    /// </summary>
    [Fact]
    public async Task GetAsyncEnumerator_WithCancellation_ThrowsOperationCancelledException()
    {
        var list = new ObservableList<int>();
        list.AddRange(Enumerable.Range(1, 1000));

        var cts = new CancellationTokenSource();
        var results = new List<int>();
        
        // Cancel after processing a few items
        var enumerationTask = Task.Run(async () =>
        {
            await foreach (var item in list.WithCancellation(cts.Token))
            {
                results.Add(item);
                if (results.Count == 5)
                {
                    cts.Cancel();
                }
            }
        });

        await Assert.ThrowsAsync<OperationCanceledException>(() => enumerationTask);
        results.Count.Should().Be(5); // Should have processed 5 items before cancellation
    }

    /// <summary>
    /// Tests that GetAsyncEnumerator detects collection modifications during enumeration.
    /// </summary>
    [Fact]
    public async Task GetAsyncEnumerator_CollectionModified_ThrowsInvalidOperationException()
    {
        var list = new ObservableList<int>();
        list.AddRange(new[] { 1, 2, 3, 4, 5 });

        var results = new List<int>();
        var enumerationTask = Task.Run(async () =>
        {
            await foreach (var item in list)
            {
                results.Add(item);
                if (results.Count == 2)
                {
                    // Modify collection during enumeration
                    list.Add(999);
                }
            }
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => enumerationTask);
    }

    /// <summary>
    /// Tests that async operations work correctly with large datasets to verify batching.
    /// </summary>
    [Fact]
    public async Task AddRangeAsync_LargeDataset_ProcessesInBatches()
    {
        var list = new ObservableList<int>();
        var largeDataset = Enumerable.Range(1, 10000).ToList();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await list.AddRangeAsync(largeDataset);
        stopwatch.Stop();

        list.Count.Should().Be(10000);
        list.Should().Equal(largeDataset);
        
        // Should complete reasonably quickly (batching allows yielding)
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
    }

    /// <summary>
    /// Tests that async operations properly handle null arguments.
    /// </summary>
    [Fact]
    public async Task AddRangeAsync_NullItems_ThrowsArgumentNullException()
    {
        var list = new ObservableList<int>();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => list.AddRangeAsync(null!));
    }

    /// <summary>
    /// Tests that RemoveAllAsync handles null predicate correctly.
    /// </summary>
    [Fact]
    public async Task RemoveAllAsync_NullPredicate_ThrowsArgumentNullException()
    {
        var list = new ObservableList<int>();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => list.RemoveAllAsync(null!));
    }

    /// <summary>
    /// Tests that BatchUpdateAsync handles null operations correctly.
    /// </summary>
    [Fact]
    public async Task BatchUpdateAsync_NullOperations_ThrowsArgumentNullException()
    {
        var list = new ObservableList<int>();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => list.BatchUpdateAsync(null!));
    }

    #endregion

    private record Person(string Name, int Age);
}
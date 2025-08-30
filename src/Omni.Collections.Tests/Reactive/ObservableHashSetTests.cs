using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using FluentAssertions;
using Omni.Collections.Reactive;
using Xunit;

namespace Omni.Collections.Tests.Reactive;

public class ObservableHashSetTests
{
    /// <summary>
    /// Tests that an ObservableHashSet can be constructed with default parameters.
    /// The set should initialize empty with proper event handlers ready.
    /// </summary>
    [Fact]
    public void Constructor_Default_InitializesCorrectly()
    {
        var set = new ObservableHashSet<int>();

        set.Count.Should().Be(0);
        set.IsReadOnly.Should().BeFalse();
        set.Version.Should().Be(0);
    }

    /// <summary>
    /// Tests that an ObservableHashSet can be constructed with initial capacity.
    /// The set should initialize empty with the specified capacity hint.
    /// </summary>
    [Fact]
    public void Constructor_WithCapacity_InitializesCorrectly()
    {
        var set = new ObservableHashSet<int>(100);

        set.Count.Should().Be(0);
        set.IsReadOnly.Should().BeFalse();
    }

    /// <summary>
    /// Tests that an ObservableHashSet can be constructed with initial collection.
    /// The set should contain all unique items from the provided collection.
    /// </summary>
    [Fact]
    public void Constructor_WithCollection_InitializesWithItems()
    {
        var items = new[] { 1, 2, 3, 2, 4, 3 }; // Contains duplicates
        var set = new ObservableHashSet<int>(items);

        set.Count.Should().Be(4); // Duplicates removed
        set.Should().Contain(new[] { 1, 2, 3, 4 });
    }

    /// <summary>
    /// Tests that an ObservableHashSet can be constructed with custom equality comparer.
    /// The set should use the provided comparer for item comparison.
    /// </summary>
    [Fact]
    public void Constructor_WithComparer_UsesCustomComparer()
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        var set = new ObservableHashSet<string>(comparer);

        set.Add("Hello");
        set.Add("HELLO");

        set.Count.Should().Be(1, "case-insensitive comparer should treat these as equal");
    }

    /// <summary>
    /// Tests that CreateWithEventPooling creates a set with event pooling enabled.
    /// The set should use pooled event arguments to reduce allocation pressure.
    /// </summary>
    [Fact]
    public void CreateWithEventPooling_CreatesSetWithPooling()
    {
        var set = ObservableHashSet<int>.CreateWithEventPooling();

        set.Count.Should().Be(0);
        set.IsReadOnly.Should().BeFalse();
    }

    /// <summary>
    /// Tests that CreateWithEventPooling with collection creates pooled set with initial items.
    /// The set should contain all unique items with event pooling enabled.
    /// </summary>
    [Fact]
    public void CreateWithEventPooling_WithCollection_CreatesPooledSetWithItems()
    {
        var items = new[] { 1, 2, 3 };
        var set = ObservableHashSet<int>.CreateWithEventPooling(items);

        set.Count.Should().Be(3);
        set.Should().Contain(items);
    }

    /// <summary>
    /// Tests that Add successfully adds new items and fires appropriate events.
    /// Adding new items should trigger CollectionChanged, PropertyChanged, and custom events.
    /// </summary>
    [Fact]
    public void Add_NewItem_AddsItemAndFiresEvents()
    {
        var set = new ObservableHashSet<int>();
        var collectionChangedFired = false;
        var propertyChangedFired = false;
        var itemAddedFired = false;
        var setChangedFired = false;

        set.CollectionChanged += (s, e) => 
        {
            collectionChangedFired = true;
            e.Action.Should().Be(NotifyCollectionChangedAction.Add);
            e.NewItems!.Cast<int>().Should().Contain(42);
        };
        set.PropertyChanged += (s, e) => 
        {
            if (e.PropertyName == "Count") propertyChangedFired = true;
        };
        set.ItemAdded += item => 
        {
            itemAddedFired = true;
            item.Should().Be(42);
        };
        set.SetChanged += () => setChangedFired = true;

        var result = set.Add(42);

        result.Should().BeTrue();
        set.Count.Should().Be(1);
        set.Version.Should().Be(1);
        collectionChangedFired.Should().BeTrue();
        propertyChangedFired.Should().BeTrue();
        itemAddedFired.Should().BeTrue();
        setChangedFired.Should().BeTrue();
    }

    /// <summary>
    /// Tests that Add returns false for duplicate items without firing events.
    /// Adding existing items should not modify the set or trigger events.
    /// </summary>
    [Fact]
    public void Add_DuplicateItem_ReturnsFalseWithoutEvents()
    {
        var set = new ObservableHashSet<int>();
        set.Add(42);

        var collectionChangedFired = false;
        var propertyChangedFired = false;
        set.CollectionChanged += (s, e) => collectionChangedFired = true;
        set.PropertyChanged += (s, e) => propertyChangedFired = true;

        var result = set.Add(42);

        result.Should().BeFalse();
        set.Count.Should().Be(1);
        set.Version.Should().Be(1); // Should not increment
        collectionChangedFired.Should().BeFalse();
        propertyChangedFired.Should().BeFalse();
    }

    /// <summary>
    /// Tests that AddRange adds multiple new items and fires batch events.
    /// The method should add all unique items and fire appropriate events for the batch.
    /// </summary>
    [Fact]
    public void AddRange_MultipleItems_AddsBatchAndFiresEvents()
    {
        var set = new ObservableHashSet<int>();
        var collectionChangedFired = false;
        var itemAddedCount = 0;

        set.CollectionChanged += (s, e) => 
        {
            collectionChangedFired = true;
            e.Action.Should().Be(NotifyCollectionChangedAction.Add);
            e.NewItems!.Cast<int>().Should().HaveCount(3);
        };
        set.ItemAdded += item => itemAddedCount++;

        var itemsToAdd = new[] { 1, 2, 3 };
        var addedCount = set.AddRange(itemsToAdd);

        addedCount.Should().Be(3);
        set.Count.Should().Be(3);
        set.Should().Contain(itemsToAdd);
        collectionChangedFired.Should().BeTrue();
        itemAddedCount.Should().Be(3);
    }

    /// <summary>
    /// Tests that AddRange handles duplicates correctly in batch operations.
    /// Only new items should be added and included in event notifications.
    /// </summary>
    [Fact]
    public void AddRange_WithDuplicates_AddsOnlyNewItems()
    {
        var set = new ObservableHashSet<int>();
        set.Add(2); // Pre-existing item

        var itemsToAdd = new[] { 1, 2, 3, 2 }; // Contains duplicates and existing item
        var addedCount = set.AddRange(itemsToAdd);

        addedCount.Should().Be(2); // Only 1 and 3 should be added
        set.Count.Should().Be(3);
        set.Should().Contain(new[] { 1, 2, 3 });
    }

    /// <summary>
    /// Tests that Remove successfully removes existing items and fires appropriate events.
    /// Removing existing items should trigger CollectionChanged, PropertyChanged, and custom events.
    /// </summary>
    [Fact]
    public void Remove_ExistingItem_RemovesItemAndFiresEvents()
    {
        var set = new ObservableHashSet<int> { 42 };
        var collectionChangedFired = false;
        var propertyChangedFired = false;
        var itemRemovedFired = false;

        set.CollectionChanged += (s, e) => 
        {
            collectionChangedFired = true;
            e.Action.Should().Be(NotifyCollectionChangedAction.Remove);
            e.OldItems!.Cast<int>().Should().Contain(42);
        };
        set.PropertyChanged += (s, e) => 
        {
            if (e.PropertyName == "Count") propertyChangedFired = true;
        };
        set.ItemRemoved += item => 
        {
            itemRemovedFired = true;
            item.Should().Be(42);
        };

        var result = set.Remove(42);

        result.Should().BeTrue();
        set.Count.Should().Be(0);
        set.Version.Should().Be(2); // Add + Remove
        collectionChangedFired.Should().BeTrue();
        propertyChangedFired.Should().BeTrue();
        itemRemovedFired.Should().BeTrue();
    }

    /// <summary>
    /// Tests that Remove returns false for non-existent items without firing events.
    /// Removing non-existent items should not modify the set or trigger events.
    /// </summary>
    [Fact]
    public void Remove_NonExistentItem_ReturnsFalseWithoutEvents()
    {
        var set = new ObservableHashSet<int> { 1, 2, 3 };
        var collectionChangedFired = false;
        var propertyChangedFired = false;

        set.CollectionChanged += (s, e) => collectionChangedFired = true;
        set.PropertyChanged += (s, e) => propertyChangedFired = true;

        var result = set.Remove(99);

        result.Should().BeFalse();
        set.Count.Should().Be(3);
        collectionChangedFired.Should().BeFalse();
        propertyChangedFired.Should().BeFalse();
    }

    /// <summary>
    /// Tests that RemoveWhere removes items matching predicate and fires batch events.
    /// All items matching the condition should be removed with appropriate event notifications.
    /// </summary>
    [Fact]
    public void RemoveWhere_MatchingItems_RemovesBatchAndFiresEvents()
    {
        var set = new ObservableHashSet<int> { 1, 2, 3, 4, 5 };
        var collectionChangedFired = false;
        var itemRemovedCount = 0;

        set.CollectionChanged += (s, e) => 
        {
            collectionChangedFired = true;
            e.Action.Should().Be(NotifyCollectionChangedAction.Remove);
        };
        set.ItemRemoved += item => itemRemovedCount++;

        var removedCount = set.RemoveWhere(x => x % 2 == 0); // Remove even numbers

        removedCount.Should().Be(2); // 2 and 4
        set.Count.Should().Be(3);
        set.Should().Contain(new[] { 1, 3, 5 });
        collectionChangedFired.Should().BeTrue();
        itemRemovedCount.Should().Be(2);
    }

    /// <summary>
    /// Tests that RemoveWhere with no matches doesn't fire events.
    /// When no items match the predicate, no events should be fired.
    /// </summary>
    [Fact]
    public void RemoveWhere_NoMatches_DoesNotFireEvents()
    {
        var set = new ObservableHashSet<int> { 1, 3, 5 };
        var collectionChangedFired = false;

        set.CollectionChanged += (s, e) => collectionChangedFired = true;

        var removedCount = set.RemoveWhere(x => x % 2 == 0); // No even numbers

        removedCount.Should().Be(0);
        set.Count.Should().Be(3);
        collectionChangedFired.Should().BeFalse();
    }

    /// <summary>
    /// Tests that Clear removes all items and fires appropriate events.
    /// Clearing should reset the set and trigger reset events.
    /// </summary>
    [Fact]
    public void Clear_RemovesAllItemsAndFiresEvents()
    {
        var set = new ObservableHashSet<int> { 1, 2, 3 };
        var collectionChangedFired = false;
        var propertyChangedFired = false;
        var setClearedFired = false;

        set.CollectionChanged += (s, e) => 
        {
            collectionChangedFired = true;
            e.Action.Should().Be(NotifyCollectionChangedAction.Reset);
        };
        set.PropertyChanged += (s, e) => 
        {
            if (e.PropertyName == "Count") propertyChangedFired = true;
        };
        set.SetCleared += () => setClearedFired = true;

        set.Clear();

        set.Count.Should().Be(0);
        collectionChangedFired.Should().BeTrue();
        propertyChangedFired.Should().BeTrue();
        setClearedFired.Should().BeTrue();
    }

    /// <summary>
    /// Tests that Contains correctly identifies existing and non-existing items.
    /// The method should return true for items in the set and false otherwise.
    /// </summary>
    [Fact]
    public void Contains_ChecksItemExistence()
    {
        var set = new ObservableHashSet<int> { 1, 2, 3 };

        set.Contains(2).Should().BeTrue();
        set.Contains(4).Should().BeFalse();
    }

    /// <summary>
    /// Tests that CopyTo copies set items to array at specified index.
    /// All items should be copied to the target array starting at the given index.
    /// </summary>
    [Fact]
    public void CopyTo_CopiesItemsToArray()
    {
        var set = new ObservableHashSet<int> { 1, 2, 3 };
        var array = new int[5];

        set.CopyTo(array, 1);

        array[0].Should().Be(0); // Unchanged
        array.Skip(1).Take(3).Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    /// <summary>
    /// Tests that CopyTo throws exception for invalid parameters.
    /// The method should validate array and index parameters.
    /// </summary>
    [Fact]
    public void CopyTo_WithInvalidParameters_ThrowsException()
    {
        var set = new ObservableHashSet<int> { 1, 2, 3 };

        var actNull = () => set.CopyTo(null!, 0);
        var actNegativeIndex = () => set.CopyTo(new int[5], -1);
        var actInsufficientSpace = () => set.CopyTo(new int[2], 0);

        actNull.Should().Throw<ArgumentNullException>();
        actNegativeIndex.Should().Throw<ArgumentOutOfRangeException>();
        actInsufficientSpace.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Tests that enumerator iterates through all items in the set.
    /// The enumerator should visit each item exactly once.
    /// </summary>
    [Fact]
    public void GetEnumerator_IteratesAllItems()
    {
        var set = new ObservableHashSet<int> { 1, 2, 3 };
        var visitedItems = new List<int>();

        foreach (var item in set)
        {
            visitedItems.Add(item);
        }

        visitedItems.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    /// <summary>
    /// Tests that set operations (UnionWith, IntersectWith, etc.) work correctly with events.
    /// Set operations should modify the set and fire appropriate events.
    /// </summary>
    [Fact]
    public void UnionWith_ModifiesSetAndFiresEvents()
    {
        var set = new ObservableHashSet<int> { 1, 2 };
        var collectionChangedFired = false;
        var setChangedFired = false;

        set.CollectionChanged += (s, e) => collectionChangedFired = true;
        set.SetChanged += () => setChangedFired = true;

        set.UnionWith(new[] { 2, 3, 4 });

        set.Count.Should().Be(4);
        set.Should().Contain(new[] { 1, 2, 3, 4 });
        collectionChangedFired.Should().BeTrue();
        setChangedFired.Should().BeTrue();
    }

    /// <summary>
    /// Tests that IntersectWith removes items not in the other collection.
    /// Only items present in both sets should remain after intersection.
    /// </summary>
    [Fact]
    public void IntersectWith_KeepsOnlyCommonItems()
    {
        var set = new ObservableHashSet<int> { 1, 2, 3, 4 };
        var collectionChangedFired = false;

        set.CollectionChanged += (s, e) => collectionChangedFired = true;

        set.IntersectWith(new[] { 2, 3, 5 });

        set.Count.Should().Be(2);
        set.Should().Contain(new[] { 2, 3 });
        collectionChangedFired.Should().BeTrue();
    }

    /// <summary>
    /// Tests that ExceptWith removes items present in the other collection.
    /// Items present in the other collection should be removed from this set.
    /// </summary>
    [Fact]
    public void ExceptWith_RemovesItemsInOther()
    {
        var set = new ObservableHashSet<int> { 1, 2, 3, 4 };
        var collectionChangedFired = false;

        set.CollectionChanged += (s, e) => collectionChangedFired = true;

        set.ExceptWith(new[] { 2, 3, 5 });

        set.Count.Should().Be(2);
        set.Should().Contain(new[] { 1, 4 });
        collectionChangedFired.Should().BeTrue();
    }

    /// <summary>
    /// Tests that SymmetricExceptWith keeps items in either set but not both.
    /// The symmetric difference should contain items unique to each set.
    /// </summary>
    [Fact]
    public void SymmetricExceptWith_KeepsSymmetricDifference()
    {
        var set = new ObservableHashSet<int> { 1, 2, 3 };
        var collectionChangedFired = false;

        set.CollectionChanged += (s, e) => collectionChangedFired = true;

        set.SymmetricExceptWith(new[] { 2, 3, 4, 5 });

        set.Count.Should().Be(3);
        set.Should().Contain(new[] { 1, 4, 5 });
        collectionChangedFired.Should().BeTrue();
    }

    /// <summary>
    /// Tests that set relationship methods work correctly.
    /// The methods should accurately determine relationships between sets.
    /// </summary>
    [Fact]
    public void SetRelationshipMethods_WorkCorrectly()
    {
        var set = new ObservableHashSet<int> { 1, 2, 3 };

        set.IsSubsetOf(new[] { 1, 2, 3, 4 }).Should().BeTrue();
        set.IsSupersetOf(new[] { 1, 2 }).Should().BeTrue();
        set.IsProperSubsetOf(new[] { 1, 2, 3, 4 }).Should().BeTrue();
        set.IsProperSupersetOf(new[] { 1, 2 }).Should().BeTrue();
        set.Overlaps(new[] { 3, 4, 5 }).Should().BeTrue();
        set.SetEquals(new[] { 3, 2, 1 }).Should().BeTrue(); // Order doesn't matter
    }

    /// <summary>
    /// Tests that Version property increments correctly with modifications.
    /// The version should increment for each operation that modifies the set.
    /// </summary>
    [Fact]
    public void Version_IncrementsWithModifications()
    {
        var set = new ObservableHashSet<int>();

        set.Version.Should().Be(0);

        set.Add(1);
        set.Version.Should().Be(1);

        set.Add(2);
        set.Version.Should().Be(2);

        set.Remove(1);
        set.Version.Should().Be(3);

        set.Clear();
        set.Version.Should().Be(4);
    }

    /// <summary>
    /// Tests that events are not fired when no subscribers are attached.
    /// The set should function normally without event overhead when no one is listening.
    /// </summary>
    [Fact]
    public void NoEventSubscribers_PerformsNormally()
    {
        var set = new ObservableHashSet<int>();

        // Should not throw even without event subscribers
        set.Add(1);
        set.Add(2);
        set.Remove(1);
        set.Clear();

        set.Count.Should().Be(0);
    }

    /// <summary>
    /// Tests that ObservableHashSet works correctly with custom object types.
    /// Custom objects should be handled properly with event notifications.
    /// </summary>
    [Fact]
    public void ObservableHashSet_WithCustomObjects_WorksCorrectly()
    {
        var set = new ObservableHashSet<Person>();
        var person1 = new Person("Alice", 25);
        var person2 = new Person("Bob", 30);

        var addedItems = new List<Person>();
        set.ItemAdded += p => addedItems.Add(p);

        set.Add(person1);
        set.Add(person2);

        set.Count.Should().Be(2);
        set.Should().Contain(person1);
        set.Should().Contain(person2);
        addedItems.Should().Contain(person1);
        addedItems.Should().Contain(person2);
        addedItems.Count.Should().Be(2);
    }

    /// <summary>
    /// Tests that multiple event subscribers all receive notifications.
    /// All registered event handlers should be invoked for each modification.
    /// </summary>
    [Fact]
    public void MultipleEventSubscribers_AllReceiveNotifications()
    {
        var set = new ObservableHashSet<int>();
        var subscriber1Fired = false;
        var subscriber2Fired = false;

        set.CollectionChanged += (s, e) => subscriber1Fired = true;
        set.CollectionChanged += (s, e) => subscriber2Fired = true;

        set.Add(42);

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
        var set = new ObservableHashSet<int>();
        var fired = false;

        NotifyCollectionChangedEventHandler handler = (s, e) => fired = true;
        set.CollectionChanged += handler;
        set.Add(1);
        fired.Should().BeTrue();

        fired = false;
        set.CollectionChanged -= handler;
        set.Add(2);
        fired.Should().BeFalse();
    }

    /// <summary>
    /// Tests edge case of empty ObservableHashSet operations.
    /// Empty set operations should behave correctly and fire appropriate events.
    /// </summary>
    [Fact]
    public void EmptySet_HandlesOperationsCorrectly()
    {
        var set = new ObservableHashSet<int>();

        set.Count.Should().Be(0);
        set.Contains(1).Should().BeFalse();
        set.Remove(1).Should().BeFalse();
        
        // Set operations on empty set
        set.UnionWith(new[] { 1, 2 });
        set.Count.Should().Be(2);

        set.Clear();
        set.IntersectWith(new[] { 1, 2 });
        set.Count.Should().Be(0);
    }

    private record Person(string Name, int Age);
}
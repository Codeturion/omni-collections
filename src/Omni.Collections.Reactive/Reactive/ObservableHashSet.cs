using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Omni.Collections.Reactive;

/// <summary>
/// A HashSet implementation with change notification support that enables reactive programming and automatic UI updates.
/// Provides O(1) Add/Remove/Contains operations with event notification overhead only when subscribers are present.
/// Perfect for game achievement systems, unlocked content tracking, multiplayer presence lists, WPF/MVVM scenarios,
/// or any unique collection requiring automatic UI synchronization and change notifications.
/// </summary>
public class ObservableHashSet<T> : ISet<T>, INotifyCollectionChanged, INotifyPropertyChanged
{
    private readonly HashSet<T> _items;
    private int _version;
    private readonly IEventArgsPool? _eventArgsPool;
    private readonly bool _useEventPooling;
    public event NotifyCollectionChangedEventHandler? CollectionChanged;
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<T>? ItemAdded;
    public event Action<T>? ItemRemoved;
    public event Action? SetCleared;
    public event Action? SetChanged;
    public int Count => _items.Count;
    public bool IsReadOnly => false;
    public int Version => _version;
    public ObservableHashSet() : this(eventArgsPool: null)
    {
    }

    private ObservableHashSet(IEventArgsPool? eventArgsPool)
    {
        _items = [];
        _eventArgsPool = eventArgsPool;
        _useEventPooling = eventArgsPool != null;
    }

    public ObservableHashSet(int capacity) : this(capacity, null, eventArgsPool: null)
    {
    }

    public ObservableHashSet(IEnumerable<T> collection) : this(collection, null, eventArgsPool: null)
    {
    }

    public ObservableHashSet(IEqualityComparer<T>? comparer) : this(0, comparer, eventArgsPool: null)
    {
    }

    public ObservableHashSet(IEnumerable<T> collection, IEqualityComparer<T>? comparer) : this(collection, comparer, eventArgsPool: null)
    {
    }

    private ObservableHashSet(int capacity, IEqualityComparer<T>? comparer, IEventArgsPool? eventArgsPool)
    {
        _items = capacity > 0 ? new HashSet<T>(capacity, comparer) : new HashSet<T>(comparer);
        _eventArgsPool = eventArgsPool;
        _useEventPooling = eventArgsPool != null;
    }

    private ObservableHashSet(IEnumerable<T> collection, IEqualityComparer<T>? comparer, IEventArgsPool? eventArgsPool)
    {
        _items = new HashSet<T>(collection, comparer);
        _eventArgsPool = eventArgsPool;
        _useEventPooling = eventArgsPool != null;
    }

    public static ObservableHashSet<T> CreateWithEventPooling()
    {
        return new ObservableHashSet<T>(OmniEventArgsPool.Shared);
    }

    public static ObservableHashSet<T> CreateWithEventPooling(IEnumerable<T> collection)
    {
        return new ObservableHashSet<T>(collection, comparer: null, eventArgsPool: OmniEventArgsPool.Shared);
    }

    public bool Add(T item)
    {
        if (_items.Add(item))
        {
            IncrementVersion();
            OnItemAdded(item);
            OnPropertyChanged(nameof(Count));
            OnCollectionChanged(NotifyCollectionChangedAction.Add, item);
            return true;
        }
        return false;
    }
    void ICollection<T>.Add(T item) => Add(item);
    public int AddRange(IEnumerable<T> items)
    {
        var addedItems = new List<T>();
        foreach (var item in items)
        {
            if (_items.Add(item))
            {
                addedItems.Add(item);
            }
        }
        if (addedItems.Count > 0)
        {
            IncrementVersion();
            foreach (var item in addedItems)
            {
                OnItemAdded(item);
            }
            OnPropertyChanged(nameof(Count));
            OnCollectionChanged(NotifyCollectionChangedAction.Add, addedItems);
        }
        return addedItems.Count;
    }

    public bool Remove(T item)
    {
        if (_items.Remove(item))
        {
            IncrementVersion();
            OnItemRemoved(item);
            OnPropertyChanged(nameof(Count));
            OnCollectionChanged(NotifyCollectionChangedAction.Remove, item);
            return true;
        }
        return false;
    }

    public int RemoveWhere(Predicate<T> predicate)
    {
        var removedItems = new List<T>();
        foreach (var item in _items)
        {
            if (predicate(item))
            {
                removedItems.Add(item);
            }
        }
        if (removedItems.Count > 0)
        {
            foreach (var item in removedItems)
            {
                _items.Remove(item);
            }
            IncrementVersion();
            foreach (var item in removedItems)
            {
                OnItemRemoved(item);
            }
            OnPropertyChanged(nameof(Count));
            OnCollectionChanged(NotifyCollectionChangedAction.Remove, removedItems);
        }
        return removedItems.Count;
    }

    public void Clear()
    {
        if (_items.Count > 0)
        {
            _items.Clear();
            IncrementVersion();
            OnSetCleared();
            OnPropertyChanged(nameof(Count));
            OnCollectionChanged(NotifyCollectionChangedAction.Reset);
        }
    }

    public bool Contains(T item) => _items.Contains(item);
    public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
    public HashSet<T>.Enumerator GetEnumerator()
    {
        return _items.GetEnumerator();
    }
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public void UnionWith(IEnumerable<T> other)
    {
        var addedItems = new List<T>();
        foreach (var item in other)
        {
            if (_items.Add(item))
            {
                addedItems.Add(item);
            }
        }
        if (addedItems.Count > 0)
        {
            IncrementVersion();
            foreach (var item in addedItems)
            {
                OnItemAdded(item);
            }
            OnPropertyChanged(nameof(Count));
            OnCollectionChanged(NotifyCollectionChangedAction.Add, addedItems);
        }
    }

    public void IntersectWith(IEnumerable<T> other)
    {
        HashSet<T>? otherSet = other as HashSet<T> ?? [..other];
        var removedItems = new List<T>();
        foreach (var item in _items.ToArray())
        {
            if (!otherSet.Contains(item))
            {
                _items.Remove(item);
                removedItems.Add(item);
            }
        }
        if (removedItems.Count > 0)
        {
            IncrementVersion();
            foreach (var item in removedItems)
            {
                OnItemRemoved(item);
            }
            OnPropertyChanged(nameof(Count));
            OnCollectionChanged(NotifyCollectionChangedAction.Remove, removedItems);
        }
    }

    public void ExceptWith(IEnumerable<T> other)
    {
        var removedItems = new List<T>();
        foreach (var item in other)
        {
            if (_items.Remove(item))
            {
                removedItems.Add(item);
            }
        }
        if (removedItems.Count > 0)
        {
            IncrementVersion();
            foreach (var item in removedItems)
            {
                OnItemRemoved(item);
            }
            OnPropertyChanged(nameof(Count));
            OnCollectionChanged(NotifyCollectionChangedAction.Remove, removedItems);
        }
    }

    public void SymmetricExceptWith(IEnumerable<T> other)
    {
        HashSet<T>? otherSet = other as HashSet<T> ?? [..other];
        var originalItems = new HashSet<T>(_items);
        var addedItems = new List<T>();
        var removedItems = new List<T>();
        foreach (var item in _items.ToArray())
        {
            if (otherSet.Contains(item))
            {
                _items.Remove(item);
                removedItems.Add(item);
            }
        }
        foreach (var item in otherSet)
        {
            if (!originalItems.Contains(item) && _items.Add(item))
            {
                addedItems.Add(item);
            }
        }
        if (addedItems.Count > 0 || removedItems.Count > 0)
        {
            IncrementVersion();
            foreach (var item in removedItems)
            {
                OnItemRemoved(item);
            }
            foreach (var item in addedItems)
            {
                OnItemAdded(item);
            }
            OnPropertyChanged(nameof(Count));
            OnCollectionChanged(NotifyCollectionChangedAction.Reset);
        }
    }

    public bool IsSubsetOf(IEnumerable<T> other) => _items.IsSubsetOf(other);
    public bool IsSupersetOf(IEnumerable<T> other) => _items.IsSupersetOf(other);
    public bool IsProperSubsetOf(IEnumerable<T> other) => _items.IsProperSubsetOf(other);
    public bool IsProperSupersetOf(IEnumerable<T> other) => _items.IsProperSupersetOf(other);
    public bool Overlaps(IEnumerable<T> other) => _items.Overlaps(other);
    public bool SetEquals(IEnumerable<T> other) => _items.SetEquals(other);
    public T[] ToArray()
    {
        var array = new T[_items.Count];
        _items.CopyTo(array);
        return array;
    }

    public T[] CreateSnapshot() => ToArray();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void IncrementVersion()
    {
        _version++;
        OnSetChanged();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnItemAdded(T item)
    {
        ItemAdded?.Invoke(item);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnItemRemoved(T item)
    {
        ItemRemoved?.Invoke(item);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnSetCleared()
    {
        SetCleared?.Invoke();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnSetChanged()
    {
        SetChanged?.Invoke();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        if (PropertyChanged != null && propertyName != null)
        {
            if (_useEventPooling)
            {
                var eventArgs = _eventArgsPool!.RentPropertyChangedEventArgs(propertyName);
                PropertyChanged.Invoke(this, eventArgs);
                _eventArgsPool.ReturnPropertyChangedEventArgs(eventArgs);
            }
            else
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnCollectionChanged(NotifyCollectionChangedAction action)
    {
        if (CollectionChanged != null)
        {
            if (_useEventPooling && action == NotifyCollectionChangedAction.Reset)
            {
                var eventArgs = _eventArgsPool!.RentCollectionChangedEventArgs(action);
                CollectionChanged.Invoke(this, eventArgs);
                _eventArgsPool.ReturnCollectionChangedEventArgs(eventArgs);
            }
            else
            {
                CollectionChanged.Invoke(this, new NotifyCollectionChangedEventArgs(action));
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnCollectionChanged(NotifyCollectionChangedAction action, object? item)
    {
        if (CollectionChanged != null)
        {
            CollectionChanged.Invoke(this, new NotifyCollectionChangedEventArgs(action, item));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnCollectionChanged(NotifyCollectionChangedAction action, IList items)
    {
        if (CollectionChanged != null)
        {
            CollectionChanged.Invoke(this, new NotifyCollectionChangedEventArgs(action, items));
        }
    }
}

public static class ObservableHashSetExtensions
{
    public static void SubscribeToChanges<T>(this ObservableHashSet<T> set, Action onChanged)
    {
        set.SetChanged += onChanged;
    }

    public static void SubscribeToItems<T>(this ObservableHashSet<T> set, Action<T>? onAdded = null, Action<T>? onRemoved = null)
    {
        if (onAdded != null)
            set.ItemAdded += onAdded;
        if (onRemoved != null)
            set.ItemRemoved += onRemoved;
    }

    public static ObservableHashSet<T> CreateFilteredView<T>(this ObservableHashSet<T> source, Predicate<T> predicate)
    {
        var filtered = new ObservableHashSet<T>();
        foreach (var item in source)
        {
            if (predicate(item))
                filtered.Add(item);
        }
        source.ItemAdded += item =>
        {
            if (predicate(item))
                filtered.Add(item);
        };
        source.ItemRemoved += item =>
        {
            filtered.Remove(item);
        };
        source.SetCleared += () => filtered.Clear();
        return filtered;
    }

    public static bool Toggle<T>(this ObservableHashSet<T> set, T item)
    {
        if (set.Contains(item))
        {
            set.Remove(item);
            return false;
        }
        else
        {
            set.Add(item);
            return true;
        }
    }
}
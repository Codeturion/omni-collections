using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Omni.Collections.Reactive;

/// <summary>
/// A List implementation with change notification support that enables reactive programming and automatic UI updates.
/// Provides O(1) Add operations and O(n) RemoveAt with event notification overhead only when subscribers are present.
/// Excellent for game UI systems (multiplayer lobbies, leaderboards, inventory displays), WPF/MVVM scenarios,
/// or any collection requiring automatic UI synchronization and change notifications.
/// </summary>
public class ObservableList<T> : IList<T>, INotifyCollectionChanged, INotifyPropertyChanged, IDisposable, IAsyncEnumerable<T>
{
    private readonly List<T> _items;
    private int _version;
    private NotifyCollectionChangedEventHandler? _collectionChanged;
    private PropertyChangedEventHandler? _propertyChanged;
    private Action<T>? _itemAdded;
    private Action<T>? _itemRemoved;
    private Action<T, int>? _itemInserted;
    private Action<T, int>? _itemRemovedAt;
    private Action<T, T, int>? _itemReplaced;
    private Action? _listCleared;
    private Action? _listChanged;
    private bool _disposed;
    static private readonly PropertyChangedEventArgs CountChangedArgs = new PropertyChangedEventArgs("Count");
    static private readonly PropertyChangedEventArgs CapacityChangedArgs = new PropertyChangedEventArgs("Capacity");
    static private readonly NotifyCollectionChangedEventArgs ResetArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
    // Note: Add, Remove, Replace, and Move actions require specific items/indices, 
    // so we can't create static instances - they must be created per operation
    public event NotifyCollectionChangedEventHandler? CollectionChanged
    {
        add => _collectionChanged += value;
        remove => _collectionChanged -= value;
    }

    public event PropertyChangedEventHandler? PropertyChanged
    {
        add => _propertyChanged += value;
        remove => _propertyChanged -= value;
    }

    public event Action<T>? ItemAdded
    {
        add { _itemAdded += value; }
        remove { _itemAdded -= value; }
    }

    public event Action<T>? ItemRemoved
    {
        add { _itemRemoved += value; }
        remove { _itemRemoved -= value; }
    }

    public event Action<T, int>? ItemInserted
    {
        add { _itemInserted += value; }
        remove { _itemInserted -= value; }
    }

    public event Action<T, int>? ItemRemovedAt
    {
        add { _itemRemovedAt += value; }
        remove { _itemRemovedAt -= value; }
    }

    public event Action<T, T, int>? ItemReplaced
    {
        add { _itemReplaced += value; }
        remove { _itemReplaced -= value; }
    }

    public event Action? ListCleared
    {
        add { _listCleared += value; }
        remove { _listCleared -= value; }
    }

    public event Action? ListChanged
    {
        add { _listChanged += value; }
        remove { _listChanged -= value; }
    }

    public int Count => _items.Count;
    public int Capacity
    {
        get => _items.Capacity;
        set 
        { 
            if (_items.Capacity != value)
            {
                _items.Capacity = value;
                OnPropertyChanged(nameof(Capacity));
            }
        }
    }

    public bool IsReadOnly => false;
    public int Version => _version;
    public ObservableList()
    {
        _items = [];
    }

    public ObservableList(int capacity)
    {
        _items = new List<T>(capacity);
    }

    public ObservableList(IEnumerable<T> collection)
    {
        _items = [..collection];
    }

    public T this[int index]
    {
        get => _items[index];
        set
        {
            var oldItem = _items[index];
            _items[index] = value;
            IncrementVersion();
            OnItemReplaced(oldItem, value, index);
            OnPropertyChanged(nameof(Count));
            OnCollectionChanged(NotifyCollectionChangedAction.Replace, value, oldItem, index);
        }
    }

    public void Add(T item)
    {
        ThrowIfDisposed();
        var index = _items.Count;
        _items.Add(item);
        IncrementVersion();
        OnItemAdded(item);
        OnItemInserted(item, index);
        OnPropertyChanged(nameof(Count));
        OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
        OnListChanged();
    }

    public void AddRange(IEnumerable<T> items)
    {
        var startIndex = _items.Count;
        IList<T>? itemsList = items as IList<T> ?? items.ToList();
        if (itemsList.Count == 0) return;
        _items.AddRange(itemsList);
        IncrementVersion();
        
        // Fire ItemAdded events for each item
        foreach (T item in itemsList)
        {
            OnItemAdded(item);
        }
        
        OnPropertyChanged(nameof(Count));
        OnCollectionChanged(NotifyCollectionChangedAction.Add, itemsList, startIndex);
        OnListChanged();
    }

    public void Insert(int index, T item)
    {
        _items.Insert(index, item);
        IncrementVersion();
        OnItemAdded(item);
        OnItemInserted(item, index);
        OnPropertyChanged(nameof(Count));
        OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
        OnListChanged();
    }

    public void InsertRange(int index, IEnumerable<T> items)
    {
        IList<T>? itemsList = items as IList<T> ?? items.ToList();
        if (itemsList.Count == 0) return;
        _items.InsertRange(index, itemsList);
        IncrementVersion();
        OnPropertyChanged(nameof(Count));
        OnCollectionChanged(NotifyCollectionChangedAction.Add, itemsList, index);
        OnListChanged();
    }

    public bool Remove(T item)
    {
        var index = _items.IndexOf(item);
        if (index >= 0)
        {
            RemoveAt(index);
            return true;
        }
        return false;
    }

    public void RemoveAt(int index)
    {
        var item = _items[index];
        _items.RemoveAt(index);
        IncrementVersion();
        OnItemRemoved(item);
        OnItemRemovedAt(item, index);
        OnPropertyChanged(nameof(Count));
        OnCollectionChanged(NotifyCollectionChangedAction.Remove, item, index);
        OnListChanged();
    }

    public int RemoveAll(Predicate<T> predicate)
    {
        var removedItems = new List<T>();
        var removedIndices = new List<int>();
        for (int i = _items.Count - 1; i >= 0; i--)
        {
            if (predicate(_items[i]))
            {
                removedItems.Add(_items[i]);
                removedIndices.Add(i);
            }
        }
        if (removedItems.Count > 0)
        {
            for (int i = 0; i < removedIndices.Count; i++)
            {
                _items.RemoveAt(removedIndices[i]);
            }
            IncrementVersion();
            OnPropertyChanged(nameof(Count));
            OnCollectionChanged(NotifyCollectionChangedAction.Remove, removedItems, removedIndices[removedIndices.Count - 1]);
            OnListChanged();
        }
        return removedItems.Count;
    }

    public void Clear()
    {
        ThrowIfDisposed();
        if (_items.Count > 0)
        {
            _items.Clear();
            IncrementVersion();
            OnListCleared();
            OnPropertyChanged(nameof(Count));
            OnCollectionChanged(NotifyCollectionChangedAction.Reset);
        }
    }

    public void BatchUpdate(Action<ObservableList<T>> operations)
    {
        if (operations == null) throw new ArgumentNullException(nameof(operations));
        var initialCount = _items.Count;
        var initialVersion = _version;
        try
        {
            operations(this);
            if (_version != initialVersion)
            {
                if (_items.Count != initialCount)
                {
                    OnPropertyChanged(nameof(Count));
                }
                OnListChanged();
                OnCollectionChanged(NotifyCollectionChangedAction.Reset);
            }
        }
        catch
        {
            _version = initialVersion;
            throw;
        }
    }

    #region Async Operations

    /// <summary>
    /// Asynchronously adds a range of items with cancellation support.
    /// </summary>
    public async Task AddRangeAsync(IEnumerable<T> items, CancellationToken cancellationToken = default)
    {
        if (items == null) throw new ArgumentNullException(nameof(items));
        
        ThrowIfDisposed();
        
        var itemsList = items.ToList();
        if (itemsList.Count == 0) return;

        // Check for cancellation before starting
        cancellationToken.ThrowIfCancellationRequested();
        
        // Add items in batches to allow cancellation checks
        const int batchSize = 1000;
        var startIndex = _items.Count;
        
        for (int i = 0; i < itemsList.Count; i += batchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var batch = itemsList.Skip(i).Take(batchSize);
            _items.AddRange(batch);
            
            // Yield control periodically for long operations
            if (i > 0 && i % (batchSize * 10) == 0)
            {
                await Task.Yield();
            }
        }
        
        IncrementVersion();
        OnPropertyChanged(nameof(Count));
        OnCollectionChanged(NotifyCollectionChangedAction.Reset);
        OnListChanged();
    }

    /// <summary>
    /// Asynchronously removes all items matching the predicate with cancellation support.
    /// </summary>
    public async Task<int> RemoveAllAsync(Predicate<T> predicate, CancellationToken cancellationToken = default)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        
        ThrowIfDisposed();
        
        var removedItems = new List<T>();
        var removedCount = 0;
        
        // Process in batches to allow cancellation
        for (int i = _items.Count - 1; i >= 0; i--)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            if (predicate(_items[i]))
            {
                var item = _items[i];
                _items.RemoveAt(i);
                removedItems.Add(item);
                removedCount++;
                
                // Yield control periodically
                if (removedCount % 1000 == 0)
                {
                    await Task.Yield();
                }
            }
        }
        
        if (removedCount > 0)
        {
            IncrementVersion();
            OnPropertyChanged(nameof(Count));
            OnCollectionChanged(NotifyCollectionChangedAction.Reset);
            OnListChanged();
        }
        
        return removedCount;
    }

    /// <summary>
    /// Asynchronously performs batch operations with cancellation support.
    /// </summary>
    public async Task BatchUpdateAsync(Func<ObservableList<T>, CancellationToken, Task> operations, CancellationToken cancellationToken = default)
    {
        if (operations == null) throw new ArgumentNullException(nameof(operations));
        
        var initialCount = _items.Count;
        var initialVersion = _version;
        
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            await operations(this, cancellationToken);
            
            if (_version != initialVersion)
            {
                if (_items.Count != initialCount)
                {
                    OnPropertyChanged(nameof(Count));
                }
                OnListChanged();
                OnCollectionChanged(NotifyCollectionChangedAction.Reset);
            }
        }
        catch
        {
            _version = initialVersion;
            throw;
        }
    }

    /// <summary>
    /// Returns an async enumerator for the collection.
    /// </summary>
    public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        var version = _version;
        var snapshot = _items.ToArray(); // Take snapshot to avoid modification issues
        
        for (int i = 0; i < snapshot.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            // Check if collection was modified during enumeration
            if (_version != version)
            {
                throw new InvalidOperationException("Collection was modified during enumeration");
            }
            
            yield return snapshot[i];
            
            // Yield control periodically for large collections
            if (i > 0 && i % 100 == 0)
            {
                await Task.Yield();
            }
        }
    }

    #endregion

    public bool Contains(T item) => _items.Contains(item);
    public int IndexOf(T item) => _items.IndexOf(item);
    public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
    public FastEnumerator GetEnumerator() => new FastEnumerator(this);
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => new FastEnumerator(this);
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public struct FastEnumerator : IEnumerator<T>
    {
        private readonly List<T> _list;
        private readonly int _version;
        private readonly ObservableList<T> _parent;
        private int _index;
        private T _current;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal FastEnumerator(ObservableList<T> parent)
        {
            _parent = parent;
            _list = parent._items;
            _version = parent._version;
            _index = 0;
            _current = default!;
        }

        public readonly T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }
        readonly object IEnumerator.Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current!;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (_version != _parent._version)
                throw new InvalidOperationException("Collection was modified during enumeration");
            if (_index < _list.Count)
            {
                _current = _list[_index];
                _index++;
                return true;
            }
            _current = default!;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            if (_version != _parent._version)
                throw new InvalidOperationException("Collection was modified during enumeration");
            _index = 0;
            _current = default!;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Dispose()
        {
        }
    }

    public T[] ToArray() => _items.ToArray();
    public T? Find(Predicate<T> predicate) => _items.Find(predicate);
    public List<T> FindAll(Predicate<T> predicate) => _items.FindAll(predicate);
    public void Sort()
    {
        _items.Sort();
        IncrementVersion();
        OnListChanged();
        OnCollectionChanged(NotifyCollectionChangedAction.Reset);
    }

    public void Sort(IComparer<T> comparer)
    {
        _items.Sort(comparer);
        IncrementVersion();
        OnListChanged();
        OnCollectionChanged(NotifyCollectionChangedAction.Reset);
    }

    public void Reverse()
    {
        _items.Reverse();
        IncrementVersion();
        OnListChanged();
        OnCollectionChanged(NotifyCollectionChangedAction.Reset);
    }

    public T[] CreateSnapshot() => _items.ToArray();
    public ArrayEnumerator GetUnsafeEnumerator() => new ArrayEnumerator(this);
    public struct ArrayEnumerator : IEnumerator<T>
    {
        private readonly List<T> _list;
        private readonly int _count;
        private int _index;
        private T _current;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ArrayEnumerator(ObservableList<T> parent)
        {
            _list = parent._items;
            _count = parent._items.Count;
            _index = -1;
            _current = default!;
        }

        public readonly T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }
        readonly object IEnumerator.Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current!;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (++_index < _count)
            {
                _current = _list[_index];
                return true;
            }
            _current = default!;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            _index = -1;
            _current = default!;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Dispose()
        {
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void IncrementVersion()
    {
        _version++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnItemAdded(T item)
    {
        _itemAdded?.Invoke(item);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnItemRemoved(T item)
    {
        _itemRemoved?.Invoke(item);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnItemInserted(T item, int index)
    {
        _itemInserted?.Invoke(item, index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnItemRemovedAt(T item, int index)
    {
        _itemRemovedAt?.Invoke(item, index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnItemReplaced(T oldItem, T newItem, int index)
    {
        _itemReplaced?.Invoke(oldItem, newItem, index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnListCleared()
    {
        _listCleared?.Invoke();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnListChanged()
    {
        _listChanged?.Invoke();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        if (_propertyChanged != null && propertyName != null)
        {
            var eventArgs = propertyName switch
            {
                nameof(Count) => CountChangedArgs,
                nameof(Capacity) => CapacityChangedArgs,
                _ => new PropertyChangedEventArgs(propertyName)
            };
            _propertyChanged.Invoke(this, eventArgs);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnCollectionChanged(NotifyCollectionChangedAction action)
    {
        if (_collectionChanged != null)
        {
            // Only Reset action can be created without items/indices
            var eventArgs = action == NotifyCollectionChangedAction.Reset 
                ? ResetArgs 
                : new NotifyCollectionChangedEventArgs(action);
            _collectionChanged.Invoke(this, eventArgs);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnCollectionChanged(NotifyCollectionChangedAction action, object? item, int index)
    {
        _collectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, item, index));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnCollectionChanged(NotifyCollectionChangedAction action, object? newItem, object? oldItem, int index)
    {
        _collectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, newItem, oldItem, index));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnCollectionChanged(NotifyCollectionChangedAction action, IList items, int startingIndex)
    {
        _collectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, items, startingIndex));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            // Clear all event handlers to prevent memory leaks
            _collectionChanged = null;
            _propertyChanged = null;
            _itemAdded = null;
            _itemRemoved = null;
            _itemInserted = null;
            _itemRemovedAt = null;
            _itemReplaced = null;
            _listCleared = null;
            _listChanged = null;
            _disposed = true;
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ObservableList<T>));
    }
}

public static class ObservableListExtensions
{
    public static void SubscribeToChanges<T>(this ObservableList<T> list, Action onChanged)
    {
        list.ListChanged += onChanged;
    }

    public static void SubscribeToItems<T>(this ObservableList<T> list, Action<T>? onAdded = null, Action<T>? onRemoved = null)
    {
        if (onAdded != null)
            list.ItemAdded += onAdded;
        if (onRemoved != null)
            list.ItemRemoved += onRemoved;
    }

    public static ObservableList<T> CreateFilteredView<T>(this ObservableList<T> source, Predicate<T> predicate)
    {
        var filtered = new ObservableList<T>(source.FindAll(predicate));
        source.ItemAdded += item =>
        {
            if (predicate(item))
                filtered.Add(item);
        };
        source.ItemRemoved += item =>
        {
            filtered.Remove(item);
        };
        source.ListCleared += () => filtered.Clear();
        return filtered;
    }
}
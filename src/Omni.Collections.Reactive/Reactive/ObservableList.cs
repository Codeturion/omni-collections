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
    private bool _isNotifying;
    private static readonly PropertyChangedEventArgs CountChangedArgs = new PropertyChangedEventArgs("Count");
    private static readonly PropertyChangedEventArgs CapacityChangedArgs = new PropertyChangedEventArgs("Capacity");
    // Reset args have no per-call state and are safely shared via OmniEventArgsPool.Shared.
    // Add / Remove / Replace / Move args carry per-call state (items, indices) and cannot be
    // reused; OmniEventArgsPool.RentXxx allocates fresh instances for those — see IEventArgsPool docs.
    private static readonly IEventArgsPool _eventArgsPool = OmniEventArgsPool.Shared;
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
            ThrowIfNotifying();
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
        ThrowIfNotifying();
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
        ThrowIfNotifying();
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
        // Materialize as a non-generic IList so OnCollectionChanged binds to the (action, IList, int)
        // overload — not the (action, object, int) overload, which would box the whole list as a single item.
        OnCollectionChanged(NotifyCollectionChangedAction.Add, AsNonGenericList(itemsList), startIndex);
        OnListChanged();
    }

    public void Insert(int index, T item)
    {
        ThrowIfNotifying();
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
        ThrowIfNotifying();
        IList<T>? itemsList = items as IList<T> ?? items.ToList();
        if (itemsList.Count == 0) return;
        _items.InsertRange(index, itemsList);
        IncrementVersion();
        OnPropertyChanged(nameof(Count));
        // Materialize as non-generic IList — see AddRange comment.
        OnCollectionChanged(NotifyCollectionChangedAction.Add, AsNonGenericList(itemsList), index);
        OnListChanged();
    }

    public bool Remove(T item)
    {
        ThrowIfNotifying();
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
        ThrowIfNotifying();
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
        ThrowIfNotifying();
        // Single-pass swap-and-truncate: walk forward, copy survivors leftward over
        // removed slots, then truncate the tail. O(N) total — vs the previous
        // descending-index RemoveAt loop which was O(N*k) for k matches due to per-
        // match list shifts.
        int writeIndex = 0;
        int removedCount = 0;
        int count = _items.Count;
        for (int readIndex = 0; readIndex < count; readIndex++)
        {
            T item = _items[readIndex];
            if (predicate(item))
            {
                removedCount++;
            }
            else
            {
                if (writeIndex != readIndex)
                    _items[writeIndex] = item;
                writeIndex++;
            }
        }
        if (removedCount > 0)
        {
            _items.RemoveRange(writeIndex, removedCount);
            IncrementVersion();
            OnPropertyChanged(nameof(Count));
            // Removed items are typically non-contiguous, so the (Remove, items, index) shape
            // would be semantically wrong (INotifyCollectionChanged contract requires a single
            // contiguous range). Reset is the right action; subscribers needing per-item
            // notifications use ItemRemoved-aware variants.
            OnCollectionChanged(NotifyCollectionChangedAction.Reset);
            OnListChanged();
        }
        return removedCount;
    }

    public void Clear()
    {
        ThrowIfDisposed();
        ThrowIfNotifying();
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
    /// Fires a single batch <see cref="NotifyCollectionChangedAction.Add"/> notification with all items
    /// and the starting index, matching the synchronous <see cref="AddRange"/> contract.
    /// </summary>
    public async Task AddRangeAsync(IEnumerable<T> items, CancellationToken cancellationToken = default)
    {
        if (items == null) throw new ArgumentNullException(nameof(items));

        ThrowIfDisposed();
        ThrowIfNotifying();

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

        // Fire per-item ItemAdded events for fine-grained subscribers (separate channel from CollectionChanged).
        foreach (T item in itemsList)
        {
            OnItemAdded(item);
        }

        OnPropertyChanged(nameof(Count));
        OnCollectionChanged(NotifyCollectionChangedAction.Add, itemsList, startIndex);
        OnListChanged();
    }

    /// <summary>
    /// Asynchronously removes all items matching the predicate with cancellation support.
    /// Fires a single batch <see cref="NotifyCollectionChangedAction.Remove"/> notification with all
    /// removed items and the lowest removed index, matching the synchronous <see cref="RemoveAll"/> contract.
    /// </summary>
    public async Task<int> RemoveAllAsync(Predicate<T> predicate, CancellationToken cancellationToken = default)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        ThrowIfDisposed();
        ThrowIfNotifying();

        // Single-pass swap-and-truncate, same shape as the synchronous RemoveAll
        // but yields periodically and collects removed items so per-item
        // ItemRemoved events can fire after the structural mutation completes.
        var removedItems = new List<T>();
        int writeIndex = 0;
        int removedCount = 0;
        int yieldCounter = 0;
        int count = _items.Count;
        for (int readIndex = 0; readIndex < count; readIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            T item = _items[readIndex];
            if (predicate(item))
            {
                removedItems.Add(item);
                removedCount++;
            }
            else
            {
                if (writeIndex != readIndex)
                    _items[writeIndex] = item;
                writeIndex++;
            }
            if (++yieldCounter >= 1000)
            {
                yieldCounter = 0;
                await Task.Yield();
            }
        }

        if (removedCount > 0)
        {
            _items.RemoveRange(writeIndex, removedCount);
            IncrementVersion();

            // Fire per-item ItemRemoved events for fine-grained subscribers
            // (removedItems was collected in ascending index order — natural shape).
            foreach (T item in removedItems)
            {
                OnItemRemoved(item);
            }

            OnPropertyChanged(nameof(Count));
            // Non-contiguous removals: Reset is the correct action per the
            // INotifyCollectionChanged contract. Per-item ItemRemoved fires above for
            // subscribers that need fine-grained updates.
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

    /// <summary>
    /// Returns the supplied generic list as a non-generic <see cref="IList"/> so that
    /// <see cref="OnCollectionChanged(NotifyCollectionChangedAction, IList, int)"/> binds correctly.
    /// <c>IList&lt;T&gt;</c> has no implicit conversion to non-generic <see cref="IList"/>; without
    /// this cast the compiler picks the (object, int) overload and the entire list ends up as a
    /// single boxed entry in <c>NewItems</c>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IList AsNonGenericList(IList<T> items)
    {
        // Both List<T> and T[] (the producers in this class) implement the non-generic IList,
        // so the cast is a free reference conversion. Fall back to copying for exotic IList<T>
        // implementations that don't also implement IList.
        return items as IList ?? new List<T>(items);
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
            var eventArgs = _eventArgsPool.RentCollectionChangedEventArgs(action);
            _isNotifying = true;
            try
            {
                _collectionChanged.Invoke(this, eventArgs);
            }
            finally
            {
                _isNotifying = false;
                _eventArgsPool.ReturnCollectionChangedEventArgs(eventArgs);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnCollectionChanged(NotifyCollectionChangedAction action, object? item, int index)
    {
        if (_collectionChanged != null)
        {
            NotifyCollectionChangedEventArgs eventArgs = action == NotifyCollectionChangedAction.Add
                ? _eventArgsPool.RentAdd(item, index)
                : _eventArgsPool.RentRemove(item, index);
            _isNotifying = true;
            try
            {
                _collectionChanged.Invoke(this, eventArgs);
            }
            finally
            {
                _isNotifying = false;
                _eventArgsPool.ReturnCollectionChangedEventArgs(eventArgs);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnCollectionChanged(NotifyCollectionChangedAction action, object? newItem, object? oldItem, int index)
    {
        if (_collectionChanged != null)
        {
            var eventArgs = _eventArgsPool.RentReplace(newItem, oldItem, index);
            _isNotifying = true;
            try
            {
                _collectionChanged.Invoke(this, eventArgs);
            }
            finally
            {
                _isNotifying = false;
                _eventArgsPool.ReturnCollectionChangedEventArgs(eventArgs);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnCollectionChanged(NotifyCollectionChangedAction action, IList items, int startingIndex)
    {
        if (_collectionChanged != null)
        {
            NotifyCollectionChangedEventArgs eventArgs = action == NotifyCollectionChangedAction.Add
                ? _eventArgsPool.RentAddRange(items, startingIndex)
                : _eventArgsPool.RentRemoveRange(items, startingIndex);
            _isNotifying = true;
            try
            {
                _collectionChanged.Invoke(this, eventArgs);
            }
            finally
            {
                _isNotifying = false;
                _eventArgsPool.ReturnCollectionChangedEventArgs(eventArgs);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfNotifying()
    {
        if (_isNotifying)
            throw new InvalidOperationException("Cannot mutate the collection during a notification callback.");
    }

    public virtual void Dispose()
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

/// <summary>
/// A live filtered view over an <see cref="ObservableList{T}"/>. Subscribes to the source's
/// item events on construction; <see cref="Dispose"/> unsubscribes those handlers so the source
/// no longer holds a reference to this view.
/// </summary>
/// <remarks>
/// Direct mutation of the view (e.g., <c>view.Add(item)</c>) modifies the view in isolation —
/// the source is unaffected, and a subsequent source-side mutation that the predicate filters
/// can overwrite the view's local change. Treat the view as a read projection driven by the
/// source; mutate the source instead.
/// </remarks>
public sealed class FilteredObservableListView<T> : ObservableList<T>
{
    private ObservableList<T>? _source;
    private readonly Action<T> _onSourceItemAdded;
    private readonly Action<T> _onSourceItemRemoved;
    private readonly Action _onSourceListCleared;

    internal FilteredObservableListView(ObservableList<T> source, Predicate<T> predicate)
        : base(source.FindAll(predicate))
    {
        _source = source;
        _onSourceItemAdded = item =>
        {
            if (predicate(item))
                Add(item);
        };
        _onSourceItemRemoved = item => Remove(item);
        _onSourceListCleared = () => Clear();
        source.ItemAdded += _onSourceItemAdded;
        source.ItemRemoved += _onSourceItemRemoved;
        source.ListCleared += _onSourceListCleared;
    }

    public override void Dispose()
    {
        if (_source != null)
        {
            _source.ItemAdded -= _onSourceItemAdded;
            _source.ItemRemoved -= _onSourceItemRemoved;
            _source.ListCleared -= _onSourceListCleared;
            _source = null;
        }
        base.Dispose();
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

    /// <summary>
    /// Creates a live filtered view over <paramref name="source"/>. The returned view subscribes
    /// to the source's item events; dispose the view to release those subscriptions and prevent
    /// the source from keeping the view alive.
    /// </summary>
    public static FilteredObservableListView<T> CreateFilteredView<T>(this ObservableList<T> source, Predicate<T> predicate)
    {
        return new FilteredObservableListView<T>(source, predicate);
    }
}
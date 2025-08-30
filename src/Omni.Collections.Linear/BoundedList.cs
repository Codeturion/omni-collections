using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Omni.Collections.Linear;

/// <summary>
/// A fixed-capacity list that guarantees zero reallocations while matching standard List performance for bounded scenarios.
/// Provides O(1) Add/Remove operations at end, O(n) for indexed operations, with absolute memory predictability.
/// Perfect for real-time systems, circular buffers, and memory-critical applications where predictable O(1) performance
/// and strict memory bounds are non-negotiable requirements.
/// </summary>
public class BoundedList<T> : IList<T>, IDisposable
{
    private readonly T[] _items;
    private readonly ArrayPool<T>? _arrayPool;
    private readonly bool _usePooling;
    private int _count;
    static private readonly ConcurrentQueue<BoundedList<T>> InstancePool = new ConcurrentQueue<BoundedList<T>>();
    public int Count => _count;
    public int Capacity => _items.Length;
    public bool IsReadOnly => false;
    public bool IsFull => _count >= _items.Length;
    public int RemainingCapacity => _items.Length - _count;
    public BoundedList(int capacity) : this(capacity, arrayPool: null) { }

    private BoundedList(int capacity, ArrayPool<T>? arrayPool)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));
        _arrayPool = arrayPool;
        _usePooling = arrayPool != null;
        if (_usePooling)
        {
            _items = _arrayPool!.Rent(capacity);
            if (_items.Length > capacity)
            {
            }
        }
        else
        {
            _items = new T[capacity];
        }
        _count = 0;
    }

    public static BoundedList<T> CreateWithArrayPool(int capacity)
    {
        return new BoundedList<T>(capacity, ArrayPool<T>.Shared);
    }

    public static BoundedList<T> Rent(int capacity)
    {
        if (InstancePool.TryDequeue(out BoundedList<T>? instance))
        {
            instance.ResetForReuse(capacity);
            return instance;
        }
        return new BoundedList<T>(capacity);
    }

    public void Return()
    {
        Clear();
        InstancePool.Enqueue(this);
    }

    private void ResetForReuse(int newCapacity)
    {
        Clear();
        if (_items.Length < newCapacity)
        {
            throw new ArgumentException($"Requested capacity {newCapacity} exceeds instance capacity {_items.Length}");
        }
    }

    public BoundedList(int capacity, IEnumerable<T> items) : this(capacity)
    {
        foreach (var item in items)
        {
            if (!TryAdd(item))
                throw new InvalidOperationException("Initial items exceed capacity");
        }
    }

    public void Dispose()
    {
        DisposeCore();
    }

    private void DisposeCore()
    {
        if (_usePooling && _arrayPool != null)
        {
            _arrayPool.Return(_items, clearArray: true);
        }
    }

    public T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if ((uint)index >= (uint)_count)
                throw new IndexOutOfRangeException();
            return _items[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            if ((uint)index >= (uint)_count)
                throw new IndexOutOfRangeException();
            _items[index] = value;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetRef(int index)
    {
        if ((uint)index >= (uint)_count)
            throw new IndexOutOfRangeException();
        return ref _items[index];
    }

    public void Add(T item)
    {
        if (_count >= _items.Length)
            throw new InvalidOperationException("BoundedList has reached its capacity");
        _items[_count++] = item;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAdd(T item)
    {
        if (_count >= _items.Length)
            return false;
        _items[_count++] = item;
        return true;
    }

    public int AddRange(IEnumerable<T> items)
    {
        int added = 0;
        foreach (var item in items)
        {
            if (!TryAdd(item))
                break;
            added++;
        }
        return added;
    }

    public int AddRange(ReadOnlySpan<T> items)
    {
        int available = _items.Length - _count;
        int toAdd = Math.Min(items.Length, available);
        if (toAdd > 0)
        {
            items.Slice(0, toAdd).CopyTo(_items.AsSpan(_count));
            _count += toAdd;
        }
        return toAdd;
    }

    public void Insert(int index, T item)
    {
        if ((uint)index > (uint)_count)
            throw new ArgumentOutOfRangeException(nameof(index));
        if (_count >= _items.Length)
            throw new InvalidOperationException("BoundedList has reached its capacity");
        if (index < _count)
        {
            Array.Copy(_items, index, _items, index + 1, _count - index);
        }
        _items[index] = item;
        _count++;
    }

    public bool TryInsert(int index, T item)
    {
        if ((uint)index > (uint)_count || _count >= _items.Length)
            return false;
        if (index < _count)
        {
            Array.Copy(_items, index, _items, index + 1, _count - index);
        }
        _items[index] = item;
        _count++;
        return true;
    }

    public bool Remove(T item)
    {
        int index = IndexOf(item);
        if (index >= 0)
        {
            RemoveAt(index);
            return true;
        }
        return false;
    }

    public void RemoveAt(int index)
    {
        if ((uint)index >= (uint)_count)
            throw new ArgumentOutOfRangeException(nameof(index));
        _count--;
        if (index < _count)
        {
            Array.Copy(_items, index + 1, _items, index, _count - index);
        }
        _items[_count] = default!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T RemoveLast()
    {
        if (_count == 0)
            throw new InvalidOperationException("BoundedList is empty");
        var item = _items[--_count];
        _items[_count] = default!;
        return item;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryRemoveLast(out T result)
    {
        if (_count == 0)
        {
            result = default!;
            return false;
        }
        result = _items[--_count];
        _items[_count] = default!;
        return true;
    }

    public void RemoveAtSwap(int index)
    {
        if ((uint)index >= (uint)_count)
            throw new ArgumentOutOfRangeException(nameof(index));
        _count--;
        if (index < _count)
        {
            _items[index] = _items[_count];
        }
        _items[_count] = default!;
    }

    public int IndexOf(T item)
    {
        return Array.IndexOf(_items, item, 0, _count);
    }

    public bool Contains(T item)
    {
        return IndexOf(item) >= 0;
    }

    public void Clear()
    {
        if (_count > 0)
        {
            if (_usePooling)
            {
                Array.Clear(_items, 0, _count);
            }
            else
            {
                Array.Clear(_items, 0, _count);
            }
            _count = 0;
        }
    }

    public ReadOnlySpan<T> AsSpan()
    {
        return _items.AsSpan(0, _count);
    }

    public Span<T> AsSpanMutable()
    {
        return _items.AsSpan(0, _count);
    }

    public void CopyTo(Span<T> destination)
    {
        AsSpan().CopyTo(destination);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array));
        if (arrayIndex < 0 || arrayIndex + _count > array.Length)
            throw new ArgumentException("Invalid array or index");
        Array.Copy(_items, 0, array, arrayIndex, _count);
    }

    public void ForEachRef(RefAction<T> action)
    {
        for (int i = 0; i < _count; i++)
        {
            action(ref _items[i]);
        }
    }

    public Enumerator GetEnumerator() => new Enumerator(this);
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public struct Enumerator : IEnumerator<T>
    {
        private readonly BoundedList<T> _list;
        private int _index;
        private T _current;
        internal Enumerator(BoundedList<T> list)
        {
            _list = list;
            _index = 0;
            _current = default!;
        }

        public bool MoveNext()
        {
            if (_index < _list._count)
            {
                _current = _list._items[_index];
                _index++;
                return true;
            }
            _current = default!;
            return false;
        }

        public T Current => _current;
        object? IEnumerator.Current => Current;
        public void Reset()
        {
            _index = 0;
            _current = default!;
        }

        public void Dispose() { }
    }
}

public delegate void RefAction<T>(ref T item);
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Omni.Collections.Linear;

/// <summary>
/// A list that leverages ArrayPool to achieve near-zero allocation overhead in high-frequency scenarios.
/// Maintains O(1) amortized Add operations and O(1) indexed access while reusing pooled memory for optimal GC behavior.
/// Essential for game engines, real-time data processing, and server applications where frequent list creation
/// would otherwise trigger excessive garbage collection cycles.
/// </summary>
public class PooledList<T> : IList<T>, IDisposable
{
    private T[] _buffer;
    private int _size;
    private readonly ArrayPool<T>? _pool;
    private readonly bool _usePooling;
    private bool _disposed;
    public int Count => _size;
    public int Capacity => _buffer?.Length ?? 0;
    public bool IsReadOnly => false;
    public PooledList(int initialCapacity = 16) : this(initialCapacity, pool: null)
    {
    }

    private PooledList(int initialCapacity, ArrayPool<T>? pool)
    {
        if (initialCapacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(initialCapacity));
        _pool = pool;
        _usePooling = pool != null;
        if (_usePooling)
        {
            _buffer = _pool!.Rent(initialCapacity);
        }
        else
        {
            _buffer = new T[initialCapacity];
        }
        _size = 0;
        _disposed = false;
    }

    public static PooledList<T> CreateWithArrayPool(int initialCapacity = 16)
    {
        return new PooledList<T>(initialCapacity, ArrayPool<T>.Shared);
    }

    public T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
#if DEBUG
            if ((uint)index >= (uint)_size)
                throw new IndexOutOfRangeException();
#endif
            return _buffer[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
#if DEBUG
            if ((uint)index >= (uint)_size)
                throw new IndexOutOfRangeException();
#endif
            _buffer[index] = value;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetRef(int index)
    {
#if DEBUG
        if ((uint)index >= (uint)_size)
            throw new IndexOutOfRangeException();
#endif
        return ref _buffer[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item)
    {
#if DEBUG
        ThrowIfDisposed();
#endif
        if (_size >= _buffer.Length)
            Resize();
        _buffer[_size++] = item;
    }

    public void AddRange(ReadOnlySpan<T> items)
    {
#if DEBUG
        ThrowIfDisposed();
#endif
        if (items.IsEmpty)
            return;
        while (_size + items.Length > _buffer.Length)
            Resize();
        items.CopyTo(_buffer.AsSpan(_size));
        _size += items.Length;
    }

    public void AddRange(IEnumerable<T> items)
    {
        ThrowIfDisposed();
        if (items is ICollection<T> collection)
        {
            while (_size + collection.Count > _buffer.Length)
                Resize();
        }
        foreach (var item in items)
        {
            if (_size >= _buffer.Length)
                Resize();
            _buffer[_size++] = item;
        }
    }

    public void Insert(int index, T item)
    {
        ThrowIfDisposed();
        if ((uint)index > (uint)_size)
            throw new ArgumentOutOfRangeException(nameof(index));
        if (_size >= _buffer.Length)
            Resize();
        if (index < _size)
        {
            Array.Copy(_buffer, index, _buffer, index + 1, _size - index);
        }
        _buffer[index] = item;
        _size++;
    }

    public bool Remove(T item)
    {
        ThrowIfDisposed();
        int index = IndexOf(item);
        if (index >= 0)
        {
            RemoveAt(index);
            return true;
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveAt(int index)
    {
#if DEBUG
        if (_disposed)
            throw new ObjectDisposedException(nameof(PooledList<T>));
        if ((uint)index >= (uint)_size)
            throw new ArgumentOutOfRangeException(nameof(index));
#endif
        _size--;
        if (index < _size)
        {
            Array.Copy(_buffer, index + 1, _buffer, index, _size - index);
        }
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            _buffer[_size] = default!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T RemoveLast()
    {
        ThrowIfDisposed();
        if (_size == 0)
            throw new InvalidOperationException("List is empty");
        var item = _buffer[--_size];
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            _buffer[_size] = default!;
        return item;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryRemoveLast(out T result)
    {
        ThrowIfDisposed();
        if (_size == 0)
        {
            result = default!;
            return false;
        }
        result = _buffer[--_size];
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            _buffer[_size] = default!;
        return true;
    }

    public int IndexOf(T item)
    {
        ThrowIfDisposed();
        return Array.IndexOf(_buffer, item, 0, _size);
    }

    public bool Contains(T item)
    {
        ThrowIfDisposed();
        return IndexOf(item) >= 0;
    }

    public void Clear()
    {
        ThrowIfDisposed();
        if (_size > 0)
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                Array.Clear(_buffer, 0, _size);
            _size = 0;
        }
    }

    public ReadOnlySpan<T> AsSpan()
    {
        ThrowIfDisposed();
        return _buffer.AsSpan(0, _size);
    }

    public Span<T> AsSpanMutable()
    {
        ThrowIfDisposed();
        return _buffer.AsSpan(0, _size);
    }

    public Enumerator GetEnumerator() => new Enumerator(this);
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public void CopyTo(T[] array, int arrayIndex)
    {
        ThrowIfDisposed();
        if (array == null)
            throw new ArgumentNullException(nameof(array));
        if (arrayIndex < 0 || arrayIndex + _size > array.Length)
            throw new ArgumentException("Invalid array or index");
        Array.Copy(_buffer, 0, array, arrayIndex, _size);
    }

    private void Resize()
    {
        var newCapacity = Math.Max(_buffer.Length * 2, 16);
        if (_usePooling)
        {
            T[]? newBuffer = _pool!.Rent(newCapacity);
            if (_size > 0)
                Array.Copy(_buffer, 0, newBuffer, 0, _size);
            _pool.Return(_buffer, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<T>());
            _buffer = newBuffer;
        }
        else
        {
            var newBuffer = new T[newCapacity];
            if (_size > 0)
                Array.Copy(_buffer, 0, newBuffer, 0, _size);
            _buffer = newBuffer;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(PooledList<T>));
    }

    public void Dispose()
    {
        if (_disposed) {
            return;
        }
        if (_usePooling)
        {
            _pool!.Return(_buffer, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<T>());
        }
        _buffer = null!;
        _size = 0;
        _disposed = true;
    }

    public struct Enumerator : IEnumerator<T>
    {
        private readonly PooledList<T> _list;
        private int _index;
        private T _current;
        internal Enumerator(PooledList<T> list)
        {
            _list = list;
            _index = 0;
            _current = default!;
        }

        public bool MoveNext()
        {
            if (_index < _list._size)
            {
                _current = _list._buffer[_index];
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
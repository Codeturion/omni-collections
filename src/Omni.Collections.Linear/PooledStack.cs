using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Omni.Collections.Linear;

/// <summary>
/// A stack that eliminates allocation overhead through intelligent ArrayPool integration while maintaining blazing-fast LIFO operations.
/// Guarantees O(1) Push/Pop/Peek operations with microsecond-level performance and minimal memory footprint.
/// Ideal for expression parsers, undo/redo systems, and recursive algorithms where O(1) stack operations
/// and allocation frequency would otherwise dominate performance profiles.
/// </summary>
public sealed class PooledStack<T> : IEnumerable<T>, IDisposable
{
    private T[] _buffer;
    private int _size;
    private readonly ArrayPool<T>? _pool;
    private readonly bool _usePooling;
    private bool _disposed;
    public int Count => _size;
    public int Capacity => _buffer?.Length ?? 0;
    public bool IsEmpty => _size == 0;
    public PooledStack(int initialCapacity = 16) : this(initialCapacity, pool: null)
    {
    }

    private PooledStack(int initialCapacity, ArrayPool<T>? pool)
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

    public static PooledStack<T> CreateWithArrayPool(int initialCapacity = 16)
    {
        return new PooledStack<T>(initialCapacity, ArrayPool<T>.Shared);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Push(T item)
    {
#if DEBUG
        ThrowIfDisposed();
#endif
        if (_size >= _buffer.Length)
        {
            Resize();
        }
        _buffer[_size++] = item;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Pop()
    {
        ThrowIfDisposed();
        if (IsEmpty)
            throw new InvalidOperationException("Stack is empty");

        var item = _buffer[--_size];
        _buffer[_size] = default!;
        return item;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryPop(out T result)
    {
        ThrowIfDisposed();
        if (IsEmpty)
        {
            result = default!;
            return false;
        }
        result = _buffer[--_size];
        _buffer[_size] = default!;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Peek()
    {
        ThrowIfDisposed();
        if (IsEmpty)
            throw new InvalidOperationException("Stack is empty");
        return _buffer[_size - 1];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryPeek(out T result)
    {
        ThrowIfDisposed();
        if (IsEmpty)
        {
            result = default!;
            return false;
        }
        result = _buffer[_size - 1];
        return true;
    }

    public void PushSpan(ReadOnlySpan<T> items)
    {
        ThrowIfDisposed();
        if (items.IsEmpty)
            return;
        while (_size + items.Length > _buffer.Length)
        {
            Resize();
        }
        items.CopyTo(_buffer.AsSpan(_size));
        _size += items.Length;
    }

    public ReadOnlySpan<T> PopSpan(int count)
    {
        ThrowIfDisposed();
        if (count <= 0)
            return ReadOnlySpan<T>.Empty;
        if (count > _size)
            count = _size;
        if (count == 0)
            return ReadOnlySpan<T>.Empty;
        var startIndex = _size - count;
        
        // Create a copy of the data in reverse order (LIFO)
        T[] resultArray = new T[count];
        for (int i = 0; i < count; i++)
        {
            resultArray[i] = _buffer[_size - 1 - i];
        }
        
        // Now clear the buffer
        for (int i = startIndex; i < _size; i++)
        {
            _buffer[i] = default!;
        }
        _size -= count;
        
        return resultArray.AsSpan();
    }

    public void PushRange(IEnumerable<T> items)
    {
        ThrowIfDisposed();
        if (items is ICollection<T> collection)
        {
            while (_size + collection.Count > _buffer.Length)
            {
                Resize();
            }
        }
        foreach (var item in items)
        {
            if (_size >= _buffer.Length)
                Resize();
            _buffer[_size++] = item;
        }
    }

    public void Clear()
    {
        ThrowIfDisposed();
        if (_size > 0)
        {
            Array.Clear(_buffer, 0, _size);
            _size = 0;
        }
    }

    public ReadOnlySpan<T> AsSpan()
    {
        ThrowIfDisposed();
        return _buffer.AsSpan(0, _size);
    }

    public Enumerator GetEnumerator() => new Enumerator(this);
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    private void Resize()
    {
        var newCapacity = Math.Max(_buffer.Length * 2, 16);
        if (_usePooling)
        {
            T[] newBuffer = _pool!.Rent(newCapacity);
            if (_size > 0)
                Array.Copy(_buffer, 0, newBuffer, 0, _size);
            _pool.Return(_buffer, clearArray: true);
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
            throw new ObjectDisposedException(nameof(PooledStack<T>));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_usePooling)
            {
                _pool!.Return(_buffer, clearArray: true);
            }
            _buffer = null!;
            _size = 0;
            _disposed = true;
        }
    }

    public struct Enumerator : IEnumerator<T>
    {
        private readonly PooledStack<T> _stack;
        private int _index;
        private T _current;
        internal Enumerator(PooledStack<T> stack)
        {
            _stack = stack;
            _index = stack._size;
            _current = default!;
        }

        public bool MoveNext()
        {
            if (_index > 0)
            {
                _current = _stack._buffer[--_index];
                return true;
            }
            _current = default!;
            return false;
        }

        public T Current => _current;
        object? IEnumerator.Current => Current;
        public void Reset()
        {
            _index = _stack._size;
            _current = default!;
        }

        public void Dispose() { }
    }
}
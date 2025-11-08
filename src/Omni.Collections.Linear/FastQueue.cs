using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Omni.Collections.Linear;

/// <summary>
/// A high-performance circular buffer queue that significantly outperforms standard Queue in throughput scenarios.
/// Provides O(1) amortized Enqueue/Dequeue operations with microsecond-level performance through intelligent circular buffer design.
/// Perfect for high-throughput producer-consumer patterns where consistent O(1) operations and minimal GC pressure
/// are critical for maintaining system responsiveness.
/// </summary>
public sealed class FastQueue<T> : IEnumerable<T>, IDisposable
{
    private T[] _buffer;
    private readonly ArrayPool<T>? _arrayPool;
    private readonly bool _usePooling;
    private int _head;
    private int _tail;
    private int _size;
    private int _mask;
    private bool _disposed;
    static private readonly ConcurrentQueue<FastQueue<T>> InstancePool = new ConcurrentQueue<FastQueue<T>>();
    public int Count => _size;
    public int Capacity => _buffer.Length;
    private bool IsEmpty => _size == 0;
    private bool IsFull => _size == _buffer.Length;
    public FastQueue(int capacity = 16) : this(capacity, arrayPool: null) { }

    private FastQueue(int capacity, ArrayPool<T>? arrayPool)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));
        _arrayPool = arrayPool;
        _usePooling = arrayPool != null;
        var actualCapacity = RoundUpToPowerOfTwo(capacity);
        if (_usePooling)
        {
            _buffer = _arrayPool!.Rent(actualCapacity);
            _mask = _buffer.Length - 1;
        }
        else
        {
            _buffer = new T[actualCapacity];
            _mask = actualCapacity - 1;
        }
        _head = 0;
        _tail = 0;
        _size = 0;
    }

    public static FastQueue<T> CreateWithArrayPool(int capacity = 16)
    {
        return new FastQueue<T>(capacity, ArrayPool<T>.Shared);
    }

    public static FastQueue<T> Rent(int capacity = 16)
    {
        if (InstancePool.TryDequeue(out FastQueue<T>? instance))
        {
            instance.ResetForReuse(capacity);
            return instance;
        }
        return new FastQueue<T>(capacity);
    }

    public void Return()
    {
        Clear();
        InstancePool.Enqueue(this);
    }

    private void ResetForReuse(int newCapacity)
    {
        Clear();
        var actualCapacity = RoundUpToPowerOfTwo(newCapacity);
        if (_buffer.Length < actualCapacity)
        {
            DisposeCore(true);
            if (_usePooling)
            {
                _buffer = _arrayPool!.Rent(actualCapacity);
                _mask = _buffer.Length - 1;
            }
            else
            {
                _buffer = new T[actualCapacity];
                _mask = actualCapacity - 1;
            }
        }
        _head = 0;
        _tail = 0;
        _size = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Enqueue(T item)
    {
        ThrowIfDisposed();
        if (IsFull)
        {
            Resize();
        }
        _buffer[_tail] = item;
        _tail = (_tail + 1) & _mask;
        _size++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Dequeue()
    {
        ThrowIfDisposed();
        if (IsEmpty)
            throw new InvalidOperationException("Queue is empty");

        var item = _buffer[_head];
        _buffer[_head] = default!;
        _head = (_head + 1) & _mask;
        _size--;
        return item;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryDequeue(out T result)
    {
        ThrowIfDisposed();
        if (IsEmpty)
        {
            result = default!;
            return false;
        }
        result = _buffer[_head];
        _buffer[_head] = default!;
        _head = (_head + 1) & _mask;
        _size--;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Peek()
    {
        if (IsEmpty)
            throw new InvalidOperationException("Queue is empty");
        return _buffer[_head];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryPeek(out T result)
    {
        if (IsEmpty)
        {
            result = default!;
            return false;
        }
        result = _buffer[_head];
        return true;
    }

    public ReadOnlySpan<T> DequeueSpan(int count)
    {
        ThrowIfDisposed();
        if (count <= 0)
            return ReadOnlySpan<T>.Empty;
        if (count > _size)
            count = _size;
        if (count == 0)
            return ReadOnlySpan<T>.Empty;
        if (_head + count <= _buffer.Length)
        {
            // Create a copy before clearing the buffer
            T[] resultArray = new T[count];
            Array.Copy(_buffer, _head, resultArray, 0, count);
            
            // Now clear the buffer
            for (int i = 0; i < count; i++)
            {
                _buffer[_head + i] = default!;
            }
            _head = (_head + count) & _mask;
            _size -= count;
            return resultArray.AsSpan();
        }
        else
        {
            if (_usePooling)
            {
                // Use pooled array, must copy result since we return the pooled array
                T[] temp = _arrayPool!.Rent(count);
                try
                {
                    int copied = 0;
                    for (int i = 0; i < count; i++)
                    {
                        temp[i] = _buffer[_head];
                        _buffer[_head] = default!;
                        _head = (_head + 1) & _mask;
                        copied++;
                    }
                    _size -= copied;
                    var result = new T[count];
                    Array.Copy(temp, 0, result, 0, count);
                    return result.AsSpan();
                }
                finally
                {
                    _arrayPool!.Return(temp, clearArray: true);
                }
            }
            else
            {
                // Use regular array, can return span directly
                T[] result = new T[count];
                int copied = 0;
                for (int i = 0; i < count; i++)
                {
                    result[i] = _buffer[_head];
                    _buffer[_head] = default!;
                    _head = (_head + 1) & _mask;
                    copied++;
                }
                _size -= copied;
                return new ReadOnlySpan<T>(result, 0, count);
            }
        }
    }

    public void EnqueueSpan(ReadOnlySpan<T> items)
    {
        if (items.IsEmpty)
            return;
        while (_size + items.Length > _buffer.Length)
        {
            Resize();
        }
        foreach (var item in items)
        {
            _buffer[_tail] = item;
            _tail = (_tail + 1) & _mask;
            _size++;
        }
    }

    public void Clear()
    {
        if (_size > 0)
        {
            Array.Clear(_buffer, 0, _buffer.Length);
            _head = 0;
            _tail = 0;
            _size = 0;
        }
    }

    public Enumerator GetEnumerator() => new Enumerator(this);
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    private void Resize()
    {
        var newCapacity = _buffer.Length * 2;
        T[] newBuffer;
        if (_usePooling)
        {
            newBuffer = _arrayPool!.Rent(newCapacity);
        }
        else
        {
            newBuffer = new T[newCapacity];
        }
        var newMask = newBuffer.Length - 1;
        for (int i = 0; i < _size; i++)
        {
            newBuffer[i] = _buffer[(_head + i) & _mask];
        }
        if (_usePooling)
        {
            _arrayPool!.Return(_buffer, clearArray: true);
        }
        _buffer = newBuffer;
        _mask = newMask;
        _head = 0;
        _tail = _size;
    }
    ~FastQueue()
    {
        DisposeCore(false);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            DisposeCore(true);
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    private void DisposeCore(bool disposing)
    {
        if (disposing && _usePooling && _arrayPool != null && _buffer != null)
        {
            _arrayPool.Return(_buffer, clearArray: true);
            _buffer = null!;
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(FastQueue<T>));
    }

    static private int RoundUpToPowerOfTwo(int value)
    {
        value--;
        value |= value >> 1;
        value |= value >> 2;
        value |= value >> 4;
        value |= value >> 8;
        value |= value >> 16;
        return value + 1;
    }

    public struct Enumerator : IEnumerator<T>
    {
        private readonly FastQueue<T> _queue;
        private int _index;
        private T _current;
        internal Enumerator(FastQueue<T> queue)
        {
            _queue = queue;
            _index = 0;
            _current = default!;
        }

        public bool MoveNext()
        {
            if (_index < _queue._size)
            {
                _current = _queue._buffer[(_queue._head + _index) & _queue._mask];
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
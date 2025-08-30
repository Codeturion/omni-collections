using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Omni.Collections.Linear;

/// <summary>
/// A binary max-heap that vastly outperforms SortedSet for maximum element operations through optimized heap algorithms.
/// Guarantees O(log n) Insert/ExtractMax with O(1) PeekMax, maintaining perfect heap ordering with microsecond-level performance.
/// Critical for real-time systems, load balancing, top-K algorithms, and priority scheduling where O(log n) max operations
/// directly impact system throughput.
/// </summary>
public class MaxHeap<T> : IEnumerable<T>, IDisposable where T : IComparable<T>
{
    private T[] _heap;
    private readonly ArrayPool<T>? _arrayPool;
    private readonly bool _usePooling;
    private int _size;
    static private readonly ConcurrentQueue<MaxHeap<T>> InstancePool = new ConcurrentQueue<MaxHeap<T>>();
    public int Count => _size;
    public int Capacity => _heap.Length;
    public bool IsEmpty => _size == 0;
    public MaxHeap(int initialCapacity = 16) : this(initialCapacity, arrayPool: null) { }

    private MaxHeap(int initialCapacity, ArrayPool<T>? arrayPool)
    {
        if (initialCapacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(initialCapacity));
        _arrayPool = arrayPool;
        _usePooling = arrayPool != null;
        if (_usePooling)
        {
            _heap = _arrayPool!.Rent(initialCapacity);
        }
        else
        {
            _heap = new T[initialCapacity];
        }
        _size = 0;
    }

    public static MaxHeap<T> CreateWithArrayPool(int initialCapacity = 16)
    {
        return new MaxHeap<T>(initialCapacity, ArrayPool<T>.Shared);
    }

    public static MaxHeap<T> Rent(int initialCapacity = 16)
    {
        if (InstancePool.TryDequeue(out MaxHeap<T>? instance))
        {
            instance.ResetForReuse(initialCapacity);
            return instance;
        }
        return new MaxHeap<T>(initialCapacity);
    }

    public void Return()
    {
        Clear();
        InstancePool.Enqueue(this);
    }

    private void ResetForReuse(int newCapacity)
    {
        Clear();
        if (_heap.Length < newCapacity)
        {
            DisposeCore(true);
            if (_usePooling)
            {
                _heap = _arrayPool!.Rent(newCapacity);
            }
            else
            {
                _heap = new T[newCapacity];
            }
        }
        _size = 0;
    }

    public MaxHeap(IEnumerable<T> items, int capacity = 16) : this(capacity)
    {
        if (items is ICollection<T> collection)
        {
            var requiredCapacity = Math.Max(capacity, collection.Count);
            if (_heap.Length < requiredCapacity)
            {
                DisposeCore(true);
                if (_usePooling)
                {
                    _heap = _arrayPool!.Rent(requiredCapacity);
                }
                else
                {
                    _heap = new T[requiredCapacity];
                }
            }
        }
        foreach (var item in items)
        {
            Insert(item);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Insert(T item)
    {
        if (_size >= _heap.Length)
        {
            Resize();
        }
        _heap[_size] = item;
        HeapifyUp(_size);
        _size++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T ExtractMax()
    {
        if (IsEmpty)
            throw new InvalidOperationException("Heap is empty");
        var max = _heap[0];
        _heap[0] = _heap[_size - 1];
        _heap[_size - 1] = default!;
        _size--;
        if (_size > 0)
        {
            HeapifyDown(0);
        }
        return max;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryExtractMax(out T result)
    {
        if (IsEmpty)
        {
            result = default!;
            return false;
        }
        result = ExtractMax();
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T PeekMax()
    {
        if (IsEmpty)
            throw new InvalidOperationException("Heap is empty");
        return _heap[0];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryPeekMax(out T result)
    {
        if (IsEmpty)
        {
            result = default!;
            return false;
        }
        result = _heap[0];
        return true;
    }

    public void InsertRange(IEnumerable<T> items)
    {
        if (items is ICollection<T> collection)
        {
            while (_size + collection.Count > _heap.Length)
            {
                Resize();
            }
        }
        foreach (var item in items)
        {
            Insert(item);
        }
    }

    public void Clear()
    {
        if (_size > 0)
        {
            Array.Clear(_heap, 0, _size);
            _size = 0;
        }
    }

    public static MaxHeap<T> BuildHeap(T[] array)
    {
        var heap = new MaxHeap<T>(array.Length);
        T[] tempArray;
        bool usingPooledTemp = false;
        if (heap._usePooling && heap._arrayPool != null)
        {
            tempArray = heap._arrayPool.Rent(array.Length);
            usingPooledTemp = true;
        }
        else
        {
            tempArray = new T[array.Length];
        }
        try
        {
            Array.Copy(array, tempArray, array.Length);
            Array.Copy(tempArray, heap._heap, array.Length);
            heap._size = array.Length;
            for (int i = (array.Length / 2) - 1; i >= 0; i--)
            {
                heap.HeapifyDown(i);
            }
        }
        finally
        {
            if (usingPooledTemp && heap._arrayPool != null)
            {
                heap._arrayPool.Return(tempArray, clearArray: true);
            }
        }
        return heap;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void HeapifyUp(int index)
    {
        while (index > 0)
        {
            int parentIndex = (index - 1) / 2;
            if (_heap[index].CompareTo(_heap[parentIndex]) <= 0)
                break;
            Swap(index, parentIndex);
            index = parentIndex;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void HeapifyDown(int index)
    {
        while (true)
        {
            int leftChild = 2 * index + 1;
            int rightChild = 2 * index + 2;
            int largest = index;
            if (leftChild < _size && _heap[leftChild].CompareTo(_heap[largest]) > 0)
                largest = leftChild;
            if (rightChild < _size && _heap[rightChild].CompareTo(_heap[largest]) > 0)
                largest = rightChild;
            if (largest == index)
                break;
            Swap(index, largest);
            index = largest;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Swap(int i, int j)
    {
        (_heap[i], _heap[j]) = (_heap[j], _heap[i]);
    }

    private void Resize()
    {
        var newCapacity = _heap.Length * 2;
        T[] newHeap;
        if (_usePooling)
        {
            newHeap = _arrayPool!.Rent(newCapacity);
        }
        else
        {
            newHeap = new T[newCapacity];
        }
        Array.Copy(_heap, newHeap, _size);
        if (_usePooling)
        {
            _arrayPool!.Return(_heap, clearArray: true);
        }
        _heap = newHeap;
    }
    ~MaxHeap()
    {
        DisposeCore(false);
    }

    public void Dispose()
    {
        DisposeCore(true);
        GC.SuppressFinalize(this);
    }

    private void DisposeCore(bool disposing)
    {
        if (disposing && _usePooling && _arrayPool != null && _heap != null)
        {
            _arrayPool.Return(_heap, clearArray: true);
            _heap = null!;
        }
    }

    public ReadOnlySpan<T> AsSpan() => _heap.AsSpan(0, _size);
    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < _size; i++)
        {
            yield return _heap[i];
        }
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public bool IsValidHeap()
    {
        for (int i = 0; i < _size; i++)
        {
            int leftChild = 2 * i + 1;
            int rightChild = 2 * i + 2;
            if (leftChild < _size && _heap[i].CompareTo(_heap[leftChild]) < 0)
                return false;
            if (rightChild < _size && _heap[i].CompareTo(_heap[rightChild]) < 0)
                return false;
        }
        return true;
    }
}
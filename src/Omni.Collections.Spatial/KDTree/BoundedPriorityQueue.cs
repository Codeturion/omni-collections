using System;

namespace Omni.Collections.Spatial.KDTree;

class BoundedPriorityQueue<T>
{
    private readonly (T item, double priority)[] _heap;
    private int _count;
    public int Count => _count;
    public int Capacity { get; }

    public double MaxPriority => _count > 0 ? _heap[0].priority : double.MinValue;
    public BoundedPriorityQueue(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentException("Capacity must be positive", nameof(capacity));
        Capacity = capacity;
        _heap = new (T, double)[capacity];
    }

    public bool TryAdd(T item, double priority)
    {
        if (_count < Capacity)
        {
            _heap[_count] = (item, priority);
            HeapifyUp(_count);
            _count++;
            return true;
        }
        if (priority < MaxPriority)
        {
            _heap[0] = (item, priority);
            HeapifyDown(0);
            return true;
        }
        return false;
    }

    public T ExtractMax()
    {
        if (_count == 0)
            throw new InvalidOperationException("Queue is empty");
        var max = _heap[0];
        _count--;
        if (_count > 0)
        {
            _heap[0] = _heap[_count];
            HeapifyDown(0);
        }
        return max.item;
    }

    private void HeapifyUp(int index)
    {
        while (index > 0)
        {
            int parent = (index - 1) / 2;
            if (_heap[index].priority <= _heap[parent].priority)
                break;
            (_heap[index], _heap[parent]) = (_heap[parent], _heap[index]);
            index = parent;
        }
    }

    private void HeapifyDown(int index)
    {
        while (true)
        {
            int largest = index;
            int left = 2 * index + 1;
            int right = 2 * index + 2;
            if (left < _count && _heap[left].priority > _heap[largest].priority)
                largest = left;
            if (right < _count && _heap[right].priority > _heap[largest].priority)
                largest = right;
            if (largest == index)
                break;
            (_heap[index], _heap[largest]) = (_heap[largest], _heap[index]);
            index = largest;
        }
    }
}
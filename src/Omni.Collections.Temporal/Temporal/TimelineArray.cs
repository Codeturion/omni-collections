using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Buffers;

namespace Omni.Collections.Temporal;

/// <summary>
/// A circular buffer optimized for time-series data that provides efficient temporal indexing and playback capabilities.
/// Provides O(1) Record operations and O(log n) GetAtTime queries with built-in replay functionality.
/// Perfect for game replay systems (racing games, fighting games), undo functionality in applications,
/// sensor data recording, or any time-series data requiring efficient temporal access and playback.
/// </summary>
public class TimelineArray<T> : IDisposable
{
    private readonly int _capacity;
    private readonly int _frameDuration;
    private T[] _buffer;
    private long[] _timestamps;
    private int _head;
    private int _tail;
    private int _count;
    private long _startTime;
    private long _currentTime;
    private int _currentPlaybackIndex;
    private readonly ArrayPool<T>? _bufferPool;
    private readonly ArrayPool<long>? _timestampPool;
    private readonly bool _usePooling;
    public int Count => _count;
    public int Capacity => _capacity;
    public long CurrentTime => _currentTime;
    public long StartTime => _startTime;
    public long EndTime => _count > 0 ? _timestamps[(_head - 1 + _capacity) % _capacity] : _startTime;
    public TimelineArray(int capacity, int frameDurationMs = 16) : this(capacity, frameDurationMs, bufferPool: null, timestampPool: null)
    {
    }

    private TimelineArray(int capacity, int frameDurationMs, ArrayPool<T>? bufferPool, ArrayPool<long>? timestampPool)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive");
        if (frameDurationMs <= 0)
            throw new ArgumentOutOfRangeException(nameof(frameDurationMs), "Frame duration must be positive");
        _capacity = capacity;
        _frameDuration = frameDurationMs;
        _bufferPool = bufferPool;
        _timestampPool = timestampPool;
        _usePooling = bufferPool != null && timestampPool != null;
        if (_usePooling)
        {
            _buffer = _bufferPool!.Rent(capacity);
            _timestamps = _timestampPool!.Rent(capacity);
        }
        else
        {
            _buffer = new T[capacity];
            _timestamps = new long[capacity];
        }
        _head = 0;
        _tail = 0;
        _count = 0;
        _startTime = 0;
        _currentTime = 0;
        _currentPlaybackIndex = -1;
    }

    public static TimelineArray<T> CreateWithArrayPool(int capacity, int frameDurationMs = 16)
    {
        return new TimelineArray<T>(capacity, frameDurationMs, ArrayPool<T>.Shared, ArrayPool<long>.Shared);
    }

    public void Record(T snapshot)
    {
        Record(snapshot, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }

    public void Record(T snapshot, long timestamp)
    {
        if (timestamp < _currentTime)
            throw new ArgumentException("Cannot record in the past", nameof(timestamp));
        
        // Pre-calculate if buffer will be full after this operation
        bool willOverwrite = _count == _capacity;
        int oldHead = _head;
        
        // Store the data first
        _buffer[_head] = snapshot;
        _timestamps[_head] = timestamp;
        _head = (_head + 1) % _capacity;
        
        if (willOverwrite)
        {
            // Update start time using the next position that will be oldest
            _startTime = _timestamps[_head];
            _tail = _head;
        }
        else
        {
            _count++;
            if (_count == 1)
                _startTime = timestamp;
        }
        
        _currentTime = timestamp;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T? GetAtTime(long time)
    {
        if (_count == 0 || time < _startTime || time > EndTime)
            return default;
            
        int index = BinarySearchTime(time);
        if (index >= 0)
        {
            return _buffer[(_tail + index) % _capacity];
        }
        
        index = ~index;
        if (index == 0) index = 0;
        else if (index >= _count) index = _count - 1;
        else index = index - 1;
        
        return _buffer[(_tail + index) % _capacity];
    }

    public bool RewindTo(long time)
    {
        if (_count == 0 || time < _startTime || time > EndTime)
            return false;
        _currentTime = time;
        _currentPlaybackIndex = BinarySearchTime(time);
        if (_currentPlaybackIndex < 0)
            _currentPlaybackIndex = ~_currentPlaybackIndex - 1;
        return true;
    }

    public IEnumerable<T> Replay(long startTime)
    {
        return Replay(startTime, EndTime);
    }

    public IEnumerable<T> Replay(long startTime, long endTime)
    {
        if (_count == 0 || startTime > endTime)
            yield break;
        int startIndex = BinarySearchTime(startTime);
        if (startIndex < 0)
            startIndex = ~startIndex;
        int endIndex = BinarySearchTime(endTime);
        if (endIndex < 0)
            endIndex = ~endIndex;
        else
            endIndex++;
        for (int i = startIndex; i < endIndex && i < _count; i++)
        {
            yield return _buffer[GetBufferIndex(i)];
        }
    }

    public IEnumerable<(T snapshot, long timestamp)> ReplayAtFps(long startTime, int fps = 60)
    {
        if (_count == 0 || fps <= 0)
            yield break;
        long frameInterval = 1000 / fps;
        long currentReplayTime = startTime;
        while (currentReplayTime <= EndTime)
        {
            var snapshot = GetAtTime(currentReplayTime);
            if (snapshot != null)
                yield return (snapshot, currentReplayTime);
            currentReplayTime += frameInterval;
        }
    }

    public IEnumerable<T> GetTimeWindow(long startTime, long duration)
    {
        return Replay(startTime, startTime + duration);
    }

    public void Clear()
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            for (int i = 0; i < _count; i++)
            {
                _buffer[GetBufferIndex(i)] = default!;
            }
        }
        _head = 0;
        _tail = 0;
        _count = 0;
        _currentTime = 0;
        _startTime = 0;
        _currentPlaybackIndex = -1;
    }

    public T? Current => GetAtTime(_currentTime);
    public bool NextFrame()
    {
        if (_currentPlaybackIndex < 0 || _currentPlaybackIndex >= _count - 1)
            return false;
        _currentPlaybackIndex++;
        _currentTime = _timestamps[GetBufferIndex(_currentPlaybackIndex)];
        return true;
    }

    public bool PreviousFrame()
    {
        if (_currentPlaybackIndex <= 0)
            return false;
        _currentPlaybackIndex--;
        _currentTime = _timestamps[GetBufferIndex(_currentPlaybackIndex)];
        return true;
    }

    public bool JumpForward(long milliseconds)
    {
        return RewindTo(_currentTime + milliseconds);
    }

    public bool JumpBackward(long milliseconds)
    {
        return RewindTo(_currentTime - milliseconds);
    }

    public T[] ToArray()
    {
        if (_count == 0)
            return [];
        
        // Single allocation - enumerate in insertion order (much faster)
        var result = new T[_count];
        for (int i = 0; i < _count; i++)
        {
            result[i] = _buffer[GetBufferIndex(i)];
        }
        return result;
    }

    public TimelineStats GetStats()
    {
        if (_count == 0)
            return new TimelineStats();
        long duration = EndTime - _startTime;
        double averageFrameTime = _count > 1 ? (double)duration / (_count - 1) : 0;
        return new TimelineStats
        {
            SnapshotCount = _count,
            Capacity = _capacity,
            StartTime = _startTime,
            EndTime = EndTime,
            Duration = duration,
            AverageFrameTime = averageFrameTime,
            MemoryUsage = _capacity * (Unsafe.SizeOf<T>() + sizeof(long))
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetBufferIndex(int logicalIndex)
    {
        return (_tail + logicalIndex) % _capacity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int BinarySearchTime(long time)
    {
        int left = 0, right = _count - 1;
        
        while (left <= right)
        {
            int mid = left + ((right - left) >> 1);
            int bufferIndex = (_tail + mid) % _capacity;
            long midTime = _timestamps[bufferIndex];
            
            if (midTime == time) return mid;
            if (midTime < time) left = mid + 1;
            else right = mid - 1;
        }
        return ~left;
    }

    public void Dispose()
    {
        if (_usePooling)
        {
            if (_buffer != null)
            {
                _bufferPool!.Return(_buffer, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<T>());
                _buffer = null!;
            }
            if (_timestamps != null)
            {
                _timestampPool!.Return(_timestamps);
                _timestamps = null!;
            }
        }
        else
        {
            _buffer = null!;
            _timestamps = null!;
        }
    }
}

public struct TimelineStats
{
    public int SnapshotCount { get; init; }

    public int Capacity { get; init; }

    public long StartTime { get; init; }

    public long EndTime { get; init; }

    public long Duration { get; init; }

    public double AverageFrameTime { get; init; }

    public long MemoryUsage { get; init; }
}
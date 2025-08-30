using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Omni.Collections.Spatial;

namespace Omni.Collections.Temporal;

/// <summary>
/// A temporal-spatial data structure that combines time-series snapshots with spatial hash grids for efficient 4D indexing.
/// Provides O(1) Insert operations and O(k) spatial queries at any point in time, where k is the number of results.
/// Excellent for simulation replay systems, temporal GIS data, motion tracking, or any scenario
/// requiring efficient queries across both time and space dimensions simultaneously.
/// </summary>
public class TemporalSpatialGrid<T> : IDisposable where T : notnull
{
    private readonly TimelineArray<SpatialSnapshot<T>> _timeline;
    private readonly float _cellSize;
    private readonly long _frameDuration;
    private readonly object _lock = new object();
    static private readonly ConcurrentQueue<SpatialSnapshot<T>> SnapshotPool = new ConcurrentQueue<SpatialSnapshot<T>>();
    static private readonly ConcurrentQueue<SpatialHashGrid<T>> GridPool = new ConcurrentQueue<SpatialHashGrid<T>>();
    private readonly bool _usePooling;
    private SpatialHashGrid<T> _currentGrid;
    private long _lastRecordedTime;
    private bool _autoRecord;
    public int SnapshotCount => _timeline.Count;
    public long CurrentTime => _timeline.CurrentTime;
    public (long start, long end) TimeRange => _timeline.Count > 0 ? (_timeline.StartTime, _timeline.EndTime) : (0, 0);
    public int CurrentObjectCount => _currentGrid.Count;
    public float CellSize => _cellSize;
    public TemporalSpatialGrid(int capacity = 3600, float cellSize = 64.0f, long frameDuration = 16, bool autoRecord = false)
        : this(capacity, cellSize, frameDuration, autoRecord, usePooling: false) { }

    private TemporalSpatialGrid(int capacity, float cellSize, long frameDuration, bool autoRecord, bool usePooling)
    {
        _timeline = new TimelineArray<SpatialSnapshot<T>>(capacity, (int)frameDuration);
        _cellSize = cellSize;
        _frameDuration = frameDuration;
        _autoRecord = autoRecord;
        _usePooling = usePooling;
        if (_usePooling && GridPool.TryDequeue(out SpatialHashGrid<T>? pooledGrid))
        {
            pooledGrid.Clear();
            _currentGrid = pooledGrid;
        }
        else
        {
            _currentGrid = new SpatialHashGrid<T>(cellSize);
        }
        _lastRecordedTime = 0; // Initialize to 0 so first auto-record can trigger immediately
    }

    public static TemporalSpatialGrid<T> CreateWithArrayPool(int capacity = 3600, float cellSize = 64.0f, long frameDuration = 16, bool autoRecord = false)
    {
        return new TemporalSpatialGrid<T>(capacity, cellSize, frameDuration, autoRecord, usePooling: true);
    }

    static private readonly ConcurrentQueue<TemporalSpatialGrid<T>> InstancePool = new ConcurrentQueue<TemporalSpatialGrid<T>>();
    public static TemporalSpatialGrid<T> Rent(int capacity = 3600, float cellSize = 64.0f, long frameDuration = 16, bool autoRecord = false)
    {
        if (InstancePool.TryDequeue(out TemporalSpatialGrid<T>? instance))
        {
            instance.ResetForReuse(capacity, cellSize, frameDuration, autoRecord);
            return instance;
        }
        return new TemporalSpatialGrid<T>(capacity, cellSize, frameDuration, autoRecord);
    }

    public void Return()
    {
        Clear();
        InstancePool.Enqueue(this);
    }

    private void ResetForReuse(int capacity, float cellSize, long frameDuration, bool autoRecord)
    {
        Clear();
        _autoRecord = autoRecord;
        _lastRecordedTime = 0; // Initialize to 0 so first auto-record can trigger immediately
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Insert(float x, float y, T item)
    {
        lock (_lock)
        {
            _currentGrid.Insert(x, y, item);
            if (_autoRecord)
            {
                var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                if (_lastRecordedTime == 0 || currentTime - _lastRecordedTime >= _frameDuration)
                {
                    RecordCurrentState(currentTime);
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(float x, float y, T item)
    {
        lock (_lock)
        {
            var removed = _currentGrid.Remove(x, y, item);
            if (removed && _autoRecord)
            {
                var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                if (currentTime - _lastRecordedTime >= _frameDuration)
                {
                    RecordCurrentState(currentTime);
                }
            }
            return removed;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<T> GetObjectsInRadius(float x, float y, float radius)
    {
        lock (_lock)
        {
            return _currentGrid.GetObjectsInRadius(x, y, radius);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<T> GetObjectsInRectangle(float minX, float minY, float maxX, float maxY)
    {
        lock (_lock)
        {
            return _currentGrid.GetObjectsInRectangle(minX, minY, maxX, maxY);
        }
    }

    public void RecordSnapshot(long? timestamp = null)
    {
        var time = timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        lock (_lock)
        {
            RecordCurrentState(time);
        }
    }

    public void StartAutoRecording(long intervalMs)
    {
        _autoRecord = true;
    }

    public void StopAutoRecording()
    {
        _autoRecord = false;
    }

    public IEnumerable<T> GetObjectsInRadiusAtTime(float x, float y, float radius, long timestamp)
    {
        lock (_lock)
        {
            SpatialSnapshot<T>? snapshot = _timeline.GetAtTime(timestamp);
            if (snapshot == null) yield break;
            foreach (var obj in snapshot.Grid.GetObjectsInRadius(x, y, radius))
            {
                yield return obj;
            }
        }
    }

    public IEnumerable<T> GetObjectsInRectangleAtTime(float minX, float minY, float maxX, float maxY, long timestamp)
    {
        lock (_lock)
        {
            SpatialSnapshot<T>? snapshot = _timeline.GetAtTime(timestamp);
            if (snapshot == null) yield break;
            foreach (var obj in snapshot.Grid.GetObjectsInRectangle(minX, minY, maxX, maxY))
            {
                yield return obj;
            }
        }
    }

    public bool WereObjectsCollidingAtTime(long timestamp, T objectA, T objectB, float collisionRadius)
    {
        lock (_lock)
        {
            SpatialSnapshot<T>? snapshot = _timeline.GetAtTime(timestamp);
            if (snapshot == null) return false;
            IEnumerable<(T first, T second)>? collisions = snapshot.Grid.GetPotentialCollisions();
            return false;
        }
    }

    public Dictionary<(int cellX, int cellY), int> GenerateHeatMap(
        float minX, float minY, float maxX, float maxY,
        long startTime, long endTime, float cellSize)
    {
        var heatMap = new Dictionary<(int, int), int>();
        lock (_lock)
        {
            foreach (SpatialSnapshot<T>? snapshot in _timeline.Replay(startTime, endTime))
            {
                foreach (var obj in snapshot.Grid.GetObjectsInRectangle(minX, minY, maxX, maxY))
                {
                    var cellX = (int)((minX) / cellSize);
                    var cellY = (int)((minY) / cellSize);
                    var key = (cellX, cellY);
                    heatMap[key] = heatMap.GetValueOrDefault(key) + 1;
                }
            }
        }
        return heatMap;
    }

    public List<(long timestamp, float x, float y)> TrackObjectMovement(T targetObject, long startTime, long endTime)
    {
        var path = new List<(long, float, float)>();
        lock (_lock)
        {
            foreach (SpatialSnapshot<T>? snapshot in _timeline.Replay(startTime, endTime))
            {
            }
        }
        return path;
    }

    public SpatialHashGrid<T>? GetSpatialStateAtTime(long timestamp)
    {
        lock (_lock)
        {
            SpatialSnapshot<T>? snapshot = _timeline.GetAtTime(timestamp);
            return snapshot != null ? snapshot.Grid : null;
        }
    }

    public IEnumerable<SpatialSnapshot<T>> ReplaySpatialHistory(long startTime, long endTime)
    {
        lock (_lock)
        {
            return _timeline.Replay(startTime, endTime).ToList();
        }
    }

    public SpatialHashGrid<T>? StepInTime(bool forward = true)
    {
        lock (_lock)
        {
            var success = forward ? _timeline.NextFrame() : _timeline.PreviousFrame();
            
            // If first step fails and we have data, try to auto-position and step again
            if (!success && _timeline.Count > 0)
            {
                // Position to the end for backward stepping, start for forward stepping
                var targetTime = forward ? _timeline.StartTime : _timeline.EndTime;
                if (_timeline.RewindTo(targetTime))
                {
                    success = forward ? _timeline.NextFrame() : _timeline.PreviousFrame();
                }
            }
            
            if (!success) return null;
            SpatialSnapshot<T>? snapshot = _timeline.Current;
            return snapshot?.Grid;
        }
    }

    public TemporalSpatialStats GetStats()
    {
        lock (_lock)
        {
            var timelineStats = _timeline.GetStats();
            var spatialStats = _currentGrid.GetStats();
            return new TemporalSpatialStats
            {
                SnapshotCount = _timeline.Count,
                TimeRange = TimeRange,
                TotalMemoryUsage = timelineStats.MemoryUsage + (spatialStats.OccupiedCells * 64),
                AverageObjectsPerSnapshot = _timeline.Count > 0 ? (double)spatialStats.TotalObjects / _timeline.Count : 0,
                SpatialCellCount = spatialStats.OccupiedCells,
                SpatialCellSize = _cellSize,
                LastRecordingTime = _lastRecordedTime,
                AutoRecording = _autoRecord
            };
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _timeline.Clear();
            _currentGrid.Clear();
            _lastRecordedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_usePooling && _currentGrid != null)
            {
                _currentGrid.Clear();
                GridPool.Enqueue(_currentGrid);
            }
            _timeline?.Dispose();
        }
    }

    private void RecordCurrentState(long timestamp)
    {
        SpatialHashGrid<T> gridCopy;
        if (_usePooling && GridPool.TryDequeue(out SpatialHashGrid<T>? pooledGrid))
        {
            pooledGrid.Clear();
            gridCopy = pooledGrid;
        }
        else
        {
            gridCopy = new SpatialHashGrid<T>(_cellSize);
        }
        foreach (var (x, y, obj) in _currentGrid.GetAllObjects())
        {
            gridCopy.Insert(x, y, obj);
        }
        SpatialSnapshot<T> snapshot;
        if (_usePooling && SnapshotPool.TryDequeue(out SpatialSnapshot<T>? pooledSnapshot))
        {
            pooledSnapshot.Timestamp = timestamp;
            pooledSnapshot.Grid = gridCopy;
            pooledSnapshot.ObjectCount = _currentGrid.Count;
            snapshot = pooledSnapshot;
        }
        else
        {
            snapshot = new SpatialSnapshot<T>
            {
                Timestamp = timestamp,
                Grid = gridCopy,
                ObjectCount = _currentGrid.Count
            };
        }
        _timeline.Record(snapshot, timestamp);
        _lastRecordedTime = timestamp;
    }

    private void ReturnSnapshotToPool(SpatialSnapshot<T> snapshot)
    {
        if (_usePooling && snapshot != null)
        {
            if (snapshot.Grid != null)
            {
                snapshot.Grid.Clear();
                GridPool.Enqueue(snapshot.Grid);
            }
            snapshot.Grid = null!;
            snapshot.ObjectCount = 0;
            snapshot.Timestamp = 0;
            SnapshotPool.Enqueue(snapshot);
        }
    }
}

public class SpatialSnapshot<T> where T : notnull
{
    public long Timestamp { get; set; }

    public SpatialHashGrid<T> Grid { get; set; } = null!;
    public int ObjectCount { get; set; }
}

public struct TemporalSpatialStats
{
    public int SnapshotCount { get; set; }

    public (long start, long end) TimeRange { get; set; }

    public long TotalMemoryUsage { get; set; }

    public double AverageObjectsPerSnapshot { get; set; }

    public int SpatialCellCount { get; set; }

    public float SpatialCellSize { get; set; }

    public long LastRecordingTime { get; set; }

    public bool AutoRecording { get; set; }
}

public static class TemporalSpatialGridExtensions
{
    public static TemporalSpatialGrid<T> CreateForRts<T>(float mapSize, int maxHistoryMinutes = 10) where T : notnull
    {
        var cellSize = mapSize / 50;
        var capacity = maxHistoryMinutes * 60 * 60;
        return new TemporalSpatialGrid<T>(capacity, cellSize, frameDuration: 16, autoRecord: true);
    }

    public static TemporalSpatialGrid<T> CreateForReplay<T>(int fps = 60, int maxHistorySeconds = 300) where T : notnull
    {
        var frameDuration = 1000 / fps;
        var capacity = fps * maxHistorySeconds;
        return new TemporalSpatialGrid<T>(capacity, cellSize: 32.0f, frameDuration, autoRecord: true);
    }
}
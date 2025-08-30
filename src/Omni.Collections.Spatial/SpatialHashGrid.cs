using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Omni.Collections.Linear;

namespace Omni.Collections.Spatial;

/// <summary>
/// A spatial indexing structure using fixed-size grid cells that provides consistent performance for uniformly distributed data.
/// Provides O(1) Insert/Remove operations and O(k) spatial queries where k is the number of results.
/// Ideal for particle systems, game collision detection, uniform spatial distributions, or scenarios
/// where objects are relatively evenly distributed and frequent spatial queries are required.
/// </summary>
public class SpatialHashGrid<T> : IDisposable where T : notnull
{
    private const int SpatialThreshold = 5000;
    private Dictionary<long, List<SpatialEntry<T>>>? _grid;
    private readonly float _cellSize;
    private readonly float _inverseCellSize;
    private int _count;
    private List<SpatialEntry<T>>? _linearList;
    private bool _useSpatialMode;
    private readonly int _spatialThreshold;
    public int Count => _count;
    public float CellSize => _cellSize;
    public int OccupiedCells => _grid?.Count ?? 0;
    public SpatialHashGrid(float cellSize = 64.0f, int spatialThreshold = SpatialThreshold, int expectedItems = 0)
    {
        if (cellSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(cellSize), "Cell size must be positive");
        _cellSize = cellSize;
        _inverseCellSize = 1.0f / cellSize;
        _spatialThreshold = spatialThreshold;
        _count = 0;
        _useSpatialMode = spatialThreshold == 0;
        var initialCapacity = Math.Max(16, expectedItems);
        if (_useSpatialMode)
        {
            var estimatedCells = Math.Max(16, expectedItems / 8);
            _grid = new Dictionary<long, List<SpatialEntry<T>>>(estimatedCells);
        }
        else
        {
            _linearList = new List<SpatialEntry<T>>(initialCapacity);
        }
    }

    public void Insert(float x, float y, T item)
    {
        var entry = new SpatialEntry<T>(x, y, item);
        if (_useSpatialMode)
        {
            InsertIntoSpatialStructure(entry);
        }
        else
        {
            _linearList!.Add(entry);
            _count++;
            if (_count > _spatialThreshold)
            {
                ConvertToSpatialMode();
            }
        }
    }

    private void InsertIntoSpatialStructure(SpatialEntry<T> entry)
    {
        var cellKey = GetCellKey(entry.X, entry.Y);
        if (!_grid!.TryGetValue(cellKey, out List<SpatialEntry<T>>? cell))
        {
            cell = new List<SpatialEntry<T>>();
            _grid[cellKey] = cell;
        }
        cell.Add(entry);
        _count++;
    }

    private void ConvertToSpatialMode()
    {
        if (_useSpatialMode || _linearList == null) return;
        var estimatedCells = Math.Max(16, _linearList.Count / 8);
        _grid = new Dictionary<long, List<SpatialEntry<T>>>(estimatedCells);
        foreach (SpatialEntry<T> entry in _linearList)
        {
            var cellKey = GetCellKey(entry.X, entry.Y);
            if (!_grid.TryGetValue(cellKey, out List<SpatialEntry<T>>? cell))
            {
                cell = new List<SpatialEntry<T>>();
                _grid[cellKey] = cell;
            }
            cell.Add(entry);
        }
        _useSpatialMode = true;
        _linearList = null;
    }

    public void InsertBounds(float x, float y, float width, float height, T item)
    {
        var entry = new SpatialEntry<T>(x, y, item);
        if (_useSpatialMode)
        {
            int minCellX = (int)Math.Floor((x - width * 0.5f) * _inverseCellSize);
            int maxCellX = (int)Math.Floor((x + width * 0.5f) * _inverseCellSize);
            int minCellY = (int)Math.Floor((y - height * 0.5f) * _inverseCellSize);
            int maxCellY = (int)Math.Floor((y + height * 0.5f) * _inverseCellSize);
            for (int cellX = minCellX; cellX <= maxCellX; cellX++)
            {
                for (int cellY = minCellY; cellY <= maxCellY; cellY++)
                {
                    var cellKey = GetCellKey(cellX, cellY);
                    if (!_grid!.TryGetValue(cellKey, out List<SpatialEntry<T>>? cell))
                    {
                        cell = new List<SpatialEntry<T>>();
                        _grid[cellKey] = cell;
                    }
                    cell.Add(entry);
                }
            }
            _count++;
        }
        else
        {
            _linearList!.Add(entry);
            _count++;
            if (_count > _spatialThreshold)
            {
                ConvertToSpatialMode();
            }
        }
    }

    public bool Remove(float x, float y, T item)
    {
        if (_useSpatialMode)
        {
            var cellKey = GetCellKey(x, y);
            if (!_grid!.TryGetValue(cellKey, out List<SpatialEntry<T>>? cell))
                return false;
            for (int i = cell.Count - 1; i >= 0; i--)
            {
                if (EqualityComparer<T>.Default.Equals(cell[i].Item, item))
                {
                    cell.RemoveAt(i);
                    _count--;
                    if (cell.Count == 0)
                    {
                        _grid.Remove(cellKey);
                    }
                    return true;
                }
            }
            return false;
        }
        else
        {
            for (int i = _linearList!.Count - 1; i >= 0; i--)
            {
                SpatialEntry<T> entry = _linearList[i];
                if (Math.Abs(entry.X - x) < 0.001f && Math.Abs(entry.Y - y) < 0.001f &&
                    EqualityComparer<T>.Default.Equals(entry.Item, item))
                {
                    _linearList.RemoveAt(i);
                    _count--;
                    return true;
                }
            }
            return false;
        }
    }

    public IEnumerable<T> GetObjectsAt(float x, float y)
    {
        if (_useSpatialMode)
        {
            var cellKey = GetCellKey(x, y);
            if (_grid!.TryGetValue(cellKey, out List<SpatialEntry<T>>? cell))
            {
                foreach (SpatialEntry<T> entry in cell)
                {
                    if (Math.Abs(entry.X - x) < 0.001f && Math.Abs(entry.Y - y) < 0.001f)
                        yield return entry.Item;
                }
            }
        }
        else
        {
            foreach (SpatialEntry<T> entry in _linearList!)
            {
                if (Math.Abs(entry.X - x) < 0.001f && Math.Abs(entry.Y - y) < 0.001f)
                    yield return entry.Item;
            }
        }
    }

    public IEnumerable<T> GetObjectsInRadius(float x, float y, float radius)
    {
        var radiusSquared = radius * radius;
        if (_useSpatialMode)
        {
            int cellRadius = (int)Math.Ceiling(radius * _inverseCellSize);
            int centerCellX = (int)Math.Floor(x * _inverseCellSize);
            int centerCellY = (int)Math.Floor(y * _inverseCellSize);
            for (int dx = -cellRadius; dx <= cellRadius; dx++)
            {
                for (int dy = -cellRadius; dy <= cellRadius; dy++)
                {
                    var cellKey = GetCellKey(centerCellX + dx, centerCellY + dy);
                    if (_grid!.TryGetValue(cellKey, out List<SpatialEntry<T>>? cell))
                    {
                        foreach (SpatialEntry<T> entry in cell)
                        {
                            var distanceSquared = (entry.X - x) * (entry.X - x) + (entry.Y - y) * (entry.Y - y);
                            if (distanceSquared <= radiusSquared)
                                yield return entry.Item;
                        }
                    }
                }
            }
        }
        else
        {
            foreach (SpatialEntry<T> entry in _linearList!)
            {
                var distanceSquared = (entry.X - x) * (entry.X - x) + (entry.Y - y) * (entry.Y - y);
                if (distanceSquared <= radiusSquared)
                    yield return entry.Item;
            }
        }
    }

    public IEnumerable<T> GetObjectsInRectangle(float minX, float minY, float maxX, float maxY)
    {
        if (_useSpatialMode)
        {
            int minCellX = (int)Math.Floor(minX * _inverseCellSize);
            int maxCellX = (int)Math.Floor(maxX * _inverseCellSize);
            int minCellY = (int)Math.Floor(minY * _inverseCellSize);
            int maxCellY = (int)Math.Floor(maxY * _inverseCellSize);
            for (int cellX = minCellX; cellX <= maxCellX; cellX++)
            {
                for (int cellY = minCellY; cellY <= maxCellY; cellY++)
                {
                    var cellKey = GetCellKey(cellX, cellY);
                    if (_grid!.TryGetValue(cellKey, out List<SpatialEntry<T>>? cell))
                    {
                        foreach (SpatialEntry<T> entry in cell)
                        {
                            if (entry.X >= minX && entry.X <= maxX &&
                                entry.Y >= minY && entry.Y <= maxY)
                                yield return entry.Item;
                        }
                    }
                }
            }
        }
        else
        {
            foreach (SpatialEntry<T> entry in _linearList!)
            {
                if (entry.X >= minX && entry.X <= maxX &&
                    entry.Y >= minY && entry.Y <= maxY)
                    yield return entry.Item;
            }
        }
    }

    public IEnumerable<(T first, T second)> GetPotentialCollisions()
    {
        if (_useSpatialMode)
        {
            foreach (List<SpatialEntry<T>> cell in _grid!.Values)
            {
                for (int i = 0; i < cell.Count; i++)
                {
                    for (int j = i + 1; j < cell.Count; j++)
                    {
                        yield return (cell[i].Item, cell[j].Item);
                    }
                }
            }
        }
        else
        {
            List<SpatialEntry<T>>? entries = _linearList!;
            for (int i = 0; i < entries.Count; i++)
            {
                for (int j = i + 1; j < entries.Count; j++)
                {
                    yield return (entries[i].Item, entries[j].Item);
                }
            }
        }
    }

    public int GetPotentialCollisions(PooledList<(T, T)> results)
    {
        results.Clear();
        if (_useSpatialMode)
        {
            foreach (List<SpatialEntry<T>> cell in _grid!.Values)
            {
                for (int i = 0; i < cell.Count; i++)
                {
                    for (int j = i + 1; j < cell.Count; j++)
                    {
                        results.Add((cell[i].Item, cell[j].Item));
                    }
                }
            }
        }
        else
        {
            List<SpatialEntry<T>>? entries = _linearList!;
            for (int i = 0; i < entries.Count; i++)
            {
                for (int j = i + 1; j < entries.Count; j++)
                {
                    results.Add((entries[i].Item, entries[j].Item));
                }
            }
        }
        return results.Count;
    }

    public IEnumerable<T> GetPotentialCollisions(float x, float y, T? excludeItem = default)
    {
        if (_useSpatialMode)
        {
            var cellKey = GetCellKey(x, y);
            if (_grid!.TryGetValue(cellKey, out List<SpatialEntry<T>>? cell))
            {
                foreach (SpatialEntry<T> entry in cell)
                {
                    if (excludeItem == null || !EqualityComparer<T>.Default.Equals(entry.Item, excludeItem))
                        yield return entry.Item;
                }
            }
        }
        else
        {
            foreach (SpatialEntry<T> entry in _linearList!)
            {
                if (excludeItem == null || !EqualityComparer<T>.Default.Equals(entry.Item, excludeItem))
                    yield return entry.Item;
            }
        }
    }

    public void Clear()
    {
        if (_useSpatialMode && _grid != null)
        {
            _grid.Clear();
        }
        else
        {
            _linearList?.Clear();
        }
        _count = 0;
    }

    public IEnumerable<(float x, float y, T item)> GetAllObjects()
    {
        if (_useSpatialMode)
        {
            foreach (List<SpatialEntry<T>> cell in _grid!.Values)
            {
                foreach (SpatialEntry<T> entry in cell)
                    yield return (entry.X, entry.Y, entry.Item);
            }
        }
        else
        {
            foreach (SpatialEntry<T> entry in _linearList!)
                yield return (entry.X, entry.Y, entry.Item);
        }
    }

    public SpatialHashGridStats GetStats()
    {
        if (_useSpatialMode)
        {
            Span<int> cellCountsBuffer = stackalloc int[Math.Min(_grid!.Count, 1024)];
            Span<int> cellCounts = _grid.Count <= 1024 ? cellCountsBuffer : new int[_grid.Count];
            int index = 0;
            foreach (List<SpatialEntry<T>> cell in _grid.Values)
                cellCounts[index++] = cell.Count;
            cellCounts.Sort();
            return new SpatialHashGridStats
            {
                TotalObjects = _count,
                OccupiedCells = _grid.Count,
                AverageObjectsPerCell = _count > 0 ? (float)_count / _grid.Count : 0,
                MaxObjectsPerCell = cellCounts.Length > 0 ? cellCounts[^1] : 0,
                MedianObjectsPerCell = cellCounts.Length > 0 ? cellCounts[cellCounts.Length / 2] : 0
            };
        }
        else
        {
            return new SpatialHashGridStats
            {
                TotalObjects = _count,
                OccupiedCells = _count > 0 ? 1 : 0,
                AverageObjectsPerCell = _count,
                MaxObjectsPerCell = _count,
                MedianObjectsPerCell = _count
            };
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private long GetCellKey(float x, float y)
    {
        return GetCellKey((int)Math.Floor(x / _cellSize), (int)Math.Floor(y / _cellSize));
    }

    public void BuildFromItems(IEnumerable<(float x, float y, T item)> items)
    {
        (float x, float y, T item)[] itemArray = items.ToArray();
        Clear();
        if (itemArray.Length == 0) return;
        if (itemArray.Length > _spatialThreshold)
        {
            var estimatedCells = Math.Max(16, itemArray.Length / 8);
            _grid = new Dictionary<long, List<SpatialEntry<T>>>(estimatedCells);
            _useSpatialMode = true;
            foreach (var (x, y, item) in itemArray)
            {
                var entry = new SpatialEntry<T>(x, y, item);
                var cellKey = GetCellKey(x, y);
                if (!_grid.TryGetValue(cellKey, out List<SpatialEntry<T>>? cell))
                {
                    cell = new List<SpatialEntry<T>>();
                    _grid[cellKey] = cell;
                }
                cell.Add(entry);
                _count++;
            }
        }
        else
        {
            _linearList = new List<SpatialEntry<T>>(itemArray.Length);
            foreach (var (x, y, item) in itemArray)
            {
                _linearList.Add(new SpatialEntry<T>(x, y, item));
                _count++;
            }
        }
    }

    public void Dispose()
    {
        Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static private long GetCellKey(int cellX, int cellY)
    {
        return ((long)cellX << 32) | (uint)cellY;
    }
}
readonly struct SpatialEntry<T>
{
    public readonly float X;
    public readonly float Y;
    public readonly T Item;
    public SpatialEntry(float x, float y, T item)
    {
        X = x;
        Y = y;
        Item = item;
    }
}

public readonly struct SpatialHashGridStats
{
    public int TotalObjects { get; init; }

    public int OccupiedCells { get; init; }

    public float AverageObjectsPerCell { get; init; }

    public int MaxObjectsPerCell { get; init; }

    public int MedianObjectsPerCell { get; init; }
}
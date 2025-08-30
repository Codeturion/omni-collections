using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Omni.Collections.Grid2D.HexGrid;

/// <summary>
/// A hexagonal grid data structure using axial coordinates that provides natural hexagonal topology operations.
/// Provides O(1) get/set operations and efficient hexagonal neighbor queries with built-in pathfinding support.
/// Perfect for strategy games, tactical grids, board games, or any application requiring hexagonal topology
/// with specialized neighbor relationships and pathfinding algorithms.
/// </summary>
public class HexGrid2D<T> : IEnumerable<HexCell<T>>
{
    private readonly Dictionary<HexCoord, T> _cells;
    private readonly HexLayout _layout;
    private int _version;
    public int Count => _cells.Count;
    public HexLayout Layout => _layout;
    public int Version => _version;
    public IEnumerator<HexCell<T>> GetEnumerator() => GetCells().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public HexGrid2D() : this(HexLayout.FlatTop()) { }

    public HexGrid2D(HexLayout layout)
    {
        _cells = new Dictionary<HexCoord, T>();
        _layout = layout ?? throw new ArgumentNullException(nameof(layout));
    }

    public HexGrid2D(int capacity) : this(capacity, HexLayout.FlatTop()) { }

    public HexGrid2D(int capacity, HexLayout layout)
    {
        _cells = new Dictionary<HexCoord, T>(capacity);
        _layout = layout ?? throw new ArgumentNullException(nameof(layout));
    }

    public T this[HexCoord coord]
    {
        get => _cells.TryGetValue(coord, out var value) ? value : default!;
        set
        {
            var wasPresent = _cells.ContainsKey(coord);
            _cells[coord] = value;
            if (!wasPresent) _version++;
        }
    }

    public T this[int q, int r]
    {
        get => this[new HexCoord(q, r)];
        set => this[new HexCoord(q, r)] = value;
    }

    public bool Contains(HexCoord coord) => _cells.ContainsKey(coord);
    public bool TryGetValue(HexCoord coord, out T value) => _cells.TryGetValue(coord, out value!);
    public void Set(HexCoord coord, T value)
    {
        this[coord] = value;
    }

    public void Set(int q, int r, T value)
    {
        this[q, r] = value;
    }

    public bool Remove(HexCoord coord)
    {
        if (_cells.Remove(coord))
        {
            _version++;
            return true;
        }
        return false;
    }

    public void Clear()
    {
        if (_cells.Count > 0)
        {
            _cells.Clear();
            _version++;
        }
    }

    public IEnumerable<HexCoord> GetCoordinates() => _cells.Keys;
    public IEnumerable<T> GetValues() => _cells.Values;
    public IEnumerable<HexCell<T>> GetCells()
    {
        return _cells.Select(kvp => new HexCell<T>(kvp.Key, kvp.Value));
    }

    public IEnumerable<HexCell<T>> GetNeighbors(HexCoord coord, bool includeEmpty = false)
    {
        foreach (var neighborCoord in coord.GetNeighbors())
        {
            if (_cells.TryGetValue(neighborCoord, out var value))
            {
                yield return new HexCell<T>(neighborCoord, value);
            }
            else if (includeEmpty)
            {
                yield return new HexCell<T>(neighborCoord, default!);
            }
        }
    }

    public IEnumerable<HexCell<T>> GetWithinDistance(HexCoord center, int distance, bool includeEmpty = false)
    {
        foreach (var coord in center.GetWithinDistance(distance))
        {
            if (_cells.TryGetValue(coord, out var value))
            {
                yield return new HexCell<T>(coord, value);
            }
            else if (includeEmpty)
            {
                yield return new HexCell<T>(coord, default!);
            }
        }
    }

    public IEnumerable<HexCell<T>> GetRing(HexCoord center, int distance, bool includeEmpty = false)
    {
        foreach (var coord in center.GetRing(distance))
        {
            if (_cells.TryGetValue(coord, out var value))
            {
                yield return new HexCell<T>(coord, value);
            }
            else if (includeEmpty)
            {
                yield return new HexCell<T>(coord, default!);
            }
        }
    }

    public IEnumerable<HexCell<T>> GetLine(HexCoord start, HexCoord end, bool includeEmpty = false)
    {
        foreach (var coord in start.GetLineTo(end))
        {
            if (_cells.TryGetValue(coord, out var value))
            {
                yield return new HexCell<T>(coord, value);
            }
            else if (includeEmpty)
            {
                yield return new HexCell<T>(coord, default!);
            }
        }
    }

    public IEnumerable<HexCoord> FindPath(HexCoord start, HexCoord goal,
        Func<HexCoord, bool> isBlocked,
        Func<HexCoord, double>? getCost = null)
    {
        return HexPathfinding.FindPath(start, goal, isBlocked, getCost ?? (_ => 1.0));
    }

    public IEnumerable<(HexCoord coord, double remainingMovement)> GetReachable(HexCoord start, double movementPoints,
        Func<HexCoord, bool> isBlocked,
        Func<HexCoord, double>? getCost = null)
    {
        return HexPathfinding.GetReachable(start, movementPoints, isBlocked, getCost ?? (_ => 1.0));
    }

    public (double x, double y) ToPixel(HexCoord coord) => _layout.ToPixel(coord);
    public HexCoord FromPixel(double x, double y) => _layout.FromPixel(x, y);
    public (HexCoord min, HexCoord max) GetBounds()
    {
        if (_cells.Count == 0)
            return (new HexCoord(0, 0), new HexCoord(0, 0));
        var coords = _cells.Keys;
        var minQ = coords.Min(c => c.Q);
        var maxQ = coords.Max(c => c.Q);
        var minR = coords.Min(c => c.R);
        var maxR = coords.Max(c => c.R);
        return (new HexCoord(minQ, minR), new HexCoord(maxQ, maxR));
    }

    public void FillHexagon(HexCoord center, int radius, T value)
    {
        foreach (var coord in center.GetWithinDistance(radius))
        {
            this[coord] = value;
        }
    }

    public void FillRing(HexCoord center, int radius, T value)
    {
        foreach (var coord in center.GetRing(radius))
        {
            this[coord] = value;
        }
    }

    public void FillRectangle(int left, int right, int top, int bottom, T value)
    {
        for (int q = left; q <= right; q++)
        {
            for (int r = top; r <= bottom; r++)
            {
                this[q, r] = value;
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Omni.Collections.Spatial;

/// <summary>
/// A 2D spatial partitioning tree using quadrant subdivision that automatically switches between linear and tree modes for optimal performance.
/// Provides O(log n) Insert/Remove operations and O(log n + k) range queries where k is the number of results.
/// Ideal for 2D spatial indexing, collision detection, geographic data systems, or any scenario requiring
/// efficient spatial queries with dynamic point distributions.
/// </summary>
public class QuadTree<T>
{
    private const int SpatialThreshold = 5000;
    private readonly QuadTreeNode _root;
    private readonly int _maxDepth;
    private readonly int _maxItemsPerNode;
    private readonly int _spatialThreshold;
    private List<QuadTreeItem<T>>? _linearList;
    private bool _useSpatialMode;
    public int Count { get; private set; }

    public Rectangle Bounds => _root.Bounds;
    public QuadTree(Rectangle bounds, int maxDepth = 8, int maxItemsPerNode = 16, int spatialThreshold = SpatialThreshold)
    {
        if (maxDepth <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxDepth));
        if (maxItemsPerNode <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxItemsPerNode));
        _maxDepth = maxDepth;
        _maxItemsPerNode = maxItemsPerNode;
        _spatialThreshold = spatialThreshold;
        _root = new QuadTreeNode(bounds, 0);
        Count = 0;
        if (spatialThreshold <= 0)
        {
            _useSpatialMode = true;
            _linearList = null;
        }
        else
        {
            _useSpatialMode = false;
            _linearList = [];
        }
    }

    public void Insert(Point point, T item)
    {
        if (!_root.Bounds.Contains(point))
            throw new ArgumentException("Point outside tree bounds");
        var quadItem = new QuadTreeItem<T>(point, item);
        if (_useSpatialMode)
        {
            _root.Insert(quadItem, _maxDepth, _maxItemsPerNode);
        }
        else
        {
            _linearList!.Add(quadItem);
            if (Count + 1 > _spatialThreshold)
            {
                ConvertToSpatialMode();
            }
        }
        Count++;
    }

    public bool Remove(Point point, T item)
    {
        bool removed;
        if (_useSpatialMode)
        {
            removed = _root.Remove(point, item);
        }
        else
        {
            removed = false;
            for (int i = _linearList!.Count - 1; i >= 0; i--)
            {
                QuadTreeItem<T> listItem = _linearList[i];
                if (listItem.Point.Equals(point) && EqualityComparer<T>.Default.Equals(listItem.Item, item))
                {
                    _linearList.RemoveAt(i);
                    removed = true;
                    break;
                }
            }
        }
        if (removed)
            Count--;
        return removed;
    }

    public List<T> Query(Rectangle region)
    {
        var results = new List<T>();
        if (_useSpatialMode)
        {
            _root.Query(region, results);
        }
        else
        {
            foreach (QuadTreeItem<T> item in _linearList!)
            {
                if (region.Contains(item.Point))
                    results.Add(item.Item);
            }
        }
        return results;
    }

    public void Query(Rectangle region, List<T> results)
    {
        results.Clear();
        if (_useSpatialMode)
        {
            _root.Query(region, results);
        }
        else
        {
            foreach (QuadTreeItem<T> item in _linearList!)
            {
                if (region.Contains(item.Point))
                    results.Add(item.Item);
            }
        }
    }

    public void Query(Rectangle region, Action<T> callback)
    {
        if (_useSpatialMode)
        {
            _root.Query(region, callback);
        }
        else
        {
            foreach (QuadTreeItem<T> item in _linearList!)
            {
                if (region.Contains(item.Point))
                    callback(item.Item);
            }
        }
    }

    public T FindNearest(Point point)
    {
        if (Count == 0)
            throw new InvalidOperationException("QuadTree is empty");
        var best = default(T)!;
        var bestDistance = double.MaxValue;
        if (_useSpatialMode)
        {
            _root.FindNearest(point, ref best, ref bestDistance);
        }
        else
        {
            var bestDistanceSquared = bestDistance * bestDistance;
            foreach (QuadTreeItem<T> item in _linearList!)
            {
                var distanceSquared = point.DistanceSquaredTo(item.Point);
                if (distanceSquared < bestDistanceSquared)
                {
                    bestDistanceSquared = distanceSquared;
                    bestDistance = Math.Sqrt(distanceSquared);
                    best = item.Item;
                }
            }
        }
        if (bestDistance == double.MaxValue)
            throw new InvalidOperationException("No items found");
        return best;
    }

    private void ConvertToSpatialMode()
    {
        if (_useSpatialMode || _linearList == null) return;
        QuadTreeItem<T>[] itemsToInsert = _linearList.ToArray();
        _linearList = null;
        _useSpatialMode = true;
        foreach (QuadTreeItem<T> item in itemsToInsert)
        {
            _root.Insert(item, _maxDepth, _maxItemsPerNode);
        }
    }

    private class QuadTreeNode
    {
        public Rectangle Bounds { get; }

        public int Depth { get; }

        private List<QuadTreeItem<T>>? _items;
        private QuadTreeNode? _northWest, _northEast, _southWest, _southEast;
        private bool _isLeaf = true;
        public QuadTreeNode(Rectangle bounds, int depth)
        {
            Bounds = bounds;
            Depth = depth;
        }

        public void Insert(QuadTreeItem<T> item, int maxDepth, int maxItemsPerNode)
        {
            if (!_isLeaf)
            {
                GetQuadrant(item.Point)?.Insert(item, maxDepth, maxItemsPerNode);
                return;
            }
            _items ??= [];
            _items.Add(item);
            if (_items.Count > maxItemsPerNode && Depth < maxDepth)
            {
                Subdivide();
                List<QuadTreeItem<T>>? itemsToRedistribute = _items;
                _items = null;
                _isLeaf = false;
                foreach (QuadTreeItem<T> existingItem in itemsToRedistribute)
                {
                    GetQuadrant(existingItem.Point)?.Insert(existingItem, maxDepth, maxItemsPerNode);
                }
            }
        }

        public bool Remove(Point point, T item)
        {
            if (!_isLeaf)
            {
                return GetQuadrant(point)?.Remove(point, item) ?? false;
            }
            if (_items == null)
                return false;
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].Point.Equals(point) &&
                    EqualityComparer<T>.Default.Equals(_items[i].Item, item))
                {
                    _items.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Query(Rectangle region, List<T> results)
        {
            if (!(Bounds.X < region.Right && Bounds.Right > region.X &&
                  Bounds.Y < region.Bottom && Bounds.Bottom > region.Y))
                return;
            if (_isLeaf)
            {
                if (_items != null)
                {
                    var itemCount = _items.Count;
                    for (int i = 0; i < itemCount; i++)
                    {
                        QuadTreeItem<T> item = _items[i];
                        if (item.Point.X >= region.X && item.Point.X < region.Right &&
                            item.Point.Y >= region.Y && item.Point.Y < region.Bottom)
                        {
                            results.Add(item.Item);
                        }
                    }
                }
            }
            else
            {
                _northWest?.Query(region, results);
                _northEast?.Query(region, results);
                _southWest?.Query(region, results);
                _southEast?.Query(region, results);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Query(Rectangle region, Action<T> callback)
        {
            if (!(Bounds.X < region.Right && Bounds.Right > region.X &&
                  Bounds.Y < region.Bottom && Bounds.Bottom > region.Y))
                return;
            if (_isLeaf)
            {
                if (_items != null)
                {
                    var itemCount = _items.Count;
                    for (int i = 0; i < itemCount; i++)
                    {
                        QuadTreeItem<T> item = _items[i];
                        if (item.Point.X >= region.X && item.Point.X < region.Right &&
                            item.Point.Y >= region.Y && item.Point.Y < region.Bottom)
                        {
                            callback(item.Item);
                        }
                    }
                }
            }
            else
            {
                _northWest?.Query(region, callback);
                _northEast?.Query(region, callback);
                _southWest?.Query(region, callback);
                _southEast?.Query(region, callback);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FindNearest(Point point, ref T best, ref double bestDistance)
        {
            var bounds = Bounds;
            var dx = Math.Max(0, Math.Max(bounds.X - point.X, point.X - bounds.Right));
            var dy = Math.Max(0, Math.Max(bounds.Y - point.Y, point.Y - bounds.Bottom));
            var boundsDistanceSquared = dx * dx + dy * dy;
            if (boundsDistanceSquared > bestDistance * bestDistance)
                return;
            if (_isLeaf)
            {
                if (_items != null)
                {
                    var bestDistanceSquared = bestDistance * bestDistance;
                    var itemCount = _items.Count;
                    for (int i = 0; i < itemCount; i++)
                    {
                        QuadTreeItem<T> item = _items[i];
                        var itemDx = point.X - item.Point.X;
                        var itemDy = point.Y - item.Point.Y;
                        var distanceSquared = itemDx * itemDx + itemDy * itemDy;
                        if (distanceSquared < bestDistanceSquared)
                        {
                            best = item.Item;
                            bestDistanceSquared = distanceSquared;
                            bestDistance = Math.Sqrt(distanceSquared);
                        }
                    }
                }
            }
            else
            {
                var nwDist = (_northWest != null) ? GetQuadrantDistanceSquared(point, _northWest.Bounds) : double.MaxValue;
                var neDist = (_northEast != null) ? GetQuadrantDistanceSquared(point, _northEast.Bounds) : double.MaxValue;
                var swDist = (_southWest != null) ? GetQuadrantDistanceSquared(point, _southWest.Bounds) : double.MaxValue;
                var seDist = (_southEast != null) ? GetQuadrantDistanceSquared(point, _southEast.Bounds) : double.MaxValue;
                if (nwDist <= neDist && nwDist <= swDist && nwDist <= seDist)
                {
                    _northWest?.FindNearest(point, ref best, ref bestDistance);
                    if (neDist <= swDist && neDist <= seDist)
                    {
                        _northEast?.FindNearest(point, ref best, ref bestDistance);
                        if (swDist <= seDist)
                        {
                            _southWest?.FindNearest(point, ref best, ref bestDistance);
                            _southEast?.FindNearest(point, ref best, ref bestDistance);
                        }
                        else
                        {
                            _southEast?.FindNearest(point, ref best, ref bestDistance);
                            _southWest?.FindNearest(point, ref best, ref bestDistance);
                        }
                    }
                    else if (swDist <= seDist)
                    {
                        _southWest?.FindNearest(point, ref best, ref bestDistance);
                        _northEast?.FindNearest(point, ref best, ref bestDistance);
                        _southEast?.FindNearest(point, ref best, ref bestDistance);
                    }
                    else
                    {
                        _southEast?.FindNearest(point, ref best, ref bestDistance);
                        _northEast?.FindNearest(point, ref best, ref bestDistance);
                        _southWest?.FindNearest(point, ref best, ref bestDistance);
                    }
                }
                else if (neDist <= swDist && neDist <= seDist)
                {
                    _northEast?.FindNearest(point, ref best, ref bestDistance);
                    _northWest?.FindNearest(point, ref best, ref bestDistance);
                    if (swDist <= seDist)
                    {
                        _southWest?.FindNearest(point, ref best, ref bestDistance);
                        _southEast?.FindNearest(point, ref best, ref bestDistance);
                    }
                    else
                    {
                        _southEast?.FindNearest(point, ref best, ref bestDistance);
                        _southWest?.FindNearest(point, ref best, ref bestDistance);
                    }
                }
                else if (swDist <= seDist)
                {
                    _southWest?.FindNearest(point, ref best, ref bestDistance);
                    _northWest?.FindNearest(point, ref best, ref bestDistance);
                    _northEast?.FindNearest(point, ref best, ref bestDistance);
                    _southEast?.FindNearest(point, ref best, ref bestDistance);
                }
                else
                {
                    _southEast?.FindNearest(point, ref best, ref bestDistance);
                    _northWest?.FindNearest(point, ref best, ref bestDistance);
                    _northEast?.FindNearest(point, ref best, ref bestDistance);
                    _southWest?.FindNearest(point, ref best, ref bestDistance);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private double GetQuadrantDistanceSquared(Point point, Rectangle bounds)
        {
            var dx = Math.Max(0, Math.Max(bounds.X - point.X, point.X - bounds.Right));
            var dy = Math.Max(0, Math.Max(bounds.Y - point.Y, point.Y - bounds.Bottom));
            return dx * dx + dy * dy;
        }

        private void Subdivide()
        {
            double halfWidth = Bounds.Width / 2;
            double halfHeight = Bounds.Height / 2;
            double x = Bounds.X;
            double y = Bounds.Y;
            _northWest = new QuadTreeNode(new Rectangle(x, y, halfWidth, halfHeight), Depth + 1);
            _northEast = new QuadTreeNode(new Rectangle(x + halfWidth, y, halfWidth, halfHeight), Depth + 1);
            _southWest = new QuadTreeNode(new Rectangle(x, y + halfHeight, halfWidth, halfHeight), Depth + 1);
            _southEast = new QuadTreeNode(new Rectangle(x + halfWidth, y + halfHeight, halfWidth, halfHeight), Depth + 1);
        }

        private QuadTreeNode? GetQuadrant(Point point)
        {
            double midX = Bounds.X + Bounds.Width / 2;
            double midY = Bounds.Y + Bounds.Height / 2;
            bool north = point.Y < midY;
            bool west = point.X < midX;
            return (north, west) switch
            {
                (true, true) => _northWest,
                (true, false) => _northEast,
                (false, true) => _southWest,
                (false, false) => _southEast
            };
        }
    }
}

public readonly record struct Point(double X, double Y)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double DistanceTo(Point other)
    {
        double dx = X - other.X;
        double dy = Y - other.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double DistanceSquaredTo(Point other)
    {
        double dx = X - other.X;
        double dy = Y - other.Y;
        return dx * dx + dy * dy;
    }
}

public readonly record struct Rectangle(double X, double Y, double Width, double Height)
{
    public static readonly Rectangle Empty = new Rectangle(0, 0, 0, 0);
    public double Right => X + Width;
    public double Bottom => Y + Height;
    public Point Center => new Point(X + Width / 2, Y + Height / 2);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(Point point)
    {
        return point.X >= X && point.X < Right &&
               point.Y >= Y && point.Y < Bottom;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Intersects(Rectangle other)
    {
        return X < other.Right && Right > other.X &&
               Y < other.Bottom && Bottom > other.Y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double DistanceTo(Point point)
    {
        double dx = Math.Max(0, Math.Max(X - point.X, point.X - Right));
        double dy = Math.Max(0, Math.Max(Y - point.Y, point.Y - Bottom));
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
readonly record struct QuadTreeItem<T>(Point Point, T Item);
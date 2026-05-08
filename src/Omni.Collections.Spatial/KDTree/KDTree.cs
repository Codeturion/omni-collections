using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Omni.Collections.Linear;
using Omni.Collections.Spatial.DistanceMetrics;

namespace Omni.Collections.Spatial.KDTree;

/// <summary>
/// A k-dimensional tree that efficiently partitions multi-dimensional space for high-performance spatial queries.
/// Provides O(log n) Insert operations and O(log n) nearest neighbor searches with automatic rebalancing.
/// Excellent for machine learning applications, nearest neighbor searches, multi-dimensional range queries,
/// or any scenario requiring efficient proximity searches in high-dimensional space.
/// </summary>
public class KdTree<T> : IDisposable
{
    private KdNode? _root;
    private readonly int _dimensions;
    private readonly IKdPointProvider<T> _pointProvider;
    private readonly IDistanceMetric _distanceMetric;
    private int _count;
    public int Count => _count;
    public int Dimensions => _dimensions;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private KdNode CreateNode()
    {
        return new KdNode();
    }

    public KdTree(IKdPointProvider<T> pointProvider, int dimensions = 2, IDistanceMetric? distanceMetric = null)
    {
        if (dimensions <= 0)
            throw new ArgumentOutOfRangeException(nameof(dimensions), "Dimensions must be positive");
        if (pointProvider == null)
            throw new ArgumentNullException(nameof(pointProvider));
        _pointProvider = pointProvider;
        _distanceMetric = distanceMetric ?? new EuclideanDistance();
        _dimensions = dimensions;
    }

    public static KdTree<T> Create2D(Func<T, double> getX, Func<T, double> getY, IDistanceMetric? distanceMetric = null)
    {
        return new KdTree<T>(new KdPointProvider2D<T>(getX, getY), 2, distanceMetric);
    }

    public static KdTree<T> Create3D(Func<T, double> getX, Func<T, double> getY, Func<T, double> getZ, IDistanceMetric? distanceMetric = null)
    {
        return new KdTree<T>(new KdPointProvider3D<T>(getX, getY, getZ), 3, distanceMetric);
    }

    public void Insert(T item)
    {
        _root = InsertRecursive(_root, item, 0);
        _count++;
        // Amortize the O(n) height check: only consider rebalancing every 64 inserts past 1000.
        if (_count > 1000 && (_count & 63) == 0)
        {
            RebalanceIfNeeded();
        }
    }

    private void RebalanceIfNeeded()
    {
        if (_root == null) return;
        var height = EstimateTreeHeight(_root);
#if NET5_0_OR_GREATER
        var optimalHeight = Math.Log2(_count);
#else
        var optimalHeight = Math.Log(_count, 2.0);
#endif
        // Rebuild when height exceeds 2 * log2(count) + 2 — i.e., the tree has degraded
        // significantly past balanced. Constant + slack term avoids spurious rebuilds at small count.
        if (height > optimalHeight * 2 + 2)
        {
            T[]? items = GetAllItems().ToArray();
            Clear();
            Build(items);
        }
    }

    private int EstimateTreeHeight(KdNode node)
    {
        if (node == null) return 0;
        return 1 + Math.Max(
            node.Left != null ? EstimateTreeHeight(node.Left) : 0,
            node.Right != null ? EstimateTreeHeight(node.Right) : 0
        );
    }

    public void InsertRange(IEnumerable<T> items)
    {
        T[]? itemArray = items.ToArray();
        if (itemArray.Length == 0)
            return;
        if (_root == null)
        {
            Build(itemArray);
            return;
        }
        if (itemArray.Length > _count / 2 && _count > 100)
        {
            T[]? existingItems = GetAllItems().ToArray();
            var allItems = new T[existingItems.Length + itemArray.Length];
            Array.Copy(existingItems, 0, allItems, 0, existingItems.Length);
            Array.Copy(itemArray, 0, allItems, existingItems.Length, itemArray.Length);
            Clear();
            Build(allItems);
        }
        else
        {
            foreach (var item in itemArray)
            {
                _root = InsertRecursive(_root, item, 0);
                _count++;
            }
        }
    }

    public void Build(IEnumerable<T> items)
    {
        T[]? itemArray = items.ToArray();
        _root = BuildRecursiveOptimized(itemArray, 0, itemArray.Length - 1, 0);
        _count = itemArray.Length;
    }

    public void BatchInsert(IEnumerable<T> items)
    {
        T[]? itemArray = items.ToArray();
        if (itemArray.Length == 0)
            return;
        if (_root == null || itemArray.Length > 50)
        {
            if (_root == null)
            {
                Build(itemArray);
            }
            else
            {
                T[]? existingItems = GetAllItems().ToArray();
                var allItems = new T[existingItems.Length + itemArray.Length];
                Array.Copy(existingItems, 0, allItems, 0, existingItems.Length);
                Array.Copy(itemArray, 0, allItems, existingItems.Length, itemArray.Length);
                Clear();
                Build(allItems);
            }
        }
        else
        {
            InsertRange(itemArray);
        }
    }

    public T? FindNearest(T target)
    {
        if (_root == null)
            return default;
        var targetCoords = _pointProvider.GetCoordinates(target);
        var best = new NearestResult<T> { Item = default!, DistanceSquared = double.MaxValue, HasResult = false };
        FindNearestRecursive(_root, targetCoords, 0, ref best);
        return best.HasResult ? best.Item : default;
    }

    public List<T> FindNearestK(T target, int k)
    {
        if (_root == null || k <= 0)
            return [];
        var targetCoords = _pointProvider.GetCoordinates(target);
        var heap = new BoundedPriorityQueue<T>(k);
        FindNearestKRecursive(_root, targetCoords, 0, heap);
        var result = new List<T>();
        while (heap.Count > 0)
        {
            result.Add(heap.ExtractMax());
        }
        result.Reverse();
        return result;
    }

    public void FindNearestK(T target, int k, PooledList<T> results)
    {
        results.Clear();
        if (_root == null || k <= 0)
            return;
        var targetCoords = _pointProvider.GetCoordinates(target);
        var heap = new BoundedPriorityQueue<T>(k);
        FindNearestKRecursive(_root, targetCoords, 0, heap);
        var tempResults = new T[heap.Count];
        int index = heap.Count - 1;
        while (heap.Count > 0)
        {
            tempResults[index--] = heap.ExtractMax();
        }
        results.AddRange((IEnumerable<T>)tempResults);
    }

    public List<T> FindWithinRadius(T target, double radius)
    {
        if (_root == null)
            return [];
        var targetCoords = _pointProvider.GetCoordinates(target);
        var radiusSquared = radius * radius;
        var result = new List<T>();
        FindWithinRadiusRecursive(_root, targetCoords, radiusSquared, 0, result);
        return result;
    }

    public void FindWithinRadius(T target, double radius, PooledList<T> results)
    {
        results.Clear();
        if (_root == null)
            return;
        var targetCoords = _pointProvider.GetCoordinates(target);
        var radiusSquared = radius * radius;
        FindWithinRadiusRecursive(_root, targetCoords, radiusSquared, 0, results);
    }

    public List<T> FindInRange(double[] minCoords, double[] maxCoords)
    {
        if (_root == null)
            return [];
        if (minCoords.Length != _dimensions || maxCoords.Length != _dimensions)
            throw new ArgumentException("Coordinate arrays must match tree dimensions");
        var result = new List<T>();
        FindInRangeRecursive(_root, minCoords, maxCoords, 0, result);
        return result;
    }

    public void FindInRange(double[] minCoords, double[] maxCoords, PooledList<T> results)
    {
        results.Clear();
        if (_root == null)
            return;
        if (minCoords.Length != _dimensions || maxCoords.Length != _dimensions)
            throw new ArgumentException("Coordinate arrays must match tree dimensions");
        FindInRangeRecursive(_root, minCoords, maxCoords, 0, results);
    }

    public IEnumerable<T> GetAllItems()
    {
        if (_root == null)
            yield break;
        var stack = new Stack<KdNode>();
        stack.Push(_root);
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            yield return node.Item;
            if (node.Right != null)
                stack.Push(node.Right);
            if (node.Left != null)
                stack.Push(node.Left);
        }
    }

    private KdNode InsertRecursive(KdNode? node, T item, int depth)
    {
        if (node == null)
        {
            var newNode = CreateNode();
            newNode.Item = item;
            return newNode;
        }
        var dimension = depth % _dimensions;
        var itemCoords = _pointProvider.GetCoordinates(item);
        var nodeCoords = _pointProvider.GetCoordinates(node.Item);
        if (itemCoords[dimension] < nodeCoords[dimension])
            node.Left = InsertRecursive(node.Left, item, depth + 1);
        else
            node.Right = InsertRecursive(node.Right, item, depth + 1);
        return node;
    }

    private KdNode? BuildRecursiveOptimized(T[] items, int start, int end, int depth)
    {
        if (start > end)
            return null;
        var dimension = depth % _dimensions;
        var median = (start + end) / 2;
        // Quickselect partitions items[start..end] in O(end - start + 1) average,
        // placing the median-by-coordinate at items[median] with everything to its
        // left smaller and everything to its right at-or-greater. Per-level cost
        // drops from O(N log N) (full Sort) to O(N), so total Build cost is
        // O(N log N) instead of the previous O(N log² N).
        QuickselectByDimension(items, start, end, median, dimension);
        var node = CreateNode();
        node.Item = items[median];
        node.Left = BuildRecursiveOptimized(items, start, median - 1, depth + 1);
        node.Right = BuildRecursiveOptimized(items, median + 1, end, depth + 1);
        return node;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private double CoordAt(T item, int dimension)
    {
        return _pointProvider.GetCoordinates(item)[dimension];
    }

    private void QuickselectByDimension(T[] items, int low, int high, int k, int dimension)
    {
        // Iterative Quickselect with median-of-three pivot selection. Partitions in-place
        // until items[k] is the kth-smallest by `dimension`-coordinate within [low..high];
        // items[low..k-1] are <= items[k] and items[k+1..high] are >= items[k] (no further
        // ordering guaranteed within each side, which is exactly what kd-tree build needs).
        // Average O(high - low + 1) per call; worst case O((high - low + 1)²) on adversarial
        // input (mitigated by median-of-three).
        while (low < high)
        {
            int mid = low + (high - low) / 2;
            // Sort {items[low], items[mid], items[high]} so items[mid] holds the median
            // of the three, then swap median to items[high] for Lomuto partition.
            if (CoordAt(items[low], dimension) > CoordAt(items[high], dimension))
                (items[low], items[high]) = (items[high], items[low]);
            if (CoordAt(items[mid], dimension) > CoordAt(items[high], dimension))
                (items[mid], items[high]) = (items[high], items[mid]);
            if (CoordAt(items[low], dimension) > CoordAt(items[mid], dimension))
                (items[low], items[mid]) = (items[mid], items[low]);
            // Now items[low] <= items[mid] <= items[high]; pivot is items[mid].
            (items[mid], items[high]) = (items[high], items[mid]);
            double pivot = CoordAt(items[high], dimension);
            // Lomuto partition over [low..high - 1] with pivot at items[high].
            int i = low - 1;
            for (int j = low; j < high; j++)
            {
                if (CoordAt(items[j], dimension) < pivot)
                {
                    i++;
                    if (i != j) (items[i], items[j]) = (items[j], items[i]);
                }
            }
            i++;
            (items[i], items[high]) = (items[high], items[i]);
            if (i == k) return;
            if (i > k) high = i - 1;
            else low = i + 1;
        }
    }

    private void FindNearestRecursive(KdNode node, double[] target, int depth, ref NearestResult<T> best)
    {
        var nodeCoords = _pointProvider.GetCoordinates(node.Item);
        var distanceSquared = CalculateDistanceSquared(target, nodeCoords);
        if (distanceSquared < best.DistanceSquared)
        {
            best.Item = node.Item;
            best.DistanceSquared = distanceSquared;
            best.HasResult = true;
        }
        var dimension = depth % _dimensions;
        var diff = target[dimension] - nodeCoords[dimension];
        var firstSide = diff < 0 ? node.Left : node.Right;
        var secondSide = diff < 0 ? node.Right : node.Left;
        if (firstSide != null)
            FindNearestRecursive(firstSide, target, depth + 1, ref best);
        if (secondSide != null && diff * diff < best.DistanceSquared)
            FindNearestRecursive(secondSide, target, depth + 1, ref best);
    }

    private void FindNearestKRecursive(KdNode node, double[] target, int depth, BoundedPriorityQueue<T> heap)
    {
        var nodeCoords = _pointProvider.GetCoordinates(node.Item);
        var distanceSquared = CalculateDistanceSquared(target, nodeCoords);
        heap.TryAdd(node.Item, distanceSquared);
        var dimension = depth % _dimensions;
        var diff = target[dimension] - nodeCoords[dimension];
        var firstSide = diff < 0 ? node.Left : node.Right;
        var secondSide = diff < 0 ? node.Right : node.Left;
        if (firstSide != null)
            FindNearestKRecursive(firstSide, target, depth + 1, heap);
        if (secondSide != null && (heap.Count < heap.Capacity || diff * diff < heap.MaxPriority))
            FindNearestKRecursive(secondSide, target, depth + 1, heap);
    }

    private void FindWithinRadiusRecursive(KdNode node, double[] target, double radiusSquared, int depth, ICollection<T> result)
    {
        var nodeCoords = _pointProvider.GetCoordinates(node.Item);
        var distanceSquared = CalculateDistanceSquared(target, nodeCoords);
        if (distanceSquared <= radiusSquared)
            result.Add(node.Item);
        var dimension = depth % _dimensions;
        var diff = target[dimension] - nodeCoords[dimension];
        if (node.Left != null && diff - Math.Sqrt(radiusSquared) <= 0)
            FindWithinRadiusRecursive(node.Left, target, radiusSquared, depth + 1, result);
        if (node.Right != null && diff + Math.Sqrt(radiusSquared) >= 0)
            FindWithinRadiusRecursive(node.Right, target, radiusSquared, depth + 1, result);
    }

    private void FindInRangeRecursive(KdNode node, double[] minCoords, double[] maxCoords, int depth, ICollection<T> result)
    {
        var nodeCoords = _pointProvider.GetCoordinates(node.Item);
        bool inRange = true;
        for (int i = 0; i < _dimensions; i++)
        {
            if (nodeCoords[i] < minCoords[i] || nodeCoords[i] > maxCoords[i])
            {
                inRange = false;
                break;
            }
        }
        if (inRange)
            result.Add(node.Item);
        var dimension = depth % _dimensions;
        if (node.Left != null && minCoords[dimension] <= nodeCoords[dimension])
            FindInRangeRecursive(node.Left, minCoords, maxCoords, depth + 1, result);
        if (node.Right != null && maxCoords[dimension] >= nodeCoords[dimension])
            FindInRangeRecursive(node.Right, minCoords, maxCoords, depth + 1, result);
    }

    public void Clear()
    {
        _root = null;
        _count = 0;
    }

    public void Dispose()
    {
        Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private double CalculateDistanceSquared(double[] a, double[] b)
    {
        return _distanceMetric.CalculateDistanceSquared(a, b);
    }

    private class KdNode
    {
        public T Item { get; set; } = default!;
        public KdNode? Left { get; set; }

        public KdNode? Right { get; set; }
    }

    private struct NearestResult<TItem>
    {
        public TItem Item;
        public double DistanceSquared;
        public bool HasResult;
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Omni.Collections.Spatial;

/// <summary>
/// A 3D spatial partitioning tree using octant subdivision that efficiently manages 3D spatial data through recursive space partitioning.
/// Provides O(log n) Insert operations and O(log n + k) spatial queries where k is the number of results.
/// Perfect for 3D collision detection in games, particle systems, 3D graphics culling, or any 3D point cloud
/// applications requiring efficient spatial queries and nearest neighbor searches.
/// </summary>
public class OctTree<T> : IDisposable
{
    private OctNode? _root;
    private readonly IOctPointProvider<T> _pointProvider;
    private readonly float _minSize;
    private int _count;
    public int Count => _count;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private OctNode CreateNode(OctBounds bounds)
    {
        return new OctNode(bounds)
        {
            Items = new List<T>()
        };
    }

    public float MinSize => _minSize;
    public OctTree(IOctPointProvider<T> pointProvider, float minSize = 1.0f)
    {
        if (minSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(minSize), "MinSize must be positive");
        if (pointProvider == null)
            throw new ArgumentNullException(nameof(pointProvider));
        _pointProvider = pointProvider;
        _minSize = minSize;
    }

    public static OctTree<T> Create3D(Func<T, float> getX, Func<T, float> getY, Func<T, float> getZ, float minSize = 1.0f)
    {
        return new OctTree<T>(new OctPointProvider3D<T>(getX, getY, getZ), minSize);
    }

    public void Insert(T item, OctBounds? bounds = null)
    {
        var position = _pointProvider.GetPosition(item);
        if (_root == null)
        {
            if (bounds == null)
            {
                var size = Math.Max(_minSize * 16, 100.0f);
                bounds = new OctBounds(
                    position.X - size, position.Y - size, position.Z - size,
                    position.X + size, position.Y + size, position.Z + size);
            }
            _root = CreateNode(bounds.Value);
        }
        while (!_root.Bounds.Contains(position))
        {
            _root = ExpandRoot(_root, position);
        }
        InsertRecursive(_root, item, position);
        _count++;
    }

    public void InsertRange(IEnumerable<T> items, OctBounds? bounds = null)
    {
        T[]? itemArray = items.ToArray();
        if (itemArray.Length == 0)
            return;
        if (bounds == null && itemArray.Length > 0)
        {
            Vector3[]? positions = itemArray.Select(item => _pointProvider.GetPosition(item)).ToArray();
            var minX = positions.Min(p => p.X);
            var minY = positions.Min(p => p.Y);
            var minZ = positions.Min(p => p.Z);
            var maxX = positions.Max(p => p.X);
            var maxY = positions.Max(p => p.Y);
            var maxZ = positions.Max(p => p.Z);
            var padding = Math.Max(_minSize * 2, 10.0f);
            bounds = new OctBounds(minX - padding, minY - padding, minZ - padding,
                                 maxX + padding, maxY + padding, maxZ + padding);
        }
        foreach (var item in itemArray)
        {
            Insert(item, bounds);
        }
    }

    public List<T> FindInSphere(Vector3 center, float radius)
    {
        if (_root == null)
            return new List<T>();
        var result = new List<T>();
        var radiusSquared = radius * radius;
        FindInSphereRecursive(_root, center, radiusSquared, result);
        return result;
    }

    public List<T> FindInBounds(OctBounds bounds)
    {
        if (_root == null)
            return new List<T>();
        var result = new List<T>();
        FindInBoundsRecursive(_root, bounds, result);
        return result;
    }

    public T? FindNearest(Vector3 target)
    {
        if (_root == null)
            return default;
        var best = new NearestResult3D<T> { Item = default, DistanceSquared = float.MaxValue, HasResult = false };
        FindNearestRecursive(_root, target, ref best);
        return best.HasResult ? best.Item : default;
    }

    public List<T> FindCollisions(T item, float radius)
    {
        var position = _pointProvider.GetPosition(item);
        List<T>? candidates = FindInSphere(position, radius);
        candidates.Remove(item);
        return candidates;
    }

    public List<T> FindInFrustum(Frustum frustum)
    {
        if (_root == null)
            return new List<T>();
        var result = new List<T>();
        FindInFrustumRecursive(_root, frustum, result);
        return result;
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

    public IEnumerable<T> GetAllItems()
    {
        if (_root == null)
            yield break;
        var stack = new Stack<OctNode>();
        stack.Push(_root);
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            if (node.Items != null)
        {
            foreach (var item in node.Items)
                yield return item;
        }
            if (node.Children != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    if (node.Children[i] != null)
                        stack.Push(node.Children[i]);
                }
            }
        }
    }

    private OctNode ExpandRoot(OctNode currentRoot, Vector3 newPoint)
    {
        var bounds = currentRoot.Bounds;
        var newSize = Math.Max(bounds.Width, Math.Max(bounds.Height, bounds.Depth)) * 2;
        var newBounds = new OctBounds(
            Math.Min(bounds.MinX, newPoint.X - newSize/2),
            Math.Min(bounds.MinY, newPoint.Y - newSize/2),
            Math.Min(bounds.MinZ, newPoint.Z - newSize/2),
            Math.Max(bounds.MaxX, newPoint.X + newSize/2),
            Math.Max(bounds.MaxY, newPoint.Y + newSize/2),
            Math.Max(bounds.MaxZ, newPoint.Z + newSize/2));
        var newRoot = CreateNode(newBounds);
        var octant = GetOctant(newRoot.Bounds, bounds.Center);
        if (newRoot.Children == null)
            newRoot.Children = new OctNode[8];
        newRoot.Children[octant] = currentRoot;
        return newRoot;
    }

    private void InsertRecursive(OctNode node, T item, Vector3 position)
    {
        if (node.Children == null && node.Items != null && node.Items.Count < 10)
        {
            node.Items.Add(item);
            return;
        }
        if (node.Children == null)
        {
            Subdivide(node);
        }
        if (node.Children == null)
        {
            node.Items?.Add(item);
            return;
        }
        var octant = GetOctant(node.Bounds, position);
        if (node.Children[octant] == null)
        {
            node.Children[octant] = CreateNode(GetOctantBounds(node.Bounds, octant));
        }
        InsertRecursive(node.Children[octant], item, position);
    }

    private void Subdivide(OctNode node)
    {
        if (node.Bounds.Width <= _minSize || node.Bounds.Height <= _minSize || node.Bounds.Depth <= _minSize)
            return;
        node.Children = new OctNode[8];
        var itemsToRedistribute = new List<T>();
        if (node.Items != null)
        {
            itemsToRedistribute.AddRange(node.Items);
            node.Items.Clear();
        }
        foreach (var item in itemsToRedistribute)
        {
            var position = _pointProvider.GetPosition(item);
            var octant = GetOctant(node.Bounds, position);
            if (node.Children[octant] == null)
            {
                node.Children[octant] = CreateNode(GetOctantBounds(node.Bounds, octant));
            }
            node.Children[octant].Items?.Add(item);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetOctant(OctBounds bounds, Vector3 point)
    {
        var center = bounds.Center;
        int octant = 0;
        if (point.X >= center.X) octant |= 1;
        if (point.Y >= center.Y) octant |= 2;
        if (point.Z >= center.Z) octant |= 4;
        return octant;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private OctBounds GetOctantBounds(OctBounds parentBounds, int octant)
    {
        var center = parentBounds.Center;
        var halfWidth = parentBounds.Width * 0.5f;
        var halfHeight = parentBounds.Height * 0.5f;
        var halfDepth = parentBounds.Depth * 0.5f;
        var minX = (octant & 1) == 0 ? parentBounds.MinX : center.X;
        var maxX = (octant & 1) == 0 ? center.X : parentBounds.MaxX;
        var minY = (octant & 2) == 0 ? parentBounds.MinY : center.Y;
        var maxY = (octant & 2) == 0 ? center.Y : parentBounds.MaxY;
        var minZ = (octant & 4) == 0 ? parentBounds.MinZ : center.Z;
        var maxZ = (octant & 4) == 0 ? center.Z : parentBounds.MaxZ;
        return new OctBounds(minX, minY, minZ, maxX, maxY, maxZ);
    }

    private void FindInSphereRecursive(OctNode node, Vector3 center, float radiusSquared, List<T> result)
    {
        if (!node.Bounds.IntersectsSphere(center, radiusSquared))
            return;
        if (node.Items != null)
        {
            foreach (var item in node.Items)
        {
            var position = _pointProvider.GetPosition(item);
            var distanceSquared = DistanceSquared(center, position);
            if (distanceSquared <= radiusSquared)
                result.Add(item);
        }
        }
        if (node.Children != null)
        {
            for (int i = 0; i < 8; i++)
            {
                if (node.Children[i] != null)
                    FindInSphereRecursive(node.Children[i], center, radiusSquared, result);
            }
        }
    }

    private void FindInBoundsRecursive(OctNode node, OctBounds bounds, List<T> result)
    {
        if (!node.Bounds.Intersects(bounds))
            return;
        if (node.Items != null)
        {
            foreach (var item in node.Items)
        {
            var position = _pointProvider.GetPosition(item);
            if (bounds.Contains(position))
                result.Add(item);
        }
        }
        if (node.Children != null)
        {
            for (int i = 0; i < 8; i++)
            {
                if (node.Children[i] != null)
                    FindInBoundsRecursive(node.Children[i], bounds, result);
            }
        }
    }

    private void FindNearestRecursive(OctNode node, Vector3 target, ref NearestResult3D<T> best)
    {
        var distanceToNode = node.Bounds.DistanceSquaredTo(target);
        if (distanceToNode >= best.DistanceSquared)
            return;
        if (node.Items != null)
        {
            foreach (var item in node.Items)
        {
            var position = _pointProvider.GetPosition(item);
            var distanceSquared = DistanceSquared(target, position);
            if (distanceSquared < best.DistanceSquared)
            {
                best.Item = item;
                best.DistanceSquared = distanceSquared;
                best.HasResult = true;
            }
        }
        }
        if (node.Children != null)
        {
            var childDistances = new (int index, float distance)[8];
            int childCount = 0;
            for (int i = 0; i < 8; i++)
            {
                if (node.Children[i] != null)
                {
                    var distance = node.Children[i].Bounds.DistanceSquaredTo(target);
                    childDistances[childCount++] = (i, distance);
                }
            }
            Array.Sort(childDistances, 0, childCount, Comparer<(int, float)>.Create((a, b) => a.Item2.CompareTo(b.Item2)));
            for (int j = 0; j < childCount; j++)
            {
                FindNearestRecursive(node.Children[childDistances[j].index], target, ref best);
            }
        }
    }

    private void FindInFrustumRecursive(OctNode node, Frustum frustum, List<T> result)
    {
        if (!frustum.IntersectsBounds(node.Bounds))
            return;
        if (node.Items != null)
        {
            foreach (var item in node.Items)
        {
            var position = _pointProvider.GetPosition(item);
            if (frustum.ContainsPoint(position))
                result.Add(item);
        }
        }
        if (node.Children != null)
        {
            for (int i = 0; i < 8; i++)
            {
                if (node.Children[i] != null)
                    FindInFrustumRecursive(node.Children[i], frustum, result);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static private float DistanceSquared(Vector3 a, Vector3 b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        var dz = a.Z - b.Z;
        return dx * dx + dy * dy + dz * dz;
    }

    private class OctNode
    {
        public OctBounds Bounds { get; set; }

        public List<T>? Items { get; set; }

        public OctNode[]? Children { get; set; }

        public OctNode(OctBounds bounds)
        {
            Bounds = bounds;
            Items = null;
        }
    }
}

public interface IOctPointProvider<T>
{
    Vector3 GetPosition(T item);
}

public class OctPointProvider3D<T> : IOctPointProvider<T>
{
    private readonly Func<T, float> _getX;
    private readonly Func<T, float> _getY;
    private readonly Func<T, float> _getZ;
    public OctPointProvider3D(Func<T, float> getX, Func<T, float> getY, Func<T, float> getZ)
    {
        _getX = getX ?? throw new ArgumentNullException(nameof(getX));
        _getY = getY ?? throw new ArgumentNullException(nameof(getY));
        _getZ = getZ ?? throw new ArgumentNullException(nameof(getZ));
    }

    public Vector3 GetPosition(T item)
    {
        return new Vector3(_getX(item), _getY(item), _getZ(item));
    }
}

public readonly struct Vector3
{
    public readonly float X, Y, Z;
    public Vector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static Vector3 Zero => new Vector3(0, 0, 0);
}

public readonly struct OctBounds
{
    public readonly float MinX, MinY, MinZ;
    public readonly float MaxX, MaxY, MaxZ;
    public OctBounds(float minX, float minY, float minZ, float maxX, float maxY, float maxZ)
    {
        MinX = minX;
        MinY = minY;
        MinZ = minZ;
        MaxX = maxX;
        MaxY = maxY;
        MaxZ = maxZ;
    }

    public float Width => MaxX - MinX;
    public float Height => MaxY - MinY;
    public float Depth => MaxZ - MinZ;
    public Vector3 Center => new Vector3((MinX + MaxX) * 0.5f, (MinY + MaxY) * 0.5f, (MinZ + MaxZ) * 0.5f);
    public bool Contains(Vector3 point)
    {
        return point.X >= MinX && point.X <= MaxX &&
               point.Y >= MinY && point.Y <= MaxY &&
               point.Z >= MinZ && point.Z <= MaxZ;
    }

    public bool Intersects(OctBounds other)
    {
        return MinX <= other.MaxX && MaxX >= other.MinX &&
               MinY <= other.MaxY && MaxY >= other.MinY &&
               MinZ <= other.MaxZ && MaxZ >= other.MinZ;
    }

    public bool IntersectsSphere(Vector3 center, float radiusSquared)
    {
        var closestX = Math.Max(MinX, Math.Min(center.X, MaxX));
        var closestY = Math.Max(MinY, Math.Min(center.Y, MaxY));
        var closestZ = Math.Max(MinZ, Math.Min(center.Z, MaxZ));
        var dx = center.X - closestX;
        var dy = center.Y - closestY;
        var dz = center.Z - closestZ;
        return dx * dx + dy * dy + dz * dz <= radiusSquared;
    }

    public float DistanceSquaredTo(Vector3 point)
    {
        var dx = Math.Max(0, Math.Max(MinX - point.X, point.X - MaxX));
        var dy = Math.Max(0, Math.Max(MinY - point.Y, point.Y - MaxY));
        var dz = Math.Max(0, Math.Max(MinZ - point.Z, point.Z - MaxZ));
        return dx * dx + dy * dy + dz * dz;
    }
}

public class Frustum
{
    private readonly Plane[] _planes;
    public Frustum(Plane[] planes)
    {
        if (planes == null || planes.Length != 6)
            throw new ArgumentException("Frustum must have exactly 6 planes");
        _planes = planes;
    }

    public bool ContainsPoint(Vector3 point)
    {
        foreach (var plane in _planes)
        {
            if (plane.DistanceTo(point) < 0)
                return false;
        }
        return true;
    }

    public bool IntersectsBounds(OctBounds bounds)
    {
        foreach (var plane in _planes)
        {
            if (plane.DistanceTo(GetPositiveVertex(bounds, plane)) < 0)
                return false;
        }
        return true;
    }

    private Vector3 GetPositiveVertex(OctBounds bounds, Plane plane)
    {
        return new Vector3(
            plane.Normal.X >= 0 ? bounds.MaxX : bounds.MinX,
            plane.Normal.Y >= 0 ? bounds.MaxY : bounds.MinY,
            plane.Normal.Z >= 0 ? bounds.MaxZ : bounds.MinZ);
    }
}

public readonly struct Plane
{
    public readonly Vector3 Normal;
    public readonly float Distance;
    public Plane(Vector3 normal, float distance)
    {
        Normal = normal;
        Distance = distance;
    }

    public float DistanceTo(Vector3 point)
    {
        return Normal.X * point.X + Normal.Y * point.Y + Normal.Z * point.Z + Distance;
    }
}

internal struct NearestResult3D<T>
{
    public T? Item;
    public float DistanceSquared;
    public bool HasResult;
}
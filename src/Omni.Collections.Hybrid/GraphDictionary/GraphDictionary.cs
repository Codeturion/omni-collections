using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Omni.Collections.Hybrid.GraphDictionary
{
    /// <summary>
    /// A graph-based dictionary that seamlessly maintains vertex-edge relationships with automatic bidirectional updates.
    /// Provides O(1) vertex operations and O(degree) edge operations with built-in path finding and cycle detection.
    /// Essential for social networks, dependency resolution, workflow engines, and any domain where relationships
    /// are as important as the data itself.
    /// </summary>
    public class GraphDictionary<TKey, TValue> : IDisposable, IEnumerable<KeyValuePair<TKey, TValue>>
        where TKey : notnull
    {
        #region Core Data Structures
        private readonly Dictionary<TKey, GraphNode> _nodes;

        private readonly ReaderWriterLockSlim _structureLock;
        private volatile bool _disposed;
        private readonly ConcurrentDictionary<string, object> _metricsCache;
        private volatile int _cacheVersion;
        private long _edgeCount;
        private long _lookupCount;
        private long _traversalCount;
        static private readonly ArrayPool<GraphNode> NodeArrayPool = ArrayPool<GraphNode>.Shared;
        static private readonly ArrayPool<TKey> KeyArrayPool = ArrayPool<TKey>.Shared;
        private GraphNode CreateNode()
        {
            return new GraphNode();
        }

        private readonly Dictionary<TKey, HashSet<TKey>> _incomingEdges;
        #endregion
        #region Node Structure
        private class GraphNode
        {
            public TValue Value { get; set; } = default!;
            public ConcurrentDictionary<TKey, EdgeInfo>? Neighbors { get; private set; }

            public DateTime LastAccessed { get; set; }

            public int AccessCount;
            public GraphNode()
            {
                Reset();
            }

            public void Initialize(TValue value)
            {
                Value = value;
                Neighbors = null;
                LastAccessed = default;
                AccessCount = 1;
            }

            public ConcurrentDictionary<TKey, EdgeInfo> GetOrCreateNeighbors()
            {
                return Neighbors ??= new ConcurrentDictionary<TKey, EdgeInfo>();
            }

            public void Reset()
            {
                Value = default!;
                Neighbors?.Clear();
                Neighbors = null;
                LastAccessed = default;
                AccessCount = 0;
            }

            public bool CanBePooled()
            {
                return true;
            }

            public static GraphNode CreateInstance()
            {
                return new GraphNode();
            }
        }

        private class EdgeInfo
        {
            public double Weight { get; set; }

            public DateTime Created { get; set; }

            public object? Metadata { get; set; }

            public EdgeInfo(double weight = 1.0, object? metadata = null)
            {
                Weight = weight;
                Created = DateTime.UtcNow;
                Metadata = metadata;
            }
        }
        #endregion
        #region Constructors
        public GraphDictionary()
        {
            _nodes = new Dictionary<TKey, GraphNode>();
            _structureLock = new ReaderWriterLockSlim();
            _metricsCache = new ConcurrentDictionary<string, object>();
            _cacheVersion = 0;
            _incomingEdges = new Dictionary<TKey, HashSet<TKey>>();
        }
        #endregion
        #region Dictionary Operations (O(1))
        public TValue this[TKey key]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                Interlocked.Increment(ref _lookupCount);
                _structureLock.EnterReadLock();
                try
                {
                    if (_nodes.TryGetValue(key, out var node))
                    {
                        Interlocked.Increment(ref node.AccessCount);
                        return node.Value;
                    }
                }
                finally
                {
                    _structureLock.ExitReadLock();
                }
                throw new KeyNotFoundException($"Key '{key}' not found in GraphDictionary");
            }
            set
            {
                _structureLock.EnterWriteLock();
                try
                {
                    if (_nodes.TryGetValue(key, out var existingNode))
                    {
                        existingNode.Value = value;
                        Interlocked.Increment(ref existingNode.AccessCount);
                    }
                    else
                    {
                        var newNode = CreateNode();
                        newNode.Initialize(value);
                        _nodes[key] = newNode;
                    }
                }
                finally
                {
                    _structureLock.ExitWriteLock();
                }
                InvalidateCache();
            }
        }

        public void Add(TKey key, TValue value)
        {
            var newNode = CreateNode();
            newNode.Initialize(value);
            _structureLock.EnterWriteLock();
            try
            {
                if (_nodes.ContainsKey(key))
                {
                    throw new ArgumentException($"Key '{key}' already exists in GraphDictionary");
                }
                _nodes[key] = newNode;
            }
            finally
            {
                _structureLock.ExitWriteLock();
            }
            InvalidateCache();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(TKey key, out TValue value)
        {
            Interlocked.Increment(ref _lookupCount);
            _structureLock.EnterReadLock();
            try
            {
                if (_nodes.TryGetValue(key, out var node))
                {
                    Interlocked.Increment(ref node.AccessCount);
                    value = node.Value;
                    return true;
                }
            }
            finally
            {
                _structureLock.ExitReadLock();
            }
            value = default!;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsKey(TKey key)
        {
            _structureLock.EnterReadLock();
            try
            {
                return _nodes.ContainsKey(key);
            }
            finally
            {
                _structureLock.ExitReadLock();
            }
        }

        public bool Remove(TKey key)
        {
            _structureLock.EnterWriteLock();
            try
            {
                if (_nodes.TryGetValue(key, out var node))
                {
                    _nodes.Remove(key);
                    if (_incomingEdges.TryGetValue(key, out HashSet<TKey>? incomingSet))
                    {
                        foreach (var sourceKey in incomingSet)
                        {
                            if (_nodes.TryGetValue(sourceKey, out var sourceNode))
                            {
                                if (sourceNode.Neighbors?.TryRemove(key, out _) ?? false)
                                {
                                    Interlocked.Decrement(ref _edgeCount);
                                }
                            }
                        }
                        _incomingEdges.Remove(key);
                    }
                    if (node.Neighbors != null)
                    {
                        foreach (var targetKey in node.Neighbors.Keys)
                        {
                            if (_incomingEdges.TryGetValue(targetKey, out HashSet<TKey>? targetIncomingSet))
                            {
                                targetIncomingSet.Remove(key);
                            }
                        }
                        Interlocked.Add(ref _edgeCount, -node.Neighbors.Count);
                    }
                    InvalidateCache();
                    return true;
                }
            }
            finally
            {
                _structureLock.ExitWriteLock();
            }
            return false;
        }

        public int Count
        {
            get
            {
                _structureLock.EnterReadLock();
                try
                {
                    return _nodes.Count;
                }
                finally
                {
                    _structureLock.ExitReadLock();
                }
            }
        }

        public ICollection<TKey> Keys
        {
            get
            {
                _structureLock.EnterReadLock();
                try
                {
                    return _nodes.Keys.ToList();
                }
                finally
                {
                    _structureLock.ExitReadLock();
                }
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                _structureLock.EnterReadLock();
                try
                {
                    return _nodes.Values.Select(n => n.Value).ToList();
                }
                finally
                {
                    _structureLock.ExitReadLock();
                }
            }
        }
        #endregion
        #region Graph Operations
        public bool AddEdge(TKey from, TKey to, double weight = 1.0, object? metadata = null)
        {
            _structureLock.EnterWriteLock();
            try
            {
                if (!_nodes.ContainsKey(from) || !_nodes.ContainsKey(to))
                    return false;
                var fromNode = _nodes[from];
                var edgeInfo = new EdgeInfo(weight, metadata);
                if (fromNode.GetOrCreateNeighbors().TryAdd(to, edgeInfo))
                {
                    if (!_incomingEdges.TryGetValue(to, out HashSet<TKey>? incomingSet))
                    {
                        incomingSet = [];
                        _incomingEdges[to] = incomingSet;
                    }
                    incomingSet.Add(from);
                    Interlocked.Increment(ref _edgeCount);
                    InvalidateCache();
                    return true;
                }
                return false;
            }
            finally
            {
                _structureLock.ExitWriteLock();
            }
        }

        public bool AddBidirectionalEdge(TKey from, TKey to, double weight = 1.0, object? metadata = null)
        {
            return AddEdge(from, to, weight, metadata) && AddEdge(to, from, weight, metadata);
        }

        public IEnumerable<TKey> GetNeighbors(TKey key)
        {
            _structureLock.EnterReadLock();
            try
            {
                if (_nodes.TryGetValue(key, out var node))
                {
                    return node.Neighbors?.Keys.ToList() ?? Enumerable.Empty<TKey>();
                }
                return [];
            }
            finally
            {
                _structureLock.ExitReadLock();
            }
        }

        public IEnumerable<(TKey Key, double Weight, object? Metadata)> GetNeighborsWithEdgeInfo(TKey key)
        {
            _structureLock.EnterReadLock();
            try
            {
                if (_nodes.TryGetValue(key, out var node))
                {
                    return node.Neighbors?.Select(kvp => (kvp.Key, kvp.Value.Weight, kvp.Value.Metadata)).ToList() ?? Enumerable.Empty<(TKey, double, object?)>();
                }
                return [];
            }
            finally
            {
                _structureLock.ExitReadLock();
            }
        }

        public bool HasEdge(TKey from, TKey to)
        {
            _structureLock.EnterReadLock();
            try
            {
                return _nodes.TryGetValue(from, out var node) && (node.Neighbors?.ContainsKey(to) ?? false);
            }
            finally
            {
                _structureLock.ExitReadLock();
            }
        }

        public bool RemoveEdge(TKey from, TKey to)
        {
            _structureLock.EnterWriteLock();
            try
            {
                if (_nodes.TryGetValue(from, out var node) && (node.Neighbors?.TryRemove(to, out _) ?? false))
                {
                    if (_incomingEdges.TryGetValue(to, out HashSet<TKey>? incomingSet))
                    {
                        incomingSet.Remove(from);
                    }
                    Interlocked.Decrement(ref _edgeCount);
                    InvalidateCache();
                    return true;
                }
                return false;
            }
            finally
            {
                _structureLock.ExitWriteLock();
            }
        }
        #endregion
        #region Advanced Graph Algorithms
        public GraphPath<TKey>? FindShortestPath(TKey start, TKey end)
        {
            Interlocked.Increment(ref _traversalCount);
            _structureLock.EnterReadLock();
            try
            {
                if (!_nodes.ContainsKey(start) || !_nodes.ContainsKey(end))
                    return null;
                var cacheKey = $"shortest_path_{start}_{end}_{_cacheVersion}";
                if (_metricsCache.TryGetValue(cacheKey, out var cached))
                    return (GraphPath<TKey>?)cached;
                var distances = new Dictionary<TKey, double>();
                var previous = new Dictionary<TKey, TKey>();
                var visited = new HashSet<TKey>();
                var queue = new SortedSet<(double Distance, TKey Key)>();
                foreach (var key in _nodes.Keys)
                {
                    distances[key] = key.Equals(start) ? 0 : double.PositiveInfinity;
                }
                queue.Add((0, start));
                while (queue.Count > 0)
                {
                    var (currentDistance, current) = queue.Min;
                    queue.Remove(queue.Min);
                    if (visited.Contains(current)) continue;
                    visited.Add(current);
                    if (current.Equals(end))
                    {
                        var path = new List<TKey>();
                        var totalWeight = distances[end];
                        var currentNode = end;
                        while (!currentNode.Equals(start))
                        {
                            path.Add(currentNode);
                            currentNode = previous[currentNode];
                        }
                        path.Add(start);
                        path.Reverse();
                        var result = new GraphPath<TKey>(path, totalWeight);
                        _metricsCache.TryAdd(cacheKey, result);
                        return result;
                    }
                    if (_nodes.TryGetValue(current, out var node) && node.Neighbors != null)
                    {
                        foreach (var (neighbor, edgeInfo) in node.Neighbors)
                        {
                            if (visited.Contains(neighbor)) continue;
                            var newDistance = currentDistance + edgeInfo.Weight;
                            if (newDistance < distances[neighbor])
                            {
                                distances[neighbor] = newDistance;
                                previous[neighbor] = current;
                                queue.Add((newDistance, neighbor));
                            }
                        }
                    }
                }
                _metricsCache.TryAdd(cacheKey, null!);
                return null;
            }
            finally
            {
                _structureLock.ExitReadLock();
            }
        }

        public IEnumerable<(TKey Key, double Distance)> FindNodesWithinDistance(TKey source, double maxDistance)
        {
            Interlocked.Increment(ref _traversalCount);
            _structureLock.EnterReadLock();
            try
            {
                if (!_nodes.ContainsKey(source))
                    return [];
                var distances = new Dictionary<TKey, double> { [source] = 0 };
                var queue = new Queue<TKey>();
                queue.Enqueue(source);
                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    var currentDistance = distances[current];
                    if (currentDistance >= maxDistance) continue;
                    if (_nodes.TryGetValue(current, out var node) && node.Neighbors != null)
                    {
                        foreach (var (neighbor, edgeInfo) in node.Neighbors)
                        {
                            var newDistance = currentDistance + edgeInfo.Weight;
                            if (newDistance <= maxDistance &&
                                (!distances.ContainsKey(neighbor) || newDistance < distances[neighbor]))
                            {
                                distances[neighbor] = newDistance;
                                queue.Enqueue(neighbor);
                            }
                        }
                    }
                }
                return distances.Where(kvp => !kvp.Key.Equals(source)).Select(kvp => (kvp.Key, kvp.Value));
            }
            finally
            {
                _structureLock.ExitReadLock();
            }
        }

        public IEnumerable<IEnumerable<TKey>> FindStronglyConnectedComponents()
        {
            Interlocked.Increment(ref _traversalCount);
            var cacheKey = $"scc_{_cacheVersion}";
            if (_metricsCache.TryGetValue(cacheKey, out var cached))
                return (IEnumerable<IEnumerable<TKey>>)cached;
            _structureLock.EnterReadLock();
            try
            {
                var components = new List<List<TKey>>();
                var visited = new HashSet<TKey>();
                var stack = new Stack<TKey>();
                var lowLinks = new Dictionary<TKey, int>();
                var indices = new Dictionary<TKey, int>();
                var onStack = new HashSet<TKey>();
                var index = 0;
                foreach (var node in _nodes.Keys)
                {
                    if (!visited.Contains(node))
                    {
                        StrongConnect(node, ref index, visited, stack, lowLinks, indices, onStack, components);
                    }
                }
                _metricsCache.TryAdd(cacheKey, components);
                return components;
            }
            finally
            {
                _structureLock.ExitReadLock();
            }
        }

        private void StrongConnect(TKey node, ref int index, HashSet<TKey> visited, Stack<TKey> stack,
            Dictionary<TKey, int> lowLinks, Dictionary<TKey, int> indices, HashSet<TKey> onStack,
            List<List<TKey>> components)
        {
            indices[node] = index;
            lowLinks[node] = index;
            index++;
            stack.Push(node);
            onStack.Add(node);
            visited.Add(node);
            if (_nodes.TryGetValue(node, out var graphNode) && graphNode.Neighbors != null)
            {
                foreach (var neighbor in graphNode.Neighbors.Keys)
                {
                    if (!visited.Contains(neighbor))
                    {
                        StrongConnect(neighbor, ref index, visited, stack, lowLinks, indices, onStack, components);
                        lowLinks[node] = Math.Min(lowLinks[node], lowLinks[neighbor]);
                    }
                    else if (onStack.Contains(neighbor))
                    {
                        lowLinks[node] = Math.Min(lowLinks[node], indices[neighbor]);
                    }
                }
            }
            if (lowLinks[node] == indices[node])
            {
                var component = new List<TKey>();
                TKey w;
                do
                {
                    w = stack.Pop();
                    onStack.Remove(w);
                    component.Add(w);
                } while (!w.Equals(node));
                components.Add(component);
            }
        }
        #endregion
        #region Graph Analytics
        public double GetClusteringCoefficient(TKey key)
        {
            _structureLock.EnterReadLock();
            try
            {
                if (!_nodes.TryGetValue(key, out var node) || node.Neighbors == null)
                    return 0.0;
                var neighbors = node.Neighbors.Keys.ToList();
                if (neighbors.Count < 2) return 0.0;
                var edges = 0;
                var possibleEdges = neighbors.Count * (neighbors.Count - 1) / 2;
                for (int i = 0; i < neighbors.Count; i++)
                {
                    for (int j = i + 1; j < neighbors.Count; j++)
                    {
                        if (_nodes.TryGetValue(neighbors[i], out var sourceNode) &&
                            (sourceNode.Neighbors?.ContainsKey(neighbors[j]) ?? false))
                            edges++;
                    }
                }
                return (double)edges / possibleEdges;
            }
            finally
            {
                _structureLock.ExitReadLock();
            }
        }

        public GraphStatistics GetStatistics()
        {
            var cacheKey = $"stats_{_cacheVersion}";
            if (_metricsCache.TryGetValue(cacheKey, out var cached))
                return (GraphStatistics)cached;
            _structureLock.EnterReadLock();
            try
            {
                var nodeCount = _nodes.Count;
                var edgeCount = _edgeCount;
                var avgDegree = nodeCount > 0 ? (double)edgeCount / nodeCount : 0;
                var degrees = _nodes.Values.Select(n => n.Neighbors?.Count ?? 0).ToList();
                var maxDegree = degrees.Count > 0 ? degrees.Max() : 0;
                var minDegree = degrees.Count > 0 ? degrees.Min() : 0;
                var density = nodeCount > 1 ? (double)edgeCount / (nodeCount * (nodeCount - 1)) : 0;
                var stats = new GraphStatistics
                {
                    NodeCount = nodeCount,
                    EdgeCount = edgeCount,
                    AverageDegree = avgDegree,
                    MaxDegree = maxDegree,
                    MinDegree = minDegree,
                    Density = density,
                    LookupCount = _lookupCount,
                    TraversalCount = _traversalCount,
                    CacheHits = _metricsCache.Count,
                    MemoryUsageBytes = EstimateMemoryUsage()
                };
                _metricsCache.TryAdd(cacheKey, stats);
                return stats;
            }
            finally
            {
                _structureLock.ExitReadLock();
            }
        }

        private long EstimateMemoryUsage()
        {
            const int nodeOverhead = 64;
            const int edgeOverhead = 32;
            return _nodes.Count * nodeOverhead + _edgeCount * edgeOverhead;
        }
        #endregion
        #region Cache Management
        private void InvalidateCache()
        {
            Interlocked.Increment(ref _cacheVersion);
        }

        public void ClearCache()
        {
            InvalidateCache();
        }
        #endregion
        #region IEnumerable Implementation
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return GetEnumeratorFast();
        }

        public IEnumerable<KeyValuePair<TKey, TValue>> GetChunkedEnumerator(int chunkSize = 1000)
        {
            if (chunkSize <= 0)
                throw new ArgumentException("Chunk size must be positive", nameof(chunkSize));
            var totalCount = _nodes.Count;
            if (totalCount == 0)
                yield break;
            var processedCount = 0;
            while (processedCount < totalCount)
            {
                var currentChunkSize = Math.Min(chunkSize, totalCount - processedCount);
                GraphNode[] nodeChunk = NodeArrayPool.Rent(currentChunkSize);
                TKey[] keyChunk = KeyArrayPool.Rent(currentChunkSize);
                try
                {
                    _structureLock.EnterReadLock();
                    try
                    {
                        var chunkIndex = 0;
                        var skipCount = processedCount;
                        foreach (KeyValuePair<TKey, GraphNode> kvp in _nodes)
                        {
                            if (skipCount > 0)
                            {
                                skipCount--;
                                continue;
                            }
                            if (chunkIndex >= currentChunkSize)
                                break;
                            nodeChunk[chunkIndex] = kvp.Value;
                            keyChunk[chunkIndex] = kvp.Key;
                            chunkIndex++;
                        }
                        for (int i = 0; i < chunkIndex; i++)
                        {
                            yield return new KeyValuePair<TKey, TValue>(keyChunk[i], nodeChunk[i].Value);
                        }
                        processedCount += chunkIndex;
                    }
                    finally
                    {
                        _structureLock.ExitReadLock();
                    }
                }
                finally
                {
                    NodeArrayPool.Return(nodeChunk, clearArray: true);
                    KeyArrayPool.Return(keyChunk, clearArray: true);
                }
            }
        }

        private IEnumerator<KeyValuePair<TKey, TValue>> GetEnumeratorFast()
        {
            var count = _nodes.Count;
            if (count == 0)
            {
                return new Enumerator(this);
            }
            GraphNode[] nodeSnapshot = NodeArrayPool.Rent(count);
            TKey[] keySnapshot = KeyArrayPool.Rent(count);
            try
            {
                _structureLock.EnterReadLock();
                try
                {
                    var index = 0;
                    foreach (KeyValuePair<TKey, GraphNode> kvp in _nodes)
                    {
                        nodeSnapshot[index] = kvp.Value;
                        keySnapshot[index] = kvp.Key;
                        index++;
                    }
                    return GetPooledEnumerator(nodeSnapshot, keySnapshot, count);
                }
                finally
                {
                    _structureLock.ExitReadLock();
                }
            }
            catch
            {
                NodeArrayPool.Return(nodeSnapshot, clearArray: true);
                KeyArrayPool.Return(keySnapshot, clearArray: true);
                throw;
            }
        }

        private IEnumerator<KeyValuePair<TKey, TValue>> GetPooledEnumerator(GraphNode[] nodes, TKey[] keys, int count)
        {
            try
            {
                for (int i = 0; i < count; i++)
                {
                    yield return new KeyValuePair<TKey, TValue>(keys[i], nodes[i].Value);
                }
            }
            finally
            {
                NodeArrayPool.Return(nodes, clearArray: true);
                KeyArrayPool.Return(keys, clearArray: true);
            }
        }

        private IEnumerator<KeyValuePair<TKey, TValue>> GetEnumeratorDirect()
        {
            foreach (KeyValuePair<TKey, GraphNode> kvp in _nodes)
            {
                var node = kvp.Value;
                yield return new KeyValuePair<TKey, TValue>(kvp.Key, node.Value);
            }
        }

        public IEnumerable<KeyValuePair<TKey, TValue>> GetUnsafeEnumerator()
        {
            foreach (KeyValuePair<TKey, GraphNode> kvp in _nodes)
            {
                var node = kvp.Value;
                yield return new KeyValuePair<TKey, TValue>(kvp.Key, node.Value);
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            private readonly GraphDictionary<TKey, TValue> _dictionary;
            private Dictionary<TKey, GraphNode>.Enumerator _nodeEnumerator;
            private KeyValuePair<TKey, TValue> _current;
            private bool _lockTaken;
            internal Enumerator(GraphDictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary;
                _current = default;
                _lockTaken = false;
                _dictionary._structureLock.EnterReadLock();
                _lockTaken = true;
                _nodeEnumerator = _dictionary._nodes.GetEnumerator();
            }

            public KeyValuePair<TKey, TValue> Current => _current;
            object IEnumerator.Current => Current;
            public bool MoveNext()
            {
                if (!_lockTaken)
                    throw new InvalidOperationException("Enumerator has been disposed");
                if (_nodeEnumerator.MoveNext())
                {
                    var kvp = _nodeEnumerator.Current;
                    _current = new KeyValuePair<TKey, TValue>(kvp.Key, kvp.Value.Value);
                    return true;
                }
                return false;
            }

            public void Reset()
            {
                throw new NotSupportedException("Reset is not supported");
            }

            public void Dispose()
            {
                if (_lockTaken)
                {
                    _dictionary._structureLock.ExitReadLock();
                    _lockTaken = false;
                }
                _nodeEnumerator.Dispose();
            }
        }
        #endregion
        #region IDisposable Implementation
        public void Dispose()
        {
            if (!_disposed)
            {
                _structureLock.Dispose();
                _nodes.Clear();
                _metricsCache.Clear();
                _incomingEdges.Clear();
                _disposed = true;
            }
        }
        #endregion
    }
}
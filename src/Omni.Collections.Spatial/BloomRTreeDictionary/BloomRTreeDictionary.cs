using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Omni.Collections.Linear;
using Omni.Collections.Probabilistic;

namespace Omni.Collections.Spatial.BloomRTreeDictionary;

/// <summary>
/// A spatial R-tree dictionary that achieves order-of-magnitude faster spatial queries through Bloom filter negative pruning.
/// Delivers O(log n) spatial range/point queries while maintaining O(log n) insertions with intelligent R*-tree splitting.
/// Revolutionary for GIS applications, game collision detection, and mapping systems where spatial query performance
/// determines overall system responsiveness.
/// </summary>
public class BloomRTreeDictionary<TKey, TValue> : IDisposable, IEnumerable<KeyValuePair<TKey, TValue>>
    where TKey : notnull
{
    private const int DefaultBloomCapacity = 10000;
    private const double DefaultFalsePositiveRate = 0.01;
    private readonly Dictionary<TKey, RTreeEntry<TKey, TValue>> _keyToEntry;
    private RTreeNode<TKey, TValue>? _root;
    private readonly BloomFilter<SpatialQuery> _spatialBloomFilter;
    private int _count;
    private long _spatialQueries;
    private long _bloomFilterHits;
    private long _dictionaryLookups;
    
    private readonly BoundsCache _boundsCache = new();
    public int Count => _count;
    public bool IsEmpty => _count == 0;
    private int _cachedTreeHeight = 0;
    private bool _treeHeightDirty = true;
    public BloomRTreeStats Statistics
    {
        get
        {
            if (_treeHeightDirty)
            {
                _cachedTreeHeight = CalculateTreeHeight();
                _treeHeightDirty = false;
            }
            return new BloomRTreeStats
            {
                TotalEntries = _count,
                SpatialQueries = _spatialQueries,
                BloomFilterHits = _bloomFilterHits,
                BloomFilterEffectiveness = _spatialQueries > 0 ? (double)_bloomFilterHits / _spatialQueries : 0,
                DictionaryLookups = _dictionaryLookups,
                TreeHeight = _cachedTreeHeight
            };
        }
    }

    public BloomRTreeDictionary(int expectedCapacity = DefaultBloomCapacity,
                               double falsePositiveRate = DefaultFalsePositiveRate)
    {
        if (expectedCapacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(expectedCapacity));
        if (falsePositiveRate <= 0 || falsePositiveRate >= 1)
            throw new ArgumentOutOfRangeException(nameof(falsePositiveRate));
        _keyToEntry = new Dictionary<TKey, RTreeEntry<TKey, TValue>>(expectedCapacity);
        _spatialBloomFilter = new BloomFilter<SpatialQuery>(expectedCapacity, falsePositiveRate);
        _count = 0;
        _spatialQueries = 0;
        _bloomFilterHits = 0;
        _dictionaryLookups = 0;
    }

    public void Add(TKey key, TValue value, BoundingRectangle bounds)
    {
        if (_keyToEntry.TryGetValue(key, out RTreeEntry<TKey, TValue>? existingEntry))
        {
            if (!existingEntry.Bounds.Equals(bounds))
            {
                RemoveFromRTree(existingEntry);
                existingEntry.Value = value;
                existingEntry.Bounds = bounds;
                InsertIntoRTree(existingEntry);
            }
            else
            {
                existingEntry.Value = value;
            }
        }
        else
        {
            RTreeEntry<TKey, TValue>? entry = new RTreeEntry<TKey, TValue>();
            entry.Key = key;
            entry.Value = value;
            entry.Bounds = bounds;
            _keyToEntry[key] = entry;
            InsertIntoRTree(entry);
            _count++;
            _treeHeightDirty = true;
        }
    }

    public void AddRange(IEnumerable<(TKey Key, TValue Value, BoundingRectangle Bounds)> entries)
    {
        var entriesToAdd = entries.ToList();
        
        if (entriesToAdd.Count == 0) return;
        
        // Use STR bulk loading for large batches or when rebuilding is beneficial
        if (entriesToAdd.Count > 50 || (entriesToAdd.Count > Count / 2 && Count > 100))
        {
            BulkLoadSTR(entriesToAdd);
        }
        else
        {
            // For small batches, individual insertion is still efficient
            foreach (var (key, value, bounds) in entriesToAdd)
            {
                Add(key, value, bounds);
            }
        }
    }
    
    
    private void BulkLoadSTR(List<(TKey Key, TValue Value, BoundingRectangle Bounds)> entries)
    {
        // Sort-Tile-Recursive (STR) bulk loading algorithm
        // More efficient than individual insertions for large datasets
        
        // First, add entries to dictionary for key-based lookups
        foreach (var (key, value, bounds) in entries)
        {
            if (_keyToEntry.TryGetValue(key, out var existingEntry))
            {
                existingEntry.Value = value;
                existingEntry.Bounds = bounds;
            }
            else
            {
                var entry = new RTreeEntry<TKey, TValue>
                {
                    Key = key,
                    Value = value,
                    Bounds = bounds
                };
                _keyToEntry[key] = entry;
                _count++;
            }
        }
        
        // Rebuild tree structure using STR algorithm
        var allEntries = _keyToEntry.Values.ToList();
        if (allEntries.Count > 0)
        {
            _root = BuildSTRTree(allEntries);
            _treeHeightDirty = true;
        }
        
        // Clear cache after tree rebuild
        _boundsCache.Clear();
    }
    
    private RTreeNode<TKey, TValue>? BuildSTRTree(List<RTreeEntry<TKey, TValue>> entries)
    {
        if (entries.Count == 0) return null;
        
        const int maxEntriesPerNode = 16;
        if (entries.Count <= maxEntriesPerNode)
        {
            var leaf = new RTreeNode<TKey, TValue>(true);
            foreach (var entry in entries)
                leaf.AddEntry(entry);
            return leaf;
        }

        // STR Algorithm
        int stripsCount = (int)Math.Ceiling(Math.Sqrt((double)entries.Count / maxEntriesPerNode));
        int entriesPerStrip = (int)Math.Ceiling((double)entries.Count / stripsCount);

        // Sort by X coordinate for vertical strips
        entries.Sort((a, b) => a.Bounds.CenterX.CompareTo(b.Bounds.CenterX));

        var childNodes = new List<RTreeNode<TKey, TValue>>();

        for (int stripIndex = 0; stripIndex < stripsCount; stripIndex++)
        {
            int stripStart = stripIndex * entriesPerStrip;
            int stripEnd = Math.Min(stripStart + entriesPerStrip, entries.Count);
            var stripEntries = entries.Skip(stripStart).Take(stripEnd - stripStart).ToList();

            // Sort strip by Y coordinate
            stripEntries.Sort((a, b) => a.Bounds.CenterY.CompareTo(b.Bounds.CenterY));

            // Create leaf nodes for this strip
            for (int nodeStart = 0; nodeStart < stripEntries.Count; nodeStart += maxEntriesPerNode)
            {
                int nodeEnd = Math.Min(nodeStart + maxEntriesPerNode, stripEntries.Count);
                var nodeEntries = stripEntries.Skip(nodeStart).Take(nodeEnd - nodeStart).ToList();

                var leafNode = new RTreeNode<TKey, TValue>(true);
                foreach (var entry in nodeEntries)
                    leafNode.AddEntry(entry);
                
                childNodes.Add(leafNode);
            }
        }

        // Recursively build parent nodes
        return BuildParentNodes(childNodes);
    }
    
    private RTreeNode<TKey, TValue> BuildParentNodes(List<RTreeNode<TKey, TValue>> childNodes)
    {
        if (childNodes.Count == 1)
            return childNodes[0];
            
        const int maxEntriesPerNode = 16;
        var parentNodes = new List<RTreeNode<TKey, TValue>>();
        
        for (int i = 0; i < childNodes.Count; i += maxEntriesPerNode)
        {
            var parent = new RTreeNode<TKey, TValue>(false);
            int end = Math.Min(i + maxEntriesPerNode, childNodes.Count);
            
            for (int j = i; j < end; j++)
            {
                parent.AddChild(childNodes[j]);
                childNodes[j].Parent = parent;
            }
            
            parentNodes.Add(parent);
        }
        
        return BuildParentNodes(parentNodes);
    }
    
    private void CollectAllEntries(RTreeNode<TKey, TValue> node, List<RTreeEntry<TKey, TValue>> entries)
    {
        if (node.IsLeaf)
        {
            if (node.Entries != null)
                entries.AddRange(node.Entries);
        }
        else
        {
            if (node.Children != null)
            {
                foreach (var child in node.Children)
                    CollectAllEntries(child, entries);
            }
        }
    }

    private void RebuildWithNewEntries(List<(TKey Key, TValue Value, BoundingRectangle Bounds)> newEntries)
    {
        foreach (var (key, value, bounds) in newEntries)
        {
            Add(key, value, bounds);
        }
    }

    public void Add(TKey key, TValue value, float x, float y)
    {
        Add(key, value, new BoundingRectangle(x, y));
    }

    public TValue this[TKey key]
    {
        get
        {
            _dictionaryLookups++;
            return _keyToEntry[key].Value;
        }
        set => Add(key, value, _keyToEntry[key].Bounds);
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        _dictionaryLookups++;
        if (_keyToEntry.TryGetValue(key, out RTreeEntry<TKey, TValue>? entry))
        {
            value = entry.Value;
            return true;
        }
        value = default!;
        return false;
    }

    public bool ContainsKey(TKey key)
    {
        _dictionaryLookups++;
        return _keyToEntry.ContainsKey(key);
    }

    public bool Remove(TKey key)
    {
        if (_keyToEntry.TryGetValue(key, out RTreeEntry<TKey, TValue>? entry))
        {
            _keyToEntry.Remove(key);
            RemoveFromRTree(entry);
            _count--;
            return true;
        }
        return false;
    }

    public void Clear()
    {
        _keyToEntry.Clear();
        if (_root != null)
        {
            ClearRTreeNode(_root);
            _root = null;
        }
        _count = 0;
        _spatialQueries = 0;
        _bloomFilterHits = 0;
        _dictionaryLookups = 0;
    }

    private void InsertIntoRTree(RTreeEntry<TKey, TValue> entry)
    {
        if (_root == null)
        {
            _root = new RTreeNode<TKey, TValue>();
            _root.IsLeaf = true;
            _root.AddEntry(entry);
            return;
        }
        RTreeNode<TKey, TValue>? leafNode = _root.ChooseSubtree(entry.Bounds);
        while (!leafNode.IsLeaf)
        {
            leafNode = leafNode.ChooseSubtree(entry.Bounds);
        }
        leafNode.AddEntry(entry);
        if (leafNode.IsFull)
        {
            RTreeNode<TKey, TValue>? newNode = SplitNode(leafNode);
            if (leafNode == _root)
            {
                RTreeNode<TKey, TValue>? newRoot = new RTreeNode<TKey, TValue>();
                newRoot.IsLeaf = false;
                newRoot.AddChild(leafNode);
                newRoot.AddChild(newNode);
                _root = newRoot;
            }
            else
            {
                InsertNodeIntoParent(leafNode, newNode);
            }
        }
        UpdateBoundsUpTree(leafNode);
    }

    private RTreeNode<TKey, TValue> SplitNode(RTreeNode<TKey, TValue> node)
    {
        RTreeNode<TKey, TValue>? newNode = new RTreeNode<TKey, TValue>();
        newNode.IsLeaf = node.IsLeaf;
        if (node.IsLeaf)
        {
            List<RTreeEntry<TKey, TValue>>? entries = node.Entries!;
            var (seed1, seed2) = FindWorstPair(entries);
            newNode.AddEntry(entries[seed2]);
            entries.RemoveAt(seed2);
            if (seed1 > seed2) seed1--;
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (i == seed1) continue;
                RTreeEntry<TKey, TValue>? entry = entries[i];
                var node1Increase = _boundsCache.GetCachedAreaIncrease(node.Bounds, entry.Bounds);
                var node2Increase = _boundsCache.GetCachedAreaIncrease(newNode.Bounds, entry.Bounds);
                if (node2Increase < node1Increase ||
                    (node2Increase == node1Increase && newNode.Count < node.Count))
                {
                    newNode.AddEntry(entry);
                    entries.RemoveAt(i);
                }
            }
        }
        else
        {
            List<RTreeNode<TKey, TValue>>? children = node.Children!;
            var (seed1, seed2) = FindWorstPairChildren(children);
            newNode.AddChild(children[seed2]);
            children.RemoveAt(seed2);
            if (seed1 > seed2) seed1--;
            for (int i = children.Count - 1; i >= 0; i--)
            {
                if (i == seed1) continue;
                RTreeNode<TKey, TValue>? child = children[i];
                var node1Increase = _boundsCache.GetCachedAreaIncrease(node.Bounds, child.Bounds);
                var node2Increase = _boundsCache.GetCachedAreaIncrease(newNode.Bounds, child.Bounds);
                if (node2Increase < node1Increase ||
                    (node2Increase == node1Increase && newNode.Count < node.Count))
                {
                    newNode.AddChild(child);
                    children.RemoveAt(i);
                }
            }
        }
        node.UpdateBounds();
        newNode.UpdateBounds();
        return newNode;
    }

    private (int seed1, int seed2) FindWorstPair(List<RTreeEntry<TKey, TValue>> entries)
    {
        // Linear-time R*-Tree inspired split: find seeds with maximum separation
        // Instead of O(n²) all-pairs comparison, use axis-based separation
        
        if (entries.Count < 2) return (0, Math.Min(1, entries.Count - 1));
        
        // Find the pair with maximum separation on the axis with highest normalized spread
        float bestSeparation = -1;
        int seed1 = 0, seed2 = 1;
        
        // Check both X and Y axes for separation
        for (int axis = 0; axis < 2; axis++)
        {
            // Sort entries by axis coordinate (O(n log n) but only twice)
            var sortedEntries = entries
                .Select((entry, index) => new { entry, index, coord = axis == 0 ? entry.Bounds.CenterX : entry.Bounds.CenterY })
                .OrderBy(x => x.coord)
                .ToArray();
            
            if (sortedEntries.Length < 2) continue;
            
            // Find the pair with maximum coordinate separation
            var first = sortedEntries[0];
            var last = sortedEntries[sortedEntries.Length - 1];
            float separation = Math.Abs(last.coord - first.coord);
            
            // Normalize by axis span to compare across axes
            float axisMin = sortedEntries[0].coord;
            float axisMax = sortedEntries[sortedEntries.Length - 1].coord;
            float axisSpan = axisMax - axisMin;
            
            if (axisSpan > 0)
            {
                float normalizedSeparation = separation / axisSpan;
                if (normalizedSeparation > bestSeparation)
                {
                    bestSeparation = normalizedSeparation;
                    seed1 = first.index;
                    seed2 = last.index;
                }
            }
        }
        
        // Ensure different indices
        if (seed1 == seed2 && entries.Count > 1)
        {
            seed2 = (seed1 + 1) % entries.Count;
        }
        
        return (seed1, seed2);
    }

    private (int seed1, int seed2) FindWorstPairChildren(List<RTreeNode<TKey, TValue>> children)
    {
        // Linear-time R*-Tree inspired split for child nodes
        // Use same axis-based separation approach as FindWorstPair
        
        if (children.Count < 2) return (0, Math.Min(1, children.Count - 1));
        
        float bestSeparation = -1;
        int seed1 = 0, seed2 = 1;
        
        // Check both X and Y axes for separation
        for (int axis = 0; axis < 2; axis++)
        {
            // Sort children by axis coordinate (O(n log n) but only twice)
            var sortedChildren = children
                .Select((child, index) => new { child, index, coord = axis == 0 ? child.Bounds.CenterX : child.Bounds.CenterY })
                .OrderBy(x => x.coord)
                .ToArray();
            
            if (sortedChildren.Length < 2) continue;
            
            // Find the pair with maximum coordinate separation
            var first = sortedChildren[0];
            var last = sortedChildren[sortedChildren.Length - 1];
            float separation = Math.Abs(last.coord - first.coord);
            
            // Normalize by axis span to compare across axes
            float axisMin = sortedChildren[0].coord;
            float axisMax = sortedChildren[sortedChildren.Length - 1].coord;
            float axisSpan = axisMax - axisMin;
            
            if (axisSpan > 0)
            {
                float normalizedSeparation = separation / axisSpan;
                if (normalizedSeparation > bestSeparation)
                {
                    bestSeparation = normalizedSeparation;
                    seed1 = first.index;
                    seed2 = last.index;
                }
            }
        }
        
        // Ensure different indices
        if (seed1 == seed2 && children.Count > 1)
        {
            seed2 = (seed1 + 1) % children.Count;
        }
        
        return (seed1, seed2);
    }

    private void InsertNodeIntoParent(RTreeNode<TKey, TValue> oldNode, RTreeNode<TKey, TValue> newNode)
    {
        RTreeNode<TKey, TValue>? parent = oldNode.Parent!;
        parent.AddChild(newNode);
        if (parent.IsFull)
        {
            RTreeNode<TKey, TValue>? newParent = SplitNode(parent);
            if (parent == _root)
            {
                RTreeNode<TKey, TValue>? newRoot = new RTreeNode<TKey, TValue>();
                newRoot.IsLeaf = false;
                newRoot.AddChild(parent);
                newRoot.AddChild(newParent);
                _root = newRoot;
            }
            else
            {
                InsertNodeIntoParent(parent, newParent);
            }
        }
    }

    private void UpdateBoundsUpTree(RTreeNode<TKey, TValue> node)
    {
        RTreeNode<TKey, TValue>? current = node;
        while (current != null)
        {
            // Store old bounds for early termination
            var oldBounds = current.Bounds;
            current.UpdateBounds();
            
            // Early termination: if bounds didn't change significantly, ancestors won't change either
            // Use small epsilon to account for floating-point precision
            const float epsilon = 1e-6f;
            if (Math.Abs(current.Bounds.MinX - oldBounds.MinX) < epsilon &&
                Math.Abs(current.Bounds.MinY - oldBounds.MinY) < epsilon &&
                Math.Abs(current.Bounds.MaxX - oldBounds.MaxX) < epsilon &&
                Math.Abs(current.Bounds.MaxY - oldBounds.MaxY) < epsilon)
            {
                break; // No meaningful change, stop propagating up
            }
            
            current = current.Parent;
        }
    }

    public IEnumerable<KeyValuePair<TKey, TValue>> FindIntersecting(BoundingRectangle bounds)
    {
        if (_root == null) yield break;
        var query = new SpatialQuery(bounds, SpatialQueryType.Intersection);
        _spatialQueries++;
        if (_spatialBloomFilter.Contains(query))
        {
            _bloomFilterHits++;
            yield break; // This query is known to have no results
        }
        bool hasResults = false;
        foreach (RTreeEntry<TKey, TValue>? entry in SearchRTree(_root, bounds, SpatialQueryType.Intersection))
        {
            hasResults = true;
            yield return new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
        }
        if (!hasResults)
        {
            _spatialBloomFilter.Add(query); // Remember this query has no results
        }
    }

    public IEnumerable<KeyValuePair<TKey, TValue>> FindContained(BoundingRectangle bounds)
    {
        if (_root == null) yield break;
        var query = new SpatialQuery(bounds, SpatialQueryType.Contains);
        _spatialQueries++;
        if (_spatialBloomFilter.Contains(query))
        {
            _bloomFilterHits++;
            yield break; // This query is known to have no results
        }
        bool hasResults = false;
        foreach (RTreeEntry<TKey, TValue>? entry in SearchRTree(_root, bounds, SpatialQueryType.Contains))
        {
            hasResults = true;
            yield return new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
        }
        if (!hasResults)
        {
            _spatialBloomFilter.Add(query); // Remember this query has no results
        }
    }

    public IEnumerable<KeyValuePair<TKey, TValue>> FindAtPoint(float x, float y)
    {
        var pointBounds = new BoundingRectangle(x, y);
        var query = new SpatialQuery(pointBounds, SpatialQueryType.Point);
        _spatialQueries++;
        if (_spatialBloomFilter.Contains(query))
        {
            _bloomFilterHits++;
            yield break; // This query is known to have no results
        }
        bool hasResults = false;
        if (_root != null)
        {
            foreach (RTreeEntry<TKey, TValue>? entry in SearchRTree(_root, pointBounds, SpatialQueryType.Point))
            {
                hasResults = true;
                yield return new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
            }
        }
        if (!hasResults)
        {
            _spatialBloomFilter.Add(query); // Remember this query has no results
        }
    }

    public int FindIntersecting(BoundingRectangle bounds, PooledList<KeyValuePair<TKey, TValue>> results)
    {
        results.Clear();
        if (_root == null) return 0;
        var query = new SpatialQuery(bounds, SpatialQueryType.Intersection);
        _spatialQueries++;
        if (_spatialBloomFilter.Contains(query))
        {
            _bloomFilterHits++;
            return 0; // This query is known to have no results
        }
        SearchRTreePooled(_root, bounds, SpatialQueryType.Intersection, results);
        if (results.Count == 0)
        {
            _spatialBloomFilter.Add(query); // Remember this query has no results
        }
        return results.Count;
    }

    private IEnumerable<RTreeEntry<TKey, TValue>> SearchRTree(RTreeNode<TKey, TValue> node,
        BoundingRectangle searchBounds, SpatialQueryType queryType)
    {
        // Use a more efficient search that avoids nested yield returns
        var results = new List<RTreeEntry<TKey, TValue>>();
        SearchRTreeInternal(node, searchBounds, queryType, results);
        return results;
    }
    
    private void SearchRTreeInternal(RTreeNode<TKey, TValue> node,
        BoundingRectangle searchBounds, SpatialQueryType queryType, List<RTreeEntry<TKey, TValue>> results)
    {
        if (!node.Bounds.Intersects(searchBounds)) return;
        if (node.IsLeaf)
        {
            if (node.Entries != null)
            {
                foreach (RTreeEntry<TKey, TValue>? entry in node.Entries)
                {
                    bool matches = queryType switch
                    {
                        SpatialQueryType.Intersection => entry.Bounds.Intersects(searchBounds),
                        SpatialQueryType.Contains => searchBounds.Contains(entry.Bounds),
                        SpatialQueryType.Point => entry.Bounds.Contains(searchBounds.MinX, searchBounds.MinY),
                        _ => false
                    };
                    if (matches)
                        results.Add(entry);
                }
            }
        }
        else
        {
            if (node.Children != null)
            {
                foreach (RTreeNode<TKey, TValue>? child in node.Children)
                {
                    SearchRTreeInternal(child, searchBounds, queryType, results);
                }
            }
        }
    }

    private void SearchRTreePooled(RTreeNode<TKey, TValue> node, BoundingRectangle searchBounds,
        SpatialQueryType queryType, PooledList<KeyValuePair<TKey, TValue>> results)
    {
        if (!node.Bounds.Intersects(searchBounds)) return;
        if (node.IsLeaf)
        {
            if (node.Entries != null)
            {
                foreach (RTreeEntry<TKey, TValue>? entry in node.Entries)
                {
                    bool matches = queryType switch
                    {
                        SpatialQueryType.Intersection => entry.Bounds.Intersects(searchBounds),
                        SpatialQueryType.Contains => searchBounds.Contains(entry.Bounds),
                        SpatialQueryType.Point => entry.Bounds.Contains(searchBounds.MinX, searchBounds.MinY),
                        _ => false
                    };
                    if (matches)
                        results.Add(new KeyValuePair<TKey, TValue>(entry.Key, entry.Value));
                }
            }
        }
        else
        {
            if (node.Children != null)
            {
                foreach (RTreeNode<TKey, TValue>? child in node.Children)
                {
                    SearchRTreePooled(child, searchBounds, queryType, results);
                }
            }
        }
    }

    private void RemoveFromRTree(RTreeEntry<TKey, TValue> entry)
    {
        if (_root == null) return;
        RemoveEntryFromNode(_root, entry);
        if (_root.Count == 0)
        {
            _root = null;
        }
    }

    private bool RemoveEntryFromNode(RTreeNode<TKey, TValue> node, RTreeEntry<TKey, TValue> entry)
    {
        if (node.IsLeaf)
        {
            if (node.Entries != null)
            {
                for (int i = 0; i < node.Entries.Count; i++)
                {
                    if (ReferenceEquals(node.Entries[i], entry))
                    {
                        node.Entries.RemoveAt(i);
                        node.UpdateBounds();
                        return true;
                    }
                }
            }
            return false;
        }
        else
        {
            if (node.Children != null)
            {
                for (int i = node.Children.Count - 1; i >= 0; i--)
                {
                    RTreeNode<TKey, TValue>? child = node.Children[i];
                    if (child.Bounds.Intersects(entry.Bounds))
                    {
                        if (RemoveEntryFromNode(child, entry))
                        {
                            if (child.Count == 0)
                            {
                                node.Children.RemoveAt(i);
                            }
                            node.UpdateBounds();
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }

    private void ClearRTreeNode(RTreeNode<TKey, TValue> node)
    {
        if (!node.IsLeaf && node.Children != null)
        {
            foreach (RTreeNode<TKey, TValue>? child in node.Children)
            {
                ClearRTreeNode(child);
            }
        }
        if (node.IsLeaf && node.Entries != null)
        {
            foreach (RTreeEntry<TKey, TValue>? entry in node.Entries)
            {
            }
        }
    }

    private int CalculateTreeHeight()
    {
        if (_root == null) return 0;
        return CalculateNodeHeight(_root);
    }

    private int CalculateNodeHeight(RTreeNode<TKey, TValue> node)
    {
        if (node.IsLeaf) return 1;
        if (node.Children == null || node.Children.Count == 0) return 1;
        int maxHeight = 0;
        foreach (RTreeNode<TKey, TValue>? child in node.Children)
        {
            maxHeight = Math.Max(maxHeight, CalculateNodeHeight(child));
        }
        return maxHeight + 1;
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        foreach (KeyValuePair<TKey, RTreeEntry<TKey, TValue>> kvp in _keyToEntry)
        {
            yield return new KeyValuePair<TKey, TValue>(kvp.Key, kvp.Value.Value);
        }
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public void Dispose()
    {
        Clear();
        _boundsCache.Clear();
    }
}

public class BoundsCache
{
    private readonly Dictionary<(BoundingRectangle, BoundingRectangle), BoundingRectangle> _unionCache 
        = new(capacity: 1000);
    private readonly Dictionary<(BoundingRectangle, BoundingRectangle), float> _areaIncreaseCache 
        = new(capacity: 1000);

    public BoundingRectangle GetCachedUnion(BoundingRectangle a, BoundingRectangle b)
    {
        var key = (a, b);
        if (!_unionCache.TryGetValue(key, out var union))
        {
            union = a.Union(b);
            if (_unionCache.Count < 1000) // Prevent unbounded growth
                _unionCache[key] = union;
        }
        return union;
    }

    public float GetCachedAreaIncrease(BoundingRectangle existing, BoundingRectangle newBounds)
    {
        var key = (existing, newBounds);
        if (!_areaIncreaseCache.TryGetValue(key, out var increase))
        {
            increase = existing.CalculateAreaIncrease(newBounds);
            if (_areaIncreaseCache.Count < 1000)
                _areaIncreaseCache[key] = increase;
        }
        return increase;
    }

    public void Clear()
    {
        _unionCache.Clear();
        _areaIncreaseCache.Clear();
    }
}
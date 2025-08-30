using System;
using System.Collections.Generic;

namespace Omni.Collections.Spatial.BloomRTreeDictionary;

sealed class RTreeNode<TKey, TValue>
    where TKey : notnull
{
    private const int MaxEntries = 16;
    private const int MinEntries = MaxEntries / 2;
    public BoundingRectangle Bounds { get; set; }

    public bool IsLeaf { get; set; }

    public List<RTreeEntry<TKey, TValue>>? Entries { get; set; }

    public List<RTreeNode<TKey, TValue>>? Children { get; set; }

    public RTreeNode<TKey, TValue>? Parent { get; set; }

    public int Count => IsLeaf ? (Entries?.Count ?? 0) : (Children?.Count ?? 0);
    public bool IsFull => Count >= MaxEntries;
    public bool IsUnderflow => Count < MinEntries;
    public RTreeNode() { }

    public RTreeNode(bool isLeaf)
    {
        IsLeaf = isLeaf;
        if (isLeaf)
        {
            Entries = new List<RTreeEntry<TKey, TValue>>(MaxEntries);
        }
        else
        {
            Children = new List<RTreeNode<TKey, TValue>>(MaxEntries);
        }
    }

    public void AddEntry(RTreeEntry<TKey, TValue> entry)
    {
        if (!IsLeaf) throw new InvalidOperationException("Cannot add entry to non-leaf node");
        Entries ??= new List<RTreeEntry<TKey, TValue>>(MaxEntries);
        Entries.Add(entry);
        if (Entries.Count == 1)
        {
            Bounds = entry.Bounds;
        }
        else
        {
            Bounds = Bounds.Union(entry.Bounds);
        }
    }

    public void AddChild(RTreeNode<TKey, TValue> child)
    {
        if (IsLeaf) throw new InvalidOperationException("Cannot add child to leaf node");
        Children ??= new List<RTreeNode<TKey, TValue>>(MaxEntries);
        Children.Add(child);
        child.Parent = this;
        if (Children.Count == 1)
        {
            Bounds = child.Bounds;
        }
        else
        {
            Bounds = Bounds.Union(child.Bounds);
        }
    }

    public RTreeNode<TKey, TValue> ChooseSubtree(in BoundingRectangle bounds)
    {
        if (IsLeaf) return this;
        RTreeNode<TKey, TValue>? bestChild = Children![0];
        var bestIncrease = bestChild.Bounds.CalculateAreaIncrease(bounds);
        for (int i = 1; i < Children.Count; i++)
        {
            RTreeNode<TKey, TValue>? child = Children[i];
            var increase = child.Bounds.CalculateAreaIncrease(bounds);
            if (increase < bestIncrease ||
                (increase == bestIncrease && child.Bounds.Area < bestChild.Bounds.Area))
            {
                bestChild = child;
                bestIncrease = increase;
            }
        }
        return bestChild;
    }

    public void UpdateBounds()
    {
        if (IsLeaf && Entries?.Count > 0)
        {
            Bounds = Entries[0].Bounds;
            for (int i = 1; i < Entries.Count; i++)
            {
                Bounds = Bounds.Union(Entries[i].Bounds);
            }
        }
        else if (!IsLeaf && Children?.Count > 0)
        {
            Bounds = Children[0].Bounds;
            for (int i = 1; i < Children.Count; i++)
            {
                Bounds = Bounds.Union(Children[i].Bounds);
            }
        }
    }

    public override string ToString() => $"Node({(IsLeaf ? "Leaf" : "Internal")}, {Count} items): {Bounds}";
}
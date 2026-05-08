using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Omni.Collections.Probabilistic;

/// <summary>
/// A t-digest variant for approximate quantile estimation over streaming data with bounded memory.
/// Centroids are kept in a width-augmented (indexable) skip list keyed by Mean, so Add, Quantile, Cdf
/// and Percentile are all O(log c) per call where c is the centroid count (bounded by compression × 2).
/// Each node carries Forward and per-level cumulative-weight Width arrays — Quantile/Cdf descend from
/// the top level, accumulating weight along covered spans, instead of scanning every centroid linearly.
/// Merge is O(c1 + c2) — linear merge of the two sorted centroid lists followed by a Compress pass.
/// Memory is bounded by the configured compression parameter; per-node overhead is ~2 pointers on
/// average (skip list with p=1/2 level distribution).
/// Suited to scenarios where exact sorting is too memory-heavy and per-op latency matters at high
/// compression — SLA monitoring, latency dashboards, distributed shards merged at query time.
/// </summary>
public sealed class Digest
{
    // Skip list parameters. MaxLevel=16 supports up to ~65k centroids — well past
    // _compression*2 for any compression in the [20, 1000] valid range.
    private const int MaxLevel = 16;

    private readonly double _compression;
    private readonly Node _head;
    private readonly Random _rng;
    private double _count;
    private double _min = double.PositiveInfinity;
    private double _max = double.NegativeInfinity;
    private int _centroidCount;
    private int _level; // current highest level in use (1..MaxLevel)

    // Reusable update buffers for Add — sized to MaxLevel, allocated once per digest.
    private readonly Node?[] _updateNodes;
    private readonly double[] _updateRanks;

    public Digest(double compression = 100.0)
    {
        if (compression < 20 || compression > 1000)
            throw new ArgumentOutOfRangeException(nameof(compression), "Compression must be between 20 and 1000");
        _compression = compression;
        _head = new Node(default, MaxLevel);
        _rng = new Random();
        _level = 1;
        _updateNodes = new Node?[MaxLevel];
        _updateRanks = new double[MaxLevel];
    }

    public double Count => _count;
    public double Min => _count == 0 ? double.NaN : _min;
    public double Max => _count == 0 ? double.NaN : _max;
    public int ClusterCount => _centroidCount;

    // Per-node overhead: object header (~24B) + Centroid (16B) + 2 × array refs (16B) +
    // (avg ~2 forward + 2 width) × 8B ≈ 88B per node. Round up to 96B to stay honest.
    public long EstimatedMemoryUsage => _centroidCount * 96L + 128L;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(double value)
    {
        Add(value, 1.0);
    }

    public void Add(double value, double weight = 1.0)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            throw new ArgumentException("Value cannot be NaN or Infinity", nameof(value));
        if (weight <= 0 || double.IsNaN(weight) || double.IsInfinity(weight))
            throw new ArgumentException("Weight must be positive and finite", nameof(weight));
        UpdateMinMax(value);
        _count += weight;

        // Walk the skip list from top to bottom, recording the rightmost node at each
        // level we descend from. While walking, accumulate the prefix weight (rank)
        // up to the current position so we know where in the cumulative-weight order
        // the insertion lands — needed for the CanMerge scale-function check.
        var update = _updateNodes;
        var ranks = _updateRanks;
        Node x = _head;
        double rank = 0;
        for (int i = _level - 1; i >= 0; i--)
        {
            Node? next = x.Forward[i];
            while (next != null && next.Centroid.Mean < value)
            {
                rank += x.Width[i];
                x = next;
                next = x.Forward[i];
            }
            update[i] = x;
            ranks[i] = rank;
        }

        // Merge into the existing centroid at this position if its mean matches and
        // its weight budget under the scale function still has room.
        Node? candidate = x.Forward[0];
        if (candidate != null && Math.Abs(candidate.Centroid.Mean - value) <= 1e-10)
        {
            // rank at this point = cumulative weight strictly before `candidate`.
            double q = rank / _count;
            double maxWeight = _count * ScaleFunction(q, q);
            if (candidate.Centroid.Weight + weight <= maxWeight)
            {
                candidate.Centroid = candidate.Centroid.AddWeight(value, weight);
                // The candidate's weight increased by `weight`. Every level-i forward
                // pointer that traverses this node (i.e., update[i].Forward[i] reaches
                // candidate or beyond) now covers `weight` more. At levels < candidate's
                // own height the pointer ends at candidate or lands within its range; at
                // levels >= candidate's height the parent pointer skips over candidate.
                // In both cases we add `weight` to that parent pointer's Width.
                for (int i = 0; i < _level; i++)
                {
                    update[i]!.Width[i] += weight;
                }
                return;
            }
        }

        // No merge — splice in a brand new node.
        int newLevel = RandomLevel();
        if (newLevel > _level)
        {
            for (int i = _level; i < newLevel; i++)
            {
                update[i] = _head;
                ranks[i] = 0;
            }
            _level = newLevel;
        }

        var node = new Node(new Centroid(value, weight), newLevel);

        // For each level i in [0, newLevel-1]:
        //  - The new node's Forward[i] takes update[i]'s previous next pointer.
        //  - update[i]'s Forward[i] is rewired to the new node.
        //  - Widths split: previously update[i] covered all weight from rank[i] to
        //    rank[i] + update[i].Width[i]. Now that span splits at the new node's
        //    rank position (= prefix rank just before the insertion). update[i]'s
        //    Width[i] becomes (newNodeRank - ranks[i]) + weight (covering up to and
        //    including the new node), and the new node's Width[i] becomes the
        //    remainder of the original span.
        // For levels i in [newLevel, _level-1]: the new node is invisible at those
        // levels but its weight still flows under their parent links, so add `weight`.
        double newNodeRank = ranks[0]; // rank strictly before the insertion point
        for (int i = 0; i < newLevel; i++)
        {
            Node parent = update[i]!;
            node.Forward[i] = parent.Forward[i];
            parent.Forward[i] = node;

            // Width semantics: at node v, Width[i] = cumWeight(Forward[i]) - cumWeight(v),
            // where cumWeight is computed AFTER the insert. cumWeight of every node
            // at-or-after newNode shifts up by `weight`. So:
            //   parent.Width[i]_after  = cumWeight(newNode) - cumWeight(parent_i)
            //                          = (ranks[0] + weight) - ranks[i]
            //                          = headPart + weight
            //   newNode.Width[i]_after = cumWeight(oldNext_i)_after - cumWeight(newNode)
            //                          = (cumWeight(oldNext_i)_before + weight) - (ranks[0] + weight)
            //                          = cumWeight(oldNext_i)_before - ranks[0]
            //                          = parent.Width[i]_before - headPart
            //                          = originalParentWidth - headPart
            // If parent.Forward[i] was null (end of list at this level), originalParentWidth
            // is 0 and node.Forward[i] becomes null too — newNode.Width[i] is then 0.
            double originalParentWidth = parent.Width[i];
            double headPart = newNodeRank - ranks[i];
            parent.Width[i] = headPart + weight;
            node.Width[i] = node.Forward[i] == null ? 0 : (originalParentWidth - headPart);
        }
        for (int i = newLevel; i < _level; i++)
        {
            // Levels above newNode's height: parent's pointer still skips over newNode,
            // so its covered weight grows by `weight`.
            update[i]!.Width[i] += weight;
        }

        _centroidCount++;

        if (_centroidCount > _compression * 2)
        {
            Compress();
        }
    }

    public void AddRange(IEnumerable<double> values)
    {
        foreach (double value in values)
        {
            Add(value);
        }
    }

    public double Quantile(double q)
    {
        if (q < 0.0 || q > 1.0)
            throw new ArgumentOutOfRangeException(nameof(q), "Quantile must be between 0.0 and 1.0");
        if (_count == 0)
            return double.NaN;
        if (q == 0.0) return _min;
        if (q == 1.0) return _max;
        double targetRank = q * _count;

        // Descend levels accumulating cumulative weight until we land on the centroid
        // whose [cumulative-weight-before, cumulative-weight-after] bracket contains targetRank.
        Node x = _head;
        double cumulative = 0;
        for (int i = _level - 1; i >= 0; i--)
        {
            Node? next = x.Forward[i];
            while (next != null && cumulative + x.Width[i] < targetRank)
            {
                cumulative += x.Width[i];
                x = next;
                next = x.Forward[i];
            }
        }

        // After descent, x.Forward[0] is the candidate centroid bracket. cumulative is
        // the total weight strictly before x.Forward[0]'s span starts (== weight before
        // the candidate centroid). The candidate centroid covers [cumulative, cumulative + cw].
        Node? cand = x.Forward[0];
        if (cand == null)
            return _max;

        var centroid = cand.Centroid;
        double nextRank = cumulative + centroid.Weight;

        // Edge cases mirroring the original list-based implementation: if targetRank
        // lands exactly on a centroid boundary, average with the neighbor on that side.
        if (targetRank == cumulative && x != _head)
        {
            // targetRank == start of this centroid's bracket → average with predecessor (x).
            return (x.Centroid.Mean + centroid.Mean) * 0.5;
        }
        if (targetRank == nextRank && cand.Forward[0] != null)
        {
            // targetRank == end of this centroid's bracket → average with successor.
            return (centroid.Mean + cand.Forward[0]!.Centroid.Mean) * 0.5;
        }
        if (targetRank <= nextRank)
            return centroid.Mean;

        // Numerical edge: floating-point cumulative drift past the last centroid.
        return _max;
    }

    public double Percentile(double p) => Quantile(p / 100.0);

    public double Cdf(double x)
    {
        if (_count == 0)
            return double.NaN;
        if (x < _min) return 0.0;
        if (x >= _max) return 1.0;

        // Find the first centroid whose Mean >= x. Cumulative weight up to (but not
        // including) that centroid + half its weight, divided by total count, is the
        // CDF estimate (matches the original linear-scan formula).
        Node node = _head;
        double cumulative = 0;
        for (int i = _level - 1; i >= 0; i--)
        {
            Node? next = node.Forward[i];
            while (next != null && next.Centroid.Mean < x)
            {
                cumulative += node.Width[i];
                node = next;
                next = node.Forward[i];
            }
        }
        // node.Forward[0] is the first centroid with Mean >= x (or null if none).
        Node? cand = node.Forward[0];
        if (cand == null)
            return 1.0;
        double halfWeight = cand.Centroid.Weight * 0.5;
        return Math.Min(1.0, (cumulative + halfWeight) / _count);
    }

    public void Merge(Digest other)
    {
        if (other == null || other._count == 0)
            return;

        // Two-pointer linear merge over the level-0 spines of both digests.
        // Build the merged sequence into a flat array, then rebuild this digest from
        // the sorted array. Compress collapses adjacent centroids that fit under the
        // scale function.
        var merged = new List<Centroid>(_centroidCount + other._centroidCount);
        Node? a = _head.Forward[0];
        Node? b = other._head.Forward[0];
        while (a != null && b != null)
        {
            if (a.Centroid.Mean <= b.Centroid.Mean)
            {
                merged.Add(a.Centroid);
                a = a.Forward[0];
            }
            else
            {
                merged.Add(b.Centroid);
                b = b.Forward[0];
            }
        }
        while (a != null) { merged.Add(a.Centroid); a = a.Forward[0]; }
        while (b != null) { merged.Add(b.Centroid); b = b.Forward[0]; }

        _count += other._count;
        if (other._min < _min) _min = other._min;
        if (other._max > _max) _max = other._max;

        RebuildFromSorted(merged);

        if (_centroidCount > _compression)
            Compress();
    }

    public Digest Clone()
    {
        var result = new Digest(_compression);
        // Walk level 0 of source and replicate centroids in order.
        var orderedCentroids = new List<Centroid>(_centroidCount);
        for (Node? n = _head.Forward[0]; n != null; n = n.Forward[0])
        {
            orderedCentroids.Add(n.Centroid);
        }
        result._count = _count;
        result._min = _min;
        result._max = _max;
        result.RebuildFromSorted(orderedCentroids);
        return result;
    }

    public void Clear()
    {
        for (int i = 0; i < MaxLevel; i++)
        {
            _head.Forward[i] = null;
            _head.Width[i] = 0;
        }
        _level = 1;
        _centroidCount = 0;
        _count = 0;
        _min = double.PositiveInfinity;
        _max = double.NegativeInfinity;
    }

    public void Compress()
    {
        if (_centroidCount <= 1)
            return;

        // Walk level 0 in order; build a merged list under the scale function. Then
        // rebuild the skip list from the sorted/merged list.
        var newCentroids = new List<Centroid>(_centroidCount);
        double currentWeight = 0;
        for (Node? n = _head.Forward[0]; n != null; n = n.Forward[0])
        {
            var centroid = n.Centroid;
            double q0 = currentWeight / _count;
            double q1 = (currentWeight + centroid.Weight) / _count;
            double maxWeight = _count * ScaleFunction(q0, q1);
            if (newCentroids.Count > 0 &&
                newCentroids[^1].Weight + centroid.Weight <= maxWeight)
            {
                newCentroids[^1] = newCentroids[^1].AddWeight(centroid.Mean, centroid.Weight);
            }
            else
            {
                newCentroids.Add(centroid);
            }
            currentWeight += centroid.Weight;
        }
        RebuildFromSorted(newCentroids);
    }

    /// <summary>
    /// Reset the skip list and bulk-insert from a sorted list of centroids. Keeps
    /// _count/_min/_max as set by the caller; recomputes _centroidCount and _level.
    ///
    /// Single-pass left-to-right bulk-build: maintain an update[]/rank[] stack of
    /// the rightmost level-i predecessor and its cumulative weight. For each new
    /// centroid: pick a level via the same RandomLevel() distribution Add uses,
    /// splice the node in at its levels in O(level) by referencing the stack.
    /// Width values fall out as (newRank - ranks[i]) — no descent needed since the
    /// input is already sorted.
    ///
    /// Total work: sum of node levels across the build = O(c) on expectation
    /// (geometric-distribution levels sum to ~2c). Replaces the previous
    /// per-centroid Add-style splice which did a full O(log c) descent for each
    /// centroid → O(c log c).
    /// </summary>
    private void RebuildFromSorted(List<Centroid> sorted)
    {
        // Clear current structure.
        for (int i = 0; i < MaxLevel; i++)
        {
            _head.Forward[i] = null;
            _head.Width[i] = 0;
        }
        _level = 1;
        _centroidCount = 0;

        int n = sorted.Count;
        if (n == 0)
            return;

        // Initialize the rightmost-predecessor stacks. update[i] is the most-
        // recently-linked level-i node (initially _head); ranks[i] is its
        // cumulative weight (initially 0).
        var update = _updateNodes;
        var ranks = _updateRanks;
        for (int i = 0; i < MaxLevel; i++)
        {
            update[i] = _head;
            ranks[i] = 0;
        }

        int maxLevelUsed = 1;
        double cumWeight = 0;
        for (int k = 0; k < n; k++)
        {
            var centroid = sorted[k];
            int level = RandomLevel();
            if (level > maxLevelUsed)
                maxLevelUsed = level;

            cumWeight += centroid.Weight;
            // newRank = cumulative weight at the new node (sum of weights of all
            // centroids 0..k, inclusive). This matches the Width[i] semantic:
            // Width[i] of X = cumWeight(X.Forward[i]) - cumWeight(X). Each level-i
            // predecessor's Forward[i] now points at this new node, so its Width[i]
            // is (newRank - rank-at-that-predecessor).
            double newRank = cumWeight;

            var node = new Node(centroid, level);
            for (int i = 0; i < level; i++)
            {
                Node parent = update[i]!;
                parent.Forward[i] = node;
                parent.Width[i] = newRank - ranks[i];
                update[i] = node;
                ranks[i] = newRank;
            }
            // Levels above `level`: this node is invisible at those levels and the
            // current update[i] still points to whatever prior level-i predecessor
            // (or _head if none yet). Its Width[i] will be set when the next level-i
            // node arrives, OR will remain at the cleared 0 if no such node exists
            // (matching the convention that tail-of-level Width[i] = 0 when
            // Forward[i] = null). No work needed here.

            _centroidCount++;
        }

        _level = maxLevelUsed;
    }

    private double ScaleFunction(double q0, double q1)
    {
        double q = (q0 + q1) * 0.5;
        return 4.0 * _compression * q * (1.0 - q);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateMinMax(double value)
    {
        if (value < _min) _min = value;
        if (value > _max) _max = value;
    }

    /// <summary>
    /// Pugh's geometric level distribution with p = 1/2. Levels are 1..MaxLevel.
    /// </summary>
    private int RandomLevel()
    {
        int level = 1;
        // Draw 32 bits at once and count trailing zeros for the level. Cap at MaxLevel.
        int bits = _rng.Next();
        while ((bits & 1) == 1 && level < MaxLevel)
        {
            level++;
            bits >>= 1;
        }
        return level;
    }

    private struct Centroid
    {
        public double Mean;
        public double Weight;

        public Centroid(double mean, double weight)
        {
            Mean = mean;
            Weight = weight;
        }

        public Centroid AddWeight(double value, double weight)
        {
            double newWeight = Weight + weight;
            double newMean = (Mean * Weight + value * weight) / newWeight;
            return new Centroid(newMean, newWeight);
        }
    }

    private sealed class Node
    {
        public Centroid Centroid;
        public readonly Node?[] Forward;
        public readonly double[] Width;

        public Node(Centroid centroid, int level)
        {
            Centroid = centroid;
            Forward = new Node?[level];
            Width = new double[level];
        }
    }
}

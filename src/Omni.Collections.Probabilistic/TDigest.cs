using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Omni.Collections.Probabilistic;

/// <summary>
/// A t-digest implementation that provides accurate quantile estimation on streaming data with bounded memory usage.
/// Provides O(log n) Add/Quantile operations with adaptive compression that maintains high accuracy for extreme percentiles.
/// Excellent for streaming quantile estimation, percentile monitoring, performance analytics, or scenarios
/// where maintaining exact sorted data is memory-prohibitive but accurate percentiles are critical.
/// </summary>
public sealed class Digest
{
    private readonly double _compression;
    private readonly List<Centroid> _centroids;
    private double _count;
    private double _min = double.PositiveInfinity;
    private double _max = double.NegativeInfinity;
    private bool _needsSort;
    public Digest(double compression = 100.0)
    {
        if (compression < 20 || compression > 1000)
            throw new ArgumentOutOfRangeException(nameof(compression), "Compression must be between 20 and 1000");
        _compression = compression;
        _centroids = [];
    }

    public double Count => _count;
    public double Min => _count == 0 ? double.NaN : _min;
    public double Max => _count == 0 ? double.NaN : _max;
    public int ClusterCount => _centroids.Count;
    public long EstimatedMemoryUsage => ClusterCount * 24 + 64;
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
        if (_centroids.Count == 0)
        {
            _centroids.Add(new Centroid(value, weight));
            return;
        }
        int insertionIndex = FindInsertionPoint(value);
        if (CanMerge(insertionIndex, value, weight))
        {
            _centroids[insertionIndex] = _centroids[insertionIndex].AddWeight(value, weight);
        }
        else
        {
            _centroids.Insert(insertionIndex, new Centroid(value, weight));
            _needsSort = true;
        }
        if (_centroids.Count > _compression * 2)
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
        EnsureSorted();
        if (q == 0.0) return _min;
        if (q == 1.0) return _max;
        double targetRank = q * _count;
        double currentRank = 0;
        for (int i = 0; i < _centroids.Count; i++)
        {
            var centroid = _centroids[i];
            double nextRank = currentRank + centroid.Weight;
            if (targetRank <= nextRank)
            {
                if (targetRank == currentRank && i > 0)
                {
                    return (_centroids[i - 1].Mean + centroid.Mean) * 0.5;
                }
                if (targetRank == nextRank && i < _centroids.Count - 1)
                {
                    return (centroid.Mean + _centroids[i + 1].Mean) * 0.5;
                }
                return centroid.Mean;
            }
            currentRank = nextRank;
        }
        return _max;
    }

    public double Percentile(double p) => Quantile(p / 100.0);
    public double Cdf(double x)
    {
        if (_count == 0)
            return double.NaN;
        if (x < _min) return 0.0;
        if (x >= _max) return 1.0;
        EnsureSorted();
        double currentRank = 0;
        for (int i = 0; i < _centroids.Count; i++)
        {
            var centroid = _centroids[i];
            if (x <= centroid.Mean)
            {
                double halfWeight = centroid.Weight * 0.5;
                return Math.Min(1.0, (currentRank + halfWeight) / _count);
            }
            currentRank += centroid.Weight;
        }
        return 1.0;
    }

    public void Merge(Digest other)
    {
        if (other == null || other._count == 0)
            return;
        // Linear merge: walk both centroid lists in lockstep (two-pointer) and
        // build a sorted union in one pass. Previously Merge called Add per
        // centroid; each Add does a List<T>.Insert that shifts O(c) elements,
        // making Merge O(c1*c2) worst case. Linear merge is O(c1+c2), then
        // Compress collapses adjacent centroids that fit under the scale
        // function. Compress's EnsureSorted is a no-op because we build the
        // merged list in sorted order.
        EnsureSorted();
        var thisCentroids = _centroids;
        var otherCentroids = other._centroids;
        if (other._needsSort)
        {
            // Sort 'other' on its own copy so we don't mutate the caller's state.
            otherCentroids = new List<Centroid>(other._centroids);
            otherCentroids.Sort((a, b) => a.Mean.CompareTo(b.Mean));
        }
        var merged = new List<Centroid>(thisCentroids.Count + otherCentroids.Count);
        int i = 0, j = 0;
        while (i < thisCentroids.Count && j < otherCentroids.Count)
        {
            if (thisCentroids[i].Mean <= otherCentroids[j].Mean)
                merged.Add(thisCentroids[i++]);
            else
                merged.Add(otherCentroids[j++]);
        }
        while (i < thisCentroids.Count) merged.Add(thisCentroids[i++]);
        while (j < otherCentroids.Count) merged.Add(otherCentroids[j++]);

        _centroids.Clear();
        _centroids.AddRange(merged);
        _count += other._count;
        if (other._min < _min) _min = other._min;
        if (other._max > _max) _max = other._max;
        _needsSort = false;
        if (_centroids.Count > _compression)
            Compress();
    }

    public Digest Clone()
    {
        var result = new Digest(_compression);
        foreach (var centroid in _centroids)
        {
            result._centroids.Add(centroid);
        }
        result._count = _count;
        result._min = _min;
        result._max = _max;
        result._needsSort = _needsSort;
        return result;
    }

    public void Clear()
    {
        _centroids.Clear();
        _count = 0;
        _min = double.PositiveInfinity;
        _max = double.NegativeInfinity;
        _needsSort = false;
    }

    public void Compress()
    {
        if (_centroids.Count <= 1)
            return;
        EnsureSorted();
        var newCentroids = new List<Centroid>();
        double currentWeight = 0;
        foreach (var centroid in _centroids)
        {
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
        _centroids.Clear();
        _centroids.AddRange(newCentroids);
        _needsSort = false;
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

    private int FindInsertionPoint(double value)
    {
        if (_centroids.Count == 0)
            return 0;
        int left = 0, right = _centroids.Count;
        while (left < right)
        {
            int mid = (left + right) / 2;
            if (_centroids[mid].Mean < value)
                left = mid + 1;
            else
                right = mid;
        }
        return left;
    }

    private bool CanMerge(int index, double value, double weight)
    {
        if (index >= _centroids.Count)
            return false;
        var centroid = _centroids[index];
        if (Math.Abs(centroid.Mean - value) > 1e-10)
            return false;
        double currentWeight = 0;
        for (int i = 0; i < index; i++)
            currentWeight += _centroids[i].Weight;
        double q = currentWeight / _count;
        double maxWeight = _count * ScaleFunction(q, q);
        return centroid.Weight + weight <= maxWeight;
    }

    private void EnsureSorted()
    {
        if (_needsSort)
        {
            _centroids.Sort((a, b) => a.Mean.CompareTo(b.Mean));
            _needsSort = false;
        }
    }

    private readonly struct Centroid
    {
        public readonly double Mean;
        public readonly double Weight;
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
}
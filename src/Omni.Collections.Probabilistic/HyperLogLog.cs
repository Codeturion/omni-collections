using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Omni.Collections.Probabilistic;

/// <summary>
/// A probabilistic cardinality estimation algorithm that efficiently counts distinct elements using logarithmic space.
/// Provides O(1) Add operations and O(m) EstimateCardinality where m is the number of buckets, with constant memory regardless of input size.
/// Perfect for distinct count estimation in big data scenarios, stream processing, database analytics,
/// or situations where memory usage must remain constant regardless of the actual cardinality.
/// </summary>
public class HyperLogLog<T> where T : notnull
{
    private readonly byte[] _buckets;
    private readonly int _bucketCount;
    private readonly int _bucketBits;
    private readonly double _alpha;
    private bool _hasSmallRangeCorrection;
    private long _cachedCardinality = 0;
    private bool _cardinalityDirty = true;
    public int BucketCount => _bucketCount;
    public int BucketBits => _bucketBits;
    public double StandardError => 1.04 / Math.Sqrt(_bucketCount);
    public HyperLogLog(int bucketBits = 12)
    {
        if (bucketBits < 4 || bucketBits > 16)
            throw new ArgumentOutOfRangeException(nameof(bucketBits), "Bucket bits must be between 4 and 16");
        _bucketBits = bucketBits;
        _bucketCount = 1 << bucketBits;
        _buckets = new byte[_bucketCount];
        _alpha = bucketBits switch
        {
            4 => 0.673,
            5 => 0.697,
            6 => 0.709,
            >= 7 => 0.7213 / (1.0 + 1.079 / _bucketCount),
            _ => 0.7213 / (1.0 + 1.079 / _bucketCount)
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item)
    {
        var hash = (uint)item.GetHashCode();
        hash ^= hash >> 16;
        hash *= 0x85ebca6b;
        hash ^= hash >> 13;
        hash *= 0xc2b2ae35;
        hash ^= hash >> 16;
        var bucketIndex = (int)(hash & ((1u << _bucketBits) - 1));
        var remainingBits = hash >> _bucketBits;
        var leadingZeros = BitOperations.LeadingZeroCount(remainingBits) - _bucketBits + 1;
        if (leadingZeros > _buckets[bucketIndex])
        {
            _buckets[bucketIndex] = (byte)Math.Min(leadingZeros, 255);
            _cardinalityDirty = true;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long EstimateCardinality()
    {
        if (!_cardinalityDirty)
            return _cachedCardinality;
        double rawEstimate = CalculateRawEstimate();
        if (rawEstimate <= 2.5 * _bucketCount)
        {
            var zeroBuckets = CountZeroBuckets();
            if (zeroBuckets > 0)
            {
                _hasSmallRangeCorrection = true;
                _cachedCardinality = (long)(_bucketCount * Math.Log((double)_bucketCount / zeroBuckets));
                _cardinalityDirty = false;
                return _cachedCardinality;
            }
        }
        else if (rawEstimate <= (1.0 / 30.0) * (1L << 32))
        {
            _hasSmallRangeCorrection = false;
            _cachedCardinality = (long)rawEstimate;
            _cardinalityDirty = false;
            return _cachedCardinality;
        }
        else
        {
            _hasSmallRangeCorrection = false;
            _cachedCardinality = (long)(-1 * (1L << 32) * Math.Log(1.0 - rawEstimate / (1L << 32)));
            _cardinalityDirty = false;
            return _cachedCardinality;
        }
        _cachedCardinality = (long)rawEstimate;
        _cardinalityDirty = false;
        return _cachedCardinality;
    }

    public double GetRelativeError()
    {
        return StandardError;
    }

    public bool IsSmallRange => _hasSmallRangeCorrection;
    public void Merge(HyperLogLog<T> other)
    {
        if (other._bucketCount != _bucketCount)
            throw new ArgumentException("HyperLogLogs must have same bucket count to merge");
        bool anyChanged = false;
        for (int i = 0; i < _bucketCount; i++)
        {
            var newValue = Math.Max(_buckets[i], other._buckets[i]);
            if (newValue != _buckets[i])
            {
                _buckets[i] = newValue;
                anyChanged = true;
            }
        }
        if (anyChanged)
            _cardinalityDirty = true;
    }

    public void Clear()
    {
        Array.Clear(_buckets);
        _hasSmallRangeCorrection = false;
        _cardinalityDirty = true;
    }

    public HyperLogLog<T> Clone()
    {
        var clone = new HyperLogLog<T>(_bucketBits);
        Array.Copy(_buckets, clone._buckets, _bucketCount);
        clone._hasSmallRangeCorrection = _hasSmallRangeCorrection;
        clone._cachedCardinality = _cachedCardinality;
        clone._cardinalityDirty = _cardinalityDirty;
        return clone;
    }

    public void AddRange(ReadOnlySpan<T> items)
    {
        foreach (var item in items)
        {
            Add(item);
        }
    }

    public long EstimateUnion(HyperLogLog<T> other)
    {
        HyperLogLog<T>? union = Clone();
        union.Merge(other);
        return union.EstimateCardinality();
    }

    public long EstimateIntersection(HyperLogLog<T> other)
    {
        var thisCard = EstimateCardinality();
        var otherCard = other.EstimateCardinality();
        var unionCard = EstimateUnion(other);
        var intersection = thisCard + otherCard - unionCard;
        return Math.Max(0, intersection);
    }

    public HyperLogLogStats GetStats()
    {
        var zeroBuckets = CountZeroBuckets();
        var maxBucketValue = 0;
        var totalBucketSum = 0;
        foreach (var bucket in _buckets)
        {
            totalBucketSum += bucket;
            if (bucket > maxBucketValue)
                maxBucketValue = bucket;
        }
        return new HyperLogLogStats
        {
            BucketCount = _bucketCount,
            ZeroBuckets = zeroBuckets,
            MaxBucketValue = maxBucketValue,
            AverageBucketValue = (double)totalBucketSum / _bucketCount,
            FillRatio = (double)(_bucketCount - zeroBuckets) / _bucketCount,
            EstimatedCardinality = EstimateCardinality(),
            StandardError = StandardError,
            IsSmallRange = _hasSmallRangeCorrection
        };
    }

    public long GetMemoryUsage()
    {
        return _bucketCount + 64;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private double CalculateRawEstimate()
    {
        double harmonicMean = 0.0;
        ReadOnlySpan<double> pow2Lookup =
        [
            1.0, 0.5, 0.25, 0.125, 0.0625, 0.03125, 0.015625, 0.0078125,
            0.00390625, 0.001953125, 0.0009765625, 0.00048828125, 0.000244140625,
            0.0001220703125, 0.00006103515625, 0.000030517578125
        ];
        for (int i = 0; i < _bucketCount; i++)
        {
            var bucketValue = _buckets[i];
            if (bucketValue < pow2Lookup.Length)
                harmonicMean += pow2Lookup[bucketValue];
            else
                harmonicMean += Math.Pow(2, -bucketValue);
        }
        return _alpha * _bucketCount * _bucketCount / harmonicMean;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int CountZeroBuckets()
    {
        int count = 0;
        for (int i = 0; i < _bucketCount; i++)
        {
            if (_buckets[i] == 0)
                count++;
        }
        return count;
    }
}

public readonly struct HyperLogLogStats
{
    public int BucketCount { get; init; }

    public int ZeroBuckets { get; init; }

    public int MaxBucketValue { get; init; }

    public double AverageBucketValue { get; init; }

    public double FillRatio { get; init; }

    public long EstimatedCardinality { get; init; }

    public double StandardError { get; init; }

    public bool IsSmallRange { get; init; }
}
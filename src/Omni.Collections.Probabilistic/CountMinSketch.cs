using System;
using System.Runtime.CompilerServices;
using Omni.Collections.Core.Hashing;

namespace Omni.Collections.Probabilistic;

/// <summary>
/// A probabilistic data structure that estimates frequency counts in data streams using bounded memory.
/// Provides O(d) Add/EstimateCount operations where d is the sketch depth, with constant memory regardless of stream size.
/// Ideal for frequency counting in large data streams, heavy hitters detection, real-time analytics,
/// or scenarios where approximate counts are acceptable but memory usage must remain constant.
/// </summary>
public class CountMinSketch<T> where T : notnull
{
    private readonly uint[,] _table;
    private readonly int _width;
    private readonly int _depth;
    private readonly ulong[] _hashSeeds;
    private readonly IHasher<T> _hasher;
    private long _totalCount;
    public long TotalCount => _totalCount;
    public int Width => _width;
    public int Depth => _depth;
    public double MaxError => (double)_totalCount / _width;
    public CountMinSketch(int width = 1024, int depth = 4)
        : this(width, depth, Hashers.Default<T>(), seed: 0UL)
    {
    }

    public CountMinSketch(int width, int depth, IHasher<T> hasher, ulong seed = 0UL)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be positive");
        if (depth <= 0)
            throw new ArgumentOutOfRangeException(nameof(depth), "Depth must be positive");
        if (depth > 32)
            throw new ArgumentOutOfRangeException(nameof(depth), "Depth cannot exceed 32");
        if (hasher is null)
            throw new ArgumentNullException(nameof(hasher));
        _width = width;
        _depth = depth;
        _table = new uint[_depth, _width];
        _hasher = hasher;
        _hashSeeds = BuildRowSeeds(_depth, seed);
    }

    public CountMinSketch(double maxError, double confidence = 0.99)
        : this(maxError, confidence, Hashers.Default<T>(), seed: 0UL)
    {
    }

    public CountMinSketch(double maxError, double confidence, IHasher<T> hasher, ulong seed = 0UL)
    {
        if (maxError <= 0 || maxError >= 1)
            throw new ArgumentOutOfRangeException(nameof(maxError), "Error must be between 0 and 1");
        if (confidence <= 0 || confidence >= 1)
            throw new ArgumentOutOfRangeException(nameof(confidence), "Confidence must be between 0 and 1");
        if (hasher is null)
            throw new ArgumentNullException(nameof(hasher));
        _width = (int)Math.Ceiling(Math.E / maxError);
        _depth = (int)Math.Ceiling(Math.Log(1.0 / (1.0 - confidence)));
        _depth = Math.Max(1, Math.Min(_depth, 32));
        _table = new uint[_depth, _width];
        _hasher = hasher;
        _hashSeeds = BuildRowSeeds(_depth, seed);
    }

    public void Add(T item)
    {
        Add(item, 1);
    }

    public void Add(T item, uint count)
    {
        if (count == 0)
            return;
        for (int i = 0; i < _depth; i++)
        {
            var bucketIndex = (int)(_hasher.Hash(item, _hashSeeds[i]) % (ulong)_width);
            if (_table[i, bucketIndex] <= uint.MaxValue - count)
                _table[i, bucketIndex] += count;
            else
                _table[i, bucketIndex] = uint.MaxValue;
        }
        _totalCount += count;
    }

    public uint EstimateCount(T item)
    {
        uint minCount = uint.MaxValue;
        for (int i = 0; i < _depth; i++)
        {
            var bucketIndex = (int)(_hasher.Hash(item, _hashSeeds[i]) % (ulong)_width);
            var count = _table[i, bucketIndex];
            if (count < minCount)
                minCount = count;
        }
        return minCount;
    }

    public double EstimateFrequency(T item)
    {
        if (_totalCount == 0)
            return 0.0;
        var count = EstimateCount(item);
        return (double)count / _totalCount;
    }

    public bool IsHeavyHitter(T item, double threshold)
    {
        if (threshold <= 0 || threshold > 1)
            throw new ArgumentOutOfRangeException(nameof(threshold), "Threshold must be between 0 and 1");
        return EstimateFrequency(item) >= threshold;
    }

    public void Clear()
    {
        Array.Clear(_table, 0, _table.Length);
        _totalCount = 0;
    }

    public void Merge(CountMinSketch<T> other)
    {
        if (other._width != _width || other._depth != _depth)
            throw new ArgumentException("Sketches must have same dimensions to merge");
        for (int i = 0; i < _depth; i++)
        {
            for (int j = 0; j < _width; j++)
            {
                var sum = (ulong)_table[i, j] + other._table[i, j];
                _table[i, j] = sum > uint.MaxValue ? uint.MaxValue : (uint)sum;
            }
        }
        _totalCount += other._totalCount;
    }

    public void Scale(double factor)
    {
        if (factor < 0 || factor > 1)
            throw new ArgumentOutOfRangeException(nameof(factor), "Factor must be between 0 and 1");
        for (int i = 0; i < _depth; i++)
        {
            for (int j = 0; j < _width; j++)
            {
                _table[i, j] = (uint)(_table[i, j] * factor);
            }
        }
        _totalCount = (long)(_totalCount * factor);
    }

    public CountMinSketchStats GetStats()
    {
        uint minValue = uint.MaxValue;
        uint maxValue = 0;
        ulong sum = 0;
        int nonZeroCells = 0;
        for (int i = 0; i < _depth; i++)
        {
            for (int j = 0; j < _width; j++)
            {
                var value = _table[i, j];
                sum += value;
                if (value > 0)
                    nonZeroCells++;
                if (value < minValue)
                    minValue = value;
                if (value > maxValue)
                    maxValue = value;
            }
        }
        var totalCells = _depth * _width;
        var averageValue = totalCells > 0 ? (double)sum / totalCells : 0;
        var fillRatio = totalCells > 0 ? (double)nonZeroCells / totalCells : 0;
        return new CountMinSketchStats
        {
            TotalItems = _totalCount,
            TotalCells = totalCells,
            NonZeroCells = nonZeroCells,
            FillRatio = fillRatio,
            MinCellValue = minValue == uint.MaxValue ? 0 : minValue,
            MaxCellValue = maxValue,
            AverageCellValue = averageValue,
            TheoreticalMaxError = MaxError
        };
    }

    public long GetMemoryUsage()
    {
        return (_depth * _width * sizeof(uint)) + (_depth * sizeof(uint)) + 64;
    }

    private static ulong[] BuildRowSeeds(int depth, ulong baseSeed)
    {
        // SplitMix64-derived per-row seeds: deterministic, uncorrelated across rows.
        var seeds = new ulong[depth];
        ulong x = baseSeed;
        for (int i = 0; i < depth; i++)
        {
            x += 0x9E3779B97F4A7C15UL;
            ulong s = x;
            s = (s ^ (s >> 30)) * 0xBF58476D1CE4E5B9UL;
            s = (s ^ (s >> 27)) * 0x94D049BB133111EBUL;
            seeds[i] = s ^ (s >> 31);
        }
        return seeds;
    }
}

public readonly struct CountMinSketchStats
{
    public long TotalItems { get; init; }

    public int TotalCells { get; init; }

    public int NonZeroCells { get; init; }

    public double FillRatio { get; init; }

    public uint MinCellValue { get; init; }

    public uint MaxCellValue { get; init; }

    public double AverageCellValue { get; init; }

    public double TheoreticalMaxError { get; init; }
}
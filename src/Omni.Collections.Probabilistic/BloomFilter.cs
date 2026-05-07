using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using Omni.Collections.Core.Hashing;

namespace Omni.Collections.Probabilistic;

/// <summary>
/// A space-efficient probabilistic data structure that provides fast membership testing with configurable false positive rates.
/// Provides O(k) Add/Contains operations where k is the number of hash functions, with constant memory regardless of input size.
/// Perfect for cache filtering, duplicate detection, database query optimization, or any membership testing
/// scenario where false positives are acceptable but false negatives are not.
/// </summary>
public class BloomFilter<T> : IDisposable where T : notnull
{
    private readonly ulong[] _bits;
    private readonly int _bitCount;
    private readonly int _hashFunctionCount;
    private readonly double _falsePositiveRate;
    private readonly ArrayPool<ulong>? _arrayPool;
    private readonly bool _usePooling;
    private readonly IHasher<T> _hasher;
    private int _itemCount;
    public int Count => _itemCount;
    public double FalsePositiveRate => _falsePositiveRate;
    public int HashFunctionCount => _hashFunctionCount;
    public int BitCount => _bitCount;
    public BloomFilter(int expectedItems, double falsePositiveRate = 0.01)
        : this(expectedItems, falsePositiveRate, Hashers.Default<T>(), arrayPool: null)
    {
    }

    public BloomFilter(int expectedItems, double falsePositiveRate, IHasher<T> hasher)
        : this(expectedItems, falsePositiveRate, hasher, arrayPool: null)
    {
    }

    public static BloomFilter<T> CreateWithArrayPool(int expectedItems, double falsePositiveRate = 0.01)
    {
        return new BloomFilter<T>(expectedItems, falsePositiveRate, Hashers.Default<T>(), ArrayPool<ulong>.Shared);
    }

    public static BloomFilter<T> CreateWithArrayPool(int expectedItems, double falsePositiveRate, IHasher<T> hasher)
    {
        return new BloomFilter<T>(expectedItems, falsePositiveRate, hasher, ArrayPool<ulong>.Shared);
    }

    private BloomFilter(int expectedItems, double falsePositiveRate, IHasher<T> hasher, ArrayPool<ulong>? arrayPool)
    {
        if (expectedItems <= 0)
            throw new ArgumentOutOfRangeException(nameof(expectedItems));
        if (expectedItems > 100_000_000) // 100M items max to prevent overflow
            throw new ArgumentOutOfRangeException(nameof(expectedItems), "Expected items cannot exceed 100 million");
        if (falsePositiveRate <= 0 || falsePositiveRate >= 1)
            throw new ArgumentOutOfRangeException(nameof(falsePositiveRate));
        if (hasher is null)
            throw new ArgumentNullException(nameof(hasher));

        _falsePositiveRate = falsePositiveRate;
        _hasher = hasher;
        _arrayPool = arrayPool;
        _usePooling = arrayPool != null;
        
        // Calculate bit count with overflow protection
        double bitCountDouble = -expectedItems * Math.Log(falsePositiveRate) / (Math.Log(2) * Math.Log(2));
        if (bitCountDouble > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(expectedItems), "Configuration would require too many bits");
        _bitCount = (int)Math.Ceiling(bitCountDouble);
        
        // Ensure minimum bit count to avoid division by zero
        _bitCount = Math.Max(1, _bitCount);
        
        _hashFunctionCount = (int)Math.Ceiling((_bitCount / (double)expectedItems) * Math.Log(2));
        _hashFunctionCount = Math.Max(1, Math.Min(_hashFunctionCount, 20));
        
        int ulongCount = (_bitCount + 63) / 64;
        if (ulongCount < 0) // Additional overflow check
            throw new ArgumentOutOfRangeException(nameof(expectedItems), "Configuration would require too much memory");
        if (_usePooling)
        {
            _bits = _arrayPool!.Rent(ulongCount);
            Array.Clear(_bits, 0, ulongCount);
        }
        else
        {
            _bits = new ulong[ulongCount];
        }
    }

    public void Add(T item)
    {
        var hashes = GetHashes(item);
        for (int i = 0; i < _hashFunctionCount; i++)
        {
            uint bitIndex = (uint)((hashes.Hash1 + (ulong)i * hashes.Hash2) % (ulong)_bitCount);
            SetBit(bitIndex);
        }
        _itemCount++;
    }

    public bool Contains(T item)
    {
        var hashes = GetHashes(item);
        for (int i = 0; i < _hashFunctionCount; i++)
        {
            uint bitIndex = (uint)((hashes.Hash1 + (ulong)i * hashes.Hash2) % (ulong)_bitCount);
            if (!GetBit(bitIndex))
                return false;
        }
        return true;
    }

    public void Clear()
    {
        int ulongCount = (_bitCount + 63) / 64;
        Array.Clear(_bits, 0, ulongCount);
        _itemCount = 0;
    }

    public double GetActualFalsePositiveRate()
    {
        if (_itemCount == 0) return 0.0;
        double fillRatio = (double)(_hashFunctionCount * _itemCount) / _bitCount;
        return Math.Pow(1 - Math.Exp(-fillRatio), _hashFunctionCount);
    }

    public double GetFillRatio()
    {
        int setBits = 0;
        foreach (ulong chunk in _bits)
        {
            setBits += System.Numerics.BitOperations.PopCount(chunk);
        }
        return (double)setBits / _bitCount;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetBit(uint index)
    {
        uint chunkIndex = index / 64;
        int bitIndex = (int)(index % 64);
        _bits[chunkIndex] |= 1UL << bitIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool GetBit(uint index)
    {
        uint chunkIndex = index / 64;
        int bitIndex = (int)(index % 64);
        return (_bits[chunkIndex] & (1UL << bitIndex)) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private (ulong Hash1, ulong Hash2) GetHashes(T item)
    {
        // Two independent hash families via distinct seeds (Kirsch & Mitzenmacher
        // double-hashing). h2 forced odd to avoid degenerate cycles when bitCount is even.
        ulong hash1 = _hasher.Hash(item, 0UL);
        ulong hash2 = _hasher.Hash(item, 0x9E3779B97F4A7C15UL) | 1UL;
        return (hash1, hash2);
    }

    public BloomFilterStats GetStats()
    {
        var fillRatio = GetFillRatio();
        var actualFalsePositiveRate = GetActualFalsePositiveRate();
        return new BloomFilterStats
        {
            BitCount = _bitCount,
            HashFunctionCount = _hashFunctionCount,
            ItemCount = _itemCount,
            FillRatio = fillRatio,
            DesignedFalsePositiveRate = _falsePositiveRate,
            ActualFalsePositiveRate = actualFalsePositiveRate
        };
    }

    public void Union(BloomFilter<T> other)
    {
        if (other._bitCount != _bitCount)
            throw new ArgumentException("BloomFilters must have same bit count for union");
        for (int i = 0; i < _bits.Length; i++)
        {
            _bits[i] |= other._bits[i];
        }
        _itemCount = Math.Max(_itemCount, other._itemCount);
    }

    public void AddRange(ReadOnlySpan<T> items)
    {
        foreach (var item in items)
        {
            Add(item);
        }
    }

    public long EstimateItemCount()
    {
        var setBits = GetSetBitCount();
        if (setBits == 0) return 0;
        var ratio = (double)setBits / _bitCount;
        if (ratio >= 1.0) return long.MaxValue;
        var estimate = -((double)_bitCount / _hashFunctionCount) * Math.Log(1.0 - ratio);
        return (long)Math.Round(estimate);
    }

    public long GetMemoryUsage()
    {
        int ulongCount = (_bitCount + 63) / 64;
        return ulongCount * 8 + 64;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ulong GetSetBitCount()
    {
        ulong count = 0;
        Span<ulong> bitsSpan = _bits.AsSpan();
        for (int i = 0; i < bitsSpan.Length; i++)
        {
            count += (ulong)System.Numerics.BitOperations.PopCount(bitsSpan[i]);
        }
        return count;
    }

    public void Dispose()
    {
        if (_usePooling && _arrayPool != null)
        {
            _arrayPool.Return(_bits, clearArray: true);
        }
    }
}

public readonly struct BloomFilterStats
{
    public int BitCount { get; init; }

    public int HashFunctionCount { get; init; }

    public int ItemCount { get; init; }

    public double FillRatio { get; init; }

    public double DesignedFalsePositiveRate { get; init; }

    public double ActualFalsePositiveRate { get; init; }
}
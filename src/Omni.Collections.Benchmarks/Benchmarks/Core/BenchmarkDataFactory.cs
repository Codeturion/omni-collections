using System;
using System.Collections.Generic;
using System.Linq;

namespace Omni.Collections.Benchmarks.Core;

/// <summary>
/// Factory for creating controlled, reusable test data for RAM-safe benchmarks.
/// Provides pre-allocated data pools to prevent uncontrolled memory allocation.
/// </summary>
public static class BenchmarkDataFactory
{
    private static readonly Random _random = new(42); // Fixed seed for reproducibility
    
    // Pre-allocated data pools
    private static readonly Dictionary<int, string[]> _stringPools = new();
    private static readonly Dictionary<int, int[]> _intPools = new();
    private static readonly Dictionary<int, TestObject[]> _objectPools = new();
    
    /// <summary>
    /// Standard data sizes for consistent benchmarking
    /// </summary>
    public static class DataSizes
    {
        public const int Tiny = 100;
        public const int Small = 1_000;
        public const int Medium = 50_000;
        public const int Large = 100_000;
        
        /// <summary>
        /// Get appropriate data size for configuration profile
        /// </summary>
        public static int GetSizeForProfile(BenchmarkProfile profile) => profile switch
        {
            BenchmarkProfile.Fast => Small,
            BenchmarkProfile.Medium => Medium,
            BenchmarkProfile.Hard => Large,
            BenchmarkProfile.Precision => Medium,  // Temporary: Use 10k items for scaling comparison
            _ => Small
        };
        
        /// <summary>
        /// Get all sizes up to the specified profile
        /// </summary>
        public static int[] GetScalingSizes(BenchmarkProfile maxProfile) => maxProfile switch
        {
            BenchmarkProfile.Fast => [Tiny, Small],
            BenchmarkProfile.Medium => [Tiny, Small, Medium],
            BenchmarkProfile.Hard => [Tiny, Small, Medium, Large],
            BenchmarkProfile.Precision => [Medium],  // Temporary: Only 10k size for scaling comparison
            _ => [Small]
        };
    }
    
    /// <summary>
    /// Creates or retrieves a pool of random strings
    /// </summary>
    public static string[] GetStringPool(int size)
    {
        if (!_stringPools.TryGetValue(size, out var pool))
        {
            pool = GenerateStringPool(size);
            _stringPools[size] = pool;
        }
        return pool;
    }
    
    /// <summary>
    /// Creates or retrieves a pool of random integers
    /// </summary>
    public static int[] GetIntPool(int size)
    {
        if (!_intPools.TryGetValue(size, out var pool))
        {
            pool = GenerateIntPool(size);
            _intPools[size] = pool;
        }
        return pool;
    }
    
    /// <summary>
    /// Creates or retrieves a pool of test objects
    /// </summary>
    public static TestObject[] GetObjectPool(int size)
    {
        if (!_objectPools.TryGetValue(size, out var pool))
        {
            pool = GenerateObjectPool(size);
            _objectPools[size] = pool;
        }
        return pool;
    }
    
    /// <summary>
    /// Creates a pool of unique keys for dictionary benchmarks
    /// </summary>
    public static string[] GetUniqueKeyPool(int size)
    {
        return Enumerable.Range(0, size)
            .Select(i => $"key_{i:D8}")
            .ToArray();
    }
    
    /// <summary>
    /// Creates test data with predictable access patterns for ML benchmarks
    /// </summary>
    public static (string[] keys, string[] values) GetPatternedData(int size)
    {
        var keys = new string[size];
        var values = new string[size];
        
        for (int i = 0; i < size; i++)
        {
            // Create patterns: every 10th item accessed more frequently
            var frequency = (i % 10 == 0) ? "frequent" : "normal";
            keys[i] = $"{frequency}_key_{i:D8}";
            values[i] = $"{frequency}_value_{i:D8}";
        }
        
        return (keys, values);
    }
    
    /// <summary>
    /// Creates spatial test data for spatial data structures
    /// </summary>
    public static (float x, float y)[] GetSpatialPoints(int count, float minX = 0, float maxX = 1000, 
        float minY = 0, float maxY = 1000)
    {
        var points = new (float x, float y)[count];
        for (int i = 0; i < count; i++)
        {
            points[i] = (
                minX + (float)_random.NextDouble() * (maxX - minX),
                minY + (float)_random.NextDouble() * (maxY - minY)
            );
        }
        return points;
    }
    
    /// <summary>
    /// Clears all cached data pools to free memory
    /// </summary>
    public static void ClearAllPools()
    {
        _stringPools.Clear();
        _intPools.Clear();
        _objectPools.Clear();
        GC.Collect();
    }
    
    /// <summary>
    /// Gets memory usage of all pools in MB
    /// </summary>
    public static long GetPoolMemoryUsageMB()
    {
        var stringMemory = _stringPools.Values.Sum(pool => pool.Sum(s => s.Length * 2)); // UTF-16
        var intMemory = _intPools.Values.Sum(pool => pool.Length * 4);
        var objectMemory = _objectPools.Values.Sum(pool => pool.Length * 64); // Estimated object size
        
        return (stringMemory + intMemory + objectMemory) / (1024 * 1024);
    }
    
    private static string[] GenerateStringPool(int size)
    {
        var pool = new string[size];
        for (int i = 0; i < size; i++)
        {
            // Generate strings of varying length (8-32 characters)
            var length = 8 + _random.Next(25);
            var chars = new char[length];
            for (int j = 0; j < length; j++)
            {
                chars[j] = (char)('a' + _random.Next(26));
            }
            pool[i] = new string(chars);
        }
        return pool;
    }
    
    private static int[] GenerateIntPool(int size)
    {
        var pool = new int[size];
        for (int i = 0; i < size; i++)
        {
            pool[i] = _random.Next();
        }
        return pool;
    }
    
    private static TestObject[] GenerateObjectPool(int size)
    {
        var pool = new TestObject[size];
        for (int i = 0; i < size; i++)
        {
            pool[i] = new TestObject
            {
                Id = i,
                Name = $"Object_{i:D8}",
                Value = _random.NextDouble(),
                Timestamp = DateTime.Now.AddMinutes(_random.Next(-10000, 10000))
            };
        }
        return pool;
    }
}

/// <summary>
/// Standard test object for benchmarking
/// </summary>
public class TestObject : IEquatable<TestObject>, IComparable<TestObject>
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
    
    public bool Equals(TestObject? other)
    {
        return other != null && Id == other.Id;
    }
    
    public override bool Equals(object? obj)
    {
        return Equals(obj as TestObject);
    }
    
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
    
    public int CompareTo(TestObject? other)
    {
        return other == null ? 1 : Id.CompareTo(other.Id);
    }
    
    public override string ToString()
    {
        return $"TestObject(Id={Id}, Name={Name})";
    }
}

/// <summary>
/// Benchmark configuration profiles
/// </summary>
public enum BenchmarkProfile
{
    Fast,      // Quick validation (30-60 seconds)
    Medium,    // Reliable testing (5-10 minutes)
    Hard,      // Comprehensive analysis (30-60 minutes)
    Precision  // Ultra-precise measurements (60-90 minutes)
}
using BenchmarkDotNet.Attributes;
using Omni.Collections.Benchmarks.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Omni.Collections.Benchmarks.Scaling;

/// <summary>
/// Base class for Big-O complexity validation benchmarks.
/// Tests the same operations across multiple data sizes to validate algorithmic complexity.
/// </summary>
/// <typeparam name="TCollection">The collection type being tested</typeparam>
/// <typeparam name="TKey">Key type for the collection</typeparam>
/// <typeparam name="TValue">Value type for the collection</typeparam>
public abstract class ScalingBenchmark<TCollection, TKey, TValue> : MemorySafeBenchmarkBase
    where TCollection : new()
{
    /// <summary>
    /// Data sizes to test scaling behavior
    /// </summary>
    [Params(50000)] // Updated: Use 50k for comprehensive pooling analysis
    public int Size { get; set; }
    
    protected TCollection Collection = default!;
    protected TKey[] TestKeys = default!;
    protected TValue[] TestValues = default!;
    
    /// <summary>
    /// Expected Big-O complexity for add operations
    /// </summary>
    public abstract string ExpectedAddComplexity { get; }
    
    /// <summary>
    /// Expected Big-O complexity for get operations
    /// </summary>
    public abstract string ExpectedGetComplexity { get; }
    
    /// <summary>
    /// Expected Big-O complexity for remove operations
    /// </summary>
    public abstract string ExpectedRemoveComplexity { get; }
    
    [GlobalSetup]
    public override void GlobalSetup()
    {
        base.GlobalSetup();
        
        // Generate test data based on current size
        SetupTestData(Size);
        
        // Initialize collection
        Collection = new TCollection();
        SetupCollection();
        
        Console.WriteLine($"🔬 Scaling test: {typeof(TCollection).Name} with {Size} items");
        Console.WriteLine($"   Expected complexities: Add={ExpectedAddComplexity}, Get={ExpectedGetComplexity}, Remove={ExpectedRemoveComplexity}");
    }
    
    [GlobalCleanup]
    public override void GlobalCleanup()
    {
        CleanupCollection();
        base.GlobalCleanup();
    }
    
    /// <summary>
    /// Benchmark: Single item addition - should scale according to ExpectedAddComplexity
    /// </summary>
    [Benchmark]
    [BenchmarkCategory(nameof(BenchmarkCategory.Scaling))]
    public virtual void Add_SingleItem()
    {
        PerformAdd();
    }
    
    /// <summary>
    /// Benchmark: Single item retrieval - should scale according to ExpectedGetComplexity
    /// </summary>
    [Benchmark]
    [BenchmarkCategory(nameof(BenchmarkCategory.Scaling))]
    public virtual void Get_SingleItem()
    {
        PerformGet();
    }
    
    /// <summary>
    /// Benchmark: Single item removal - should scale according to ExpectedRemoveComplexity
    /// </summary>
    [Benchmark]
    [BenchmarkCategory(nameof(BenchmarkCategory.Scaling))]
    public virtual void Remove_SingleItem()
    {
        PerformRemove();
    }
    
    /// <summary>
    /// Benchmark: Complete enumeration - should always be O(n)
    /// </summary>
    [Benchmark]
    [BenchmarkCategory(nameof(BenchmarkCategory.Scaling))]
    public virtual int Enumerate_AllItems()
    {
        return PerformEnumerate();
    }
    
    /// <summary>
    /// Benchmark: Bulk addition - measure batch performance scaling
    /// </summary>
    [Benchmark]
    [BenchmarkCategory(nameof(BenchmarkCategory.Scaling))]
    public virtual void AddRange_MultipleItems()
    {
        PerformBulkAdd();
    }
    
    // Abstract methods that concrete implementations must provide
    
    /// <summary>
    /// Setup test data for the given size
    /// </summary>
    protected abstract void SetupTestData(int size);
    
    /// <summary>
    /// Initialize and populate the collection
    /// </summary>
    protected abstract void SetupCollection();
    
    /// <summary>
    /// Cleanup the collection
    /// </summary>
    protected virtual void CleanupCollection()
    {
        if (Collection is IDisposable disposable)
            disposable.Dispose();
    }
    
    /// <summary>
    /// Perform single add operation
    /// </summary>
    protected abstract void PerformAdd();
    
    /// <summary>
    /// Perform single get operation
    /// </summary>
    protected abstract void PerformGet();
    
    /// <summary>
    /// Perform single remove operation
    /// </summary>
    protected abstract void PerformRemove();
    
    /// <summary>
    /// Perform complete enumeration and return count
    /// </summary>
    protected abstract int PerformEnumerate();
    
    /// <summary>
    /// Perform bulk addition
    /// </summary>
    protected abstract void PerformBulkAdd();
    
    /// <summary>
    /// Helper to get random test key
    /// </summary>
    protected TKey GetRandomKey()
    {
        return TestKeys[Random.Shared.Next(TestKeys.Length)];
    }
    
    /// <summary>
    /// Helper to get random test value
    /// </summary>
    protected TValue GetRandomValue()
    {
        return TestValues[Random.Shared.Next(TestValues.Length)];
    }
}

/// <summary>
/// Results processor for analyzing Big-O complexity from benchmark results
/// </summary>
public static class BigOAnalyzer
{
    /// <summary>
    /// Analyze benchmark results to determine actual complexity
    /// </summary>
    public static BigOResult AnalyzeComplexity(IEnumerable<(int size, double timeNs)> results)
    {
        var data = results.OrderBy(r => r.size).ToArray();
        
        if (data.Length < 2)
            return new BigOResult("Unknown", 0, "Insufficient data points");
        
        // Test different complexity patterns
        var complexities = new (string, Func<(int size, double timeNs)[], double>)[]
        {
            ("O(1)", TestConstant),
            ("O(log n)", TestLogarithmic),
            ("O(n)", TestLinear),
            ("O(n log n)", TestNLogN),
            ("O(n²)", TestQuadratic)
        };
        
        var bestFit = complexities
            .Select(c => new { 
                Complexity = c.Item1, 
                RSquared = c.Item2(data),
                Coefficient = CalculateCoefficient(data, c.Item2)
            })
            .OrderByDescending(c => c.RSquared)
            .First();
        
        var confidence = bestFit.RSquared > 0.95 ? "High" :
                        bestFit.RSquared > 0.85 ? "Medium" : "Low";
        
        return new BigOResult(bestFit.Complexity, bestFit.RSquared, confidence)
        {
            Coefficient = bestFit.Coefficient,
            DataPoints = data.Length
        };
    }
    
    private static double TestConstant((int size, double timeNs)[] data)
    {
        // For O(1), time should be roughly constant
        var times = data.Select(d => d.timeNs).ToArray();
        var mean = times.Average();
        var variance = times.Select(t => Math.Pow(t - mean, 2)).Average();
        var stdDev = Math.Sqrt(variance);
        
        // High R² if standard deviation is small relative to mean
        return Math.Max(0, 1 - (stdDev / mean));
    }
    
    private static double TestLogarithmic((int size, double timeNs)[] data)
    {
        // Test correlation with log(n)
        var x = data.Select(d => Math.Log(d.size)).ToArray();
        var y = data.Select(d => d.timeNs).ToArray();
        return CalculateRSquared(x, y);
    }
    
    private static double TestLinear((int size, double timeNs)[] data)
    {
        // Test correlation with n
        var x = data.Select(d => (double)d.size).ToArray();
        var y = data.Select(d => d.timeNs).ToArray();
        return CalculateRSquared(x, y);
    }
    
    private static double TestNLogN((int size, double timeNs)[] data)
    {
        // Test correlation with n*log(n)
        var x = data.Select(d => d.size * Math.Log(d.size)).ToArray();
        var y = data.Select(d => d.timeNs).ToArray();
        return CalculateRSquared(x, y);
    }
    
    private static double TestQuadratic((int size, double timeNs)[] data)
    {
        // Test correlation with n²
        var x = data.Select(d => Math.Pow(d.size, 2)).ToArray();
        var y = data.Select(d => d.timeNs).ToArray();
        return CalculateRSquared(x, y);
    }
    
    private static double CalculateRSquared(double[] x, double[] y)
    {
        if (x.Length != y.Length || x.Length < 2)
            return 0;
        
        var xMean = x.Average();
        var yMean = y.Average();
        
        var ssRes = 0.0; // Sum of squares of residuals
        var ssTot = 0.0; // Total sum of squares
        
        // Calculate slope and intercept for linear regression
        var numerator = 0.0;
        var denominator = 0.0;
        
        for (int i = 0; i < x.Length; i++)
        {
            numerator += (x[i] - xMean) * (y[i] - yMean);
            denominator += (x[i] - xMean) * (x[i] - xMean);
        }
        
        if (Math.Abs(denominator) < 1e-10)
            return 0;
        
        var slope = numerator / denominator;
        var intercept = yMean - slope * xMean;
        
        // Calculate R²
        for (int i = 0; i < y.Length; i++)
        {
            var predicted = slope * x[i] + intercept;
            ssRes += (y[i] - predicted) * (y[i] - predicted);
            ssTot += (y[i] - yMean) * (y[i] - yMean);
        }
        
        return Math.Max(0, 1 - (ssRes / ssTot));
    }
    
    private static double CalculateCoefficient((int size, double timeNs)[] data, Func<(int, double)[], double> complexityTest)
    {
        // Return the slope coefficient for the best-fit line
        if (data.Length < 2) return 0;
        
        var x = data.Select(d => (double)d.size).ToArray();
        var y = data.Select(d => d.timeNs).ToArray();
        
        var xMean = x.Average();
        var yMean = y.Average();
        
        var numerator = x.Zip(y, (xi, yi) => (xi - xMean) * (yi - yMean)).Sum();
        var denominator = x.Select(xi => (xi - xMean) * (xi - xMean)).Sum();
        
        return Math.Abs(denominator) < 1e-10 ? 0 : numerator / denominator;
    }
}

/// <summary>
/// Result of Big-O complexity analysis
/// </summary>
public class BigOResult
{
    public string Complexity { get; }
    public double RSquared { get; }
    public string Confidence { get; }
    public double Coefficient { get; set; }
    public int DataPoints { get; set; }
    
    public BigOResult(string complexity, double rSquared, string confidence)
    {
        Complexity = complexity;
        RSquared = rSquared;
        Confidence = confidence;
    }
    
    public override string ToString()
    {
        return $"{Complexity} (R²={RSquared:F3}, {Confidence} confidence, {DataPoints} points)";
    }
}
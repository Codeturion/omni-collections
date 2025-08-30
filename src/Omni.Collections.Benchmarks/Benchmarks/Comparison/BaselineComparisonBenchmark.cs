using BenchmarkDotNet.Attributes;
using Omni.Collections.Benchmarks.Core;
using System;

namespace Omni.Collections.Benchmarks.Comparison;

/// <summary>
/// Base class for comparing Omni.Collections data structures against .NET baseline equivalents.
/// Provides side-by-side performance comparison with automatic winner determination.
/// </summary>
/// <typeparam name="TOmni">Omni.Collections data structure type</typeparam>
/// <typeparam name="TBaseline">.NET baseline equivalent type</typeparam>
/// <typeparam name="TKey">Key type for the collections</typeparam>
/// <typeparam name="TValue">Value type for the collections</typeparam>
public abstract class BaselineComparisonBenchmark<TOmni, TBaseline, TKey, TValue> : MemorySafeBenchmarkBase
{
    protected TOmni OmniCollection = default!;
    protected TBaseline BaselineCollection = default!;
    
    protected TKey[] TestKeys = default!;
    protected TValue[] TestValues = default!;
    protected int DataSize;
    
    /// <summary>
    /// Data sizes to test for the current profile
    /// </summary>
    [Params(50000)] // Updated: Use 50k for comprehensive pooling analysis
    public int Size { get; set; }
    
    public virtual void SetupComparison()
    {
        base.GlobalSetup();
        
        DataSize = Size;
        
        // Generate test data
        SetupTestData();
        
        // Setup collections with data (collections are created in these methods)
        SetupOmniCollection();
        SetupBaselineCollection();
        
        Console.WriteLine($"🔧 Comparison setup: {typeof(TOmni).Name} vs {typeof(TBaseline).Name} with {DataSize} items");
    }
    
    public virtual void CleanupComparison()
    {
        CleanupOmniCollection();
        CleanupBaselineCollection();
        
        base.GlobalCleanup();
    }
    
    /// <summary>
    /// Base method for Add operation on Omni collection - override in derived classes with [Benchmark] attribute
    /// </summary>
    public virtual object Omni_Add()
    {
        return PerformOmniAdd();
    }
    
    /// <summary>
    /// Base method for Add operation on .NET baseline collection - override in derived classes with [Benchmark] attribute
    /// </summary>
    public virtual object Baseline_Add()
    {
        return PerformBaselineAdd();
    }
    
    /// <summary>
    /// Base method for Get operation on Omni collection - override in derived classes with [Benchmark] attribute
    /// </summary>
    public virtual object Omni_Get()
    {
        return PerformOmniGet();
    }
    
    /// <summary>
    /// Base method for Get operation on .NET baseline collection - override in derived classes with [Benchmark] attribute
    /// </summary>
    public virtual object Baseline_Get()
    {
        return PerformBaselineGet();
    }
    
    /// <summary>
    /// Base method for Remove operation on Omni collection - override in derived classes with [Benchmark] attribute
    /// </summary>
    public virtual object Omni_Remove()
    {
        return PerformOmniRemove();
    }
    
    /// <summary>
    /// Base method for Remove operation on .NET baseline collection - override in derived classes with [Benchmark] attribute
    /// </summary>
    public virtual object Baseline_Remove()
    {
        return PerformBaselineRemove();
    }
    
    /// <summary>
    /// Base method for Enumerate operation on Omni collection - override in derived classes with [Benchmark] attribute
    /// </summary>
    public virtual int Omni_Enumerate()
    {
        return PerformOmniEnumerate();
    }
    
    /// <summary>
    /// Base method for Enumerate operation on .NET baseline collection - override in derived classes with [Benchmark] attribute
    /// </summary>
    public virtual int Baseline_Enumerate()
    {
        return PerformBaselineEnumerate();
    }
    
    // Abstract methods that concrete implementations must provide
    
    /// <summary>
    /// Generate test data for the benchmark
    /// </summary>
    protected abstract void SetupTestData();
    
    /// <summary>
    /// Initialize and populate the Omni collection
    /// </summary>
    protected abstract void SetupOmniCollection();
    
    /// <summary>
    /// Initialize and populate the baseline collection
    /// </summary>
    protected abstract void SetupBaselineCollection();
    
    /// <summary>
    /// Cleanup the Omni collection
    /// </summary>
    protected virtual void CleanupOmniCollection()
    {
        if (OmniCollection is IDisposable disposable)
            disposable.Dispose();
    }
    
    /// <summary>
    /// Cleanup the baseline collection
    /// </summary>
    protected virtual void CleanupBaselineCollection()
    {
        // Most .NET collections don't need explicit cleanup
    }
    
    // Operation implementations - override as needed
    
    protected abstract object PerformOmniAdd();
    protected abstract object PerformBaselineAdd();
    protected abstract object PerformOmniGet();
    protected abstract object PerformBaselineGet();
    protected abstract object PerformOmniRemove();
    protected abstract object PerformBaselineRemove();
    protected abstract int PerformOmniEnumerate();
    protected abstract int PerformBaselineEnumerate();
    
    /// <summary>
    /// Helper method to get a random test key
    /// </summary>
    protected TKey GetRandomKey()
    {
        var index = Random.Shared.Next(TestKeys.Length);
        return TestKeys[index];
    }
    
    /// <summary>
    /// Helper method to get a random test value
    /// </summary>
    protected TValue GetRandomValue()
    {
        var index = Random.Shared.Next(TestValues.Length);
        return TestValues[index];
    }
}
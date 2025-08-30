using BenchmarkDotNet.Attributes;
using System;
using System.Runtime;

namespace Omni.Collections.Benchmarks.Core;

/// <summary>
/// Base class for all RAM-safe benchmarks with controlled memory allocation
/// and automatic garbage collection management to prevent memory exhaustion.
/// </summary>
public abstract class MemorySafeBenchmarkBase
{
    private long _initialMemory;
    private const long MaxMemoryThresholdMB = 8192; // 8192 MB safety threshold
    
    /// <summary>
    /// Called before each benchmark iteration to ensure clean memory state
    /// </summary>
    [IterationSetup]
    public void IterationSetup()
    {
        // Force garbage collection before each iteration
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        // Record baseline memory usage
        _initialMemory = GC.GetTotalMemory(false);
        
        // Check if we're approaching memory limits
        CheckMemoryThreshold();
    }
    
    /// <summary>
    /// Called after each benchmark iteration to clean up and monitor memory
    /// </summary>
    [IterationCleanup] 
    public void IterationCleanup()
    {
        // Clean up any benchmark-specific resources
        CleanupIteration();
        
        // Force garbage collection after each iteration
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        // Monitor memory growth
        var currentMemory = GC.GetTotalMemory(false);
        var memoryGrowth = currentMemory - _initialMemory;
        
        // Log excessive memory growth
        if (memoryGrowth > MaxMemoryThresholdMB * 1024 * 1024)
        {
            Console.WriteLine($"⚠️ Memory growth detected: {memoryGrowth / (1024 * 1024)}MB");
        }
    }
    
    /// <summary>
    /// Called once before all benchmark iterations (override in derived class with [GlobalSetup])
    /// </summary>
    public virtual void GlobalSetup()
    {
        // Configure GC for benchmark performance
        GCSettings.LatencyMode = GCLatencyMode.Batch;
        
        Console.WriteLine($"🔧 Starting benchmarks with {GC.GetTotalMemory(false) / (1024 * 1024)}MB baseline memory");
    }
    
    /// <summary>
    /// Called once after all benchmark iterations (override in derived class with [GlobalCleanup])
    /// </summary>
    public virtual void GlobalCleanup()
    {
        // Final cleanup
        CleanupGlobal();
        
        // Restore GC settings
        GCSettings.LatencyMode = GCLatencyMode.Interactive;
        
        // Final garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        Console.WriteLine($"✅ Benchmarks completed with {GC.GetTotalMemory(false) / (1024 * 1024)}MB final memory");
    }
    
    /// <summary>
    /// Override this method to provide iteration-specific cleanup
    /// </summary>
    protected virtual void CleanupIteration()
    {
        // Default: no additional cleanup needed
    }
    
    /// <summary>
    /// Override this method to provide global cleanup
    /// </summary>
    protected virtual void CleanupGlobal()
    {
        // Default: no additional cleanup needed
    }
    
    /// <summary>
    /// Checks if memory usage is approaching dangerous levels
    /// </summary>
    private void CheckMemoryThreshold()
    {
        var currentMemory = GC.GetTotalMemory(false);
        var memoryMB = currentMemory / (1024 * 1024);
        
        if (memoryMB > MaxMemoryThresholdMB)
        {
            throw new InvalidOperationException(
                $"Memory threshold exceeded: {memoryMB}MB > {MaxMemoryThresholdMB}MB. " +
                "Benchmark parameters may be too large for RAM-safe operation.");
        }
    }
    
    /// <summary>
    /// Helper method to get current memory usage in MB
    /// </summary>
    protected long GetCurrentMemoryMB()
    {
        return GC.GetTotalMemory(false) / (1024 * 1024);
    }
    
    /// <summary>
    /// Helper method to force garbage collection (use sparingly)
    /// </summary>
    protected void ForceGarbageCollection()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}
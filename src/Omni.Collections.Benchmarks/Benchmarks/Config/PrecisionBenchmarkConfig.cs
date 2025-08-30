using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;

namespace Omni.Collections.Benchmarks.Config;

/// <summary>
/// Precision benchmark configuration designed to minimize error rates and variance.
/// Optimized for high statistical accuracy with reduced noise and stable measurements.
/// Runtime: 60-90 minutes for maximum precision.
/// MemoryDiagnoser automatically provides Gen0, Gen1, Gen2, and Allocated columns.
/// </summary>
public class PrecisionBenchmarkConfig : ManualConfig
{
    public PrecisionBenchmarkConfig()
    {
        // Ultra-stable job configuration for minimum error rates
        AddJob(Job.Default
            .WithToolchain(InProcessEmitToolchain.Instance)      // Stable in-process execution
            .WithWarmupCount(10)                                 // Extended warmup to stabilize JIT
            .WithIterationCount(20)                              // More iterations for statistical accuracy
            .WithLaunchCount(1)                                  // Single process to reduce variance
            .WithStrategy(RunStrategy.Throughput)                // Throughput strategy
            .WithGcForce(true)                                   // Force GC between benchmarks
            .WithGcServer(false)                                 // Use workstation GC for consistency
            .WithGcConcurrent(false)                             // Disable concurrent GC for predictability
            .WithPlatform(Platform.X64)                          // Force x64 for consistency
            .WithJit(Jit.RyuJit)                                 // Force RyuJIT
            .WithRuntime(CoreRuntime.Core80)                     // Force .NET 8.0
            .WithId("Precision")                                 // Custom job ID
        );
        
        // CRITICAL: Add default column providers to see memory columns (Gen0, Gen1, Gen2, Allocated)
        AddColumnProvider(DefaultColumnProviders.Instance);
        
        // Memory diagnostics for allocation tracking
        AddDiagnoser(MemoryDiagnoser.Default);
        
        // Logging and export
        AddLogger(ConsoleLogger.Default);
        AddExporter(MarkdownExporter.GitHub);
        
        // Essential columns for analysis
        AddColumn(StatisticColumn.Mean);
        AddColumn(StatisticColumn.Error);                        // Standard error
        AddColumn(StatisticColumn.StdDev);                       // Standard deviation
        AddColumn(StatisticColumn.Median);                       // Median value
        AddColumn(StatisticColumn.Min);                          // Minimum value
        AddColumn(StatisticColumn.Max);                          // Maximum value
        AddColumn(TargetMethodColumn.Method);                    // Method names
        AddColumn(BaselineRatioColumn.RatioMean);                // Performance ratios
        
        // Memory columns (Gen0, Gen1, Gen2, Allocated) are added automatically by MemoryDiagnoser
        
        // Summary style for detailed reporting
        WithSummaryStyle(BenchmarkDotNet.Reports.SummaryStyle.Default);
        
        // Add options to reduce system interference
        WithOptions(ConfigOptions.Default);
    }
}
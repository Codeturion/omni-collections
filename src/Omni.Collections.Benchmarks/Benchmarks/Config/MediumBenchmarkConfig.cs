using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;

namespace Omni.Collections.Benchmarks.Config;

/// <summary>
/// Medium benchmark configuration based on working pattern from SHA 5bb1e88.
/// Optimized for 5-10 minute total runtime with better statistical accuracy.
/// MemoryDiagnoser automatically provides Gen0, Gen1, Gen2, and Allocated columns.
/// </summary>
public class MediumBenchmarkConfig : ManualConfig
{
    public MediumBenchmarkConfig()
    {
        // Medium accuracy job configuration with working toolchain
        AddJob(Job.Default
            .WithToolchain(InProcessEmitToolchain.Instance)  // This was key in working version
            .WithWarmupCount(3)                              // More warmup
            .WithIterationCount(5)                           // More iterations
            .WithLaunchCount(1)                              // Single process
            .WithStrategy(RunStrategy.Throughput)            // Focus on throughput
            .WithGcForce(true)                               // Force GC between benchmarks
        );
        
        // CRITICAL: Add default column providers to see memory columns (Gen0, Gen1, Gen2, Allocated)
        AddColumnProvider(DefaultColumnProviders.Instance);
        
        // Memory diagnostics
        AddDiagnoser(MemoryDiagnoser.Default);
        
        // CRITICAL: Add logger and exporter (were missing)
        AddLogger(ConsoleLogger.Default);
        AddExporter(MarkdownExporter.GitHub);
        
        // Add columns including Method names and performance data
        AddColumn(StatisticColumn.Mean);
        AddColumn(StatisticColumn.Error);                  // Standard error
        AddColumn(StatisticColumn.StdDev);                 // Standard deviation
        AddColumn(RankColumn.Arabic);                      // Rank column
        AddColumn(TargetMethodColumn.Method);              // Shows "Omni_Add", "Baseline_Add", etc.
        AddColumn(BaselineRatioColumn.RatioMean);          // Shows performance ratios vs baseline
        
        // Memory columns (Gen0, Gen1, Gen2, Allocated) are added automatically by MemoryDiagnoser
        
        // Use simple summary style
        WithSummaryStyle(BenchmarkDotNet.Reports.SummaryStyle.Default);
    }
}
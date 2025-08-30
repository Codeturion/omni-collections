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
/// Fast benchmark configuration based on working SafeBenchmarkConfig from SHA 5bb1e88.
/// Optimized for 30-60 second total runtime with working report generation.
/// Solution: Inherit from DefaultConfig to get memory columns automatically.
/// </summary>
public class FastBenchmarkConfig : ManualConfig
{
    public FastBenchmarkConfig()
    {
        // Start with empty config and manually add what we need
        // This approach ensures we get MemoryDiagnoser columns
        
        // Use the working job configuration that generated reports successfully
        AddJob(Job.Default
            .WithToolchain(InProcessEmitToolchain.Instance)  // This was key in working version
            .WithWarmupCount(2)                              // Improved warmup for better accuracy
            .WithIterationCount(5)                           // More iterations for better statistics
            .WithLaunchCount(1)                              // Single process
            .WithStrategy(RunStrategy.Throughput)            // Focus on throughput
            .WithGcForce(true)                               // Force GC between benchmarks
        );
        
        // CRITICAL: Add default column providers to see memory columns (Gen0, Gen1, Gen2, Allocated)
        AddColumnProvider(DefaultColumnProviders.Instance);
        
        // Memory diagnostics - this automatically adds Gen0, Gen1, Gen2, Allocated columns
        AddDiagnoser(MemoryDiagnoser.Default);
        
        // CRITICAL: Add logger for progress visibility (was missing)
        AddLogger(ConsoleLogger.Default);
        
        // CRITICAL: Add explicit exporter with custom file naming
        AddExporter(MarkdownExporter.GitHub);
        
        // Add columns including Method names and memory diagnostics
        AddColumn(StatisticColumn.Mean);
        AddColumn(StatisticColumn.Error);                  // Standard error
        AddColumn(StatisticColumn.StdDev);                 // Standard deviation
        AddColumn(RankColumn.Arabic);                      // Rank column
        AddColumn(TargetMethodColumn.Method);              // This shows "Omni_Add", "Baseline_Add", etc.
        AddColumn(BaselineRatioColumn.RatioMean);          // Shows performance ratios vs baseline
        
        // Use simple summary style
        WithSummaryStyle(BenchmarkDotNet.Reports.SummaryStyle.Default);
        
        // Configure artifacts path for cleaner organization
        WithArtifactsPath("BenchmarkDotNet.Artifacts");
    }
}
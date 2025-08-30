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
/// Release benchmark configuration designed for comprehensive production-quality benchmarks.
/// Combines high statistical accuracy with complete memory analysis and detailed reporting.
/// Runtime: 45-75 minutes for production-ready benchmark reports.
/// Includes extensive memory diagnostics, allocation tracking, and GC analysis.
/// </summary>
public class ReleaseBenchmarkConfig : ManualConfig
{
    public ReleaseBenchmarkConfig()
    {
        // Production-quality job configuration balancing accuracy and runtime
        AddJob(Job.Default
                .WithToolchain(InProcessEmitToolchain.Instance)      // Stable in-process execution
                .WithWarmupCount(12)                                 // Balanced warmup for JIT stability
                .WithIterationCount(25)                              // High iteration count for statistical power
                .WithLaunchCount(1)                                  // Single process for consistency
                .WithStrategy(RunStrategy.Throughput)                // Throughput strategy for production analysis
                .WithGcForce(true)                                   // Force GC between benchmarks for clean state
                .WithGcServer(false)                                 // Use workstation GC for consistent measurements
                .WithGcConcurrent(false)                             // Disable concurrent GC for predictable allocation patterns
                .WithPlatform(Platform.X64)                          // Force x64 for production consistency
                .WithJit(Jit.RyuJit)                                 // Force RyuJIT for .NET 8+ performance
                .WithRuntime(CoreRuntime.Core80)                     // Target .NET 8.0 specifically
                .WithId("Release")                                   // Custom job ID for identification
                .WithInvocationCount(1)                              // Single invocation per iteration
                .WithUnrollFactor(1)                                 // No unrolling for measurement consistency
        );
        
        // CRITICAL: Add default column providers to see all memory columns (Gen0, Gen1, Gen2, Allocated)
        AddColumnProvider(DefaultColumnProviders.Instance);
        
        // Comprehensive memory diagnostics for allocation tracking and GC analysis
        AddDiagnoser(MemoryDiagnoser.Default);
        
        // Enhanced logging for production analysis
        AddLogger(ConsoleLogger.Default);
        
        // Multiple export formats for comprehensive reporting
        AddExporter(MarkdownExporter.GitHub);           // GitHub-formatted markdown
        AddExporter(HtmlExporter.Default);              // HTML reports for detailed analysis
        
        // Comprehensive statistical columns for production analysis
        AddColumn(StatisticColumn.Mean);                 // Average execution time
        AddColumn(StatisticColumn.Error);                // Standard error for confidence
        AddColumn(StatisticColumn.StdDev);               // Standard deviation for variance analysis
        AddColumn(StatisticColumn.Median);               // Median for distribution analysis
        AddColumn(StatisticColumn.Min);                  // Minimum execution time
        AddColumn(StatisticColumn.Max);                  // Maximum execution time
        AddColumn(StatisticColumn.P90);                  // 90th percentile
        AddColumn(StatisticColumn.P95);                  // 95th percentile
        AddColumn(TargetMethodColumn.Method);            // Method names for identification
        AddColumn(BaselineRatioColumn.RatioMean);        // Performance ratios vs baseline
        
        // Memory allocation columns (Gen0, Gen1, Gen2, Allocated) automatically included by MemoryDiagnoser
        
        // Enhanced summary style for production reporting
        WithSummaryStyle(BenchmarkDotNet.Reports.SummaryStyle.Default
            .WithSizeUnit(BenchmarkDotNet.Columns.SizeUnit.B)
        );
        
        // Production configuration options
        WithOptions(ConfigOptions.Default | ConfigOptions.StopOnFirstError);
        
        // Configure artifacts path for organized results
        WithArtifactsPath("BenchmarkDotNet.Artifacts");
        
        AddValidator(BenchmarkDotNet.Validators.JitOptimizationsValidator.DontFailOnError);
        AddValidator(BenchmarkDotNet.Validators.ReturnValueValidator.FailOnError);
    }
}
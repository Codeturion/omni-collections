using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;

namespace Omni.Collections.Benchmarks.Configs;

/// <summary>
/// Smoke profile: validates the suite compiles and produces results. Iteration
/// counts are intentionally too low for statistical confidence — error margins
/// will be large. Use this in CI to catch breakage; do NOT quote numbers from
/// this profile as performance characteristics.
///
/// Out-of-process toolchain is preserved (unlike the previous broken setup that
/// used InProcessEmit) so cross-benchmark state pollution is impossible.
/// </summary>
public class SmokeConfig : ManualConfig
{
    public SmokeConfig()
    {
        AddJob(Job.Default
            .WithWarmupCount(1)
            .WithIterationCount(3)
            .WithInvocationCount(16)
            .WithUnrollFactor(1)
            .WithLaunchCount(1)
            .WithId("Smoke"));

        AddDiagnoser(MemoryDiagnoser.Default);

        AddColumnProvider(DefaultColumnProviders.Instance);
        AddColumn(BaselineRatioColumn.RatioMean);

        AddExporter(MarkdownExporter.GitHub);
        AddLogger(ConsoleLogger.Default);

        WithArtifactsPath("BenchmarkDotNet.Artifacts");
        WithOption(ConfigOptions.JoinSummary, true);
    }
}

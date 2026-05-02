using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;

namespace Omni.Collections.Benchmarks.Configs;

/// <summary>
/// Rigorous profile for release validation. Higher warmup/iteration counts plus
/// multiple process launches catch JIT/GC variance that single-launch runs miss.
/// Use this profile when publishing perf numbers tied to a release.
/// </summary>
public class RigorousConfig : ManualConfig
{
    public RigorousConfig()
    {
        AddJob(Job.Default
            .WithWarmupCount(10)
            .WithIterationCount(25)
            .WithLaunchCount(3)
            .WithId("Rigorous"));

        AddDiagnoser(MemoryDiagnoser.Default);
        AddDiagnoser(ThreadingDiagnoser.Default);

        AddColumnProvider(DefaultColumnProviders.Instance);
        AddColumn(BaselineRatioColumn.RatioMean);
        AddColumn(StatisticColumn.Median);
        AddColumn(StatisticColumn.P95);
        AddColumn(StatisticColumn.OperationsPerSecond);

        AddExporter(MarkdownExporter.GitHub);
        AddExporter(CsvExporter.Default);
        AddExporter(HtmlExporter.Default);

        AddLogger(ConsoleLogger.Default);

        WithArtifactsPath("BenchmarkDotNet.Artifacts");
        WithOption(ConfigOptions.JoinSummary, true);
    }
}

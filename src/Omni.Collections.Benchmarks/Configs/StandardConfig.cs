using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

namespace Omni.Collections.Benchmarks.Configs;

/// <summary>
/// Standard benchmark configuration. BenchmarkDotNet defaults for warmup and
/// iteration counts (6 warmup / 15 iterations / 1 launch), out-of-process
/// toolchain, MemoryDiagnoser always on. This is the profile whose numbers
/// are intended to be quoted.
/// </summary>
public class StandardConfig : ManualConfig
{
    public StandardConfig()
    {
        AddJob(Job.Default);

        AddDiagnoser(MemoryDiagnoser.Default);

        AddColumnProvider(DefaultColumnProviders.Instance);
        AddColumn(BaselineRatioColumn.RatioMean);
        AddColumn(StatisticColumn.Median);
        AddColumn(StatisticColumn.OperationsPerSecond);

        AddExporter(MarkdownExporter.GitHub);
        AddExporter(CsvExporter.Default);
        AddExporter(HtmlExporter.Default);

        AddLogger(ConsoleLogger.Default);

        WithSummaryStyle(SummaryStyle.Default);
        WithArtifactsPath("BenchmarkDotNet.Artifacts");
        WithOption(ConfigOptions.JoinSummary, true);
    }
}

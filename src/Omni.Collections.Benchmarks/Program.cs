using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using Omni.Collections.Benchmarks.Config;
using Omni.Collections.Benchmarks.Core;
using System;
using System.Linq;

namespace Omni.Collections.Benchmarks;

class Program
{

    static void Main(string[] args)
    {
        Console.WriteLine("🚀 Omni Collections Benchmarks v2.0");
        Console.WriteLine("====================================");
        Console.WriteLine();
        var profile = GetBenchmarkProfile(args);
        var config = GetBenchmarkConfig(profile);
        Console.WriteLine($"📊 Running benchmark profile: {profile}");
        Console.WriteLine($"⏱️ Estimated runtime: {GetEstimatedRuntime(profile)}");
        Console.WriteLine($"💾 Memory safety: {GetMemoryLimit(profile)}");
        Console.WriteLine();
        var benchmarkTypes = new[]
        {
            typeof(LinearStructureBenchmarks.BoundedListVsList),
            typeof(LinearStructureBenchmarks.FastQueueVsQueue),
            typeof(LinearStructureBenchmarks.MaxHeapVsSortedSet),
            typeof(LinearStructureBenchmarks.MinHeapVsSortedSet),
            typeof(LinearStructureBenchmarks.PooledListVsList),
            typeof(LinearStructureBenchmarks.PooledStackVsStack),
            typeof(HybridStructureBenchmarks.LinkedDictionaryVsDict),
            typeof(HybridStructureBenchmarks.CounterDictionaryVsDict),
            typeof(HybridStructureBenchmarks.QueueDictionaryVsDict),
            typeof(HybridStructureBenchmarks.CircularDictionaryVsDict),
            typeof(HybridStructureBenchmarks.DequeDictionaryVsDict),
            typeof(HybridStructureBenchmarks.ConcurrentLinkedDictionaryVsDict),
            typeof(HybridStructureBenchmarks.LinkedMultiMapVsDict),
            typeof(HybridStructureBenchmarks.GraphDictionaryVsDict),
            typeof(HybridStructureBenchmarks.PredictiveDictionaryVsDict),
            // typeof(HybridStructureBenchmarks.SecureHashingBenchmark) expensive
            typeof(GridStructureBenchmarks.BitGrid2DVsBoolArray),
            typeof(GridStructureBenchmarks.HexGrid2DVsDict),
            typeof(GridStructureBenchmarks.LayeredGrid2DVsArray3D),
            typeof(SpatialStructureBenchmarks.QuadTreeVsList),
            typeof(SpatialStructureBenchmarks.SpatialHashGridVsDict),
            typeof(SpatialStructureBenchmarks.KDTreeVsList),
            typeof(SpatialStructureBenchmarks.KDTreeDistanceMetricsBenchmark),
            typeof(SpatialStructureBenchmarks.OctTreeVsList),
            typeof(SpatialStructureBenchmarks.BloomRTreeDictionaryVsDictionary),
            typeof(SpatialStructureBenchmarks.BloomRTreeScalingBenchmark),
            typeof(SpatialStructureBenchmarks.TemporalSpatialHashGridVsManual),
            typeof(ProbabilisticStructureBenchmarks.BloomFilterVsHashSet),
            typeof(ProbabilisticStructureBenchmarks.CountMinSketchVsDict),
            typeof(ProbabilisticStructureBenchmarks.HyperLogLogVsHashSet),
            typeof(ProbabilisticStructureBenchmarks.TDigestVsList),
            typeof(ProbabilisticStructureBenchmarks.DigestStreamingVsP2Quantile),
            typeof(ProbabilisticStructureBenchmarks.BloomDictionaryVsDict),
            typeof(ReactiveStructureBenchmarks.ObservableHashSetVsHashSet),
            typeof(ReactiveStructureBenchmarks.ObservableListVsList),
            typeof(TemporalStructureBenchmarks.TimelineArrayVsDict)
        };
        var selectedBenchmarks = FilterBenchmarks(args, benchmarkTypes);
        if (selectedBenchmarks.Length == 0)
        {
            Console.WriteLine("❌ No benchmarks selected. Available options:");
            Console.WriteLine("Categories:");
            Console.WriteLine("   --linear        Run linear structure benchmarks (6 classes)");
            Console.WriteLine("   --hybrid        Run hybrid structure benchmarks (10 classes)");
            Console.WriteLine("   --grid          Run grid/2D structure benchmarks (3 classes)");
            Console.WriteLine("   --spatial       Run spatial structure benchmarks (8 classes)");
            Console.WriteLine("   --probabilistic Run probabilistic structure benchmarks (6 classes)");
            Console.WriteLine("   --reactive      Run reactive structure benchmarks (2 classes)");
            Console.WriteLine("   --temporal      Run temporal structure benchmarks (1 class)");
            Console.WriteLine("   --all           Run all available benchmarks");
            Console.WriteLine("\nProfiles:");
            Console.WriteLine("   --fast          Quick validation (30-60 seconds)");
            Console.WriteLine("   --medium        Reliable testing (5-10 minutes)");
            Console.WriteLine("   --hard          Comprehensive analysis (30-60 minutes)");
            Console.WriteLine("   --precision     Ultra-precise measurements (60-90 minutes)");
            Console.WriteLine("   --release       Production-quality benchmarks (45-75 minutes)");
            return;
        }
        Console.WriteLine($"🎯 Running {selectedBenchmarks.Length} benchmark classes:");
        foreach (var type in selectedBenchmarks)
        {
            Console.WriteLine($"   • {type.Name}");
        }
        Console.WriteLine();
        try
        {
            var filteredArgs = args.Where(arg => !IsCustomArgument(arg)).ToArray();
            if (filteredArgs.Length == 0)
            {
                filteredArgs = new[] { "*" };
            }
            var switcher = new BenchmarkSwitcher(selectedBenchmarks);
            var summary = switcher.Run(filteredArgs, config);
            Console.WriteLine();
            if (summary != null && summary.Any())
            {
                Console.WriteLine("✅ Benchmarks completed successfully!");
                Console.WriteLine($"📊 Ran {summary.Count()} benchmark summaries");
                Console.WriteLine($"📄 Results saved to: BenchmarkDotNet.Artifacts/results/");
                var firstSummary = summary.FirstOrDefault();
                if (firstSummary != null && firstSummary.Reports.Any())
                {
                    var fastestReport = firstSummary.Reports.OrderBy(r => r.ResultStatistics?.Mean ?? double.MaxValue).FirstOrDefault();
                    if (fastestReport?.ResultStatistics != null)
                    {
                        Console.WriteLine($"⚡ Fastest: {fastestReport.BenchmarkCase.DisplayInfo} - {fastestReport.ResultStatistics.Mean:F2}ns");
                    }
                }
            }
            else
            {
                Console.WriteLine("⚠️ Benchmarks completed but no results generated.");
                Console.WriteLine("💡 Try running with: dotnet run -- --fast --linear --filter *BoundedList*");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error running benchmarks: {ex.Message}");
            Console.WriteLine($"💡 Try using --fast profile for quicker testing");
        }
    }

    private static BenchmarkProfile GetBenchmarkProfile(string[] args)
    {
        if (args.Contains("--fast", StringComparer.OrdinalIgnoreCase))
            return BenchmarkProfile.Fast;
        if (args.Contains("--medium", StringComparer.OrdinalIgnoreCase))
            return BenchmarkProfile.Medium;
        if (args.Contains("--hard", StringComparer.OrdinalIgnoreCase))
            return BenchmarkProfile.Hard;
        if (args.Contains("--precision", StringComparer.OrdinalIgnoreCase))
            return BenchmarkProfile.Precision;
        if (args.Contains("--release", StringComparer.OrdinalIgnoreCase))
            return BenchmarkProfile.Release;
        return BenchmarkProfile.Fast;
    }

    private static IConfig GetBenchmarkConfig(BenchmarkProfile profile)
    {
        return profile switch
        {
            BenchmarkProfile.Fast => new FastBenchmarkConfig(),
            BenchmarkProfile.Medium => new MediumBenchmarkConfig(),
            BenchmarkProfile.Hard => new HardBenchmarkConfig(),
            BenchmarkProfile.Precision => new PrecisionBenchmarkConfig(),
            BenchmarkProfile.Release => new ReleaseBenchmarkConfig(),
            _ => new FastBenchmarkConfig()
        };
    }

    private static string GetEstimatedRuntime(BenchmarkProfile profile)
    {
        return profile switch
        {
            BenchmarkProfile.Fast => "30-60 seconds",
            BenchmarkProfile.Medium => "5-10 minutes",
            BenchmarkProfile.Hard => "30-60 minutes",
            BenchmarkProfile.Precision => "60-90 minutes",
            BenchmarkProfile.Release => "45-75 minutes",
            _ => "30-60 seconds"
        };
    }

    private static string GetMemoryLimit(BenchmarkProfile profile)
    {
        return profile switch
        {
            BenchmarkProfile.Fast => "8GB max per benchmark",
            BenchmarkProfile.Medium => "8GB max per benchmark",
            BenchmarkProfile.Hard => "8GB max per benchmark",
            _ => "8GB max per benchmark"
        };
    }

    private static Type[] FilterBenchmarks(string[] args, Type[] allBenchmarks)
    {
        if (args.Contains("--all", StringComparer.OrdinalIgnoreCase))
            return allBenchmarks;
        if (args.Contains("--linear", StringComparer.OrdinalIgnoreCase))
        {
            return allBenchmarks.Where(t => t.FullName!.Contains("Linear")).ToArray();
        }
        if (args.Contains("--hybrid", StringComparer.OrdinalIgnoreCase))
        {
            return allBenchmarks.Where(t => t.FullName!.Contains("Hybrid")).ToArray();
        }
        if (args.Contains("--grid", StringComparer.OrdinalIgnoreCase))
        {
            return allBenchmarks.Where(t => t.FullName!.Contains("Grid")).ToArray();
        }
        if (args.Contains("--spatial", StringComparer.OrdinalIgnoreCase))
        {
            return allBenchmarks.Where(t => t.FullName!.Contains("Spatial")).ToArray();
        }
        if (args.Contains("--probabilistic", StringComparer.OrdinalIgnoreCase))
        {
            return allBenchmarks.Where(t => t.FullName!.Contains("Probabilistic")).ToArray();
        }
        if (args.Contains("--reactive", StringComparer.OrdinalIgnoreCase))
        {
            return allBenchmarks.Where(t => t.FullName!.Contains("Reactive")).ToArray();
        }
        if (args.Contains("--temporal", StringComparer.OrdinalIgnoreCase))
        {
            return allBenchmarks.Where(t => t.FullName!.Contains("Temporal")).ToArray();
        }
        return Array.Empty<Type>();
    }

    private static bool IsCustomArgument(string arg)
    {
        var customArgs = new[] { "--fast", "--medium", "--hard", "--precision", "--release", "--linear", "--hybrid", "--grid", "--spatial", "--probabilistic", "--reactive", "--temporal", "--all" };
        return customArgs.Contains(arg, StringComparer.OrdinalIgnoreCase);
    }
}

public enum BenchmarkProfile
{
    Fast,
    Medium,
    Hard,
    Precision,
    Release
}
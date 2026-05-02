using System;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Omni.Collections.Benchmarks.Configs;

namespace Omni.Collections.Benchmarks;

internal static class Program
{
    private const string SmokeFlag = "--smoke";
    private const string StandardFlag = "--standard";
    private const string RigorousFlag = "--rigorous";

    private static int Main(string[] args)
    {
        if (args.Any(a => a is "-h" or "--help") && !args.Any(IsProfileArg))
        {
            PrintUsage();
        }

        var config = SelectConfig(args);
        var bdnArgs = args.Where(a => !IsProfileArg(a)).ToArray();

        var switcher = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly);
        var summaries = switcher.Run(bdnArgs, config);

        return summaries == null ? 1 : 0;
    }

    private static IConfig SelectConfig(string[] args)
    {
        if (args.Contains(SmokeFlag, StringComparer.OrdinalIgnoreCase))
            return new SmokeConfig();
        if (args.Contains(RigorousFlag, StringComparer.OrdinalIgnoreCase))
            return new RigorousConfig();
        return new StandardConfig();
    }

    private static bool IsProfileArg(string arg)
        => arg.Equals(SmokeFlag, StringComparison.OrdinalIgnoreCase)
        || arg.Equals(StandardFlag, StringComparison.OrdinalIgnoreCase)
        || arg.Equals(RigorousFlag, StringComparison.OrdinalIgnoreCase);

    private static void PrintUsage()
    {
        Console.WriteLine("Omni.Collections benchmarks");
        Console.WriteLine();
        Console.WriteLine("Profiles (mutually exclusive, one wins):");
        Console.WriteLine("  --smoke       Fast smoke check (~1 min). NOT for performance claims.");
        Console.WriteLine("  --standard    Standard run (5-10 min per category). Default.");
        Console.WriteLine("  --rigorous    Multi-launch high-iteration (30-60 min per category).");
        Console.WriteLine();
        Console.WriteLine("Selection (any combination — passed through to BenchmarkDotNet):");
        Console.WriteLine("  --list flat                         List all benchmarks");
        Console.WriteLine("  --filter '*BoundedList*'            Match by class/method name");
        Console.WriteLine("  --anyCategories=Linear,Hybrid       Select by category");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  dotnet run -c Release -- --smoke --anyCategories=Linear");
        Console.WriteLine("  dotnet run -c Release -- --filter '*BloomFilter*'");
        Console.WriteLine("  dotnet run -c Release -- --rigorous --anyCategories=Probabilistic");
        Console.WriteLine();
        Console.WriteLine("Outputs land under BenchmarkDotNet.Artifacts/results/");
        Console.WriteLine();
    }
}

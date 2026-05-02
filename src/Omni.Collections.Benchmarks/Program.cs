using BenchmarkDotNet.Running;

namespace Omni.Collections.Benchmarks;

internal static class Program
{
    private static int Main(string[] args)
    {
        var switcher = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly);
        var summaries = switcher.Run(args);
        return summaries == null ? 1 : 0;
    }
}

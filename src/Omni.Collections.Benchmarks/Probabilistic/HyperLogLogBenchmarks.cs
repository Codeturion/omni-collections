using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Omni.Collections.Benchmarks.Common;
using Omni.Collections.Probabilistic;

namespace Omni.Collections.Benchmarks.Probabilistic;

[BenchmarkCategory(Categories.Probabilistic)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[MemoryDiagnoser]
public class HyperLogLogBenchmarks
{
    private const int OpsPerIteration = 32768;

    [Params(Sizes.Small, Sizes.Medium, Sizes.Large)]
    public int N;

    private string[] _values = null!;

    private HyperLogLog<string> _omniMut = null!;
    private HashSet<string> _baselineMut = null!;
    private int _addCounter;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _values = RandomData.Strings(N + OpsPerIteration);
    }

    [IterationSetup(Targets = new[] { nameof(Omni_Add), nameof(Baseline_Add) })]
    public void ResetForAdd()
    {
        _omniMut = new HyperLogLog<string>();
        _baselineMut = new HashSet<string>(N + OpsPerIteration);
        for (int i = 0; i < N; i++)
        {
            _omniMut.Add(_values[i]);
            _baselineMut.Add(_values[i]);
        }
        _addCounter = N;
    }

    /// Claim: HyperLogLog.Add cost is constant regardless of cardinality; HashSet.Add grows with collisions.
    [Benchmark, BenchmarkCategory("Add"), InvocationCount(OpsPerIteration)]
    public bool Omni_Add()
    {
        _omniMut.Add(_values[_addCounter++]);
        return true;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Add"), InvocationCount(OpsPerIteration)]
    public bool Baseline_Add()
    {
        _baselineMut.Add(_values[_addCounter++]);
        return true;
    }

    /// Claim: HyperLogLog uses constant ~16KB memory regardless of N; HashSet allocates proportional to N.
    [Benchmark, BenchmarkCategory("Fill"), InvocationCount(1)]
    public HyperLogLog<string> Omni_Fill()
    {
        var c = new HyperLogLog<string>();
        for (int i = 0; i < N; i++)
            c.Add(_values[i]);
        return c;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Fill"), InvocationCount(1)]
    public HashSet<string> Baseline_Fill()
    {
        var c = new HashSet<string>();
        for (int i = 0; i < N; i++)
            c.Add(_values[i]);
        return c;
    }
}

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
public class CountMinSketchBenchmarks
{
    private const int OpsPerIteration = 32768;
    private const int SketchWidth = 4096;
    private const int SketchDepth = 5;

    [Params(Sizes.Small, Sizes.Medium, Sizes.Large)]
    public int N;

    private string[] _values = null!;
    private const int ReadIndexMask = 1023;
    private int _readCounter;

    private CountMinSketch<string> _omniFilled = null!;
    private Dictionary<string, int> _baselineFilled = null!;

    private CountMinSketch<string> _omniMut = null!;
    private Dictionary<string, int> _baselineMut = null!;
    private int _addCounter;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _values = RandomData.Strings(N + OpsPerIteration);

        _omniFilled = new CountMinSketch<string>(SketchWidth, SketchDepth);
        _baselineFilled = new Dictionary<string, int>(N);
        for (int i = 0; i < N; i++)
        {
            _omniFilled.Add(_values[i]);
            _baselineFilled[_values[i]] = (_baselineFilled.TryGetValue(_values[i], out var c) ? c : 0) + 1;
        }
    }

    [IterationSetup(Targets = new[] { nameof(Omni_Add), nameof(Baseline_Add) })]
    public void ResetForAdd()
    {
        _omniMut = new CountMinSketch<string>(SketchWidth, SketchDepth);
        _baselineMut = new Dictionary<string, int>(N + OpsPerIteration);
        for (int i = 0; i < N; i++)
        {
            _omniMut.Add(_values[i]);
            _baselineMut[_values[i]] = 1;
        }
        _addCounter = N;
    }

    /// Claim: CountMinSketch.Add increments d hash buckets; Dictionary lookup-then-update is roughly comparable.
    [Benchmark, BenchmarkCategory("Add"), InvocationCount(OpsPerIteration)]
    public bool Omni_Add()
    {
        _omniMut.Add(_values[_addCounter++]);
        return true;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Add"), InvocationCount(OpsPerIteration)]
    public bool Baseline_Add()
    {
        var k = _values[_addCounter++];
        _baselineMut[k] = (_baselineMut.TryGetValue(k, out var c) ? c : 0) + 1;
        return true;
    }

    /// Claim: CountMinSketch.EstimateCount is constant; Dictionary lookup is hash + equality check.
    [Benchmark, BenchmarkCategory("Estimate")]
    public uint Omni_Estimate()
    {
        return _omniFilled.EstimateCount(_values[_readCounter++ & (_values.Length - 1)]);
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Estimate")]
    public int Baseline_Estimate()
    {
        return _baselineFilled.TryGetValue(_values[_readCounter++ & (_values.Length - 1)], out var c) ? c : 0;
    }

    /// Claim: CountMinSketch uses fixed width*depth*4 bytes; Dictionary scales with cardinality.
    [Benchmark, BenchmarkCategory("Fill"), InvocationCount(1)]
    public CountMinSketch<string> Omni_Fill()
    {
        var c = new CountMinSketch<string>(SketchWidth, SketchDepth);
        for (int i = 0; i < N; i++)
            c.Add(_values[i]);
        return c;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Fill"), InvocationCount(1)]
    public Dictionary<string, int> Baseline_Fill()
    {
        var c = new Dictionary<string, int>();
        for (int i = 0; i < N; i++)
            c[_values[i]] = (c.TryGetValue(_values[i], out var x) ? x : 0) + 1;
        return c;
    }
}

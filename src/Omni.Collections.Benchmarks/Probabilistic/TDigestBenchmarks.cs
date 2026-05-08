using System;
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
public class DigestBenchmarks
{
    private const int OpsPerIteration = 32768;

    [Params(Sizes.Small, Sizes.Medium, Sizes.Large)]
    public int N;

    [Params(100.0, 1000.0)]
    public double Compression;

    private double[] _values = null!;

    private Digest _omniFilled = null!;
    private double[] _baselineSorted = null!;

    private Digest _omniMut = null!;
    private List<double> _baselineMut = null!;
    private int _addCounter;

    private Digest _mergeLeft = null!;
    private Digest _mergeRight = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var rng = new Random(RandomData.Seed);
        _values = new double[N + OpsPerIteration];
        for (int i = 0; i < _values.Length; i++)
            _values[i] = rng.NextDouble() * 1000.0;

        _omniFilled = new Digest(Compression);
        _baselineSorted = new double[N];
        for (int i = 0; i < N; i++)
        {
            _omniFilled.Add(_values[i]);
            _baselineSorted[i] = _values[i];
        }
        Array.Sort(_baselineSorted);
    }

    [IterationSetup(Targets = new[] { nameof(Omni_Add), nameof(Baseline_Add) })]
    public void ResetForAdd()
    {
        _omniMut = new Digest(Compression);
        _baselineMut = new List<double>(N + OpsPerIteration);
        for (int i = 0; i < N; i++)
        {
            _omniMut.Add(_values[i]);
            _baselineMut.Add(_values[i]);
        }
        _addCounter = N;
    }

    /// Claim: Digest.Add maintains a compressed digest; List.Add stores raw values for later sorting.
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

    /// Claim: Digest.Quantile is O(centroids); sorted-array quantile is O(1) but requires sorting up front.
    [Benchmark, BenchmarkCategory("Quantile")]
    public double Omni_Quantile() => _omniFilled.Quantile(0.95);

    [Benchmark(Baseline = true), BenchmarkCategory("Quantile")]
    public double Baseline_Quantile() => _baselineSorted[(int)(_baselineSorted.Length * 0.95)];

    [IterationSetup(Targets = new[] { nameof(Omni_Merge) })]
    public void ResetForMerge()
    {
        // Build two distinct digests of N values each so Merge has to actually
        // combine c₁ + c₂ centroids, exercising the bulk-rebuild path.
        _mergeLeft = new Digest(Compression);
        _mergeRight = new Digest(Compression);
        for (int i = 0; i < N; i++)
        {
            _mergeLeft.Add(_values[i]);
            _mergeRight.Add(_values[(i + N) % _values.Length]);
        }
    }

    /// Claim: Digest.Merge is O(c₁+c₂) — linear merge of both digests' sorted centroid lists,
    /// then bulk-rebuild the skip list. The bulk-build replaces the previous per-centroid
    /// Add path which gave O((c₁+c₂) log(c₁+c₂)).
    [Benchmark, BenchmarkCategory("Merge"), InvocationCount(1)]
    public Digest Omni_Merge()
    {
        _mergeLeft.Merge(_mergeRight);
        return _mergeLeft;
    }

    /// Claim: Digest uses bounded memory regardless of N; storing all values + sorting allocates O(N).
    [Benchmark, BenchmarkCategory("Fill"), InvocationCount(1)]
    public Digest Omni_Fill()
    {
        var c = new Digest(Compression);
        for (int i = 0; i < N; i++)
            c.Add(_values[i]);
        return c;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Fill"), InvocationCount(1)]
    public double[] Baseline_Fill()
    {
        var c = new double[N];
        for (int i = 0; i < N; i++)
            c[i] = _values[i];
        Array.Sort(c);
        return c;
    }
}

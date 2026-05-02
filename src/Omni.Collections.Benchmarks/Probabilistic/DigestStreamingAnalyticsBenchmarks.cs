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
public class DigestStreamingAnalyticsBenchmarks
{
    private const int OpsPerIteration = 8192;

    [Params(Sizes.Small, Sizes.Medium)]
    public int N;

    private double[] _values = null!;

    private DigestStreamingAnalytics<double> _omniMut = null!;
    private List<double> _baselineMut = null!;
    private int _addCounter;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var rng = new Random(RandomData.Seed);
        _values = new double[N + OpsPerIteration];
        for (int i = 0; i < _values.Length; i++)
            _values[i] = rng.NextDouble() * 1000.0;
    }

    [IterationSetup(Targets = new[] { nameof(Omni_Add), nameof(Baseline_Add) })]
    public void ResetForAdd()
    {
        _omniMut?.Dispose();
        _omniMut = new DigestStreamingAnalytics<double>(TimeSpan.FromMinutes(10), v => v);
        _baselineMut = new List<double>(N + OpsPerIteration);
        for (int i = 0; i < N; i++)
        {
            _omniMut.Add(_values[i]);
            _baselineMut.Add(_values[i]);
        }
        _addCounter = N;
    }

    /// Claim: DigestStreamingAnalytics.Add maintains time-windowed digest; List.Add just appends.
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

    private DigestStreamingAnalytics<double>? _omniFillResult;

    [IterationCleanup(Targets = new[] { nameof(Omni_Fill) })]
    public void DisposeFill() { _omniFillResult?.Dispose(); _omniFillResult = null; }

    /// Claim: Time-windowed digest uses bounded memory regardless of N; raw List grows linearly.
    [Benchmark, BenchmarkCategory("Fill"), InvocationCount(1)]
    public DigestStreamingAnalytics<double> Omni_Fill()
    {
        _omniFillResult = new DigestStreamingAnalytics<double>(TimeSpan.FromMinutes(10), v => v);
        for (int i = 0; i < N; i++)
            _omniFillResult.Add(_values[i]);
        return _omniFillResult;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Fill"), InvocationCount(1)]
    public List<double> Baseline_Fill()
    {
        var c = new List<double>();
        for (int i = 0; i < N; i++)
            c.Add(_values[i]);
        return c;
    }
}

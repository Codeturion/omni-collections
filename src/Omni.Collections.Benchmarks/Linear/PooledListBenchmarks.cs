using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Omni.Collections.Benchmarks.Common;
using Omni.Collections.Linear;

namespace Omni.Collections.Benchmarks.Linear;

[BenchmarkCategory(Categories.Linear)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[MemoryDiagnoser]
public class PooledListBenchmarks
{
    [Params(Sizes.Small, Sizes.Medium, Sizes.Large)]
    public int N;

    private string[] _values = null!;
    private int[] _readIndices = null!;
    private const int ReadIndexMask = 1023;
    private int _readCounter;

    private PooledList<string> _omniFilled = null!;
    private List<string> _baselineFilled = null!;

    private PooledList<string> _omniEmpty = null!;
    private List<string> _baselineEmpty = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _values = RandomData.Strings(N);
        _readIndices = RandomData.IntsInRange(ReadIndexMask + 1, 0, N);

        _omniFilled = new PooledList<string>(N);
        _baselineFilled = new List<string>(N);
        for (int i = 0; i < N; i++)
        {
            _omniFilled.Add(_values[i]);
            _baselineFilled.Add(_values[i]);
        }
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _omniFilled.Dispose();
        _omniEmpty?.Dispose();
    }

    [IterationSetup(Targets = new[] { nameof(Omni_AddN), nameof(Baseline_AddN) })]
    public void ResetForAdd()
    {
        _omniEmpty?.Dispose();
        _omniEmpty = new PooledList<string>(N);
        _baselineEmpty = new List<string>(N);
    }

    /// Claim: PooledList.Add amortizes growth via ArrayPool, eliminating GC pressure vs List.Add resizing.
    [Benchmark, BenchmarkCategory("Add"), InvocationCount(1)]
    public PooledList<string> Omni_AddN()
    {
        for (int i = 0; i < _values.Length; i++)
            _omniEmpty.Add(_values[i]);
        return _omniEmpty;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Add"), InvocationCount(1)]
    public List<string> Baseline_AddN()
    {
        for (int i = 0; i < _values.Length; i++)
            _baselineEmpty.Add(_values[i]);
        return _baselineEmpty;
    }

    /// Claim: PooledList indexer matches List<T> indexer (both wrap a contiguous T[]).
    [Benchmark, BenchmarkCategory("Indexer")]
    public string Omni_Indexer()
    {
        var idx = _readIndices[_readCounter++ & ReadIndexMask];
        return _omniFilled[idx];
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Indexer")]
    public string Baseline_Indexer()
    {
        var idx = _readIndices[_readCounter++ & ReadIndexMask];
        return _baselineFilled[idx];
    }

    /// Claim: PooledList struct enumerator is at least as fast as List<T>.Enumerator.
    [Benchmark, BenchmarkCategory("Enumerate")]
    public int Omni_Enumerate()
    {
        int sum = 0;
        foreach (var s in _omniFilled)
            sum += s.Length;
        return sum;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Enumerate")]
    public int Baseline_Enumerate()
    {
        int sum = 0;
        foreach (var s in _baselineFilled)
            sum += s.Length;
        return sum;
    }
}

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
    private const int OpsPerIteration = 32768;

    [Params(Sizes.Small, Sizes.Medium, Sizes.Large)]
    public int N;

    private string[] _values = null!;
    private int[] _readIndices = null!;
    private const int ReadIndexMask = 1023;
    private int _readCounter;

    private PooledList<string> _omniFilled = null!;
    private List<string> _baselineFilled = null!;

    private PooledList<string> _omniMut = null!;
    private List<string> _baselineMut = null!;
    private int _addCounter;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _values = RandomData.Strings(N + OpsPerIteration);
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
        _omniMut?.Dispose();
    }

    [IterationSetup(Targets = new[] { nameof(Omni_Add), nameof(Baseline_Add) })]
    public void ResetForAdd()
    {
        _omniMut?.Dispose();
        _omniMut = new PooledList<string>(N + OpsPerIteration);
        _baselineMut = new List<string>(N + OpsPerIteration);
        for (int i = 0; i < N; i++)
        {
            _omniMut.Add(_values[i]);
            _baselineMut.Add(_values[i]);
        }
        _addCounter = N;
    }

    /// Claim: PooledList.Add (capacity sufficient) matches List<T>.Add per-op cost.
    [Benchmark, BenchmarkCategory("Add"), InvocationCount(OpsPerIteration)]
    public int Omni_Add()
    {
        _omniMut.Add(_values[_addCounter++]);
        return _omniMut.Count;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Add"), InvocationCount(OpsPerIteration)]
    public int Baseline_Add()
    {
        _baselineMut.Add(_values[_addCounter++]);
        return _baselineMut.Count;
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

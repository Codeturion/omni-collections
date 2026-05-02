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

    // ============= Fill (bulk allocation comparison) =============
    // PooledList rents the backing array from ArrayPool. The realistic-usage
    // comparison is "PooledList default vs List<T> default" — PooledList rents
    // pool arrays (alloc trends to zero after warmup as the pool caches arrays);
    // List<T> heap-allocates each resize.

    private PooledList<string>? _omniFillResult;

    [IterationCleanup(Targets = new[] { nameof(Omni_Fill) })]
    public void DisposeOmniFill()
    {
        _omniFillResult?.Dispose();
        _omniFillResult = null;
    }

    /// Claim: PooledList default-cap fill rents arrays from ArrayPool, eliminating GC pressure vs List<T> resizing.
    [Benchmark, BenchmarkCategory("Fill"), InvocationCount(1)]
    public PooledList<string> Omni_Fill()
    {
        _omniFillResult = new PooledList<string>();
        for (int i = 0; i < N; i++)
            _omniFillResult.Add(_values[i]);
        return _omniFillResult;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Fill"), InvocationCount(1)]
    public List<string> Baseline_Fill()
    {
        var c = new List<string>();
        for (int i = 0; i < N; i++)
            c.Add(_values[i]);
        return c;
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

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Omni.Collections.Benchmarks.Common;
using Omni.Collections.Linear;

namespace Omni.Collections.Benchmarks.Linear;

[BenchmarkCategory(Categories.Linear)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[MemoryDiagnoser]
public class BoundedListBenchmarks
{
    [Params(Sizes.Small, Sizes.Medium, Sizes.Large)]
    public int N;

    private string[] _values = null!;
    private int[] _readIndices = null!;
    private const int ReadIndexMask = 1023;
    private int _readCounter;

    private BoundedList<string> _omniFilled = null!;
    private System.Collections.Generic.List<string> _baselineFilled = null!;

    private BoundedList<string> _omniEmpty = null!;
    private System.Collections.Generic.List<string> _baselineEmpty = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _values = RandomData.Strings(N);
        _readIndices = RandomData.IntsInRange(ReadIndexMask + 1, 0, N);

        _omniFilled = new BoundedList<string>(N);
        _baselineFilled = new System.Collections.Generic.List<string>(N);
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

    // ============= Add (bulk) =============
    // InvocationCount=1 forces one full bulk-add per iteration.
    // IterationSetup recreates collections each iteration so state is fresh.

    [IterationSetup(Targets = new[] { nameof(Omni_AddN), nameof(Baseline_AddN) })]
    public void ResetForAdd()
    {
        _omniEmpty?.Dispose();
        _omniEmpty = new BoundedList<string>(N);
        _baselineEmpty = new System.Collections.Generic.List<string>(N);
    }

    /// Claim: BoundedList.Add with capacity reserved is competitive with List.Add.
    [Benchmark, BenchmarkCategory("Add"), InvocationCount(1)]
    public BoundedList<string> Omni_AddN()
    {
        for (int i = 0; i < _values.Length; i++)
            _omniEmpty.Add(_values[i]);
        return _omniEmpty;
    }

    /// Baseline: List.Add with capacity reserved.
    [Benchmark(Baseline = true), BenchmarkCategory("Add"), InvocationCount(1)]
    public System.Collections.Generic.List<string> Baseline_AddN()
    {
        for (int i = 0; i < _values.Length; i++)
            _baselineEmpty.Add(_values[i]);
        return _baselineEmpty;
    }

    // ============= Indexer (read-only) =============
    // Pre-computed _readIndices avoid Random.Next overhead inside the measured path.

    /// Claim: BoundedList indexer matches List<T> indexer (both wrap a contiguous T[]).
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

    // ============= Enumerate =============

    /// Claim: BoundedList struct enumerator is at least as fast as List<T>.Enumerator.
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

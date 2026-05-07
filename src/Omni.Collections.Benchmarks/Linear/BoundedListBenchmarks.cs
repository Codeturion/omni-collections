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
    // High invocation count puts per-op timing and per-op allocation above BDN's
    // measurement floor (~400 B for the harness itself). Each iteration runs this
    // many Add calls; IterationSetup recreates collections + pre-fills N items.
    private const int OpsPerIteration = 32768;

    [Params(Sizes.Small, Sizes.Medium, Sizes.Large)]
    public int N;

    private string[] _values = null!;
    private int[] _readIndices = null!;
    private const int ReadIndexMask = 1023;
    private int _readCounter;

    private BoundedList<string> _omniFilled = null!;
    private System.Collections.Generic.List<string> _baselineFilled = null!;

    private BoundedList<string> _omniMut = null!;
    private System.Collections.Generic.List<string> _baselineMut = null!;
    private int _addCounter;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _values = RandomData.Strings(N + OpsPerIteration);
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
        _omniMut?.Dispose();
    }

    // ============= Add (per-op via InvocationCount=32768) =============
    // IterationSetup creates fresh collections with capacity N + OpsPerIteration,
    // pre-fills N items, and resets the counter so 32768 Adds fit without resizing.
    // Mean reported = per-Add cost; Allocated reported = per-Add bytes.

    [IterationSetup(Targets = new[] { nameof(Omni_Add), nameof(Baseline_Add) })]
    public void ResetForAdd()
    {
        _omniMut?.Dispose();
        _omniMut = new BoundedList<string>(N + OpsPerIteration);
        _baselineMut = new System.Collections.Generic.List<string>(N + OpsPerIteration);
        for (int i = 0; i < N; i++)
        {
            _omniMut.Add(_values[i]);
            _baselineMut.Add(_values[i]);
        }
        _addCounter = N;
    }

    /// Claim: BoundedList.Add (capacity sufficient, no resize) matches List<T>.Add per-op cost.
    [Benchmark, BenchmarkCategory("Add")]
    [InvocationCount(OpsPerIteration)]
    public int Omni_Add()
    {
        _omniMut.Add(_values[_addCounter++]);
        return _omniMut.Count;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Add")]
    [InvocationCount(OpsPerIteration)]
    public int Baseline_Add()
    {
        _baselineMut.Add(_values[_addCounter++]);
        return _baselineMut.Count;
    }

    // ============= Fill (bulk allocation comparison) =============
    // Demonstrates the realistic populate cost. BoundedList constructor takes an
    // explicit capacity so the comparison is "presize vs grow" — the BoundedList
    // path allocates one array of size N up front; the List path resizes ~log2(N)
    // times. Allocation column on this benchmark is the value-prop signal.

    /// Claim: BoundedList sized to N allocates one array; List<T>() growing to N allocates ~log2(N) intermediate arrays.
    [Benchmark, BenchmarkCategory("Fill"), InvocationCount(1)]
    public BoundedList<string> Omni_Fill()
    {
        var c = new BoundedList<string>(N);
        for (int i = 0; i < N; i++)
            c.Add(_values[i]);
        return c;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Fill"), InvocationCount(1)]
    public System.Collections.Generic.List<string> Baseline_Fill()
    {
        var c = new System.Collections.Generic.List<string>();
        for (int i = 0; i < N; i++)
            c.Add(_values[i]);
        return c;
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

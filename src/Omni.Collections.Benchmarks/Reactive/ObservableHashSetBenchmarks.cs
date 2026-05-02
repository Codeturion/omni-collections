using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Omni.Collections.Benchmarks.Common;
using Omni.Collections.Reactive;

namespace Omni.Collections.Benchmarks.Reactive;

[BenchmarkCategory(Categories.Reactive)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[MemoryDiagnoser]
public class ObservableHashSetBenchmarks
{
    // No direct BCL analog (ObservableCollection only wraps List, not Set).
    // Benchmark vs HashSet to expose the cost of observability over a plain set.
    private const int OpsPerIteration = 32768;

    [Params(Sizes.Small, Sizes.Medium, Sizes.Large)]
    public int N;

    private string[] _values = null!;
    private string[] _readValues = null!;
    private const int ReadIndexMask = 1023;
    private int _readCounter;

    private ObservableHashSet<string> _omniFilled = null!;
    private HashSet<string> _baselineFilled = null!;

    private ObservableHashSet<string> _omniMut = null!;
    private HashSet<string> _baselineMut = null!;
    private int _addCounter;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _values = RandomData.Strings(N + OpsPerIteration);
        _readValues = RandomData.Strings(ReadIndexMask + 1, seed: RandomData.Seed + 1);

        _omniFilled = new ObservableHashSet<string>(N);
        _baselineFilled = new HashSet<string>(N);
        for (int i = 0; i < N; i++)
        {
            _omniFilled.Add(_values[i]);
            _baselineFilled.Add(_values[i]);
        }
    }

    [IterationSetup(Targets = new[] { nameof(Omni_Add), nameof(Baseline_Add) })]
    public void ResetForAdd()
    {
        _omniMut = new ObservableHashSet<string>(N + OpsPerIteration);
        _baselineMut = new HashSet<string>(N + OpsPerIteration);
        for (int i = 0; i < N; i++)
        {
            _omniMut.Add(_values[i]);
            _baselineMut.Add(_values[i]);
        }
        _addCounter = N;
    }

    /// Claim: ObservableHashSet.Add (no subscriber) cost overhead vs HashSet.Add.
    [Benchmark, BenchmarkCategory("Add"), InvocationCount(OpsPerIteration)]
    public bool Omni_Add()
    {
        var r = _omniMut.Add(_values[_addCounter++]);
        return r;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Add"), InvocationCount(OpsPerIteration)]
    public bool Baseline_Add()
    {
        var r = _baselineMut.Add(_values[_addCounter++]);
        return r;
    }

    /// Claim: ObservableHashSet.Contains matches HashSet.Contains (no notification cost on read).
    [Benchmark, BenchmarkCategory("Contains")]
    public bool Omni_Contains()
    {
        return _omniFilled.Contains(_readValues[_readCounter++ & ReadIndexMask]);
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Contains")]
    public bool Baseline_Contains()
    {
        return _baselineFilled.Contains(_readValues[_readCounter++ & ReadIndexMask]);
    }

    /// Claim: ObservableHashSet Fill cost includes notification scaffolding allocation.
    [Benchmark, BenchmarkCategory("Fill"), InvocationCount(1)]
    public ObservableHashSet<string> Omni_Fill()
    {
        var c = new ObservableHashSet<string>();
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

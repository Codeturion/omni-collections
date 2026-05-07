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
public class ObservableListBenchmarks
{
    private const int OpsPerIteration = 32768;

    [Params(Sizes.Small, Sizes.Medium, Sizes.Large)]
    public int N;

    private string[] _values = null!;

    private ObservableList<string> _omniMut = null!;
    private System.Collections.ObjectModel.ObservableCollection<string> _baselineMut = null!;
    private int _addCounter;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _values = RandomData.Strings(N + OpsPerIteration);
    }

    [IterationSetup(Targets = new[] { nameof(Omni_Add), nameof(Baseline_Add) })]
    public void ResetForAdd()
    {
        _omniMut = new ObservableList<string>(N + OpsPerIteration);
        _baselineMut = new System.Collections.ObjectModel.ObservableCollection<string>();
        for (int i = 0; i < N; i++)
        {
            _omniMut.Add(_values[i]);
            _baselineMut.Add(_values[i]);
        }
        _addCounter = N;
    }

    /// Claim: ObservableList.Add (no subscriber) cost vs BCL ObservableCollection<T>.Add (no subscriber).
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

    /// Claim: ObservableList Fill from default capacity vs ObservableCollection growth.
    [Benchmark, BenchmarkCategory("Fill"), InvocationCount(1)]
    public ObservableList<string> Omni_Fill()
    {
        var c = new ObservableList<string>();
        for (int i = 0; i < N; i++)
            c.Add(_values[i]);
        return c;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Fill"), InvocationCount(1)]
    public System.Collections.ObjectModel.ObservableCollection<string> Baseline_Fill()
    {
        var c = new System.Collections.ObjectModel.ObservableCollection<string>();
        for (int i = 0; i < N; i++)
            c.Add(_values[i]);
        return c;
    }
}

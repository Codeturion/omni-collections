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
public class MaxHeapBenchmarks
{
    private static readonly IComparer<int> DescendingComparer = Comparer<int>.Create((a, b) => b.CompareTo(a));

    [Params(Sizes.Small, Sizes.Medium, Sizes.Large)]
    public int N;

    private int[] _values = null!;

    private MaxHeap<int> _omniFilled = null!;
    private PriorityQueue<int, int> _baselineFilled = null!;

    private MaxHeap<int> _omniEmpty = null!;
    private PriorityQueue<int, int> _baselineEmpty = null!;

    private MaxHeap<int> _omniDrain = null!;
    private PriorityQueue<int, int> _baselineDrain = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _values = RandomData.Ints(N);

        _omniFilled = new MaxHeap<int>(N);
        _baselineFilled = new PriorityQueue<int, int>(N, DescendingComparer);
        for (int i = 0; i < N; i++)
        {
            _omniFilled.Insert(_values[i]);
            _baselineFilled.Enqueue(_values[i], _values[i]);
        }
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _omniFilled.Dispose();
        _omniEmpty?.Dispose();
        _omniDrain?.Dispose();
    }

    [IterationSetup(Targets = new[] { nameof(Omni_InsertN), nameof(Baseline_InsertN) })]
    public void ResetForInsert()
    {
        _omniEmpty?.Dispose();
        _omniEmpty = new MaxHeap<int>(N);
        _baselineEmpty = new PriorityQueue<int, int>(N, DescendingComparer);
    }

    [IterationSetup(Targets = new[] { nameof(Omni_ExtractMaxN), nameof(Baseline_ExtractMaxN) })]
    public void RefillForExtract()
    {
        _omniDrain?.Dispose();
        _omniDrain = new MaxHeap<int>(N);
        _baselineDrain = new PriorityQueue<int, int>(N, DescendingComparer);
        for (int i = 0; i < N; i++)
        {
            _omniDrain.Insert(_values[i]);
            _baselineDrain.Enqueue(_values[i], _values[i]);
        }
    }

    /// Claim: MaxHeap.Insert is at least as fast as PriorityQueue with reverse comparer (both O(log n)).
    [Benchmark, BenchmarkCategory("Insert"), InvocationCount(1)]
    public int Omni_InsertN()
    {
        for (int i = 0; i < _values.Length; i++)
            _omniEmpty.Insert(_values[i]);
        return _omniEmpty.Count;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Insert"), InvocationCount(1)]
    public int Baseline_InsertN()
    {
        for (int i = 0; i < _values.Length; i++)
            _baselineEmpty.Enqueue(_values[i], _values[i]);
        return _baselineEmpty.Count;
    }

    /// Claim: MaxHeap.ExtractMax is competitive with PriorityQueue.Dequeue under reverse comparer.
    [Benchmark, BenchmarkCategory("ExtractMax"), InvocationCount(1)]
    public int Omni_ExtractMaxN()
    {
        int last = 0;
        while (_omniDrain.Count > 0)
            last = _omniDrain.ExtractMax();
        return last;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("ExtractMax"), InvocationCount(1)]
    public int Baseline_ExtractMaxN()
    {
        int last = 0;
        while (_baselineDrain.Count > 0)
            last = _baselineDrain.Dequeue();
        return last;
    }

    /// Claim: MaxHeap.PeekMax is O(1) and matches the equivalent PriorityQueue.Peek.
    [Benchmark, BenchmarkCategory("Peek")]
    public int Omni_PeekMax() => _omniFilled.PeekMax();

    [Benchmark(Baseline = true), BenchmarkCategory("Peek")]
    public int Baseline_Peek() => _baselineFilled.Peek();
}

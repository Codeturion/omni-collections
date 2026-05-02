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
public class MinHeapBenchmarks
{
    [Params(Sizes.Small, Sizes.Medium, Sizes.Large)]
    public int N;

    private int[] _values = null!;

    private MinHeap<int> _omniFilled = null!;
    private PriorityQueue<int, int> _baselineFilled = null!;

    private MinHeap<int> _omniEmpty = null!;
    private PriorityQueue<int, int> _baselineEmpty = null!;

    private MinHeap<int> _omniDrain = null!;
    private PriorityQueue<int, int> _baselineDrain = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _values = RandomData.Ints(N);

        _omniFilled = new MinHeap<int>(N);
        _baselineFilled = new PriorityQueue<int, int>(N);
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
        _omniEmpty = new MinHeap<int>(N);
        _baselineEmpty = new PriorityQueue<int, int>(N);
    }

    [IterationSetup(Targets = new[] { nameof(Omni_ExtractMinN), nameof(Baseline_ExtractMinN) })]
    public void RefillForExtract()
    {
        _omniDrain?.Dispose();
        _omniDrain = new MinHeap<int>(N);
        _baselineDrain = new PriorityQueue<int, int>(N);
        for (int i = 0; i < N; i++)
        {
            _omniDrain.Insert(_values[i]);
            _baselineDrain.Enqueue(_values[i], _values[i]);
        }
    }

    /// Claim: MinHeap.Insert is at least as fast as PriorityQueue.Enqueue (both O(log n)).
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

    /// Claim: MinHeap.ExtractMin is competitive with PriorityQueue.Dequeue draining all elements in order.
    [Benchmark, BenchmarkCategory("ExtractMin"), InvocationCount(1)]
    public int Omni_ExtractMinN()
    {
        int last = 0;
        while (_omniDrain.Count > 0)
            last = _omniDrain.ExtractMin();
        return last;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("ExtractMin"), InvocationCount(1)]
    public int Baseline_ExtractMinN()
    {
        int last = 0;
        while (_baselineDrain.Count > 0)
            last = _baselineDrain.Dequeue();
        return last;
    }

    /// Claim: MinHeap.PeekMin matches PriorityQueue.Peek (both O(1) read of root).
    [Benchmark, BenchmarkCategory("Peek")]
    public int Omni_PeekMin() => _omniFilled.PeekMin();

    [Benchmark(Baseline = true), BenchmarkCategory("Peek")]
    public int Baseline_Peek() => _baselineFilled.Peek();
}

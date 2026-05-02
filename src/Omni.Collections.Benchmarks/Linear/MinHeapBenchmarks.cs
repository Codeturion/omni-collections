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
    private const int OpsPerIteration = 32768;

    [Params(Sizes.Small, Sizes.Medium, Sizes.Large)]
    public int N;

    private int[] _values = null!;

    private MinHeap<int> _omniFilled = null!;
    private PriorityQueue<int, int> _baselineFilled = null!;

    private MinHeap<int> _omniMut = null!;
    private PriorityQueue<int, int> _baselineMut = null!;
    private int _opCounter;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _values = RandomData.Ints(N + OpsPerIteration);

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
        _omniMut?.Dispose();
    }

    [IterationSetup(Targets = new[] { nameof(Omni_Insert), nameof(Baseline_Insert) })]
    public void ResetForInsert()
    {
        _omniMut?.Dispose();
        _omniMut = new MinHeap<int>(N + OpsPerIteration);
        _baselineMut = new PriorityQueue<int, int>(N + OpsPerIteration);
        for (int i = 0; i < N; i++)
        {
            _omniMut.Insert(_values[i]);
            _baselineMut.Enqueue(_values[i], _values[i]);
        }
        _opCounter = N;
    }

    [IterationSetup(Targets = new[] { nameof(Omni_ExtractMin), nameof(Baseline_ExtractMin) })]
    public void RefillForExtract()
    {
        _omniMut?.Dispose();
        _omniMut = new MinHeap<int>(N + OpsPerIteration);
        _baselineMut = new PriorityQueue<int, int>(N + OpsPerIteration);
        for (int i = 0; i < N + OpsPerIteration; i++)
        {
            _omniMut.Insert(_values[i]);
            _baselineMut.Enqueue(_values[i], _values[i]);
        }
    }

    /// Claim: MinHeap.Insert is at least as fast as PriorityQueue.Enqueue (both O(log n)).
    [Benchmark, BenchmarkCategory("Insert"), InvocationCount(OpsPerIteration)]
    public int Omni_Insert()
    {
        _omniMut.Insert(_values[_opCounter++]);
        return _omniMut.Count;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Insert"), InvocationCount(OpsPerIteration)]
    public int Baseline_Insert()
    {
        var v = _values[_opCounter++];
        _baselineMut.Enqueue(v, v);
        return _baselineMut.Count;
    }

    /// Claim: MinHeap.ExtractMin is competitive with PriorityQueue.Dequeue (both O(log n)).
    [Benchmark, BenchmarkCategory("ExtractMin"), InvocationCount(OpsPerIteration)]
    public int Omni_ExtractMin() => _omniMut.ExtractMin();

    [Benchmark(Baseline = true), BenchmarkCategory("ExtractMin"), InvocationCount(OpsPerIteration)]
    public int Baseline_ExtractMin() => _baselineMut.Dequeue();

    // ============= Fill (bulk allocation comparison) =============

    private MinHeap<int>? _omniFillResult;

    [IterationCleanup(Targets = new[] { nameof(Omni_Fill) })]
    public void DisposeOmniFill()
    {
        _omniFillResult?.Dispose();
        _omniFillResult = null;
    }

    /// Claim: MinHeap default-cap fill builds a heap with fewer or equal allocations vs PriorityQueue growing.
    [Benchmark, BenchmarkCategory("Fill"), InvocationCount(1)]
    public MinHeap<int> Omni_Fill()
    {
        _omniFillResult = new MinHeap<int>();
        for (int i = 0; i < N; i++)
            _omniFillResult.Insert(_values[i]);
        return _omniFillResult;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Fill"), InvocationCount(1)]
    public PriorityQueue<int, int> Baseline_Fill()
    {
        var c = new PriorityQueue<int, int>();
        for (int i = 0; i < N; i++)
            c.Enqueue(_values[i], _values[i]);
        return c;
    }

    /// Claim: MinHeap.PeekMin matches PriorityQueue.Peek (both O(1) read of root).
    [Benchmark, BenchmarkCategory("Peek")]
    public int Omni_PeekMin() => _omniFilled.PeekMin();

    [Benchmark(Baseline = true), BenchmarkCategory("Peek")]
    public int Baseline_Peek() => _baselineFilled.Peek();
}

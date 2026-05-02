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
    private const int OpsPerIteration = 32768;

    private static readonly IComparer<int> DescendingComparer = Comparer<int>.Create((a, b) => b.CompareTo(a));

    [Params(Sizes.Small, Sizes.Medium, Sizes.Large)]
    public int N;

    private int[] _values = null!;

    private MaxHeap<int> _omniFilled = null!;
    private PriorityQueue<int, int> _baselineFilled = null!;

    private MaxHeap<int> _omniMut = null!;
    private PriorityQueue<int, int> _baselineMut = null!;
    private int _opCounter;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _values = RandomData.Ints(N + OpsPerIteration);

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
        _omniMut?.Dispose();
    }

    [IterationSetup(Targets = new[] { nameof(Omni_Insert), nameof(Baseline_Insert) })]
    public void ResetForInsert()
    {
        _omniMut?.Dispose();
        _omniMut = new MaxHeap<int>(N + OpsPerIteration);
        _baselineMut = new PriorityQueue<int, int>(N + OpsPerIteration, DescendingComparer);
        for (int i = 0; i < N; i++)
        {
            _omniMut.Insert(_values[i]);
            _baselineMut.Enqueue(_values[i], _values[i]);
        }
        _opCounter = N;
    }

    [IterationSetup(Targets = new[] { nameof(Omni_ExtractMax), nameof(Baseline_ExtractMax) })]
    public void RefillForExtract()
    {
        _omniMut?.Dispose();
        _omniMut = new MaxHeap<int>(N + OpsPerIteration);
        _baselineMut = new PriorityQueue<int, int>(N + OpsPerIteration, DescendingComparer);
        for (int i = 0; i < N + OpsPerIteration; i++)
        {
            _omniMut.Insert(_values[i]);
            _baselineMut.Enqueue(_values[i], _values[i]);
        }
    }

    /// Claim: MaxHeap.Insert is at least as fast as PriorityQueue with descending comparer.
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

    /// Claim: MaxHeap.ExtractMax is competitive with PriorityQueue.Dequeue under descending comparer.
    [Benchmark, BenchmarkCategory("ExtractMax"), InvocationCount(OpsPerIteration)]
    public int Omni_ExtractMax() => _omniMut.ExtractMax();

    [Benchmark(Baseline = true), BenchmarkCategory("ExtractMax"), InvocationCount(OpsPerIteration)]
    public int Baseline_ExtractMax() => _baselineMut.Dequeue();

    /// Claim: MaxHeap.PeekMax is O(1) and matches PriorityQueue.Peek under descending comparer.
    [Benchmark, BenchmarkCategory("Peek")]
    public int Omni_PeekMax() => _omniFilled.PeekMax();

    [Benchmark(Baseline = true), BenchmarkCategory("Peek")]
    public int Baseline_Peek() => _baselineFilled.Peek();
}

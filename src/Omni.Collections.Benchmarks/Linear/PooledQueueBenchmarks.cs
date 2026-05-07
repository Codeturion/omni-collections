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
public class PooledQueueBenchmarks
{
    private const int OpsPerIteration = 32768;

    [Params(Sizes.Small, Sizes.Medium, Sizes.Large)]
    public int N;

    private string[] _values = null!;

    private PooledQueue<string> _omniFilled = null!;
    private Queue<string> _baselineFilled = null!;

    private PooledQueue<string> _omniMut = null!;
    private Queue<string> _baselineMut = null!;
    private int _opCounter;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _values = RandomData.Strings(N + OpsPerIteration);

        _omniFilled = new PooledQueue<string>(N);
        _baselineFilled = new Queue<string>(N);
        for (int i = 0; i < N; i++)
        {
            _omniFilled.Enqueue(_values[i]);
            _baselineFilled.Enqueue(_values[i]);
        }
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _omniFilled.Dispose();
        _omniMut?.Dispose();
    }

    [IterationSetup(Targets = new[] { nameof(Omni_Enqueue), nameof(Baseline_Enqueue) })]
    public void ResetForEnqueue()
    {
        _omniMut?.Dispose();
        _omniMut = new PooledQueue<string>(N + OpsPerIteration);
        _baselineMut = new Queue<string>(N + OpsPerIteration);
        for (int i = 0; i < N; i++)
        {
            _omniMut.Enqueue(_values[i]);
            _baselineMut.Enqueue(_values[i]);
        }
        _opCounter = N;
    }

    [IterationSetup(Targets = new[] { nameof(Omni_Dequeue), nameof(Baseline_Dequeue) })]
    public void RefillForDequeue()
    {
        _omniMut?.Dispose();
        _omniMut = new PooledQueue<string>(N + OpsPerIteration);
        _baselineMut = new Queue<string>(N + OpsPerIteration);
        for (int i = 0; i < N + OpsPerIteration; i++)
        {
            _omniMut.Enqueue(_values[i]);
            _baselineMut.Enqueue(_values[i]);
        }
    }

    /// Claim: PooledQueue.Enqueue (capacity sufficient) is at least as fast as Queue.Enqueue.
    [Benchmark, BenchmarkCategory("Enqueue"), InvocationCount(OpsPerIteration)]
    public int Omni_Enqueue()
    {
        _omniMut.Enqueue(_values[_opCounter++]);
        return _omniMut.Count;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Enqueue"), InvocationCount(OpsPerIteration)]
    public int Baseline_Enqueue()
    {
        _baselineMut.Enqueue(_values[_opCounter++]);
        return _baselineMut.Count;
    }

    /// Claim: PooledQueue.Dequeue is at least as fast as Queue.Dequeue.
    [Benchmark, BenchmarkCategory("Dequeue"), InvocationCount(OpsPerIteration)]
    public string Omni_Dequeue() => _omniMut.Dequeue();

    [Benchmark(Baseline = true), BenchmarkCategory("Dequeue"), InvocationCount(OpsPerIteration)]
    public string Baseline_Dequeue() => _baselineMut.Dequeue();

    // ============= Fill (bulk allocation comparison) =============

    /// Claim: PooledQueue default-cap fill is faster and allocates less than Queue<T> growing through resizes.
    [Benchmark, BenchmarkCategory("Fill"), InvocationCount(1)]
    public PooledQueue<string> Omni_Fill()
    {
        var c = new PooledQueue<string>();
        for (int i = 0; i < N; i++)
            c.Enqueue(_values[i]);
        return c;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Fill"), InvocationCount(1)]
    public Queue<string> Baseline_Fill()
    {
        var c = new Queue<string>();
        for (int i = 0; i < N; i++)
            c.Enqueue(_values[i]);
        return c;
    }

    /// Claim: PooledQueue.Peek matches Queue<T>.Peek (both read head index).
    [Benchmark, BenchmarkCategory("Peek")]
    public string Omni_Peek() => _omniFilled.Peek();

    [Benchmark(Baseline = true), BenchmarkCategory("Peek")]
    public string Baseline_Peek() => _baselineFilled.Peek();

    /// Claim: PooledQueue struct enumerator is at least as fast as Queue<T>.GetEnumerator.
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

    // ============= FillAndDrain (sustained reuse — pool win surfaces here) =============
    // Plain Fill captures first-allocation only. FillAndDrain builds + drains a
    // container, then disposes — pooled buffer returns to ArrayPool and is rented
    // by the next iteration. Allocated trends to zero for pooled type past warmup.

    /// Claim: PooledQueue.CreateWithArrayPool sustained fill+drain reuses pooled
    /// buffers — Allocated should drop near-zero past the first warmup iteration.
    [Benchmark, BenchmarkCategory("FillAndDrain"), InvocationCount(1)]
    public int Omni_FillAndDrain()
    {
        var c = PooledQueue<string>.CreateWithArrayPool();
        for (int i = 0; i < N; i++)
            c.Enqueue(_values[i]);
        int sum = 0;
        while (c.Count > 0)
            sum += c.Dequeue().Length;
        c.Dispose();
        return sum;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("FillAndDrain"), InvocationCount(1)]
    public int Baseline_FillAndDrain()
    {
        var c = new Queue<string>();
        for (int i = 0; i < N; i++)
            c.Enqueue(_values[i]);
        int sum = 0;
        while (c.Count > 0)
            sum += c.Dequeue().Length;
        return sum;
    }
}

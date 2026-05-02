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
public class FastQueueBenchmarks
{
    private const int OpsPerIteration = 32768;

    [Params(Sizes.Small, Sizes.Medium, Sizes.Large)]
    public int N;

    private string[] _values = null!;

    private FastQueue<string> _omniFilled = null!;
    private Queue<string> _baselineFilled = null!;

    private FastQueue<string> _omniMut = null!;
    private Queue<string> _baselineMut = null!;
    private int _opCounter;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _values = RandomData.Strings(N + OpsPerIteration);

        _omniFilled = new FastQueue<string>(N);
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
        _omniMut = new FastQueue<string>(N + OpsPerIteration);
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
        _omniMut = new FastQueue<string>(N + OpsPerIteration);
        _baselineMut = new Queue<string>(N + OpsPerIteration);
        for (int i = 0; i < N + OpsPerIteration; i++)
        {
            _omniMut.Enqueue(_values[i]);
            _baselineMut.Enqueue(_values[i]);
        }
    }

    /// Claim: FastQueue.Enqueue (capacity sufficient) is at least as fast as Queue.Enqueue.
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

    /// Claim: FastQueue.Dequeue is at least as fast as Queue.Dequeue.
    [Benchmark, BenchmarkCategory("Dequeue"), InvocationCount(OpsPerIteration)]
    public string Omni_Dequeue() => _omniMut.Dequeue();

    [Benchmark(Baseline = true), BenchmarkCategory("Dequeue"), InvocationCount(OpsPerIteration)]
    public string Baseline_Dequeue() => _baselineMut.Dequeue();

    // ============= Fill (bulk allocation comparison) =============

    /// Claim: FastQueue default-cap fill is faster and allocates less than Queue<T> growing through resizes.
    [Benchmark, BenchmarkCategory("Fill"), InvocationCount(1)]
    public FastQueue<string> Omni_Fill()
    {
        var c = new FastQueue<string>();
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

    /// Claim: FastQueue.Peek matches Queue<T>.Peek (both read head index).
    [Benchmark, BenchmarkCategory("Peek")]
    public string Omni_Peek() => _omniFilled.Peek();

    [Benchmark(Baseline = true), BenchmarkCategory("Peek")]
    public string Baseline_Peek() => _baselineFilled.Peek();

    /// Claim: FastQueue struct enumerator is at least as fast as Queue<T>.GetEnumerator.
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

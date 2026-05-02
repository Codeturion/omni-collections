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
    [Params(Sizes.Small, Sizes.Medium, Sizes.Large)]
    public int N;

    private string[] _values = null!;

    private FastQueue<string> _omniFilled = null!;
    private Queue<string> _baselineFilled = null!;

    private FastQueue<string> _omniEmpty = null!;
    private Queue<string> _baselineEmpty = null!;

    private FastQueue<string> _omniDrain = null!;
    private Queue<string> _baselineDrain = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _values = RandomData.Strings(N);

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
        _omniEmpty?.Dispose();
        _omniDrain?.Dispose();
    }

    [IterationSetup(Targets = new[] { nameof(Omni_EnqueueN), nameof(Baseline_EnqueueN) })]
    public void ResetForEnqueue()
    {
        _omniEmpty?.Dispose();
        _omniEmpty = new FastQueue<string>(N);
        _baselineEmpty = new Queue<string>(N);
    }

    [IterationSetup(Targets = new[] { nameof(Omni_DequeueN), nameof(Baseline_DequeueN) })]
    public void RefillForDequeue()
    {
        _omniDrain?.Dispose();
        _omniDrain = new FastQueue<string>(N);
        _baselineDrain = new Queue<string>(N);
        for (int i = 0; i < N; i++)
        {
            _omniDrain.Enqueue(_values[i]);
            _baselineDrain.Enqueue(_values[i]);
        }
    }

    /// Claim: FastQueue.Enqueue with capacity reserved is faster than Queue.Enqueue.
    [Benchmark, BenchmarkCategory("Enqueue"), InvocationCount(1)]
    public FastQueue<string> Omni_EnqueueN()
    {
        for (int i = 0; i < _values.Length; i++)
            _omniEmpty.Enqueue(_values[i]);
        return _omniEmpty;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Enqueue"), InvocationCount(1)]
    public Queue<string> Baseline_EnqueueN()
    {
        for (int i = 0; i < _values.Length; i++)
            _baselineEmpty.Enqueue(_values[i]);
        return _baselineEmpty;
    }

    /// Claim: FastQueue.Dequeue is at least as fast as Queue.Dequeue.
    [Benchmark, BenchmarkCategory("Dequeue"), InvocationCount(1)]
    public string Omni_DequeueN()
    {
        string last = null!;
        while (_omniDrain.Count > 0)
            last = _omniDrain.Dequeue();
        return last;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Dequeue"), InvocationCount(1)]
    public string Baseline_DequeueN()
    {
        string last = null!;
        while (_baselineDrain.Count > 0)
            last = _baselineDrain.Dequeue();
        return last;
    }

    /// Claim: FastQueue.Peek matches Queue<T>.Peek (both read head index).
    [Benchmark, BenchmarkCategory("Peek")]
    public string Omni_Peek() => _omniFilled.Peek();

    [Benchmark(Baseline = true), BenchmarkCategory("Peek")]
    public string Baseline_Peek() => _baselineFilled.Peek();

    /// Claim: FastQueue struct enumerator avoids the boxed allocation Queue<T> produces in foreach.
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

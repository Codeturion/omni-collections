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
public class PooledStackBenchmarks
{
    [Params(Sizes.Small, Sizes.Medium, Sizes.Large)]
    public int N;

    private string[] _values = null!;

    private PooledStack<string> _omniFilled = null!;
    private Stack<string> _baselineFilled = null!;

    private PooledStack<string> _omniEmpty = null!;
    private Stack<string> _baselineEmpty = null!;

    private PooledStack<string> _omniDrain = null!;
    private Stack<string> _baselineDrain = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _values = RandomData.Strings(N);

        _omniFilled = new PooledStack<string>(N);
        _baselineFilled = new Stack<string>(N);
        for (int i = 0; i < N; i++)
        {
            _omniFilled.Push(_values[i]);
            _baselineFilled.Push(_values[i]);
        }
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _omniFilled.Dispose();
        _omniEmpty?.Dispose();
        _omniDrain?.Dispose();
    }

    [IterationSetup(Targets = new[] { nameof(Omni_PushN), nameof(Baseline_PushN) })]
    public void ResetForPush()
    {
        _omniEmpty?.Dispose();
        _omniEmpty = new PooledStack<string>(N);
        _baselineEmpty = new Stack<string>(N);
    }

    [IterationSetup(Targets = new[] { nameof(Omni_PopN), nameof(Baseline_PopN) })]
    public void RefillForPop()
    {
        _omniDrain?.Dispose();
        _omniDrain = new PooledStack<string>(N);
        _baselineDrain = new Stack<string>(N);
        for (int i = 0; i < N; i++)
        {
            _omniDrain.Push(_values[i]);
            _baselineDrain.Push(_values[i]);
        }
    }

    /// Claim: PooledStack.Push amortizes growth via ArrayPool, eliminating GC pressure vs Stack.Push resizing.
    [Benchmark, BenchmarkCategory("Push"), InvocationCount(1)]
    public PooledStack<string> Omni_PushN()
    {
        for (int i = 0; i < _values.Length; i++)
            _omniEmpty.Push(_values[i]);
        return _omniEmpty;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Push"), InvocationCount(1)]
    public Stack<string> Baseline_PushN()
    {
        for (int i = 0; i < _values.Length; i++)
            _baselineEmpty.Push(_values[i]);
        return _baselineEmpty;
    }

    /// Claim: PooledStack.Pop matches Stack.Pop (both O(1) decrement + array read).
    [Benchmark, BenchmarkCategory("Pop"), InvocationCount(1)]
    public string Omni_PopN()
    {
        string last = null!;
        while (_omniDrain.Count > 0)
            last = _omniDrain.Pop();
        return last;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Pop"), InvocationCount(1)]
    public string Baseline_PopN()
    {
        string last = null!;
        while (_baselineDrain.Count > 0)
            last = _baselineDrain.Pop();
        return last;
    }

    /// Claim: PooledStack.Peek matches Stack.Peek (both O(1) array read).
    [Benchmark, BenchmarkCategory("Peek")]
    public string Omni_Peek() => _omniFilled.Peek();

    [Benchmark(Baseline = true), BenchmarkCategory("Peek")]
    public string Baseline_Peek() => _baselineFilled.Peek();
}

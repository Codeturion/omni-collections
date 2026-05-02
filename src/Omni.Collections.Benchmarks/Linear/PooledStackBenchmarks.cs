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
    private const int OpsPerIteration = 32768;

    [Params(Sizes.Small, Sizes.Medium, Sizes.Large)]
    public int N;

    private string[] _values = null!;

    private PooledStack<string> _omniFilled = null!;
    private Stack<string> _baselineFilled = null!;

    private PooledStack<string> _omniMut = null!;
    private Stack<string> _baselineMut = null!;
    private int _opCounter;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _values = RandomData.Strings(N + OpsPerIteration);

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
        _omniMut?.Dispose();
    }

    [IterationSetup(Targets = new[] { nameof(Omni_Push), nameof(Baseline_Push) })]
    public void ResetForPush()
    {
        _omniMut?.Dispose();
        _omniMut = new PooledStack<string>(N + OpsPerIteration);
        _baselineMut = new Stack<string>(N + OpsPerIteration);
        for (int i = 0; i < N; i++)
        {
            _omniMut.Push(_values[i]);
            _baselineMut.Push(_values[i]);
        }
        _opCounter = N;
    }

    [IterationSetup(Targets = new[] { nameof(Omni_Pop), nameof(Baseline_Pop) })]
    public void RefillForPop()
    {
        _omniMut?.Dispose();
        _omniMut = new PooledStack<string>(N + OpsPerIteration);
        _baselineMut = new Stack<string>(N + OpsPerIteration);
        for (int i = 0; i < N + OpsPerIteration; i++)
        {
            _omniMut.Push(_values[i]);
            _baselineMut.Push(_values[i]);
        }
    }

    /// Claim: PooledStack.Push (capacity sufficient) matches Stack<T>.Push per-op cost.
    [Benchmark, BenchmarkCategory("Push"), InvocationCount(OpsPerIteration)]
    public int Omni_Push()
    {
        _omniMut.Push(_values[_opCounter++]);
        return _omniMut.Count;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Push"), InvocationCount(OpsPerIteration)]
    public int Baseline_Push()
    {
        _baselineMut.Push(_values[_opCounter++]);
        return _baselineMut.Count;
    }

    /// Claim: PooledStack.Pop matches Stack<T>.Pop (both O(1) decrement + array read).
    [Benchmark, BenchmarkCategory("Pop"), InvocationCount(OpsPerIteration)]
    public string Omni_Pop() => _omniMut.Pop();

    [Benchmark(Baseline = true), BenchmarkCategory("Pop"), InvocationCount(OpsPerIteration)]
    public string Baseline_Pop() => _baselineMut.Pop();

    // ============= Fill (bulk allocation comparison) =============

    private PooledStack<string>? _omniFillResult;

    [IterationCleanup(Targets = new[] { nameof(Omni_Fill) })]
    public void DisposeOmniFill()
    {
        _omniFillResult?.Dispose();
        _omniFillResult = null;
    }

    /// Claim: PooledStack default-cap fill rents arrays from ArrayPool, eliminating GC pressure vs Stack<T> resizing.
    [Benchmark, BenchmarkCategory("Fill"), InvocationCount(1)]
    public PooledStack<string> Omni_Fill()
    {
        _omniFillResult = new PooledStack<string>();
        for (int i = 0; i < N; i++)
            _omniFillResult.Push(_values[i]);
        return _omniFillResult;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Fill"), InvocationCount(1)]
    public Stack<string> Baseline_Fill()
    {
        var c = new Stack<string>();
        for (int i = 0; i < N; i++)
            c.Push(_values[i]);
        return c;
    }

    /// Claim: PooledStack.Peek matches Stack<T>.Peek (both O(1) array read).
    [Benchmark, BenchmarkCategory("Peek")]
    public string Omni_Peek() => _omniFilled.Peek();

    [Benchmark(Baseline = true), BenchmarkCategory("Peek")]
    public string Baseline_Peek() => _baselineFilled.Peek();
}

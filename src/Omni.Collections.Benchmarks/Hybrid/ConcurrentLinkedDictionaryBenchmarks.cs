using System.Collections.Concurrent;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Omni.Collections.Benchmarks.Common;
using Omni.Collections.Hybrid;

namespace Omni.Collections.Benchmarks.Hybrid;

[BenchmarkCategory(Categories.Hybrid)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[MemoryDiagnoser]
public class ConcurrentLinkedDictionaryBenchmarks
{
    // Single-threaded benchmarks for API speed comparison. Real concurrency
    // tests (lock-free reads, parallel scaling) are scheduled for Phase 5.
    private const int OpsPerIteration = 32768;

    [Params(Sizes.Small, Sizes.Medium, Sizes.Large)]
    public int N;

    private string[] _keys = null!;
    private int[] _values = null!;
    private int[] _readIndices = null!;
    private const int ReadIndexMask = 1023;
    private int _readCounter;

    private ConcurrentLinkedDictionary<string, int> _omniFilled = null!;
    private ConcurrentDictionary<string, int> _baselineFilled = null!;

    private ConcurrentLinkedDictionary<string, int> _omniMut = null!;
    private ConcurrentDictionary<string, int> _baselineMut = null!;
    private int _addCounter;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _keys = RandomData.Strings(N + OpsPerIteration);
        _values = RandomData.Ints(N + OpsPerIteration);
        _readIndices = RandomData.IntsInRange(ReadIndexMask + 1, 0, N);

        _omniFilled = new ConcurrentLinkedDictionary<string, int>(N);
        _baselineFilled = new ConcurrentDictionary<string, int>();
        for (int i = 0; i < N; i++)
        {
            _omniFilled.AddOrUpdate(_keys[i], _values[i]);
            _baselineFilled.TryAdd(_keys[i], _values[i]);
        }
    }

    [IterationSetup(Targets = new[] { nameof(Omni_Add), nameof(Baseline_Add) })]
    public void ResetForAdd()
    {
        _omniMut = new ConcurrentLinkedDictionary<string, int>(N + OpsPerIteration);
        _baselineMut = new ConcurrentDictionary<string, int>();
        for (int i = 0; i < N; i++)
        {
            _omniMut.AddOrUpdate(_keys[i], _values[i]);
            _baselineMut.TryAdd(_keys[i], _values[i]);
        }
        _addCounter = N;
    }

    /// Claim: ConcurrentLinkedDictionary.AddOrUpdate single-thread cost vs ConcurrentDictionary.TryAdd.
    [Benchmark, BenchmarkCategory("Add"), InvocationCount(OpsPerIteration)]
    public bool Omni_Add()
    {
        _omniMut.AddOrUpdate(_keys[_addCounter], _values[_addCounter]);
        _addCounter++;
        return true;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Add"), InvocationCount(OpsPerIteration)]
    public bool Baseline_Add()
    {
        var r = _baselineMut.TryAdd(_keys[_addCounter], _values[_addCounter]);
        _addCounter++;
        return r;
    }

    /// Claim: ConcurrentLinkedDictionary.TryGetValue (lock-free read claim) vs ConcurrentDictionary.
    [Benchmark, BenchmarkCategory("Lookup")]
    public bool Omni_Lookup()
    {
        var k = _keys[_readIndices[_readCounter++ & ReadIndexMask]];
        return _omniFilled.TryGetValue(k, out _);
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Lookup")]
    public bool Baseline_Lookup()
    {
        var k = _keys[_readIndices[_readCounter++ & ReadIndexMask]];
        return _baselineFilled.TryGetValue(k, out _);
    }

    /// Claim: ConcurrentLinkedDictionary fill from default capacity vs ConcurrentDictionary growth.
    [Benchmark, BenchmarkCategory("Fill"), InvocationCount(1)]
    public ConcurrentLinkedDictionary<string, int> Omni_Fill()
    {
        var c = new ConcurrentLinkedDictionary<string, int>(16);
        for (int i = 0; i < N; i++)
            c.AddOrUpdate(_keys[i], _values[i]);
        return c;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Fill"), InvocationCount(1)]
    public ConcurrentDictionary<string, int> Baseline_Fill()
    {
        var c = new ConcurrentDictionary<string, int>();
        for (int i = 0; i < N; i++)
            c.TryAdd(_keys[i], _values[i]);
        return c;
    }
}

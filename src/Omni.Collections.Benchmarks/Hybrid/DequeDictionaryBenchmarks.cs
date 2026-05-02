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
public class DequeDictionaryBenchmarks
{
    private const int OpsPerIteration = 32768;

    [Params(Sizes.Small, Sizes.Medium, Sizes.Large)]
    public int N;

    private string[] _keys = null!;
    private int[] _values = null!;
    private int[] _readIndices = null!;
    private const int ReadIndexMask = 1023;
    private int _readCounter;

    private DequeDictionary<string, int> _omniFilled = null!;
    private Dictionary<string, int> _baselineFilled = null!;

    private DequeDictionary<string, int> _omniMut = null!;
    private Dictionary<string, int> _baselineMut = null!;
    private int _addCounter;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _keys = RandomData.Strings(N + OpsPerIteration);
        _values = RandomData.Ints(N + OpsPerIteration);
        _readIndices = RandomData.IntsInRange(ReadIndexMask + 1, 0, N);

        _omniFilled = new DequeDictionary<string, int>(N);
        _baselineFilled = new Dictionary<string, int>(N);
        for (int i = 0; i < N; i++)
        {
            _omniFilled.PushBack(_keys[i], _values[i]);
            _baselineFilled.Add(_keys[i], _values[i]);
        }
    }

    [IterationSetup(Targets = new[] { nameof(Omni_PushBack), nameof(Baseline_Add) })]
    public void ResetForAdd()
    {
        _omniMut = new DequeDictionary<string, int>(N + OpsPerIteration);
        _baselineMut = new Dictionary<string, int>(N + OpsPerIteration);
        for (int i = 0; i < N; i++)
        {
            _omniMut.PushBack(_keys[i], _values[i]);
            _baselineMut.Add(_keys[i], _values[i]);
        }
        _addCounter = N;
    }

    /// Claim: DequeDictionary.PushBack maintains a doubly-ended order with cost competitive to Dictionary.Add.
    [Benchmark, BenchmarkCategory("Add"), InvocationCount(OpsPerIteration)]
    public int Omni_PushBack()
    {
        _omniMut.PushBack(_keys[_addCounter], _values[_addCounter]);
        _addCounter++;
        return _omniMut.Count;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Add"), InvocationCount(OpsPerIteration)]
    public int Baseline_Add()
    {
        _baselineMut.Add(_keys[_addCounter], _values[_addCounter]);
        _addCounter++;
        return _baselineMut.Count;
    }

    /// Claim: DequeDictionary.TryGetValue matches Dictionary lookup speed.
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

    /// Claim: DequeDictionary fill (capacity preset) builds doubly-ended structure.
    [Benchmark, BenchmarkCategory("Fill"), InvocationCount(1)]
    public DequeDictionary<string, int> Omni_Fill()
    {
        var c = new DequeDictionary<string, int>(N);
        for (int i = 0; i < N; i++)
            c.PushBack(_keys[i], _values[i]);
        return c;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Fill"), InvocationCount(1)]
    public Dictionary<string, int> Baseline_Fill()
    {
        var c = new Dictionary<string, int>();
        for (int i = 0; i < N; i++)
            c.Add(_keys[i], _values[i]);
        return c;
    }
}

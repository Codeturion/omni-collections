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
public class LinkedMultiMapBenchmarks
{
    private const int OpsPerIteration = 32768;
    private const int ValuesPerKey = 4;

    [Params(Sizes.Small, Sizes.Medium, Sizes.Large)]
    public int N;

    private string[] _keys = null!;
    private int[] _values = null!;
    private int[] _readIndices = null!;
    private const int ReadIndexMask = 1023;
    private int _readCounter;

    private LinkedMultiMap<string, int> _omniFilled = null!;
    private Dictionary<string, List<int>> _baselineFilled = null!;

    private LinkedMultiMap<string, int> _omniMut = null!;
    private Dictionary<string, List<int>> _baselineMut = null!;
    private int _addCounter;

    [GlobalSetup]
    public void GlobalSetup()
    {
        // For multimap, use only N/ValuesPerKey distinct keys with multiple values each.
        var distinctKeys = N / ValuesPerKey + 1;
        _keys = RandomData.Strings(distinctKeys + OpsPerIteration);
        _values = RandomData.Ints(N + OpsPerIteration);
        _readIndices = RandomData.IntsInRange(ReadIndexMask + 1, 0, distinctKeys);

        _omniFilled = new LinkedMultiMap<string, int>(distinctKeys);
        _baselineFilled = new Dictionary<string, List<int>>(distinctKeys);
        for (int i = 0; i < N; i++)
        {
            var k = _keys[i % distinctKeys];
            _omniFilled.Add(k, _values[i]);
            if (!_baselineFilled.TryGetValue(k, out var list))
            {
                list = new List<int>();
                _baselineFilled[k] = list;
            }
            list.Add(_values[i]);
        }
    }

    [IterationSetup(Targets = new[] { nameof(Omni_Add), nameof(Baseline_Add) })]
    public void ResetForAdd()
    {
        var distinctKeys = N / ValuesPerKey + 1;
        _omniMut = new LinkedMultiMap<string, int>(distinctKeys + OpsPerIteration);
        _baselineMut = new Dictionary<string, List<int>>(distinctKeys + OpsPerIteration);
        _addCounter = N;
    }

    /// Claim: LinkedMultiMap.Add per-pair cost vs Dictionary<K,List<V>> manual lookup-and-add.
    [Benchmark, BenchmarkCategory("Add"), InvocationCount(OpsPerIteration)]
    public bool Omni_Add()
    {
        _omniMut.Add(_keys[_addCounter % _keys.Length], _values[_addCounter]);
        _addCounter++;
        return true;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Add"), InvocationCount(OpsPerIteration)]
    public bool Baseline_Add()
    {
        var k = _keys[_addCounter % _keys.Length];
        if (!_baselineMut.TryGetValue(k, out var list))
        {
            list = new List<int>();
            _baselineMut[k] = list;
        }
        list.Add(_values[_addCounter]);
        _addCounter++;
        return true;
    }

    /// Claim: LinkedMultiMap.TryGetValues vs Dictionary<K,List<V>>.TryGetValue.
    [Benchmark, BenchmarkCategory("Lookup")]
    public int Omni_Lookup()
    {
        var k = _keys[_readIndices[_readCounter++ & ReadIndexMask]];
        return _omniFilled.TryGetValues(k, out var values) ? values.Count : 0;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Lookup")]
    public int Baseline_Lookup()
    {
        var k = _keys[_readIndices[_readCounter++ & ReadIndexMask]];
        return _baselineFilled.TryGetValue(k, out var values) ? values.Count : 0;
    }

    /// Claim: LinkedMultiMap fill (multiple values per key) vs Dictionary<K,List<V>> with manual list management.
    [Benchmark, BenchmarkCategory("Fill"), InvocationCount(1)]
    public LinkedMultiMap<string, int> Omni_Fill()
    {
        var c = new LinkedMultiMap<string, int>(16);
        var distinctKeys = N / ValuesPerKey + 1;
        for (int i = 0; i < N; i++)
            c.Add(_keys[i % distinctKeys], _values[i]);
        return c;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Fill"), InvocationCount(1)]
    public Dictionary<string, List<int>> Baseline_Fill()
    {
        var c = new Dictionary<string, List<int>>();
        var distinctKeys = N / ValuesPerKey + 1;
        for (int i = 0; i < N; i++)
        {
            var k = _keys[i % distinctKeys];
            if (!c.TryGetValue(k, out var list))
            {
                list = new List<int>();
                c[k] = list;
            }
            list.Add(_values[i]);
        }
        return c;
    }
}

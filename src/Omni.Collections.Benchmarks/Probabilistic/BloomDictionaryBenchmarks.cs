using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Omni.Collections.Benchmarks.Common;
using Omni.Collections.Probabilistic;

namespace Omni.Collections.Benchmarks.Probabilistic;

[BenchmarkCategory(Categories.Probabilistic)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[MemoryDiagnoser]
public class BloomDictionaryBenchmarks
{
    private const int OpsPerIteration = 32768;

    [Params(Sizes.Small, Sizes.Medium, Sizes.Large)]
    public int N;

    private string[] _keys = null!;
    private int[] _values = null!;
    private string[] _missingKeys = null!;
    private int[] _readIndices = null!;
    private const int ReadIndexMask = 1023;
    private int _readCounter;
    private int _addCounter;

    private BloomDictionary<string, int> _omniFilled = null!;
    private Dictionary<string, int> _baselineFilled = null!;

    private BloomDictionary<string, int> _omniMut = null!;
    private Dictionary<string, int> _baselineMut = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _keys = RandomData.Strings(N + OpsPerIteration);
        _values = RandomData.Ints(N + OpsPerIteration);
        _missingKeys = RandomData.Strings(ReadIndexMask + 1, seed: RandomData.Seed + 1);
        _readIndices = RandomData.IntsInRange(ReadIndexMask + 1, 0, N);

        _omniFilled = new BloomDictionary<string, int>(N);
        _baselineFilled = new Dictionary<string, int>(N);
        for (int i = 0; i < N; i++)
        {
            _omniFilled.Add(_keys[i], _values[i]);
            _baselineFilled.Add(_keys[i], _values[i]);
        }
    }

    [IterationSetup(Targets = new[] { nameof(Omni_Add), nameof(Baseline_Add) })]
    public void ResetForAdd()
    {
        _omniMut = new BloomDictionary<string, int>(N + OpsPerIteration);
        _baselineMut = new Dictionary<string, int>(N + OpsPerIteration);
        for (int i = 0; i < N; i++)
        {
            _omniMut.Add(_keys[i], _values[i]);
            _baselineMut.Add(_keys[i], _values[i]);
        }
        _addCounter = N;
    }

    /// Claim: BloomDictionary.Add includes Bloom filter update overhead.
    [Benchmark, BenchmarkCategory("Add"), InvocationCount(OpsPerIteration)]
    public bool Omni_Add()
    {
        _omniMut.Add(_keys[_addCounter], _values[_addCounter]);
        _addCounter++;
        return true;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Add"), InvocationCount(OpsPerIteration)]
    public bool Baseline_Add()
    {
        _baselineMut.Add(_keys[_addCounter], _values[_addCounter]);
        _addCounter++;
        return true;
    }

    /// Claim: BloomDictionary lookup hits = Bloom + Dictionary lookup; lookup misses fast-fail at the Bloom check.
    [Benchmark, BenchmarkCategory("LookupMiss")]
    public bool Omni_LookupMiss()
    {
        return _omniFilled.TryGetValue(_missingKeys[_readCounter++ & ReadIndexMask], out _);
    }

    [Benchmark(Baseline = true), BenchmarkCategory("LookupMiss")]
    public bool Baseline_LookupMiss()
    {
        return _baselineFilled.TryGetValue(_missingKeys[_readCounter++ & ReadIndexMask], out _);
    }

    [Benchmark, BenchmarkCategory("LookupHit")]
    public bool Omni_LookupHit()
    {
        var k = _keys[_readIndices[_readCounter++ & ReadIndexMask]];
        return _omniFilled.TryGetValue(k, out _);
    }

    [Benchmark(Baseline = true), BenchmarkCategory("LookupHit")]
    public bool Baseline_LookupHit()
    {
        var k = _keys[_readIndices[_readCounter++ & ReadIndexMask]];
        return _baselineFilled.TryGetValue(k, out _);
    }

    /// Claim: BloomDictionary fill includes Bloom filter allocation alongside hash table.
    [Benchmark, BenchmarkCategory("Fill"), InvocationCount(1)]
    public BloomDictionary<string, int> Omni_Fill()
    {
        var c = new BloomDictionary<string, int>();
        for (int i = 0; i < N; i++)
            c.Add(_keys[i], _values[i]);
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

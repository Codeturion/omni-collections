using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Omni.Collections.Benchmarks.Common;
using Omni.Collections.Hybrid.QueueDictionary;

namespace Omni.Collections.Benchmarks.Hybrid;

[BenchmarkCategory(Categories.Hybrid)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[MemoryDiagnoser]
public class QueueDictionaryBenchmarks
{
    private const int OpsPerIteration = 32768;

    [Params(Sizes.Small, Sizes.Medium, Sizes.Large)]
    public int N;

    private string[] _keys = null!;
    private int[] _values = null!;
    private int[] _readIndices = null!;
    private const int ReadIndexMask = 1023;
    private int _readCounter;

    private QueueDictionary<string, int> _omniFilled = null!;
    private Dictionary<string, int> _baselineFilled = null!;

    private QueueDictionary<string, int> _omniMut = null!;
    private Dictionary<string, int> _baselineMut = null!;
    private int _addCounter;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _keys = RandomData.Strings(N + OpsPerIteration);
        _values = RandomData.Ints(N + OpsPerIteration);
        _readIndices = RandomData.IntsInRange(ReadIndexMask + 1, 0, N);

        _omniFilled = new QueueDictionary<string, int>(N);
        _baselineFilled = new Dictionary<string, int>(N);
        for (int i = 0; i < N; i++)
        {
            _omniFilled.Enqueue(_keys[i], _values[i]);
            _baselineFilled.Add(_keys[i], _values[i]);
        }
    }

    [GlobalCleanup]
    public void GlobalCleanup() => _omniFilled.Dispose();

    [IterationSetup(Targets = new[] { nameof(Omni_Enqueue), nameof(Baseline_Add) })]
    public void ResetForAdd()
    {
        _omniMut?.Dispose();
        _omniMut = new QueueDictionary<string, int>(N + OpsPerIteration);
        _baselineMut = new Dictionary<string, int>(N + OpsPerIteration);
        for (int i = 0; i < N; i++)
        {
            _omniMut.Enqueue(_keys[i], _values[i]);
            _baselineMut.Add(_keys[i], _values[i]);
        }
        _addCounter = N;
    }

    /// Claim: QueueDictionary.Enqueue maintains insertion order with cost competitive to Dictionary.Add.
    [Benchmark, BenchmarkCategory("Add"), InvocationCount(OpsPerIteration)]
    public int Omni_Enqueue()
    {
        _omniMut.Enqueue(_keys[_addCounter], _values[_addCounter]);
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

    /// Claim: QueueDictionary.TryGetValue matches Dictionary lookup speed.
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

    private QueueDictionary<string, int>? _omniFillResult;

    [IterationCleanup(Targets = new[] { nameof(Omni_Fill) })]
    public void DisposeFill() { _omniFillResult?.Dispose(); _omniFillResult = null; }

    /// Claim: QueueDictionary fill including ordering metadata vs plain Dictionary.
    [Benchmark, BenchmarkCategory("Fill"), InvocationCount(1)]
    public QueueDictionary<string, int> Omni_Fill()
    {
        _omniFillResult = new QueueDictionary<string, int>();
        for (int i = 0; i < N; i++)
            _omniFillResult.Enqueue(_keys[i], _values[i]);
        return _omniFillResult;
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

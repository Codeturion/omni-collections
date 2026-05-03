using System.Collections.Concurrent;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Microsoft.Extensions.Caching.Memory;
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
    private Dictionary<string, int> _dictFilled = null!;
    private MemoryCache _memCacheFilled = null!;

    private ConcurrentLinkedDictionary<string, int> _omniMut = null!;
    private ConcurrentDictionary<string, int> _baselineMut = null!;
    private Dictionary<string, int> _dictMut = null!;
    private MemoryCache _memCacheMut = null!;
    private int _addCounter;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _keys = RandomData.Strings(N + OpsPerIteration);
        _values = RandomData.Ints(N + OpsPerIteration);
        _readIndices = RandomData.IntsInRange(ReadIndexMask + 1, 0, N);

        _omniFilled = new ConcurrentLinkedDictionary<string, int>(N);
        _baselineFilled = new ConcurrentDictionary<string, int>();
        _dictFilled = new Dictionary<string, int>(N);
        _memCacheFilled = NewMemoryCache(N);
        for (int i = 0; i < N; i++)
        {
            _omniFilled.AddOrUpdate(_keys[i], _values[i]);
            _baselineFilled.TryAdd(_keys[i], _values[i]);
            _dictFilled[_keys[i]] = _values[i];
            SetMemoryCache(_memCacheFilled, _keys[i], _values[i]);
        }
    }

    private static MemoryCache NewMemoryCache(int sizeLimit)
    {
        return new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = sizeLimit
        });
    }

    private static void SetMemoryCache(MemoryCache cache, string key, int value)
    {
        using var entry = cache.CreateEntry(key);
        entry.Value = value;
        entry.Size = 1;
        entry.Priority = CacheItemPriority.Normal;
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _memCacheFilled?.Dispose();
        _memCacheMut?.Dispose();
    }

    [IterationSetup(Targets = new[] {
        nameof(Omni_Add),
        nameof(Baseline_Add),
        nameof(Baseline_Dictionary_Add),
        nameof(Baseline_MemoryCache_Add) })]
    public void ResetForAdd()
    {
        _omniMut = new ConcurrentLinkedDictionary<string, int>(N + OpsPerIteration);
        _baselineMut = new ConcurrentDictionary<string, int>();
        _dictMut = new Dictionary<string, int>(N + OpsPerIteration);
        _memCacheMut?.Dispose();
        _memCacheMut = NewMemoryCache(N + OpsPerIteration);
        for (int i = 0; i < N; i++)
        {
            _omniMut.AddOrUpdate(_keys[i], _values[i]);
            _baselineMut.TryAdd(_keys[i], _values[i]);
            _dictMut[_keys[i]] = _values[i];
            SetMemoryCache(_memCacheMut, _keys[i], _values[i]);
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

    // ============= Fair-baseline alternatives =============
    // Existing Baseline_* uses ConcurrentDictionary (closest BCL thread-safe dict, no LRU).
    // The two below extend the comparison surface so all three coexist in one report:
    //   - Dictionary<K,V>   — single-threaded reference (NOT thread-safe; floor cost)
    //   - MemoryCache       — closest BCL-ish to LRU (size-limited, eviction-driven)

    /// Claim: ConcurrentLinkedDictionary single-thread Add cost vs Dictionary.set_Item (no thread safety, no LRU).
    /// Establishes the floor: how much do thread-safety + LRU cost on top of plain Dictionary?
    [Benchmark, BenchmarkCategory("Add"), InvocationCount(OpsPerIteration)]
    public bool Baseline_Dictionary_Add()
    {
        _dictMut[_keys[_addCounter]] = _values[_addCounter];
        _addCounter++;
        return true;
    }

    /// Claim: ConcurrentLinkedDictionary single-thread Add cost vs MemoryCache.Set
    /// (size-limited eviction-driven, the closest BCL-ish to a thread-safe LRU).
    [Benchmark, BenchmarkCategory("Add"), InvocationCount(OpsPerIteration)]
    public bool Baseline_MemoryCache_Add()
    {
        SetMemoryCache(_memCacheMut, _keys[_addCounter], _values[_addCounter]);
        _addCounter++;
        return true;
    }

    /// Claim: ConcurrentLinkedDictionary lookup cost vs Dictionary.TryGetValue (no thread safety, no LRU).
    [Benchmark, BenchmarkCategory("Lookup")]
    public bool Baseline_Dictionary_Lookup()
    {
        var k = _keys[_readIndices[_readCounter++ & ReadIndexMask]];
        return _dictFilled.TryGetValue(k, out _);
    }

    /// Claim: ConcurrentLinkedDictionary lookup cost vs MemoryCache.TryGetValue.
    [Benchmark, BenchmarkCategory("Lookup")]
    public bool Baseline_MemoryCache_Lookup()
    {
        var k = _keys[_readIndices[_readCounter++ & ReadIndexMask]];
        return _memCacheFilled.TryGetValue(k, out _);
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

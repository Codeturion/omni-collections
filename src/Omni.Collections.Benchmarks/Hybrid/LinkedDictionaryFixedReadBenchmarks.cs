using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Omni.Collections.Benchmarks.Common;
using Omni.Collections.Hybrid.LinkedDictionary;

namespace Omni.Collections.Benchmarks.Hybrid;

/// <summary>
/// Isolates the cost of the MoveToFront LRU reorder on the Fixed-mode read path.
///
/// TryGetValue and ContainsKey run an identical hash + modulo + bucket walk; the only
/// difference is that TryGetValue calls MoveToFront on a hit and ContainsKey does not.
/// The delta between them is the reorder, measured without modifying the library.
///
/// The read index pool is deliberately large (64K distinct positions over N) so the
/// touched node set is not artificially cache-resident — a small pool would keep every
/// read near the head of the LRU list and understate the pointer-chase cost.
/// </summary>
[BenchmarkCategory(Categories.Hybrid)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[MemoryDiagnoser]
public class LinkedDictionaryFixedReadBenchmarks
{
    [Params(Sizes.Small, Sizes.Medium, Sizes.Large)]
    public int N;

    private const int ReadIndexMask = 65535;

    private string[] _keys = null!;
    private int[] _values = null!;
    private int[] _readIndices = null!;
    private int _readCounter;
    private string _hotKey = null!;

    private LinkedDictionary<string, int> _fixedReorder = null!;
    private LinkedDictionary<string, int> _fixedNoReorder = null!;
    private Dictionary<string, int> _baseline = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _keys = RandomData.Strings(N);
        _values = RandomData.Ints(N);
        _readIndices = RandomData.IntsInRange(ReadIndexMask + 1, 0, N);
        _hotKey = _keys[0];

        _fixedReorder = new LinkedDictionary<string, int>(N, CapacityMode.Fixed);
        _fixedNoReorder = new LinkedDictionary<string, int>(N, CapacityMode.Fixed);
        _baseline = new Dictionary<string, int>(N);
        for (int i = 0; i < N; i++)
        {
            _fixedReorder.AddOrUpdate(_keys[i], _values[i]);
            _fixedNoReorder.AddOrUpdate(_keys[i], _values[i]);
            _baseline.Add(_keys[i], _values[i]);
        }
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _fixedReorder.Dispose();
        _fixedNoReorder.Dispose();
    }

    /// Claim: TryGetValue on a Fixed-mode dictionary pays hash + bucket walk + MoveToFront.
    [Benchmark, BenchmarkCategory("FixedRead")]
    public bool Reorder_TryGetValue()
    {
        var k = _keys[_readIndices[_readCounter++ & ReadIndexMask]];
        return _fixedReorder.TryGetValue(k, out _);
    }

    /// Claim: ContainsKey is the same lookup minus MoveToFront — the control arm.
    [Benchmark, BenchmarkCategory("FixedRead")]
    public bool NoReorder_ContainsKey()
    {
        var k = _keys[_readIndices[_readCounter++ & ReadIndexMask]];
        return _fixedNoReorder.ContainsKey(k);
    }

    /// Claim: re-reading the MRU key hits the node == _head guard and skips the reorder.
    [Benchmark, BenchmarkCategory("FixedRead")]
    public bool Reorder_TryGetValue_HotKey()
    {
        return _fixedReorder.TryGetValue(_hotKey, out _);
    }

    [Benchmark(Baseline = true), BenchmarkCategory("FixedRead")]
    public bool Baseline_Dictionary()
    {
        var k = _keys[_readIndices[_readCounter++ & ReadIndexMask]];
        return _baseline.TryGetValue(k, out _);
    }
}

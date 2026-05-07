using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Omni.Collections.Benchmarks.Common;
using Omni.Collections.Spatial;
using Omni.Collections.Spatial.BloomRTreeDictionary;

namespace Omni.Collections.Benchmarks.Spatial;

[BenchmarkCategory(Categories.Spatial)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[MemoryDiagnoser]
public class BloomRTreeDictionaryBenchmarks
{
    private const float WorldHalf = 1000f;
    private const int OpsPerIteration = 32768;

    [Params(Sizes.Small, Sizes.Medium, Sizes.Large)]
    public int N;

    private string[] _keys = null!;
    private int[] _values = null!;
    private (float x, float y)[] _points = null!;
    private int[] _readIndices = null!;
    private const int ReadIndexMask = 1023;
    private int _readCounter;
    private int _addCounter;

    // FindIntersectingEmpty fixture: entries clustered in left half (x < 0),
    // queries split 70% left half / 30% right half. The right-half queries are
    // the bloom layer's job to short-circuit.
    private const int QueryBoxCount = 1024; // power-of-two for masked cycling
    private const int QueryBoxMask = QueryBoxCount - 1;
    private BloomRTreeDictionary<string, int> _omniClustered = null!;
    private (BoundingRectangle Box, string Key, int Value)[] _baselineClustered = null!;
    private BoundingRectangle[] _queryBoxes = null!;
    private int _queryCounter;

    private BloomRTreeDictionary<string, int> _omniFilled = null!;
    private Dictionary<string, int> _baselineFilled = null!;

    private BloomRTreeDictionary<string, int> _omniMut = null!;
    private Dictionary<string, int> _baselineMut = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _keys = RandomData.Strings(N + OpsPerIteration);
        _values = RandomData.Ints(N + OpsPerIteration);
        _points = RandomData.Points2D(N + OpsPerIteration, -WorldHalf, WorldHalf);
        _readIndices = RandomData.IntsInRange(ReadIndexMask + 1, 0, N);

        _omniFilled = new BloomRTreeDictionary<string, int>(N);
        _baselineFilled = new Dictionary<string, int>(N);
        for (int i = 0; i < N; i++)
        {
            _omniFilled.Add(_keys[i], _values[i], _points[i].x, _points[i].y);
            _baselineFilled.Add(_keys[i], _values[i]);
        }

        // Clustered fixture: all entries placed in the LEFT half of the world (x < 0).
        // 30% of the query boxes target the RIGHT (empty) half — those are pure
        // bloom-screen candidates. 70% target the LEFT (populated) half — the bloom
        // can't short-circuit, so the tree is traversed.
        _omniClustered = new BloomRTreeDictionary<string, int>(N);
        _baselineClustered = new (BoundingRectangle, string, int)[N];
        var clusterPoints = RandomData.Points2D(N, -WorldHalf, 0f, seed: RandomData.Seed + 11); // left half only
        for (int i = 0; i < N; i++)
        {
            _omniClustered.Add(_keys[i], _values[i], clusterPoints[i].x, clusterPoints[i].y);
            _baselineClustered[i] = (new BoundingRectangle(clusterPoints[i].x, clusterPoints[i].y), _keys[i], _values[i]);
        }

        _queryBoxes = new BoundingRectangle[QueryBoxCount];
        var qrng = new System.Random(RandomData.Seed + 12);
        const float boxHalf = 50f; // small query boxes
        for (int i = 0; i < QueryBoxCount; i++)
        {
            // i % 10 < 3 → right (empty) half, otherwise left (populated). 30/70 split.
            bool empty = (i % 10) < 3;
            float cx = empty
                ? (float)(qrng.NextDouble() * WorldHalf) // [0, WorldHalf]
                : -(float)(qrng.NextDouble() * WorldHalf); // [-WorldHalf, 0]
            float cy = (float)(qrng.NextDouble() * (2 * WorldHalf) - WorldHalf);
            _queryBoxes[i] = new BoundingRectangle(cx - boxHalf, cy - boxHalf, cx + boxHalf, cy + boxHalf);
        }
    }

    [IterationSetup(Targets = new[] { nameof(Omni_Add), nameof(Baseline_Add) })]
    public void ResetForAdd()
    {
        _omniMut = new BloomRTreeDictionary<string, int>(N + OpsPerIteration);
        _baselineMut = new Dictionary<string, int>(N + OpsPerIteration);
        for (int i = 0; i < N; i++)
        {
            _omniMut.Add(_keys[i], _values[i], _points[i].x, _points[i].y);
            _baselineMut.Add(_keys[i], _values[i]);
        }
        _addCounter = N;
    }

    /// Claim: BloomRTreeDictionary.Add maintains spatial index alongside hash table.
    [Benchmark, BenchmarkCategory("Add"), InvocationCount(OpsPerIteration)]
    public bool Omni_Add()
    {
        _omniMut.Add(_keys[_addCounter], _values[_addCounter], _points[_addCounter].x, _points[_addCounter].y);
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

    /// Claim: BloomRTreeDictionary key lookup matches Dictionary cost (Bloom filter pre-screen).
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

    /// Claim: BloomRTreeDictionary fill builds spatial+hash structure.
    [Benchmark, BenchmarkCategory("Fill"), InvocationCount(1)]
    public BloomRTreeDictionary<string, int> Omni_Fill()
    {
        var c = new BloomRTreeDictionary<string, int>();
        for (int i = 0; i < N; i++)
            c.Add(_keys[i], _values[i], _points[i].x, _points[i].y);
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

    /// Claim: when ~30% of FindIntersecting queries target known-empty regions,
    /// the spatial bloom filter short-circuits them in O(1), beating a naive
    /// linear scan over all entries. If the wall-time win is below 5%, the bloom
    /// layer's value-prop on this op doesn't survive.
    [Benchmark, BenchmarkCategory("FindIntersectingEmpty")]
    public int Omni_FindIntersectingEmpty()
    {
        var box = _queryBoxes[_queryCounter++ & QueryBoxMask];
        int count = 0;
        foreach (var kv in _omniClustered.FindIntersecting(box))
        {
            count++;
            // consume to prevent dead-code elimination
            if (kv.Value == int.MinValue) return -1;
        }
        return count;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("FindIntersectingEmpty")]
    public int Baseline_FindIntersectingEmpty()
    {
        var box = _queryBoxes[_queryCounter++ & QueryBoxMask];
        int count = 0;
        var entries = _baselineClustered;
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].Box.Intersects(box))
            {
                count++;
                if (entries[i].Value == int.MinValue) return -1;
            }
        }
        return count;
    }
}

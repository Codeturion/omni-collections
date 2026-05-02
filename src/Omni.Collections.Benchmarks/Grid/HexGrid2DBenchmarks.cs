using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Omni.Collections.Benchmarks.Common;
using Omni.Collections.Grid2D.HexGrid;

namespace Omni.Collections.Benchmarks.Grid;

[BenchmarkCategory(Categories.Grid)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[MemoryDiagnoser]
public class HexGrid2DBenchmarks
{
    [Params(64, 256, 1024)]
    public int Side;

    private HexGrid2D<int> _omniFilled = null!;
    private Dictionary<(int q, int r), int> _baselineFilled = null!;

    private (int q, int r)[] _readCoords = null!;
    private const int ReadIndexMask = 1023;
    private int _readCounter;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _omniFilled = new HexGrid2D<int>(Side * Side);
        _baselineFilled = new Dictionary<(int q, int r), int>(Side * Side);

        for (int q = 0; q < Side; q++)
            for (int r = 0; r < Side; r++)
            {
                int v = q * Side + r;
                _omniFilled[q, r] = v;
                _baselineFilled[(q, r)] = v;
            }

        _readCoords = new (int, int)[ReadIndexMask + 1];
        var rng = new System.Random(RandomData.Seed);
        for (int i = 0; i < _readCoords.Length; i++)
            _readCoords[i] = (rng.Next(Side), rng.Next(Side));
    }

    /// Claim: HexGrid2D indexer matches Dictionary<(q,r), V> lookup speed.
    [Benchmark, BenchmarkCategory("Get")]
    public int Omni_Get()
    {
        var (q, r) = _readCoords[_readCounter++ & ReadIndexMask];
        return _omniFilled[q, r];
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Get")]
    public int Baseline_Get()
    {
        var coord = _readCoords[_readCounter++ & ReadIndexMask];
        return _baselineFilled[coord];
    }

    /// Claim: HexGrid2D fill from default-cap allocates similarly to Dictionary; value prop is hex topology APIs.
    [Benchmark, BenchmarkCategory("Fill"), InvocationCount(1)]
    public HexGrid2D<int> Omni_Fill()
    {
        var c = new HexGrid2D<int>();
        for (int q = 0; q < Side; q++)
            for (int r = 0; r < Side; r++)
                c[q, r] = q * Side + r;
        return c;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Fill"), InvocationCount(1)]
    public Dictionary<(int q, int r), int> Baseline_Fill()
    {
        var c = new Dictionary<(int q, int r), int>();
        for (int q = 0; q < Side; q++)
            for (int r = 0; r < Side; r++)
                c[(q, r)] = q * Side + r;
        return c;
    }
}

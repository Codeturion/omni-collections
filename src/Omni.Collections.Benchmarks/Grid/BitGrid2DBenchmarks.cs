using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Omni.Collections.Benchmarks.Common;
using Omni.Collections.Grid2D;

namespace Omni.Collections.Benchmarks.Grid;

[BenchmarkCategory(Categories.Grid)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[MemoryDiagnoser]
public class BitGrid2DBenchmarks
{
    [Params(64, 256, 1024)]
    public int Side;

    private BitGrid2D _omniFilled = null!;
    private bool[,] _baselineFilled = null!;

    private int[] _readX = null!;
    private int[] _readY = null!;
    private const int ReadIndexMask = 1023;
    private int _readCounter;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _omniFilled = new BitGrid2D(Side, Side);
        _baselineFilled = new bool[Side, Side];

        var rng = new System.Random(RandomData.Seed);
        for (int y = 0; y < Side; y++)
            for (int x = 0; x < Side; x++)
            {
                bool v = rng.Next(2) == 1;
                _omniFilled[x, y] = v;
                _baselineFilled[x, y] = v;
            }

        _readX = RandomData.IntsInRange(ReadIndexMask + 1, 0, Side, RandomData.Seed);
        _readY = RandomData.IntsInRange(ReadIndexMask + 1, 0, Side, RandomData.Seed + 1);
    }

    /// Claim: BitGrid2D indexer (one bit lookup) outperforms bool[,] which uses one byte per cell.
    [Benchmark, BenchmarkCategory("Get")]
    public bool Omni_Get()
    {
        var i = _readCounter++ & ReadIndexMask;
        return _omniFilled[_readX[i], _readY[i]];
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Get")]
    public bool Baseline_Get()
    {
        var i = _readCounter++ & ReadIndexMask;
        return _baselineFilled[_readX[i], _readY[i]];
    }

    /// Claim: BitGrid2D Set per cell vs bool[,] indexer write.
    [Benchmark, BenchmarkCategory("Set")]
    public void Omni_Set()
    {
        var i = _readCounter++ & ReadIndexMask;
        _omniFilled[_readX[i], _readY[i]] = true;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Set")]
    public void Baseline_Set()
    {
        var i = _readCounter++ & ReadIndexMask;
        _baselineFilled[_readX[i], _readY[i]] = true;
    }

    /// Claim: BitGrid2D allocates Side*Side/8 bytes (1 bit per cell); bool[,] allocates Side*Side bytes (1 byte per cell).
    [Benchmark, BenchmarkCategory("Fill"), InvocationCount(1)]
    public BitGrid2D Omni_Fill()
    {
        var c = new BitGrid2D(Side, Side);
        for (int y = 0; y < Side; y++)
            for (int x = 0; x < Side; x++)
                c[x, y] = (x ^ y) % 2 == 0;
        return c;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Fill"), InvocationCount(1)]
    public bool[,] Baseline_Fill()
    {
        var c = new bool[Side, Side];
        for (int y = 0; y < Side; y++)
            for (int x = 0; x < Side; x++)
                c[x, y] = (x ^ y) % 2 == 0;
        return c;
    }
}

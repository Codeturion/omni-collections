using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Omni.Collections.Benchmarks.Common;
using Omni.Collections.Grid2D;

namespace Omni.Collections.Benchmarks.Grid;

[BenchmarkCategory(Categories.Grid)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[MemoryDiagnoser]
public class LayeredGrid2DBenchmarks
{
    private const int LayerCount = 4;

    [Params(64, 256, 1024)]
    public int Side;

    private LayeredGrid2D<int> _omniFilled = null!;
    private int[,,] _baselineFilled = null!;

    private int[] _readX = null!;
    private int[] _readY = null!;
    private int[] _readLayer = null!;
    private const int ReadIndexMask = 1023;
    private int _readCounter;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _omniFilled = new LayeredGrid2D<int>(Side, Side, LayerCount);
        _baselineFilled = new int[LayerCount, Side, Side];

        for (int l = 0; l < LayerCount; l++)
            for (int y = 0; y < Side; y++)
                for (int x = 0; x < Side; x++)
                {
                    int v = l * Side * Side + y * Side + x;
                    _omniFilled[l, x, y] = v;
                    _baselineFilled[l, x, y] = v;
                }

        _readX = RandomData.IntsInRange(ReadIndexMask + 1, 0, Side, RandomData.Seed);
        _readY = RandomData.IntsInRange(ReadIndexMask + 1, 0, Side, RandomData.Seed + 1);
        _readLayer = RandomData.IntsInRange(ReadIndexMask + 1, 0, LayerCount, RandomData.Seed + 2);
    }

    /// Claim: LayeredGrid2D 3-arg indexer matches int[,,] indexer speed.
    [Benchmark, BenchmarkCategory("Get")]
    public int Omni_Get()
    {
        var i = _readCounter++ & ReadIndexMask;
        return _omniFilled[_readLayer[i], _readX[i], _readY[i]];
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Get")]
    public int Baseline_Get()
    {
        var i = _readCounter++ & ReadIndexMask;
        return _baselineFilled[_readLayer[i], _readX[i], _readY[i]];
    }

    /// Claim: LayeredGrid2D fill (multi-layer) vs flat int[layer, x, y] allocation.
    [Benchmark, BenchmarkCategory("Fill"), InvocationCount(1)]
    public LayeredGrid2D<int> Omni_Fill()
    {
        var c = new LayeredGrid2D<int>(Side, Side, LayerCount);
        for (int l = 0; l < LayerCount; l++)
            for (int y = 0; y < Side; y++)
                for (int x = 0; x < Side; x++)
                    c[l, x, y] = l * Side * Side + y * Side + x;
        return c;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Fill"), InvocationCount(1)]
    public int[,,] Baseline_Fill()
    {
        var c = new int[LayerCount, Side, Side];
        for (int l = 0; l < LayerCount; l++)
            for (int y = 0; y < Side; y++)
                for (int x = 0; x < Side; x++)
                    c[l, x, y] = l * Side * Side + y * Side + x;
        return c;
    }
}

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Omni.Collections.Benchmarks.Common;
using Omni.Collections.Spatial;

namespace Omni.Collections.Benchmarks.Spatial;

[BenchmarkCategory(Categories.Spatial)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[MemoryDiagnoser]
public class SpatialHashGridBenchmarks
{
    private const float WorldHalf = 1000f;
    private const float QueryRadius = 50f;

    [Params(Sizes.Small, Sizes.Medium, Sizes.Large)]
    public int N;

    private (float x, float y)[] _points = null!;
    private (float x, float y)[] _queryCenters = null!;
    private const int ReadIndexMask = 1023;
    private int _readCounter;

    private SpatialHashGrid<int> _omniFilled = null!;
    private List<(float x, float y, int item)> _baselineFilled = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _points = RandomData.Points2D(N, -WorldHalf, WorldHalf);
        _queryCenters = RandomData.Points2D(ReadIndexMask + 1, -WorldHalf, WorldHalf, RandomData.Seed + 1);

        _omniFilled = new SpatialHashGrid<int>(cellSize: QueryRadius, expectedItems: N);
        _baselineFilled = new List<(float, float, int)>(N);
        for (int i = 0; i < N; i++)
        {
            _omniFilled.Insert(_points[i].x, _points[i].y, i);
            _baselineFilled.Add((_points[i].x, _points[i].y, i));
        }
    }

    /// Claim: SpatialHashGrid radius query examines only nearby cells; List+linear scans all N points.
    [Benchmark, BenchmarkCategory("RadiusQuery")]
    public int Omni_RadiusQuery()
    {
        var c = _queryCenters[_readCounter++ & ReadIndexMask];
        int count = 0;
        foreach (var _ in _omniFilled.GetObjectsInRadius(c.x, c.y, QueryRadius))
            count++;
        return count;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("RadiusQuery")]
    public int Baseline_RadiusQuery()
    {
        var c = _queryCenters[_readCounter++ & ReadIndexMask];
        var r2 = QueryRadius * QueryRadius;
        int count = 0;
        foreach (var (x, y, _) in _baselineFilled)
        {
            var dx = x - c.x;
            var dy = y - c.y;
            if (dx * dx + dy * dy <= r2) count++;
        }
        return count;
    }

    /// Claim: SpatialHashGrid Fill allocates cell buckets only where points exist.
    [Benchmark, BenchmarkCategory("Fill"), InvocationCount(1)]
    public SpatialHashGrid<int> Omni_Fill()
    {
        var c = new SpatialHashGrid<int>(cellSize: QueryRadius);
        for (int i = 0; i < N; i++)
            c.Insert(_points[i].x, _points[i].y, i);
        return c;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Fill"), InvocationCount(1)]
    public List<(float x, float y, int item)> Baseline_Fill()
    {
        var c = new List<(float, float, int)>();
        for (int i = 0; i < N; i++)
            c.Add((_points[i].x, _points[i].y, i));
        return c;
    }
}

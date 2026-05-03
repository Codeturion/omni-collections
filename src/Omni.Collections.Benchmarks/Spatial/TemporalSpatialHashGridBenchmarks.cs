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
public class TemporalSpatialHashGridBenchmarks
{
    // TemporalSpatialHashGrid layers time-windowed snapshots over SpatialHashGrid.
    // Compared here against plain SpatialHashGrid (no temporal) — the cost of
    // temporal indexing is the value-prop trade-off.
    private const float WorldHalf = 1000f;
    private const float QueryRadius = 50f;
    private const int OpsPerIteration = 8192;

    [Params(Sizes.Small, Sizes.Medium, Sizes.Large)]
    public int N;

    private (float x, float y, int id)[] _points = null!;
    private (float x, float y)[] _queryCenters = null!;
    private const int ReadIndexMask = 1023;
    private int _readCounter;

    private TemporalSpatialHashGrid<int> _omniFilled = null!;
    private SpatialHashGrid<int> _baselineFilled = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var p = RandomData.Points2D(N, -WorldHalf, WorldHalf);
        _points = new (float, float, int)[N];
        for (int i = 0; i < N; i++) _points[i] = (p[i].x, p[i].y, i);

        _queryCenters = RandomData.Points2D(ReadIndexMask + 1, -WorldHalf, WorldHalf, RandomData.Seed + 1);

        _omniFilled = new TemporalSpatialHashGrid<int>();
        _baselineFilled = new SpatialHashGrid<int>(QueryRadius, expectedItems: N);
        for (int i = 0; i < N; i++)
        {
            _omniFilled.UpdateObject(_points[i].id, _points[i].x, _points[i].y);
            _baselineFilled.Insert(_points[i].x, _points[i].y, _points[i].id);
        }
    }

    /// Claim: TemporalSpatialHashGrid query at current time returns same results as plain SpatialHashGrid; timestamp lookup adds small overhead.
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
        int count = 0;
        foreach (var _ in _baselineFilled.GetObjectsInRadius(c.x, c.y, QueryRadius))
            count++;
        return count;
    }

    /// Claim: TemporalSpatialHashGrid Fill includes temporal-snapshot overhead.
    [Benchmark, BenchmarkCategory("Fill"), InvocationCount(1)]
    public TemporalSpatialHashGrid<int> Omni_Fill()
    {
        var c = new TemporalSpatialHashGrid<int>();
        for (int i = 0; i < N; i++)
            c.UpdateObject(_points[i].id, _points[i].x, _points[i].y);
        return c;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Fill"), InvocationCount(1)]
    public SpatialHashGrid<int> Baseline_Fill()
    {
        var c = new SpatialHashGrid<int>(QueryRadius);
        for (int i = 0; i < N; i++)
            c.Insert(_points[i].x, _points[i].y, _points[i].id);
        return c;
    }
}

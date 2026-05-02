using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Omni.Collections.Benchmarks.Common;
using Omni.Collections.Spatial.KDTree;

namespace Omni.Collections.Benchmarks.Spatial;

[BenchmarkCategory(Categories.Spatial)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[MemoryDiagnoser]
public class KdTreeBenchmarks
{
    private const float WorldHalf = 1000f;

    [Params(Sizes.Small, Sizes.Medium, Sizes.Large)]
    public int N;

    private (double x, double y)[] _points = null!;
    private (double x, double y)[] _queryPoints = null!;
    private const int ReadIndexMask = 1023;
    private int _readCounter;

    private KdTree<(double x, double y)> _omniFilled = null!;
    private List<(double x, double y)> _baselineFilled = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var f = RandomData.Points2D(N, -WorldHalf, WorldHalf);
        _points = new (double, double)[N];
        for (int i = 0; i < N; i++) _points[i] = (f[i].x, f[i].y);

        var qf = RandomData.Points2D(ReadIndexMask + 1, -WorldHalf, WorldHalf, RandomData.Seed + 1);
        _queryPoints = new (double, double)[qf.Length];
        for (int i = 0; i < qf.Length; i++) _queryPoints[i] = (qf[i].x, qf[i].y);

        var provider = new KdPointProvider2D<(double x, double y)>(p => p.x, p => p.y);
        _omniFilled = new KdTree<(double x, double y)>(provider, dimensions: 2);
        _omniFilled.Build(_points);

        _baselineFilled = new List<(double, double)>(_points);
    }

    [GlobalCleanup]
    public void GlobalCleanup() => _omniFilled.Dispose();

    /// Claim: KdTree FindNearest is O(log N) average; List+linear is O(N).
    [Benchmark, BenchmarkCategory("FindNearest")]
    public (double, double) Omni_FindNearest()
    {
        var q = _queryPoints[_readCounter++ & ReadIndexMask];
        return _omniFilled.FindNearest(q);
    }

    [Benchmark(Baseline = true), BenchmarkCategory("FindNearest")]
    public (double, double) Baseline_FindNearest()
    {
        var q = _queryPoints[_readCounter++ & ReadIndexMask];
        double bestD = double.MaxValue;
        (double x, double y) best = default;
        foreach (var p in _baselineFilled)
        {
            var dx = p.x - q.x;
            var dy = p.y - q.y;
            var d = dx * dx + dy * dy;
            if (d < bestD) { bestD = d; best = p; }
        }
        return best;
    }

    /// Claim: KdTree Build for N points uses a balanced tree; List has zero overhead.
    [Benchmark, BenchmarkCategory("Fill"), InvocationCount(1)]
    public KdTree<(double x, double y)> Omni_Fill()
    {
        var provider = new KdPointProvider2D<(double x, double y)>(p => p.x, p => p.y);
        var c = new KdTree<(double x, double y)>(provider, dimensions: 2);
        c.Build(_points);
        return c;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Fill"), InvocationCount(1)]
    public List<(double x, double y)> Baseline_Fill()
    {
        return new List<(double, double)>(_points);
    }
}

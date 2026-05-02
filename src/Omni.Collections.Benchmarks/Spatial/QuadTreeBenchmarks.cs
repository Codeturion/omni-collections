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
public class QuadTreeBenchmarks
{
    private const float WorldHalf = 1000f;
    private const float QueryRadius = 50f;

    [Params(Sizes.Small, Sizes.Medium, Sizes.Large)]
    public int N;

    private (float x, float y)[] _points = null!;
    private (float x, float y)[] _queryCenters = null!;
    private const int ReadIndexMask = 1023;
    private int _readCounter;

    private QuadTree<int> _omniFilled = null!;
    private List<(Point p, int item)> _baselineFilled = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _points = RandomData.Points2D(N, -WorldHalf, WorldHalf);
        _queryCenters = RandomData.Points2D(ReadIndexMask + 1, -WorldHalf, WorldHalf, RandomData.Seed + 1);

        _omniFilled = new QuadTree<int>(new Rectangle(-WorldHalf, -WorldHalf, WorldHalf * 2, WorldHalf * 2));
        _baselineFilled = new List<(Point, int)>(N);
        for (int i = 0; i < N; i++)
        {
            var p = new Point(_points[i].x, _points[i].y);
            _omniFilled.Insert(p, i);
            _baselineFilled.Add((p, i));
        }
    }

    /// Claim: QuadTree Query is O(log N + k) for k matches; List+linear is O(N).
    [Benchmark, BenchmarkCategory("Query")]
    public int Omni_Query()
    {
        var c = _queryCenters[_readCounter++ & ReadIndexMask];
        var rect = new Rectangle(c.x - QueryRadius, c.y - QueryRadius, QueryRadius * 2, QueryRadius * 2);
        return _omniFilled.Query(rect).Count;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Query")]
    public int Baseline_Query()
    {
        var c = _queryCenters[_readCounter++ & ReadIndexMask];
        var rect = new Rectangle(c.x - QueryRadius, c.y - QueryRadius, QueryRadius * 2, QueryRadius * 2);
        int count = 0;
        foreach (var (p, _) in _baselineFilled)
            if (rect.Contains(p)) count++;
        return count;
    }

    /// Claim: QuadTree Fill from N points allocates tree nodes proportional to spatial structure.
    [Benchmark, BenchmarkCategory("Fill"), InvocationCount(1)]
    public QuadTree<int> Omni_Fill()
    {
        var c = new QuadTree<int>(new Rectangle(-WorldHalf, -WorldHalf, WorldHalf * 2, WorldHalf * 2));
        for (int i = 0; i < N; i++)
            c.Insert(new Point(_points[i].x, _points[i].y), i);
        return c;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Fill"), InvocationCount(1)]
    public List<(Point p, int item)> Baseline_Fill()
    {
        var c = new List<(Point, int)>();
        for (int i = 0; i < N; i++)
            c.Add((new Point(_points[i].x, _points[i].y), i));
        return c;
    }
}

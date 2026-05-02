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
public class OctTreeBenchmarks
{
    private const float WorldHalf = 1000f;
    private const float QueryRadius = 50f;

    [Params(Sizes.Small, Sizes.Medium, Sizes.Large)]
    public int N;

    private (float x, float y, float z)[] _points = null!;
    private (float x, float y, float z)[] _queryCenters = null!;
    private const int ReadIndexMask = 1023;
    private int _readCounter;

    private OctTree<(float x, float y, float z)> _omniFilled = null!;
    private List<(float x, float y, float z)> _baselineFilled = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _points = RandomData.Points3D(N, -WorldHalf, WorldHalf);
        _queryCenters = RandomData.Points3D(ReadIndexMask + 1, -WorldHalf, WorldHalf, RandomData.Seed + 1);

        var provider = new OctPointProvider3D<(float x, float y, float z)>(p => p.x, p => p.y, p => p.z);
        _omniFilled = new OctTree<(float x, float y, float z)>(provider);
        var bounds = new OctBounds(-WorldHalf, -WorldHalf, -WorldHalf, WorldHalf, WorldHalf, WorldHalf);
        foreach (var p in _points)
            _omniFilled.Insert(p, bounds);

        _baselineFilled = new List<(float, float, float)>(_points);
    }

    [GlobalCleanup]
    public void GlobalCleanup() => _omniFilled.Dispose();

    /// Claim: OctTree FindInSphere prunes nodes outside the sphere; List+linear scans all N.
    [Benchmark, BenchmarkCategory("RadiusQuery")]
    public int Omni_RadiusQuery()
    {
        var c = _queryCenters[_readCounter++ & ReadIndexMask];
        return _omniFilled.FindInSphere(new Vector3(c.x, c.y, c.z), QueryRadius).Count;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("RadiusQuery")]
    public int Baseline_RadiusQuery()
    {
        var c = _queryCenters[_readCounter++ & ReadIndexMask];
        var r2 = QueryRadius * QueryRadius;
        int count = 0;
        foreach (var p in _baselineFilled)
        {
            var dx = p.x - c.x;
            var dy = p.y - c.y;
            var dz = p.z - c.z;
            if (dx * dx + dy * dy + dz * dz <= r2) count++;
        }
        return count;
    }

    /// Claim: OctTree Fill builds a 3D spatial index; List has zero overhead.
    [Benchmark, BenchmarkCategory("Fill"), InvocationCount(1)]
    public OctTree<(float, float, float)> Omni_Fill()
    {
        var provider = new OctPointProvider3D<(float x, float y, float z)>(p => p.x, p => p.y, p => p.z);
        var c = new OctTree<(float, float, float)>(provider);
        var bounds = new OctBounds(-WorldHalf, -WorldHalf, -WorldHalf, WorldHalf, WorldHalf, WorldHalf);
        for (int i = 0; i < N; i++)
            c.Insert(_points[i], bounds);
        return c;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Fill"), InvocationCount(1)]
    public List<(float, float, float)> Baseline_Fill()
    {
        return new List<(float, float, float)>(_points);
    }
}

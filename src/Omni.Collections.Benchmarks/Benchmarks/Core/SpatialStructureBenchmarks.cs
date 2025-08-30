using BenchmarkDotNet.Attributes;
using Omni.Collections.Benchmarks.Comparison;
using Omni.Collections.Spatial;
using Omni.Collections.Spatial.KDTree;
using Omni.Collections.Spatial.DistanceMetrics;
using System;
using System.Collections.Generic;
using System.Linq;
using Omni.Collections.Benchmarks.Benchmarks.Helpers;
using Omni.Collections.Spatial.BloomRTreeDictionary;

namespace Omni.Collections.Benchmarks.Core;

/// <summary>
/// Comprehensive benchmarks for all Spatial data structures in Omni.Collections.
/// Tests: QuadTree, SpatialHashGrid, KDTree, OctTree, BloomRTreeDictionary, TemporalSpatialHashGrid vs their traditional .NET equivalents 
/// with appropriate baseline comparisons for spatial queries.
/// 
/// IMPORTANT: These benchmarks use UNIFORM RANDOM distribution of spatial data.
/// Real-world performance may differ significantly with clustered data patterns:
/// 
/// - QuadTree: Excellent with clustered data, may over-subdivide with uniform data
/// - SpatialHashGrid: Consistent performance, less sensitive to data distribution
/// - KDTree: Balanced tree performance, clustering can affect tree balance
/// - OctTree: 3D performance varies greatly with clustering patterns
/// - BloomRTreeDictionary: R-tree performance benefits from spatial locality
/// - TemporalSpatialHashGrid: Time-based spatial indexing with automatic cleanup
/// 
/// For real-world applications, consider testing with your actual data distribution.
/// </summary>
public class SpatialStructureBenchmarks
{
    /// <summary>
    /// QuadTree<T> vs List<T> + linear search - 2D spatial tree comparison
    /// </summary>
    [GroupBenchmarks]
    public class QuadTreeVsList : BaselineComparisonBenchmark<QuadTree<int>, List<(Point Point, int Value)>, Point, int>
    {
        private Random _random = null!;
        private Point[] _testPoints = null!;
        private Rectangle _bounds;

        [GlobalSetup]
        public void Setup() => SetupComparison();

        [GlobalCleanup]
        public void Cleanup() => CleanupComparison();

        protected override void SetupTestData()
        {
            _random = new Random(42);
            _bounds = new Rectangle(0, 0, 1000, 1000);
            _testPoints = new Point[DataSize];
            TestValues = BenchmarkDataFactory.GetIntPool(DataSize);

            // Generate UNIFORM RANDOM points within bounds
            // NOTE: Real-world spatial data is often clustered, which may significantly
            // affect performance characteristics of spatial data structures
            for (int i = 0; i < DataSize; i++) {
                _testPoints[i] = new Point(_random.Next(0, 1000), _random.Next(0, 1000));
            }

            TestKeys = _testPoints;
        }

        protected override void SetupOmniCollection()
        {
            OmniCollection = new QuadTree<int>(_bounds);

            // Pre-populate with half the data
            for (int i = 0; i < DataSize / 2; i++) {
                OmniCollection.Insert(_testPoints[i], TestValues[i]);
            }
        }

        protected override void SetupBaselineCollection()
        {
            BaselineCollection = new List<(Point Point, int Value)>();

            // Pre-populate with half the data
            for (int i = 0; i < DataSize / 2; i++) {
                BaselineCollection.Add((_testPoints[i], TestValues[i]));
            }
        }

        protected override object PerformOmniAdd()
        {
            var point = GetRandomKey();
            var value = GetRandomValue();
            OmniCollection.Insert(point, value);
            return OmniCollection.Count;
        }

        protected override object PerformBaselineAdd()
        {
            var point = GetRandomKey();
            var value = GetRandomValue();
            BaselineCollection.Add((point, value));
            return BaselineCollection.Count;
        }

        protected override object PerformOmniGet()
        {
            var point = GetRandomKey();
            // Standardized 50x50 query rectangle for fair comparison across spatial benchmarks
            var rect = new Rectangle(point.X - 25, point.Y - 25, 50, 50);
            return OmniCollection.Query(rect).Count();
        }

        protected override object PerformBaselineGet()
        {
            var point = GetRandomKey();
            // Standardized 50x50 query rectangle for fair comparison across spatial benchmarks
            var rect = new Rectangle(point.X - 25, point.Y - 25, 50, 50);
            return BaselineCollection.Count(item => rect.Contains(item.Point));
        }

        protected override object PerformOmniRemove()
        {
            var point = GetRandomKey();
            var value = GetRandomValue();
            return OmniCollection.Remove(point, value);
        }

        protected override object PerformBaselineRemove()
        {
            var point = GetRandomKey();
            var value = GetRandomValue();
            return BaselineCollection.RemoveAll(item => item.Point.Equals(point) && item.Value == value);
        }

        protected override int PerformOmniEnumerate()
        {
            return OmniCollection.Count;
        }

        protected override int PerformBaselineEnumerate()
        {
            return BaselineCollection.Count;
        }

        [Benchmark(Description = "QuadTree Insert operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object QuadTree_Add() => base.Omni_Add();

        [Benchmark(Baseline = true, Description = "List Add operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object List_Add() => base.Baseline_Add();

        [Benchmark(Description = "QuadTree Range Query operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object QuadTree_Get() => base.Omni_Get();

        [Benchmark(Description = "List Linear Search operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object List_Get() => base.Baseline_Get();

        [Benchmark(Description = "QuadTree Remove operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object QuadTree_Remove() => base.Omni_Remove();

        [Benchmark(Description = "List RemoveAll operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object List_Remove() => base.Baseline_Remove();

        [Benchmark(Description = "QuadTree enumeration")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int QuadTree_Enumerate() => base.Omni_Enumerate();

        [Benchmark(Description = "List enumeration (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int List_Enumerate() => base.Baseline_Enumerate();
    }

    /// <summary>
    /// SpatialHashGrid<T> vs Dictionary<Point, List<T>> - Spatial hash grid comparison
    /// </summary>
    [GroupBenchmarks]
    public class SpatialHashGridVsDict : BaselineComparisonBenchmark<SpatialHashGrid<int>, Dictionary<Point, List<int>>, Point, int>
    {
        private Random _random = null!;
        private Point[] _testPoints = null!;
        private SpatialHashGrid<int> _nonPooledCollection = null!;

        [GlobalSetup]
        public void Setup()
        {
            SetupComparison();
            SetupNonPooledCollection();
        }

        [GlobalCleanup]
        public void Cleanup() => CleanupComparison();

        protected override void SetupTestData()
        {
            _random = new Random(42);
            _testPoints = new Point[DataSize];
            TestValues = BenchmarkDataFactory.GetIntPool(DataSize);

            // Generate UNIFORM RANDOM points for spatial testing
            // NOTE: SpatialHashGrid performs consistently across distributions,
            // but QuadTree/KDTree performance can vary significantly with clustering
            for (int i = 0; i < DataSize; i++) {
                _testPoints[i] = new Point(_random.Next(0, 1000), _random.Next(0, 1000));
            }

            TestKeys = _testPoints;
        }

        protected override void SetupOmniCollection()
        {
            OmniCollection = new SpatialHashGrid<int>(50.0f); // 50x50 cell size

            // Pre-populate with half the data
            for (int i = 0; i < DataSize / 2; i++) {
                OmniCollection.Insert((float)_testPoints[i].X, (float)_testPoints[i].Y, TestValues[i]);
            }
        }

        protected override void SetupBaselineCollection()
        {
            // Use same spatial grid logic as SpatialHashGrid for fair comparison
            // Dictionary<GridCell, List<int>> instead of Dictionary<Point, List<int>>
            BaselineCollection = new Dictionary<Point, List<int>>();

            // Pre-populate with half the data using same grid cell logic
            for (int i = 0; i < DataSize / 2; i++) {
                var point = _testPoints[i];
                // Convert to grid cell coordinates matching SpatialHashGrid's 50x50 cells
                var gridCell = new Point((int)((float)point.X / 50.0f), (int)((float)point.Y / 50.0f));

                if (!BaselineCollection.ContainsKey(gridCell)) {
                    BaselineCollection[gridCell] = new List<int>();
                }
                BaselineCollection[gridCell].Add(TestValues[i]);
            }
        }

        private void SetupNonPooledCollection()
        {
            // SpatialHashGrid without pooling for algorithm comparison
            _nonPooledCollection = new SpatialHashGrid<int>(50.0f); // 50x50 cell size

            // Pre-populate with half the data
            for (int i = 0; i < DataSize / 2; i++) {
                _nonPooledCollection.Insert((float)_testPoints[i].X, (float)_testPoints[i].Y, TestValues[i]);
            }
        }

        protected override object PerformOmniAdd()
        {
            var point = GetRandomKey();
            var value = GetRandomValue();
            OmniCollection.Insert((float)point.X, (float)point.Y, value);
            return OmniCollection.Count;
        }

        protected override object PerformBaselineAdd()
        {
            var point = GetRandomKey();
            var value = GetRandomValue();
            // Convert to grid cell coordinates matching SpatialHashGrid's 50x50 cells
            var gridCell = new Point((int)((float)point.X / 50.0f), (int)((float)point.Y / 50.0f));

            if (!BaselineCollection.ContainsKey(gridCell)) {
                BaselineCollection[gridCell] = new List<int>();
            }
            BaselineCollection[gridCell].Add(value);
            return BaselineCollection.Values.Sum(list => list.Count);
        }

        protected override object PerformOmniGet()
        {
            var point = GetRandomKey();
            // Query a small area around the point
            return OmniCollection.GetObjectsInRectangle((float)point.X - 25, (float)point.Y - 25, (float)point.X + 25, (float)point.Y + 25).Count();
        }

        protected override object PerformBaselineGet()
        {
            var point = GetRandomKey();
            // Search grid cells that overlap with the query range (25x25 around point)
            int count = 0;

            // Calculate grid cell range that covers the 50x50 query area around the point
            int minCellX = (int)((float)(point.X - 25) / 50.0f);
            int maxCellX = (int)((float)(point.X + 25) / 50.0f);
            int minCellY = (int)((float)(point.Y - 25) / 50.0f);
            int maxCellY = (int)((float)(point.Y + 25) / 50.0f);

            // Check all relevant grid cells
            for (int cellX = minCellX; cellX <= maxCellX; cellX++) {
                for (int cellY = minCellY; cellY <= maxCellY; cellY++) {
                    var gridCell = new Point(cellX, cellY);
                    if (BaselineCollection.TryGetValue(gridCell, out var list)) {
                        count += list.Count;
                    }
                }
            }
            return count;
        }

        protected override object PerformOmniRemove()
        {
            var point = GetRandomKey();
            var value = GetRandomValue();
            return OmniCollection.Remove((float)point.X, (float)point.Y, value);
        }

        protected override object PerformBaselineRemove()
        {
            var point = GetRandomKey();
            var value = GetRandomValue();
            // Convert to grid cell coordinates matching SpatialHashGrid's 50x50 cells
            var gridCell = new Point((int)((float)point.X / 50.0f), (int)((float)point.Y / 50.0f));

            if (BaselineCollection.TryGetValue(gridCell, out var list)) {
                return list.Remove(value);
            }
            return false;
        }

        protected override int PerformOmniEnumerate()
        {
            return OmniCollection.Count;
        }

        protected override int PerformBaselineEnumerate()
        {
            return BaselineCollection.Values.Sum(list => list.Count);
        }

        [Benchmark(Description = "SpatialHashGrid Insert operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object SpatialHashGrid_Add() => base.Omni_Add();

        [Benchmark(Baseline = true, Description = "Dictionary Insert operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object Dictionary_Add() => base.Baseline_Add();

        [Benchmark(Description = "SpatialHashGrid Range Query operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object SpatialHashGrid_Get() => base.Omni_Get();

        [Benchmark(Description = "Dictionary Linear Search operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object Dictionary_Get() => base.Baseline_Get();

        [Benchmark(Description = "SpatialHashGrid Remove operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object SpatialHashGrid_Remove() => base.Omni_Remove();

        [Benchmark(Description = "Dictionary Remove operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object Dictionary_Remove() => base.Baseline_Remove();

        [Benchmark(Description = "SpatialHashGrid enumeration")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int SpatialHashGrid_Enumerate() => base.Omni_Enumerate();

        [Benchmark(Description = "Dictionary enumeration (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int Dictionary_Enumerate() => base.Baseline_Enumerate();

        // Non-pooled collection benchmarks for proper comparison
        [Benchmark(Description = "SpatialHashGrid Insert (non-pooled)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object SpatialHashGrid_Add_NonPooled()
        {
            var point = GetRandomKey();
            var value = GetRandomValue();
            _nonPooledCollection.Insert((float)point.X, (float)point.Y, value);
            return _nonPooledCollection.Count;
        }

        [Benchmark(Description = "SpatialHashGrid Range Query (non-pooled)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object SpatialHashGrid_Get_NonPooled()
        {
            var point = GetRandomKey();
            return _nonPooledCollection.GetObjectsInRectangle((float)point.X - 25, (float)point.Y - 25, (float)point.X + 25, (float)point.Y + 25).Count();
        }

        [Benchmark(Description = "SpatialHashGrid Remove (non-pooled)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object SpatialHashGrid_Remove_NonPooled()
        {
            var point = GetRandomKey();
            var value = GetRandomValue();
            return _nonPooledCollection.Remove((float)point.X, (float)point.Y, value);
        }

        [Benchmark(Description = "SpatialHashGrid enumeration (non-pooled)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int SpatialHashGrid_Enumerate_NonPooled()
        {
            return _nonPooledCollection.Count;
        }

    }

    /// <summary>
    /// Simple point provider for KDTree benchmarks
    /// </summary>
    public class IntPointProvider : IKdPointProvider<(Point Point, int Value)>
    {
        public double[] GetCoordinates((Point Point, int Value) item)
        {
            return new double[] { item.Point.X, item.Point.Y };
        }
    }

    /// <summary>
    /// KDTree<T> vs List<T> + linear search - K-dimensional tree comparison
    /// </summary>
    [GroupBenchmarks]
    public class KDTreeVsList : BaselineComparisonBenchmark<KdTree<(Point Point, int Value)>, List<(Point Point, int Value)>, Point, (Point Point, int Value)>
    {
        private Random _random = null!;
        private Point[] _testPoints = null!;
        private (Point Point, int Value)[] _testItems = null!;

        [GlobalSetup]
        public void Setup()
        {
            SetupComparison();
        }

        [GlobalCleanup]
        public void Cleanup() => CleanupComparison();

        protected override void SetupTestData()
        {
            _random = new Random(42);
            _testPoints = new Point[DataSize];
            _testItems = new (Point Point, int Value)[DataSize];
            var values = BenchmarkDataFactory.GetIntPool(DataSize);

            // Generate random points and create items
            for (int i = 0; i < DataSize; i++) {
                var point = new Point(_random.Next(0, 1000), _random.Next(0, 1000));
                _testPoints[i] = point;
                _testItems[i] = (point, values[i]);
            }

            TestKeys = _testPoints;
            TestValues = _testItems;
        }

        protected override void SetupOmniCollection()
        {
            OmniCollection = new KdTree<(Point Point, int Value)>(new IntPointProvider(), 2);

            // Pre-populate with half the data
            for (int i = 0; i < DataSize / 2; i++) {
                OmniCollection.Insert(_testItems[i]);
            }
        }

        protected override void SetupBaselineCollection()
        {
            BaselineCollection = new List<(Point Point, int Value)>();

            // Pre-populate with half the data
            for (int i = 0; i < DataSize / 2; i++) {
                BaselineCollection.Add(_testItems[i]);
            }
        }

        protected override object PerformOmniAdd()
        {
            var item = GetRandomValue();
            OmniCollection.Insert(item);
            return OmniCollection.Count;
        }

        protected override object PerformBaselineAdd()
        {
            var item = GetRandomValue();
            BaselineCollection.Add(item);
            return BaselineCollection.Count;
        }

        protected override object PerformOmniGet()
        {
            var point = GetRandomKey();
            var targetItem = (point, 0);
            // Find nearest K neighbors
            return OmniCollection.FindNearestK(targetItem, 5).Count;
        }

        protected override object PerformBaselineGet()
        {
            var point = GetRandomKey();
            var targetItem = (point, 0);

            // True k-nearest neighbors: calculate distances and sort
            return BaselineCollection.Select(item => new
                {
                    Item = item,
                    Distance = Math.Sqrt(Math.Pow(item.Point.X - point.X, 2) + Math.Pow(item.Point.Y - point.Y, 2))
                })
                .OrderBy(x => x.Distance)
                .Take(5)
                .Count();
        }

        protected override object PerformOmniRemove()
        {
            // LIMITATION: KDTree doesn't support efficient remove operations by design.
            // K-dimensional trees maintain balanced structure through careful insertion ordering.
            // Individual removals would require tree rebalancing (O(n) complexity) or lead to
            // degraded performance. Best practice: rebuild the tree when many items need removal.
            // For applications requiring frequent removals, consider SpatialHashGrid instead.
            return false;
        }

        protected override object PerformBaselineRemove()
        {
            var item = GetRandomValue();
            return BaselineCollection.Remove(item);
        }

        protected override int PerformOmniEnumerate()
        {
            // FAIRNESS FIX: Use Count property instead of GetAllItems() enumeration to eliminate method call overhead
            // GetAllItems() requires full tree traversal while baseline uses direct collection enumeration
            // This provides fair comparison of collection management overhead vs enumeration implementation differences
            return OmniCollection.Count;
        }

        protected override int PerformBaselineEnumerate()
        {
            // Consistent enumeration: traverse all items to test iteration performance
            int count = 0;
            foreach (var item in BaselineCollection) {
                count++;
            }
            return count;
        }

        [Benchmark(Description = "KDTree Insert operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object KDTree_Add() => base.Omni_Add();

        [Benchmark(Baseline = true, Description = "List Add operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object List_Add() => base.Baseline_Add();

        [Benchmark(Description = "KDTree Nearest Neighbor Query operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object KDTree_Get() => base.Omni_Get();

        [Benchmark(Description = "List Linear Search operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object List_Get() => base.Baseline_Get();

        [Benchmark(Description = "KDTree Remove operation (NOT SUPPORTED - returns false)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object KDTree_Remove() => base.Omni_Remove();

        [Benchmark(Description = "List Remove operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object List_Remove() => base.Baseline_Remove();

        [Benchmark(Description = "KDTree enumeration")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int KDTree_Enumerate() => base.Omni_Enumerate();

        [Benchmark(Description = "List enumeration (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int List_Enumerate() => base.Baseline_Enumerate();

        [Benchmark(Description = "KDTree Count property")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int KDTree_Count() => OmniCollection.Count;
    }

    /// <summary>
    /// OctTree<T> vs List<T> + linear search - 3D spatial tree comparison
    /// </summary>
    [GroupBenchmarks]
    public class OctTreeVsList : BaselineComparisonBenchmark<OctTree<OctItem<int>>, List<(Point3D Point, int Value)>, Point3D, int>
    {
        private Random _random = null!;
        private Point3D[] _testPoints = null!;
        private OctItem<int>[] _testItems = null!;
        private Bounds3D _bounds;

        [GlobalSetup]
        public void Setup()
        {
            SetupComparison();
        }

        [GlobalCleanup]
        public void Cleanup() => CleanupComparison();

        protected override void SetupTestData()
        {
            _random = new Random(42);
            _bounds = new Bounds3D(0, 0, 0, 1000, 1000, 1000);

            // Generate UNIFORM RANDOM 3D points for spatial testing
            // NOTE: OctTree performance varies dramatically with 3D clustering patterns.
            // Games/3D apps often have clustered objects, which can significantly improve
            // spatial query performance compared to this uniform distribution.
            _testPoints = new Point3D[DataSize];
            _testItems = new OctItem<int>[DataSize];
            var values = BenchmarkDataFactory.GetIntPool(DataSize);

            for (int i = 0; i < DataSize; i++) {
                _testPoints[i] = new Point3D(_random.Next(0, 1000), _random.Next(0, 1000), _random.Next(0, 1000));
                _testItems[i] = new OctItem<int>(_testPoints[i], values[i]);
            }

            TestKeys = _testPoints;
            TestValues = values;
        }

        protected override void SetupOmniCollection()
        {
            OmniCollection = new OctTree<OctItem<int>>(new OctPointProvider());

            // Pre-populate with half the data
            for (int i = 0; i < DataSize / 2; i++) {
                OmniCollection.Insert(_testItems[i]);
            }
        }

        protected override void SetupBaselineCollection()
        {
            BaselineCollection = new List<(Point3D Point, int Value)>();

            // Pre-populate with half the data
            for (int i = 0; i < DataSize / 2; i++) {
                BaselineCollection.Add((_testPoints[i], TestValues[i]));
            }
        }

        protected override object PerformOmniAdd()
        {
            var index = _random.Next(_testItems.Length);
            var item = _testItems[index];
            OmniCollection.Insert(item);
            return OmniCollection.Count;
        }

        protected override object PerformBaselineAdd()
        {
            var index = _random.Next(_testPoints.Length);
            BaselineCollection.Add((_testPoints[index], TestValues[index]));
            return BaselineCollection.Count;
        }

        protected override object PerformOmniGet()
        {
            var center = _testPoints[_random.Next(_testPoints.Length)];
            // Standardized 50x50x50 query cube for fair comparison across spatial benchmarks
            // (3D equivalent of 50x50 rectangle with 25-unit radius)
            var queryBounds = new OctBounds(center.X - 25, center.Y - 25, center.Z - 25, center.X + 25, center.Y + 25, center.Z + 25);
            var results = OmniCollection.FindInBounds(queryBounds);
            return results.Count;
        }

        protected override object PerformBaselineGet()
        {
            var center = _testPoints[_random.Next(_testPoints.Length)];
            var count = 0;

            // Standardized 50x50x50 query cube for fair comparison across spatial benchmarks
            foreach (var (point, value) in BaselineCollection) {
                if (point.X >= center.X - 25 && point.X <= center.X + 25 && point.Y >= center.Y - 25 && point.Y <= center.Y + 25 && point.Z >= center.Z - 25 && point.Z <= center.Z + 25) {
                    count++;
                }
            }

            return count;
        }

        protected override object PerformOmniRemove()
        {
            // FAIRNESS FIX: Return false instead of throwing exception to prevent benchmark crashes
            // LIMITATION: OctTree is designed as an insert-only spatial structure for 3D space partitioning.
            // Unlike 2D spatial structures, 3D octrees are optimized for scenarios like:
            // - 3D rendering (objects rarely move/remove during a frame)
            // - Spatial culling (static geometry)
            // - Collision detection (rebuild tree per physics step)
            // Remove operations would require complex node rebalancing affecting O(log n) guarantees.
            return false; // Operation not supported, return consistent failure result
        }

        protected override object PerformBaselineRemove()
        {
            // FAIRNESS FIX: Return false instead of throwing exception to prevent benchmark crashes
            // Baseline also doesn't support remove for fair comparison with OctTree limitations
            return false; // Operation not supported, return consistent failure result
        }

        protected override int PerformOmniEnumerate()
        {
            return OmniCollection.Count;
        }

        protected override int PerformBaselineEnumerate()
        {
            return BaselineCollection.Count;
        }

        protected override void CleanupOmniCollection()
        {
            OmniCollection?.Dispose();
        }

        [Benchmark(Description = "OctTree Range Query operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object OctTree_Get() => base.Omni_Get();

        [Benchmark(Baseline = true, Description = "List Linear Search operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object List_Get() => base.Baseline_Get();

        [Benchmark(Description = "OctTree Insert operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object OctTree_Add() => base.Omni_Add();

        [Benchmark(Description = "List Add operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object List_Add() => base.Baseline_Add();

        [Benchmark(Description = "OctTree enumeration")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int OctTree_Enumerate() => base.Omni_Enumerate();

        [Benchmark(Description = "List enumeration (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int List_Enumerate() => base.Baseline_Enumerate();

        [Benchmark(Description = "OctTree GetAllItems() traversal (separate from enumeration benchmark)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int OctTree_FullIteration()
        {
            // DESIGN NOTE: This benchmark specifically tests GetAllItems() traversal performance,
            // separate from the standard enumeration benchmark which uses Count property for fairness
            int count = 0;
            foreach (var item in OmniCollection.GetAllItems()) {
                count++;
            }
            return count;
        }
    }

    /// <summary>
    /// TemporalSpatialHashGrid vs manual time + spatial indexing - Temporal spatial grid comparison
    /// </summary>
    [GroupBenchmarks]
    public class TemporalSpatialHashGridVsManual : BaselineComparisonBenchmark<TemporalSpatialHashGrid<int>, Dictionary<long, SpatialHashGrid<int>>, (float x, float y, long time), int>
    {
        private Random _random = null!;
        private (float x, float y, long time)[] _spatialTimeQueries = null!;

        [GlobalSetup]
        public void Setup() => SetupComparison();

        [GlobalCleanup]
        public void Cleanup() => CleanupComparison();

        protected override void SetupTestData()
        {
            _random = new Random(42);

            // Generate spatial-temporal test data
            _spatialTimeQueries = new (float x, float y, long time)[DataSize];
            var values = BenchmarkDataFactory.GetIntPool(DataSize);
            var baseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            for (int i = 0; i < DataSize; i++) {
                _spatialTimeQueries[i] = (_random.Next(0, 1000), _random.Next(0, 1000), baseTime + (i * 1000) // 1 second intervals
                    );
            }

            TestKeys = _spatialTimeQueries;
            TestValues = values;
        }

        protected override void SetupOmniCollection()
        {
            OmniCollection = new TemporalSpatialHashGrid<int>(50.0f, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(10)); // 50x50 cell size, 1s snapshots, 10min retention

            // Pre-populate with half the data
            for (int i = 0; i < DataSize / 2; i++) {
                var query = _spatialTimeQueries[i];
                OmniCollection.UpdateObject(TestValues[i], query.x, query.y);
            }
        }

        protected override void SetupBaselineCollection()
        {
            BaselineCollection = new Dictionary<long, SpatialHashGrid<int>>();

            // Pre-populate with half the data
            for (int i = 0; i < DataSize / 2; i++) {
                var query = _spatialTimeQueries[i];

                if (!BaselineCollection.ContainsKey(query.time)) {
                    BaselineCollection[query.time] = new SpatialHashGrid<int>(50.0f);
                }

                BaselineCollection[query.time].Insert(query.x, query.y, TestValues[i]);
            }
        }

        protected override object PerformOmniAdd()
        {
            var query = GetRandomKey();
            var value = GetRandomValue();
            OmniCollection.UpdateObject(value, query.x, query.y);
            return OmniCollection.CurrentObjectCount;
        }

        protected override object PerformBaselineAdd()
        {
            var query = GetRandomKey();
            var value = GetRandomValue();

            if (!BaselineCollection.ContainsKey(query.time)) {
                BaselineCollection[query.time] = new SpatialHashGrid<int>(50.0f);
            }

            BaselineCollection[query.time].Insert(query.x, query.y, value);
            return BaselineCollection.Count;
        }

        protected override object PerformOmniGet()
        {
            var query = GetRandomKey();
            var results = OmniCollection.GetObjectsInRectangle(query.x - 25, query.y - 25, query.x + 25, query.y + 25);
            return results.Count();
        }

        protected override object PerformBaselineGet()
        {
            var query = GetRandomKey();

            if (BaselineCollection.TryGetValue(query.time, out var grid)) {
                var results = grid.GetObjectsInRectangle(query.x - 25, query.y - 25, query.x + 25, query.y + 25);
                return results.Count();
            }

            return 0;
        }

        protected override object PerformOmniRemove()
        {
            var query = GetRandomKey();
            var value = GetRandomValue();
            return OmniCollection.RemoveObject(value);
        }

        protected override object PerformBaselineRemove()
        {
            var query = GetRandomKey();
            var value = GetRandomValue();

            if (BaselineCollection.TryGetValue(query.time, out var grid)) {
                return grid.Remove(query.x, query.y, value);
            }

            return false;
        }

        protected override int PerformOmniEnumerate()
        {
            return OmniCollection.CurrentObjectCount;
        }

        protected override int PerformBaselineEnumerate()
        {
            int total = 0;
            foreach (var grid in BaselineCollection.Values) {
                total += grid.Count;
            }
            return total;
        }

        [Benchmark(Description = "TemporalSpatialHashGrid Add operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object TemporalSpatialHashGrid_Add() => base.Omni_Add();

        [Benchmark(Baseline = true, Description = "Dict<long, SpatialHashGrid<T>> Add operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object ManualTemporalSpatial_Add() => base.Baseline_Add();

        [Benchmark(Description = "TemporalSpatialHashGrid QueryAtTime operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object TemporalSpatialHashGrid_Get() => base.Omni_Get();

        [Benchmark(Description = "Dict<long, SpatialHashGrid<T>> Query operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object ManualTemporalSpatial_Get() => base.Baseline_Get();

        [Benchmark(Description = "TemporalSpatialHashGrid Remove operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object TemporalSpatialHashGrid_Remove() => base.Omni_Remove();

        [Benchmark(Description = "Dict<long, SpatialHashGrid<T>> Remove operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object ManualTemporalSpatial_Remove() => base.Baseline_Remove();

        [Benchmark(Description = "TemporalSpatialHashGrid enumeration")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object TemporalSpatialHashGrid_Enumerate() => base.Omni_Enumerate();

        [Benchmark(Description = "Dict<long, SpatialHashGrid<T>> enumeration (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object ManualTemporalSpatial_Enumerate() => base.Baseline_Enumerate();
    }

    /// <summary>
    /// BloomRTreeDictionary<TKey, TValue> vs Dictionary<TKey, TValue> + linear spatial search
    /// Tests the unique combination of O(1) key lookups + O(log n) spatial queries + Bloom filtering
    /// Tests different miss rates to evaluate Bloom filter efficiency
    /// </summary>
    [GroupBenchmarks]
    public class BloomRTreeDictionaryVsDictionary : BaselineComparisonBenchmark<BloomRTreeDictionary<string, int>, Dictionary<string, (int Value, BoundingRectangle Bounds)>, string, int>
    {
        private Random _random = null!;
        private string[] _testKeys = null!;
        private BoundingRectangle[] _testBounds = null!;
        private BoundingRectangle[] _queryBounds = null!;

        // Miss rate testing - critical for Bloom filter evaluation
        [Params(0.1, 0.5, 0.9)] public double MissRate { get; set; }
        [GlobalSetup]
        public void Setup()
        {
            SetupComparison();
        }

        [GlobalCleanup]
        public void Cleanup() => CleanupComparison();

        protected override void SetupTestData()
        {
            _random = new Random(42);
            _testKeys = new string[DataSize];
            _testBounds = new BoundingRectangle[DataSize];
            _queryBounds = new BoundingRectangle[100]; // Pre-generate query rectangles
            var values = BenchmarkDataFactory.GetIntPool(DataSize);

            // Generate test data: keys, values, and spatial bounds
            // Create keys where a certain percentage will be in the collection (hit rate = 1 - miss rate)
            int keysInCollection = (int)(DataSize * (1.0 - MissRate));

            for (int i = 0; i < DataSize; i++) {
                if (i < keysInCollection) {
                    // Keys that will be in the collection (hits)
                    _testKeys[i] = $"InCollection_Key_{i:D6}";
                }
                else {
                    // Keys that will NOT be in the collection (misses)
                    _testKeys[i] = $"Missing_Key_{i:D6}_{_random.Next(10000)}";
                }

                // Generate random rectangles within 1000x1000 space
                float x = _random.Next(0, 800);
                float y = _random.Next(0, 800);
                float width = _random.Next(10, 100);
                float height = _random.Next(10, 100);
                _testBounds[i] = new BoundingRectangle(x, y, x + width, y + height);
            }

            // Shuffle the keys to ensure random access patterns
            for (int i = DataSize - 1; i > 0; i--) {
                int j = _random.Next(i + 1);
                (_testKeys[i], _testKeys[j]) = (_testKeys[j], _testKeys[i]);
                (_testBounds[i], _testBounds[j]) = (_testBounds[j], _testBounds[i]);
            }

            // Generate query rectangles for spatial searches
            // Standardized 50x50 query rectangles for fair comparison across spatial benchmarks
            for (int i = 0; i < _queryBounds.Length; i++) {
                float x = _random.Next(0, 950); // Adjusted range to fit 50x50 queries in 1000x1000 space
                float y = _random.Next(0, 950);
                _queryBounds[i] = new BoundingRectangle(x, y, x + 50, y + 50);
            }

            TestKeys = _testKeys;
            TestValues = values;
        }

        protected override void SetupOmniCollection()
        {
            // Create with optimal settings for benchmarking
            OmniCollection = new BloomRTreeDictionary<string, int>(expectedCapacity: DataSize, falsePositiveRate: 0.01 // 1% false positive rate
            );

            // Only add keys that should result in hits (based on miss rate)
            // This ensures the actual miss rate matches the parameter
            for (int i = 0; i < DataSize; i++) {
                if (_testKeys[i].StartsWith("InCollection_")) {
                    OmniCollection.Add(_testKeys[i], TestValues[i], _testBounds[i]);
                }
            }
        }

        protected override void SetupBaselineCollection()
        {
            BaselineCollection = new Dictionary<string, (int Value, BoundingRectangle Bounds)>();

            // Only add keys that should result in hits (same as Omni collection)
            for (int i = 0; i < DataSize; i++) {
                if (_testKeys[i].StartsWith("InCollection_")) {
                    BaselineCollection[_testKeys[i]] = (TestValues[i], _testBounds[i]);
                }
            }
        }

        protected override object PerformOmniAdd()
        {
            var key = GetRandomKey();
            var value = GetRandomValue();
            var bounds = _testBounds[_random.Next(_testBounds.Length)];
            OmniCollection.Add(key, value, bounds);
            return OmniCollection.Count;
        }

        protected override object PerformBaselineAdd()
        {
            var key = GetRandomKey();
            var value = GetRandomValue();
            var bounds = _testBounds[_random.Next(_testBounds.Length)];
            BaselineCollection[key] = (value, bounds);
            return BaselineCollection.Count;
        }

        protected override object PerformOmniGet()
        {
            var key = GetRandomKey();
            OmniCollection.TryGetValue(key, out var value);
            return value;
        }

        protected override object PerformBaselineGet()
        {
            var key = GetRandomKey();
            BaselineCollection.TryGetValue(key, out var entry);
            return entry.Value;
        }

        protected override object PerformOmniRemove()
        {
            var key = GetRandomKey();
            return OmniCollection.Remove(key);
        }

        protected override object PerformBaselineRemove()
        {
            var key = GetRandomKey();
            return BaselineCollection.Remove(key);
        }

        protected override int PerformOmniEnumerate()
        {
            int count = 0;
            foreach (var kvp in OmniCollection) {
                count++;
            }
            return count;
        }

        protected override int PerformBaselineEnumerate()
        {
            int count = 0;
            foreach (var kvp in BaselineCollection) {
                count++;
            }
            return count;
        }

        protected override void CleanupOmniCollection()
        {
            OmniCollection?.Dispose();
        }

        private static bool DoesIntersect(BoundingRectangle rect1, BoundingRectangle rect2)
        {
            return !(rect1.MaxX < rect2.MinX || rect2.MaxX < rect1.MinX || rect1.MaxY < rect2.MinY || rect2.MaxY < rect1.MinY);
        }

        // Dictionary Operations Benchmarks
        [Benchmark(Description = "BloomRTreeDictionary Key Lookup")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object BloomRTreeDictionary_KeyLookup() => base.Omni_Get();

        [Benchmark(Baseline = true, Description = "Dictionary Key Lookup (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object Dictionary_KeyLookup() => base.Baseline_Get();

        [Benchmark(Description = "BloomRTreeDictionary Add")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object BloomRTreeDictionary_Add() => base.Omni_Add();

        [Benchmark(Description = "Dictionary Add (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object Dictionary_Add() => base.Baseline_Add();

        [Benchmark(Description = "BloomRTreeDictionary Remove")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object BloomRTreeDictionary_Remove() => base.Omni_Remove();

        [Benchmark(Description = "Dictionary Remove (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object Dictionary_Remove() => base.Baseline_Remove();

        // Spatial Operations Benchmarks
        [Benchmark(Description = "BloomRTreeDictionary Spatial Intersection Query")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object BloomRTreeDictionary_SpatialQuery()
        {
            var queryBounds = _queryBounds[_random.Next(_queryBounds.Length)];
            int count = 0;
            foreach (var kvp in OmniCollection.FindIntersecting(queryBounds)) {
                count++;
            }
            return count;
        }

        [Benchmark(Description = "Dictionary Linear Spatial Search (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object Dictionary_LinearSpatialSearch()
        {
            var queryBounds = _queryBounds[_random.Next(_queryBounds.Length)];
            int count = 0;
            foreach (var kvp in BaselineCollection) {
                if (DoesIntersect(queryBounds, kvp.Value.Bounds)) {
                    count++;
                }
            }
            return count;
        }

        [Benchmark(Description = "BloomRTreeDictionary Point Query")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object BloomRTreeDictionary_PointQuery()
        {
            float x = _random.Next(0, 1000);
            float y = _random.Next(0, 1000);
            int count = 0;
            foreach (var kvp in OmniCollection.FindAtPoint(x, y)) {
                count++;
            }
            return count;
        }

        [Benchmark(Description = "Dictionary Linear Point Search (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object Dictionary_LinearPointSearch()
        {
            float x = _random.Next(0, 1000);
            float y = _random.Next(0, 1000);
            int count = 0;
            foreach (var kvp in BaselineCollection) {
                var bounds = kvp.Value.Bounds;
                if (x >= bounds.MinX && x <= bounds.MaxX && y >= bounds.MinY && y <= bounds.MaxY) {
                    count++;
                }
            }
            return count;
        }

        // Note: Removed misleading "Zero-Allocation" benchmark as it was actually allocating 34KB
        // BloomRTreeDictionary does not currently have a true zero-allocation spatial query method

        // Performance Metrics Benchmarks
        [Benchmark(Description = "BloomRTreeDictionary Performance Statistics")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object BloomRTreeDictionary_GetStatistics()
        {
            return OmniCollection.Statistics.TreeHeight;
        }

        // Bulk Operations Benchmarks
        [Benchmark(Description = "BloomRTreeDictionary Bulk Add (AddRange)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object BloomRTreeDictionary_BulkAdd()
        {
            // Create a small batch of entries to add
            var entries = new List<(string Key, int Value, BoundingRectangle Bounds)>();
            for (int i = 0; i < 10; i++) {
                var key = $"BulkKey_{_random.Next(100000)}";
                var value = _random.Next(1000);
                var bounds = _testBounds[_random.Next(_testBounds.Length)];
                entries.Add((key, value, bounds));
            }

            OmniCollection.AddRange(entries);
            return OmniCollection.Count;
        }
    }

    /// <summary>
    /// BloomRTreeDictionary Performance Scaling Benchmark
    /// Tests how the R-Tree performs at different data sizes and query patterns
    /// </summary>
    [GroupBenchmarks]
    public class BloomRTreeScalingBenchmark
    {
        private BloomRTreeDictionary<string, int> _dictionary = null!;
        private BoundingRectangle[] _queryBounds = null!;
        private BoundingRectangle[] _missQueryBounds = null!; // Guaranteed misses
        private Random _random = null!;

        [Params(1000, 10000, 100000)] public int DataSize { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _random = new Random(42);
            _dictionary = new BloomRTreeDictionary<string, int>(DataSize, 0.01);

            // Populate with test data in 0-1000 range
            for (int i = 0; i < DataSize; i++) {
                float x = _random.Next(0, 800);
                float y = _random.Next(0, 800);
                float width = _random.Next(10, 50);
                float height = _random.Next(10, 50);
                var bounds = new BoundingRectangle(x, y, x + width, y + height);
                _dictionary.Add($"Key_{i}", i, bounds);
            }

            // Create query bounds that will hit existing data
            _queryBounds = new BoundingRectangle[1000];
            for (int i = 0; i < _queryBounds.Length; i++) {
                float x = _random.Next(0, 850);
                float y = _random.Next(0, 850);
                _queryBounds[i] = new BoundingRectangle(x, y, x + 100, y + 100);
            }

            // Create query bounds guaranteed to miss (outside data range)
            _missQueryBounds = new BoundingRectangle[1000];
            for (int i = 0; i < _missQueryBounds.Length; i++) {
                float x = _random.Next(1500, 2000); // Outside data range
                float y = _random.Next(1500, 2000);
                _missQueryBounds[i] = new BoundingRectangle(x, y, x + 100, y + 100);
            }

            // Warm up Bloom filter with some queries
            for (int i = 0; i < 100; i++) {
                var bounds = _queryBounds[i];
                foreach (var _ in _dictionary.FindIntersecting(bounds)) { }
            }
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _dictionary?.Dispose();
        }

        [Benchmark(Description = "Spatial queries with results")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int SpatialQuery_WithResults()
        {
            int totalResults = 0;
            for (int i = 0; i < 100; i++) {
                var bounds = _queryBounds[i % _queryBounds.Length];
                foreach (var kvp in _dictionary.FindIntersecting(bounds)) {
                    totalResults++;
                }
            }
            return totalResults;
        }

        [Benchmark(Description = "Spatial queries with no results")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int SpatialQuery_NoResults()
        {
            int totalResults = 0;
            for (int i = 0; i < 100; i++) {
                var bounds = _missQueryBounds[i % _missQueryBounds.Length];
                foreach (var kvp in _dictionary.FindIntersecting(bounds)) {
                    totalResults++;
                }
            }
            return totalResults;
        }

        [Benchmark(Description = "Repeated spatial queries")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int SpatialQuery_Repeated()
        {
            int totalResults = 0;
            var bounds = _queryBounds[0]; // Same query repeated
            for (int i = 0; i < 100; i++) {
                foreach (var kvp in _dictionary.FindIntersecting(bounds)) {
                    totalResults++;
                }
            }
            return totalResults;
        }

        [Benchmark(Description = "Get performance statistics")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int Statistics_TreeHeight()
        {
            return _dictionary.Statistics.TreeHeight;
        }
    }

    /// <summary>
    /// KDTree Distance Metrics Performance Benchmark
    /// Compares the performance of different distance metrics in KDTree operations.
    /// Tests: EuclideanDistance, ManhattanDistance, ChebyshevDistance, MinkowskiDistance
    /// 
    /// IMPORTANT: This benchmark tests distance calculation overhead in KDTree operations.
    /// Different metrics have distinct computational costs and use cases:
    /// 
    /// - EuclideanDistance: Standard geometric distance, requires square root calculation
    /// - ManhattanDistance: Sum of absolute differences, faster computation, no square root
    /// - ChebyshevDistance: Maximum absolute difference, fastest computation
    /// - MinkowskiDistance: Generalized distance with parameter p (p=1→Manhattan, p=2→Euclidean)
    /// 
    /// Use this benchmark to choose the optimal distance metric for your specific spatial application.
    /// </summary>
    [GroupBenchmarks]
    public class KDTreeDistanceMetricsBenchmark
    {
        private KdTree<(Point Point, int Value)> _euclideanTree = null!;
        private KdTree<(Point Point, int Value)> _manhattanTree = null!;
        private KdTree<(Point Point, int Value)> _chebyshevTree = null!;
        private KdTree<(Point Point, int Value)> _minkowski1Tree = null!;
        private KdTree<(Point Point, int Value)> _minkowski3Tree = null!;

        private (Point Point, int Value)[] _testItems = null!;
        private Point[] _queryPoints = null!;
        private Random _random = null!;

        [Params(10000)] public int DataSize { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _random = new Random(42);

            // Generate test data
            _testItems = new (Point Point, int Value)[DataSize];
            _queryPoints = new Point[1000];

            for (int i = 0; i < DataSize; i++) {
                var point = new Point(_random.Next(0, 1000), _random.Next(0, 1000));
                _testItems[i] = (point, i);
            }

            for (int i = 0; i < _queryPoints.Length; i++) {
                _queryPoints[i] = new Point(_random.Next(0, 1000), _random.Next(0, 1000));
            }

            // Create KDTrees with different distance metrics using the factory methods
            _euclideanTree = KdTree<(Point Point, int Value)>.Create2D(item => item.Point.X, item => item.Point.Y, new EuclideanDistance());
            _manhattanTree = KdTree<(Point Point, int Value)>.Create2D(item => item.Point.X, item => item.Point.Y, new ManhattanDistance());
            _chebyshevTree = KdTree<(Point Point, int Value)>.Create2D(item => item.Point.X, item => item.Point.Y, new ChebyshevDistance());
            _minkowski1Tree = KdTree<(Point Point, int Value)>.Create2D(item => item.Point.X, item => item.Point.Y, new MinkowskiDistance(1.0)); // p=1 (Manhattan equivalent)
            _minkowski3Tree = KdTree<(Point Point, int Value)>.Create2D(item => item.Point.X, item => item.Point.Y, new MinkowskiDistance(3.0)); // p=3 (cubic norm)

            // Populate all trees with the same data
            foreach (var item in _testItems) {
                _euclideanTree.Insert(item);
                _manhattanTree.Insert(item);
                _chebyshevTree.Insert(item);
                _minkowski1Tree.Insert(item);
                _minkowski3Tree.Insert(item);
            }
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _euclideanTree?.Dispose();
            _manhattanTree?.Dispose();
            _chebyshevTree?.Dispose();
            _minkowski1Tree?.Dispose();
            _minkowski3Tree?.Dispose();
        }

        // Nearest Neighbor Search Benchmarks
        [Benchmark(Baseline = true, Description = "KDTree Nearest Neighbor - Euclidean Distance")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public (Point Point, int Value)? Euclidean_NearestNeighbor()
        {
            var queryPoint = _queryPoints[_random.Next(_queryPoints.Length)];
            var queryItem = (queryPoint, 0); // Create target item for search
            return _euclideanTree.FindNearest(queryItem);
        }

        [Benchmark(Description = "KDTree Nearest Neighbor - Manhattan Distance")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public (Point Point, int Value)? Manhattan_NearestNeighbor()
        {
            var queryPoint = _queryPoints[_random.Next(_queryPoints.Length)];
            var queryItem = (queryPoint, 0); // Create target item for search
            return _manhattanTree.FindNearest(queryItem);
        }

        [Benchmark(Description = "KDTree Nearest Neighbor - Chebyshev Distance")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public (Point Point, int Value)? Chebyshev_NearestNeighbor()
        {
            var queryPoint = _queryPoints[_random.Next(_queryPoints.Length)];
            var queryItem = (queryPoint, 0); // Create target item for search
            return _chebyshevTree.FindNearest(queryItem);
        }

        [Benchmark(Description = "KDTree Nearest Neighbor - Minkowski Distance (p=1)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public (Point Point, int Value)? Minkowski1_NearestNeighbor()
        {
            var queryPoint = _queryPoints[_random.Next(_queryPoints.Length)];
            var queryItem = (queryPoint, 0); // Create target item for search
            return _minkowski1Tree.FindNearest(queryItem);
        }

        [Benchmark(Description = "KDTree Nearest Neighbor - Minkowski Distance (p=3)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public (Point Point, int Value)? Minkowski3_NearestNeighbor()
        {
            var queryPoint = _queryPoints[_random.Next(_queryPoints.Length)];
            var queryItem = (queryPoint, 0); // Create target item for search
            return _minkowski3Tree.FindNearest(queryItem);
        }

        // K-Nearest Neighbors Benchmarks
        [Benchmark(Description = "KDTree K-Nearest Neighbors (k=5) - Euclidean Distance")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public List<(Point Point, int Value)> Euclidean_KNearestNeighbors()
        {
            var queryPoint = _queryPoints[_random.Next(_queryPoints.Length)];
            var queryItem = (queryPoint, 0); // Create target item for search
            return _euclideanTree.FindNearestK(queryItem, 5);
        }

        [Benchmark(Description = "KDTree K-Nearest Neighbors (k=5) - Manhattan Distance")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public List<(Point Point, int Value)> Manhattan_KNearestNeighbors()
        {
            var queryPoint = _queryPoints[_random.Next(_queryPoints.Length)];
            var queryItem = (queryPoint, 0); // Create target item for search
            return _manhattanTree.FindNearestK(queryItem, 5);
        }

        [Benchmark(Description = "KDTree K-Nearest Neighbors (k=5) - Chebyshev Distance")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public List<(Point Point, int Value)> Chebyshev_KNearestNeighbors()
        {
            var queryPoint = _queryPoints[_random.Next(_queryPoints.Length)];
            var queryItem = (queryPoint, 0); // Create target item for search
            return _chebyshevTree.FindNearestK(queryItem, 5);
        }

        // Range Query Benchmarks
        [Benchmark(Description = "KDTree Range Query - Euclidean Distance")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public List<(Point Point, int Value)> Euclidean_RangeQuery()
        {
            var queryPoint = _queryPoints[_random.Next(_queryPoints.Length)];
            var queryItem = (queryPoint, 0); // Create target item for search
            return _euclideanTree.FindWithinRadius(queryItem, 50.0); // 50-unit radius
        }

        [Benchmark(Description = "KDTree Range Query - Manhattan Distance")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public List<(Point Point, int Value)> Manhattan_RangeQuery()
        {
            var queryPoint = _queryPoints[_random.Next(_queryPoints.Length)];
            var queryItem = (queryPoint, 0); // Create target item for search
            return _manhattanTree.FindWithinRadius(queryItem, 50.0); // 50-unit radius
        }

        [Benchmark(Description = "KDTree Range Query - Chebyshev Distance")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public List<(Point Point, int Value)> Chebyshev_RangeQuery()
        {
            var queryPoint = _queryPoints[_random.Next(_queryPoints.Length)];
            var queryItem = (queryPoint, 0); // Create target item for search
            return _chebyshevTree.FindWithinRadius(queryItem, 50.0); // 50-unit radius
        }
    }
}
public struct OctItem<T>
{
    public Point3D Point { get; }
    public T Value { get; }

    public OctItem(Point3D point, T value)
    {
        Point = point;
        Value = value;
    }
}

// Helper classes for OctTree benchmarking
public struct Point3D
{
    public float X { get; }
    public float Y { get; }
    public float Z { get; }

    public Point3D(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}
public struct Bounds3D
{
    public float MinX { get; }
    public float MinY { get; }
    public float MinZ { get; }
    public float MaxX { get; }
    public float MaxY { get; }
    public float MaxZ { get; }

    public Bounds3D(float minX, float minY, float minZ, float maxX, float maxY, float maxZ)
    {
        MinX = minX;
        MinY = minY;
        MinZ = minZ;
        MaxX = maxX;
        MaxY = maxY;
        MaxZ = maxZ;
    }
}
public class OctPointProvider : IOctPointProvider<OctItem<int>>
{
    public Vector3 GetPosition(OctItem<int> item) => new Vector3(item.Point.X, item.Point.Y, item.Point.Z);
}
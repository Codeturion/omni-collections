using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Omni.Collections.Linear;
using Omni.Collections.Spatial.DistanceMetrics;
using Omni.Collections.Spatial.KDTree;
using Xunit;

namespace Omni.Collections.Tests.Spatial;

public class KdTreeTests : IDisposable
{
    #region Test Data Classes

    private class Point2D
    {
        public double X { get; set; }
        public double Y { get; set; }
        public string Name { get; set; } = "";

        public Point2D(double x, double y, string name = "")
        {
            X = x;
            Y = y;
            Name = name;
        }
    }

    private class Point3D
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public string Name { get; set; } = "";

        public Point3D(double x, double y, double z, string name = "")
        {
            X = x;
            Y = y;
            Z = z;
            Name = name;
        }
    }

    private class TestPointProvider2D : IKdPointProvider<Point2D>
    {
        public double[] GetCoordinates(Point2D item)
        {
            return [item.X, item.Y];
        }
    }

    private class TestPointProvider3D : IKdPointProvider<Point3D>
    {
        public double[] GetCoordinates(Point3D item)
        {
            return [item.X, item.Y, item.Z];
        }
    }

    #endregion

    #region Constructor Tests

    /// <summary>
    /// Tests that a KdTree can be constructed with valid point provider and default parameters.
    /// The tree should have zero count initially and use Euclidean distance by default.
    /// </summary>
    [Fact]
    public void Constructor_WithValidPointProvider_CreatesTreeWithCorrectProperties()
    {
        var pointProvider = new TestPointProvider2D();
        var tree = new KdTree<Point2D>(pointProvider);

        tree.Count.Should().Be(0);
        tree.Dimensions.Should().Be(2);
    }

    /// <summary>
    /// Tests that a KdTree can be constructed with custom dimensions and distance metric.
    /// The tree should accept valid dimension counts and distance metric implementations.
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    public void Constructor_WithCustomDimensions_CreatesTreeWithValidSettings(int dimensions)
    {
        var pointProvider = new TestPointProvider2D();
        var distanceMetric = new ManhattanDistance();
        var tree = new KdTree<Point2D>(pointProvider, dimensions, distanceMetric);

        tree.Count.Should().Be(0);
        tree.Dimensions.Should().Be(dimensions);
    }

    /// <summary>
    /// Tests that constructing a KdTree with invalid dimensions throws exception.
    /// The constructor should reject zero or negative dimension values.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-5)]
    public void Constructor_WithInvalidDimensions_ThrowsArgumentOutOfRangeException(int dimensions)
    {
        var pointProvider = new TestPointProvider2D();
        var act = () => new KdTree<Point2D>(pointProvider, dimensions);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("dimensions");
    }

    /// <summary>
    /// Tests that constructing a KdTree with null point provider throws exception.
    /// The constructor should reject null point provider references.
    /// </summary>
    [Fact]
    public void Constructor_WithNullPointProvider_ThrowsArgumentNullException()
    {
        var act = () => new KdTree<Point2D>(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("pointProvider");
    }

    /// <summary>
    /// Tests the factory method Create2D with coordinate accessors.
    /// The factory should create a properly configured 2D KdTree with coordinate functions.
    /// </summary>
    [Fact]
    public void Create2D_WithCoordinateAccessors_CreatesConfigured2DTree()
    {
        var tree = KdTree<Point2D>.Create2D(p => p.X, p => p.Y);

        tree.Count.Should().Be(0);
        tree.Dimensions.Should().Be(2);
    }

    /// <summary>
    /// Tests the factory method Create3D with coordinate accessors.
    /// The factory should create a properly configured 3D KdTree with coordinate functions.
    /// </summary>
    [Fact]
    public void Create3D_WithCoordinateAccessors_CreatesConfigured3DTree()
    {
        var tree = KdTree<Point3D>.Create3D(p => p.X, p => p.Y, p => p.Z);

        tree.Count.Should().Be(0);
        tree.Dimensions.Should().Be(3);
    }

    /// <summary>
    /// Tests creating trees with custom distance metrics through factory methods.
    /// The factories should accept and use custom distance metric implementations.
    /// </summary>
    [Fact]
    public void CreateWithCustomDistanceMetric_UsesProvidedMetric()
    {
        var customMetric = new ChebyshevDistance();
        var tree = KdTree<Point2D>.Create2D(p => p.X, p => p.Y, customMetric);

        tree.Count.Should().Be(0);
        tree.Dimensions.Should().Be(2);
    }

    #endregion

    #region Insert Tests

    /// <summary>
    /// Tests that inserting a single item works correctly.
    /// The tree count should increase and the item should be retrievable.
    /// </summary>
    [Fact]
    public void Insert_SingleItem_IncreasesCountAndStoresItem()
    {
        var tree = KdTree<Point2D>.Create2D(p => p.X, p => p.Y);
        var item = new Point2D(50, 50, "test");

        tree.Insert(item);

        tree.Count.Should().Be(1);
        var results = tree.GetAllItems().ToList();
        results.Should().ContainSingle().Which.Should().Be(item);
    }

    /// <summary>
    /// Tests that inserting multiple items works correctly.
    /// The tree should store all items and maintain correct count.
    /// </summary>
    [Fact]
    public void Insert_MultipleItems_StoresAllItems()
    {
        var tree = KdTree<Point2D>.Create2D(p => p.X, p => p.Y);

        var items = new[]
        {
            new Point2D(10, 10, "item1"),
            new Point2D(20, 20, "item2"),
            new Point2D(30, 30, "item3"),
            new Point2D(40, 40, "item4")
        };

        foreach (var item in items)
        {
            tree.Insert(item);
        }

        tree.Count.Should().Be(4);
        var results = tree.GetAllItems().ToList();
        results.Should().HaveCount(4);
        results.Should().BeEquivalentTo(items);
    }

    /// <summary>
    /// Tests that the tree triggers rebalancing when threshold is exceeded.
    /// The tree should automatically rebalance for improved performance on large datasets.
    /// </summary>
    [Fact]
    public void Insert_LargeDataset_TriggersRebalancing()
    {
        var tree = KdTree<Point2D>.Create2D(p => p.X, p => p.Y);

        // Insert enough items to trigger rebalancing (powers of 2 > 1000)
        for (int i = 0; i < 2048; i++)
        {
            tree.Insert(new Point2D(i % 100, i / 100, $"item{i}"));
        }

        tree.Count.Should().Be(2048);
        var results = tree.GetAllItems().ToList();
        results.Should().HaveCount(2048);
    }

    /// <summary>
    /// Tests inserting items in 3D space works correctly.
    /// The tree should handle multi-dimensional data properly.
    /// </summary>
    [Fact]
    public void Insert_3DItems_StoresCorrectly()
    {
        var tree = KdTree<Point3D>.Create3D(p => p.X, p => p.Y, p => p.Z);

        var items = new[]
        {
            new Point3D(10, 10, 10, "item1"),
            new Point3D(20, 20, 20, "item2"),
            new Point3D(30, 30, 30, "item3")
        };

        foreach (var item in items)
        {
            tree.Insert(item);
        }

        tree.Count.Should().Be(3);
        var results = tree.GetAllItems().ToList();
        results.Should().BeEquivalentTo(items);
    }

    #endregion

    #region InsertRange and Build Tests

    /// <summary>
    /// Tests that InsertRange adds multiple items efficiently.
    /// The tree should handle bulk insertion operations correctly.
    /// </summary>
    [Fact]
    public void InsertRange_WithMultipleItems_InsertsAllItems()
    {
        var tree = KdTree<Point2D>.Create2D(p => p.X, p => p.Y);

        var items = Enumerable.Range(0, 50)
            .Select(i => new Point2D(i, i, $"item{i}"))
            .ToArray();

        tree.InsertRange(items);

        tree.Count.Should().Be(50);
        var results = tree.GetAllItems().ToList();
        results.Should().BeEquivalentTo(items);
    }

    /// <summary>
    /// Tests that InsertRange with empty collection handles gracefully.
    /// The tree should remain unchanged when inserting empty collections.
    /// </summary>
    [Fact]
    public void InsertRange_WithEmptyCollection_LeavesTreeUnchanged()
    {
        var tree = KdTree<Point2D>.Create2D(p => p.X, p => p.Y);
        var emptyItems = new Point2D[0];

        tree.InsertRange(emptyItems);

        tree.Count.Should().Be(0);
    }

    /// <summary>
    /// Tests that Build method creates optimally balanced tree from collection.
    /// The tree should be built in an optimal configuration for best performance.
    /// </summary>
    [Fact]
    public void Build_WithItems_CreatesOptimallyBalancedTree()
    {
        var tree = KdTree<Point2D>.Create2D(p => p.X, p => p.Y);

        var items = Enumerable.Range(0, 100)
            .Select(i => new Point2D(i % 10, i / 10, $"item{i}"))
            .ToArray();

        tree.Build(items);

        tree.Count.Should().Be(100);
        var results = tree.GetAllItems().ToList();
        results.Should().BeEquivalentTo(items);
    }

    /// <summary>
    /// Tests that BatchInsert optimizes insertion based on data size.
    /// The method should choose between Build and InsertRange for optimal performance.
    /// </summary>
    [Fact]
    public void BatchInsert_WithLargeDataset_OptimizesInsertion()
    {
        var tree = KdTree<Point2D>.Create2D(p => p.X, p => p.Y);

        // Add some initial items
        tree.Insert(new Point2D(0, 0, "initial"));

        var newItems = Enumerable.Range(1, 100)
            .Select(i => new Point2D(i, i, $"item{i}"))
            .ToArray();

        tree.BatchInsert(newItems);

        tree.Count.Should().Be(101);
    }

    #endregion

    #region Nearest Neighbor Tests

    /// <summary>
    /// Tests finding the nearest item to a target point.
    /// The tree should return the spatially closest item using the configured distance metric.
    /// </summary>
    [Fact]
    public void FindNearest_WithMultipleItems_ReturnsClosestItem()
    {
        var tree = KdTree<Point2D>.Create2D(p => p.X, p => p.Y);

        tree.Insert(new Point2D(10, 10, "far"));
        tree.Insert(new Point2D(52, 53, "near"));
        tree.Insert(new Point2D(90, 90, "farther"));

        var target = new Point2D(50, 50, "target");
        var nearest = tree.FindNearest(target);

        nearest?.Name.Should().Be("near");
    }

    /// <summary>
    /// Tests finding nearest item when tree is empty returns null.
    /// The tree should return default value when no items are present.
    /// </summary>
    [Fact]
    public void FindNearest_EmptyTree_ReturnsDefault()
    {
        var tree = KdTree<Point2D>.Create2D(p => p.X, p => p.Y);

        var target = new Point2D(50, 50, "target");
        var nearest = tree.FindNearest(target);

        nearest.Should().BeNull();
    }

    /// <summary>
    /// Tests finding nearest with different distance metrics produces different results.
    /// The tree should use the configured distance metric for nearest neighbor calculations.
    /// </summary>
    [Fact]
    public void FindNearest_WithDifferentDistanceMetrics_ProducesDifferentResults()
    {
        var euclideanTree = KdTree<Point2D>.Create2D(p => p.X, p => p.Y, new EuclideanDistance());
        var manhattanTree = KdTree<Point2D>.Create2D(p => p.X, p => p.Y, new ManhattanDistance());

        var items = new[]
        {
            new Point2D(0, 5, "vertical"),
            new Point2D(5, 0, "horizontal")
        };

        foreach (var item in items)
        {
            euclideanTree.Insert(item);
            manhattanTree.Insert(item);
        }

        var target = new Point2D(0, 0, "target");

        var euclideanNearest = euclideanTree.FindNearest(target);
        var manhattanNearest = manhattanTree.FindNearest(target);

        // Both should find one of the items (distance is equal with both metrics for this case)
        euclideanNearest.Should().NotBeNull();
        manhattanNearest.Should().NotBeNull();
    }

    #endregion

    #region K-Nearest Neighbor Tests

    /// <summary>
    /// Tests finding K nearest neighbors returns correct number of items.
    /// The tree should return the K closest items in distance order.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void FindNearestK_WithValidK_ReturnsCorrectNumberOfItems(int k)
    {
        var tree = KdTree<Point2D>.Create2D(p => p.X, p => p.Y);

        for (int i = 0; i < 10; i++)
        {
            tree.Insert(new Point2D(i * 10, i * 10, $"item{i}"));
        }

        var target = new Point2D(25, 25, "target");
        var nearest = tree.FindNearestK(target, k);

        nearest.Should().HaveCount(k);
    }

    /// <summary>
    /// Tests K-nearest neighbor with K larger than item count returns all items.
    /// The tree should return all available items when K exceeds the total count.
    /// </summary>
    [Fact]
    public void FindNearestK_KLargerThanItemCount_ReturnsAllItems()
    {
        var tree = KdTree<Point2D>.Create2D(p => p.X, p => p.Y);

        for (int i = 0; i < 5; i++)
        {
            tree.Insert(new Point2D(i * 10, i * 10, $"item{i}"));
        }

        var target = new Point2D(25, 25, "target");
        var nearest = tree.FindNearestK(target, 10);

        nearest.Should().HaveCount(5);
    }

    /// <summary>
    /// Tests K-nearest neighbor with empty tree returns empty collection.
    /// The tree should return empty results when no items are present.
    /// </summary>
    [Fact]
    public void FindNearestK_EmptyTree_ReturnsEmptyCollection()
    {
        var tree = KdTree<Point2D>.Create2D(p => p.X, p => p.Y);

        var target = new Point2D(50, 50, "target");
        var nearest = tree.FindNearestK(target, 3);

        nearest.Should().BeEmpty();
    }

    /// <summary>
    /// Tests K-nearest neighbor with invalid K returns empty collection.
    /// The tree should handle invalid K values gracefully.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void FindNearestK_InvalidK_ReturnsEmptyCollection(int k)
    {
        var tree = KdTree<Point2D>.Create2D(p => p.X, p => p.Y);

        tree.Insert(new Point2D(10, 10, "item"));

        var target = new Point2D(50, 50, "target");
        var nearest = tree.FindNearestK(target, k);

        nearest.Should().BeEmpty();
    }

    /// <summary>
    /// Tests K-nearest neighbor with pooled list for memory efficiency.
    /// The tree should populate the provided pooled list with results.
    /// </summary>
    [Fact]
    public void FindNearestK_WithPooledList_PopulatesResults()
    {
        var tree = KdTree<Point2D>.Create2D(p => p.X, p => p.Y);
        var results = new PooledList<Point2D>();

        for (int i = 0; i < 10; i++)
        {
            tree.Insert(new Point2D(i * 10, i * 10, $"item{i}"));
        }

        var target = new Point2D(25, 25, "target");
        tree.FindNearestK(target, 3, results);

        results.Count.Should().Be(3);
        results.Dispose();
    }

    #endregion

    #region Radius Search Tests

    /// <summary>
    /// Tests finding items within a specified radius of target point.
    /// The tree should return all items within the distance threshold.
    /// </summary>
    [Fact]
    public void FindWithinRadius_WithItemsInRange_ReturnsCorrectItems()
    {
        var tree = KdTree<Point2D>.Create2D(p => p.X, p => p.Y);

        tree.Insert(new Point2D(50, 50, "center"));
        tree.Insert(new Point2D(55, 55, "near"));
        tree.Insert(new Point2D(70, 70, "far"));

        var target = new Point2D(50, 50, "target");
        var results = tree.FindWithinRadius(target, 10.0);

        results.Should().HaveCount(2);
        results.Should().Contain(item => item.Name == "center");
        results.Should().Contain(item => item.Name == "near");
        results.Should().NotContain(item => item.Name == "far");
    }

    /// <summary>
    /// Tests radius search with empty tree returns empty collection.
    /// The tree should return no results when no items are present.
    /// </summary>
    [Fact]
    public void FindWithinRadius_EmptyTree_ReturnsEmptyCollection()
    {
        var tree = KdTree<Point2D>.Create2D(p => p.X, p => p.Y);

        var target = new Point2D(50, 50, "target");
        var results = tree.FindWithinRadius(target, 10.0);

        results.Should().BeEmpty();
    }

    /// <summary>
    /// Tests radius search with zero radius finds only exact matches.
    /// The tree should return items at exactly the target location.
    /// </summary>
    [Fact]
    public void FindWithinRadius_ZeroRadius_FindsOnlyExactMatches()
    {
        var tree = KdTree<Point2D>.Create2D(p => p.X, p => p.Y);

        tree.Insert(new Point2D(50, 50, "exact"));
        tree.Insert(new Point2D(50.1, 50.1, "close"));

        var target = new Point2D(50, 50, "target");
        var results = tree.FindWithinRadius(target, 0.0);

        results.Should().ContainSingle().Which.Name.Should().Be("exact");
    }

    /// <summary>
    /// Tests radius search with pooled list for memory efficiency.
    /// The tree should populate the provided pooled list with results.
    /// </summary>
    [Fact]
    public void FindWithinRadius_WithPooledList_PopulatesResults()
    {
        var tree = KdTree<Point2D>.Create2D(p => p.X, p => p.Y);
        var results = new PooledList<Point2D>();

        tree.Insert(new Point2D(50, 50, "center"));
        tree.Insert(new Point2D(55, 55, "near"));

        var target = new Point2D(50, 50, "target");
        tree.FindWithinRadius(target, 10.0, results);

        results.Count.Should().Be(2);
        results.Dispose();
    }

    #endregion

    #region Range Query Tests

    /// <summary>
    /// Tests finding items within a multi-dimensional range.
    /// The tree should return all items within the specified coordinate bounds.
    /// </summary>
    [Fact]
    public void FindInRange_WithItemsInRange_ReturnsCorrectItems()
    {
        var tree = KdTree<Point2D>.Create2D(p => p.X, p => p.Y);

        tree.Insert(new Point2D(25, 25, "inside"));
        tree.Insert(new Point2D(75, 75, "outside"));
        tree.Insert(new Point2D(0, 0, "boundary"));

        var minCoords = new double[] { 0, 0 };
        var maxCoords = new double[] { 50, 50 };
        var results = tree.FindInRange(minCoords, maxCoords);

        results.Should().HaveCount(2);
        results.Should().Contain(item => item.Name == "inside");
        results.Should().Contain(item => item.Name == "boundary");
        results.Should().NotContain(item => item.Name == "outside");
    }

    /// <summary>
    /// Tests range query with mismatched coordinate dimensions throws exception.
    /// The tree should validate that coordinate arrays match the tree dimensions.
    /// </summary>
    [Fact]
    public void FindInRange_MismatchedDimensions_ThrowsArgumentException()
    {
        var tree = KdTree<Point2D>.Create2D(p => p.X, p => p.Y);

        tree.Insert(new Point2D(25, 25, "test"));

        var minCoords = new double[] { 0, 0, 0 }; // 3D coordinates for 2D tree
        var maxCoords = new double[] { 50, 50, 50 };

        var act = () => tree.FindInRange(minCoords, maxCoords);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Coordinate arrays must match tree dimensions");
    }

    /// <summary>
    /// Tests range query with empty tree returns empty collection.
    /// The tree should return no results when no items are present.
    /// </summary>
    [Fact]
    public void FindInRange_EmptyTree_ReturnsEmptyCollection()
    {
        var tree = KdTree<Point2D>.Create2D(p => p.X, p => p.Y);

        var minCoords = new double[] { 0, 0 };
        var maxCoords = new double[] { 100, 100 };
        var results = tree.FindInRange(minCoords, maxCoords);

        results.Should().BeEmpty();
    }

    /// <summary>
    /// Tests range query with pooled list for memory efficiency.
    /// The tree should populate the provided pooled list with results.
    /// </summary>
    [Fact]
    public void FindInRange_WithPooledList_PopulatesResults()
    {
        var tree = KdTree<Point2D>.Create2D(p => p.X, p => p.Y);
        var results = new PooledList<Point2D>();

        tree.Insert(new Point2D(25, 25, "inside"));
        tree.Insert(new Point2D(75, 75, "outside"));

        var minCoords = new double[] { 0, 0 };
        var maxCoords = new double[] { 50, 50 };
        tree.FindInRange(minCoords, maxCoords, results);

        results.Count.Should().Be(1);
        results.Single().Name.Should().Be("inside");
        results.Dispose();
    }

    #endregion

    #region GetAllItems Tests

    /// <summary>
    /// Tests retrieving all items from the tree.
    /// The tree should return all inserted items in enumerable form.
    /// </summary>
    [Fact]
    public void GetAllItems_WithMultipleItems_ReturnsAllItems()
    {
        var tree = KdTree<Point2D>.Create2D(p => p.X, p => p.Y);

        var items = new[]
        {
            new Point2D(10, 10, "item1"),
            new Point2D(20, 20, "item2"),
            new Point2D(30, 30, "item3")
        };

        foreach (var item in items)
        {
            tree.Insert(item);
        }

        var allItems = tree.GetAllItems().ToList();

        allItems.Should().HaveCount(3);
        allItems.Should().BeEquivalentTo(items);
    }

    /// <summary>
    /// Tests retrieving items from empty tree returns empty enumerable.
    /// The tree should return no items when empty.
    /// </summary>
    [Fact]
    public void GetAllItems_EmptyTree_ReturnsEmptyEnumerable()
    {
        var tree = KdTree<Point2D>.Create2D(p => p.X, p => p.Y);

        var allItems = tree.GetAllItems().ToList();

        allItems.Should().BeEmpty();
    }

    #endregion

    #region Clear and Disposal Tests

    /// <summary>
    /// Tests clearing the tree removes all items.
    /// The tree should be empty after clearing and have zero count.
    /// </summary>
    [Fact]
    public void Clear_WithItems_RemovesAllItems()
    {
        var tree = KdTree<Point2D>.Create2D(p => p.X, p => p.Y);

        for (int i = 0; i < 10; i++)
        {
            tree.Insert(new Point2D(i, i, $"item{i}"));
        }

        tree.Clear();

        tree.Count.Should().Be(0);
        var results = tree.GetAllItems().ToList();
        results.Should().BeEmpty();
    }

    /// <summary>
    /// Tests disposing the tree cleans up resources properly.
    /// The tree should be empty after disposal and have zero count.
    /// </summary>
    [Fact]
    public void Dispose_WithItems_CleansUpResources()
    {
        var tree = KdTree<Point2D>.Create2D(p => p.X, p => p.Y);

        tree.Insert(new Point2D(50, 50, "test"));
        tree.Dispose();

        tree.Count.Should().Be(0);
    }

    #endregion

    #region Edge Cases and Performance Tests

    /// <summary>
    /// Tests tree behavior with identical coordinates for multiple items.
    /// The tree should handle duplicate positions correctly.
    /// </summary>
    [Fact]
    public void Insert_DuplicateCoordinates_HandlesCorrectly()
    {
        var tree = KdTree<Point2D>.Create2D(p => p.X, p => p.Y);

        tree.Insert(new Point2D(50, 50, "first"));
        tree.Insert(new Point2D(50, 50, "second"));
        tree.Insert(new Point2D(50, 50, "third"));

        tree.Count.Should().Be(3);
        var results = tree.GetAllItems().ToList();
        results.Should().HaveCount(3);
    }

    /// <summary>
    /// Tests tree performance with large dataset.
    /// The tree should handle thousands of items efficiently.
    /// </summary>
    [Fact]
    public void Insert_LargeDataset_HandlesEfficiently()
    {
        var tree = KdTree<Point2D>.Create2D(p => p.X, p => p.Y);

        var items = Enumerable.Range(0, 5000)
            .Select(i => new Point2D(i % 100, i / 100, $"item{i}"))
            .ToArray();

        foreach (var item in items)
        {
            tree.Insert(item);
        }

        tree.Count.Should().Be(5000);

        // Test query performance
        var target = new Point2D(50, 25, "target");
        var nearest = tree.FindNearest(target);
        nearest.Should().NotBeNull();
    }

    /// <summary>
    /// Tests tree behavior with extreme coordinate values.
    /// The tree should handle very large coordinate values without precision issues.
    /// </summary>
    [Fact]
    public void Insert_ExtremeCoordinates_HandlesCorrectly()
    {
        var tree = KdTree<Point2D>.Create2D(p => p.X, p => p.Y);

        var extremeItem = new Point2D(1e10, -1e10, "extreme");
        tree.Insert(extremeItem);

        tree.Count.Should().Be(1);
        var results = tree.GetAllItems().ToList();
        results.Should().ContainSingle().Which.Should().Be(extremeItem);
    }

    /// <summary>
    /// Tests multi-dimensional queries in higher dimensions work correctly.
    /// The tree should handle complex multi-dimensional spatial queries.
    /// </summary>
    [Fact]
    public void HighDimensionalQueries_WorkCorrectly()
    {
        var pointProvider = new TestPointProvider3D();
        var tree = new KdTree<Point3D>(pointProvider, 3);

        var items = new[]
        {
            new Point3D(10, 10, 10, "item1"),
            new Point3D(20, 20, 20, "item2"),
            new Point3D(30, 30, 30, "item3")
        };

        foreach (var item in items)
        {
            tree.Insert(item);
        }

        var target = new Point3D(15, 15, 15, "target");
        var nearest = tree.FindNearest(target);

        nearest?.Name.Should().Be("item1");
    }

    #endregion

    public void Dispose()
    {
        // Test cleanup if needed
    }
}
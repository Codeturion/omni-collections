using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Omni.Collections.Spatial;
using Xunit;

namespace Omni.Collections.Tests.Spatial;

public class QuadTreeTests
{
    #region Constructor Tests

    /// <summary>
    /// Tests that a QuadTree can be constructed with valid bounds and default parameters.
    /// The tree should have the specified bounds and zero count initially.
    /// </summary>
    [Fact]
    public void Constructor_WithValidBounds_CreatesTreeWithCorrectProperties()
    {
        var bounds = new Rectangle(0, 0, 100, 100);
        var tree = new QuadTree<int>(bounds);

        tree.Bounds.Should().Be(bounds);
        tree.Count.Should().Be(0);
    }

    /// <summary>
    /// Tests that a QuadTree can be constructed with custom parameters.
    /// The tree should accept valid maxDepth, maxItemsPerNode, and spatialThreshold values.
    /// </summary>
    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(5, 8, 1000)]
    [InlineData(12, 32, 10000)]
    public void Constructor_WithCustomParameters_CreatesTreeWithValidSettings(int maxDepth, int maxItemsPerNode, int spatialThreshold)
    {
        var bounds = new Rectangle(0, 0, 100, 100);
        var tree = new QuadTree<string>(bounds, maxDepth, maxItemsPerNode, spatialThreshold);

        tree.Bounds.Should().Be(bounds);
        tree.Count.Should().Be(0);
    }

    /// <summary>
    /// Tests that constructing a QuadTree with invalid maxDepth throws exception.
    /// The constructor should reject zero or negative maxDepth values.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-5)]
    public void Constructor_WithInvalidMaxDepth_ThrowsArgumentOutOfRangeException(int maxDepth)
    {
        var bounds = new Rectangle(0, 0, 100, 100);
        var act = () => new QuadTree<int>(bounds, maxDepth);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("maxDepth");
    }

    /// <summary>
    /// Tests that constructing a QuadTree with invalid maxItemsPerNode throws exception.
    /// The constructor should reject zero or negative maxItemsPerNode values.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Constructor_WithInvalidMaxItemsPerNode_ThrowsArgumentOutOfRangeException(int maxItemsPerNode)
    {
        var bounds = new Rectangle(0, 0, 100, 100);
        var act = () => new QuadTree<int>(bounds, 8, maxItemsPerNode);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("maxItemsPerNode");
    }

    /// <summary>
    /// Tests that constructing a QuadTree with zero spatialThreshold forces spatial mode.
    /// The tree should start in spatial mode when spatialThreshold is zero or negative.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithZeroSpatialThreshold_ForcesSpatialMode(int spatialThreshold)
    {
        var bounds = new Rectangle(0, 0, 100, 100);
        var tree = new QuadTree<int>(bounds, spatialThreshold: spatialThreshold);

        // Insert one item and verify it works (indicating spatial mode)
        tree.Insert(new Point(50, 50), 42);
        tree.Count.Should().Be(1);
    }

    #endregion

    #region Insert Tests

    /// <summary>
    /// Tests that inserting a single item within bounds works correctly.
    /// The tree count should increase and the item should be queryable.
    /// </summary>
    [Fact]
    public void Insert_SingleItemWithinBounds_IncreasesCountAndStoresItem()
    {
        var bounds = new Rectangle(0, 0, 100, 100);
        var tree = new QuadTree<string>(bounds);
        var point = new Point(50, 50);
        var item = "test item";

        tree.Insert(point, item);

        tree.Count.Should().Be(1);
        var results = tree.Query(new Rectangle(0, 0, 100, 100));
        results.Should().ContainSingle().Which.Should().Be(item);
    }

    /// <summary>
    /// Tests that inserting multiple items within bounds works correctly.
    /// The tree should store all items and maintain correct count.
    /// </summary>
    [Fact]
    public void Insert_MultipleItemsWithinBounds_StoresAllItems()
    {
        var bounds = new Rectangle(0, 0, 100, 100);
        var tree = new QuadTree<int>(bounds);

        for (int i = 0; i < 10; i++)
        {
            tree.Insert(new Point(i * 10, i * 10), i);
        }

        tree.Count.Should().Be(10);
        var results = tree.Query(bounds);
        results.Should().HaveCount(10);
        results.Should().BeEquivalentTo(Enumerable.Range(0, 10));
    }

    /// <summary>
    /// Tests that inserting a point outside bounds throws exception.
    /// The tree should reject points that fall outside the specified boundaries.
    /// </summary>
    [Theory]
    [InlineData(-1, 50)]
    [InlineData(50, -1)]
    [InlineData(101, 50)]
    [InlineData(50, 101)]
    public void Insert_PointOutsideBounds_ThrowsArgumentException(double x, double y)
    {
        var bounds = new Rectangle(0, 0, 100, 100);
        var tree = new QuadTree<int>(bounds);
        var point = new Point(x, y);

        var act = () => tree.Insert(point, 42);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Point outside tree bounds");
    }

    /// <summary>
    /// Tests that inserting duplicate points with different items works correctly.
    /// The tree should allow multiple items at the same spatial location.
    /// </summary>
    [Fact]
    public void Insert_DuplicatePointsWithDifferentItems_StoresBothItems()
    {
        var bounds = new Rectangle(0, 0, 100, 100);
        var tree = new QuadTree<string>(bounds);
        var point = new Point(50, 50);

        tree.Insert(point, "first");
        tree.Insert(point, "second");

        tree.Count.Should().Be(2);
        var results = tree.Query(new Rectangle(49, 49, 2, 2));
        results.Should().HaveCount(2);
        results.Should().Contain("first");
        results.Should().Contain("second");
    }

    /// <summary>
    /// Tests mode transition from linear to spatial when threshold is exceeded.
    /// The tree should automatically convert to spatial mode for better performance.
    /// </summary>
    [Fact]
    public void Insert_ExceedsSpatialThreshold_ConvertsToSpatialMode()
    {
        var bounds = new Rectangle(0, 0, 1000, 1000);
        var tree = new QuadTree<int>(bounds, spatialThreshold: 10);

        // Insert items to exceed threshold
        for (int i = 0; i < 15; i++)
        {
            tree.Insert(new Point(i * 50, i * 50), i);
        }

        tree.Count.Should().Be(15);
        var results = tree.Query(bounds);
        results.Should().HaveCount(15);
        results.Should().BeEquivalentTo(Enumerable.Range(0, 15));
    }

    /// <summary>
    /// Tests inserting items at boundary coordinates.
    /// The tree should handle edge cases correctly including boundary points.
    /// </summary>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(99.9, 99.9)]
    [InlineData(50, 0)]
    [InlineData(0, 50)]
    public void Insert_BoundaryCoordinates_HandlesCorrectly(double x, double y)
    {
        var bounds = new Rectangle(0, 0, 100, 100);
        var tree = new QuadTree<int>(bounds);
        var point = new Point(x, y);

        tree.Insert(point, 42);

        tree.Count.Should().Be(1);
        var results = tree.Query(bounds);
        results.Should().ContainSingle().Which.Should().Be(42);
    }

    #endregion

    #region Remove Tests

    /// <summary>
    /// Tests that removing an existing item works correctly.
    /// The tree count should decrease and the item should no longer be queryable.
    /// </summary>
    [Fact]
    public void Remove_ExistingItem_DecreasesCountAndRemovesItem()
    {
        var bounds = new Rectangle(0, 0, 100, 100);
        var tree = new QuadTree<string>(bounds);
        var point = new Point(50, 50);
        var item = "test item";

        tree.Insert(point, item);
        var removed = tree.Remove(point, item);

        removed.Should().BeTrue();
        tree.Count.Should().Be(0);
        var results = tree.Query(bounds);
        results.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that removing a non-existing item returns false.
    /// The tree should remain unchanged when attempting to remove items that don't exist.
    /// </summary>
    [Fact]
    public void Remove_NonExistingItem_ReturnsFalseAndLeavesTreeUnchanged()
    {
        var bounds = new Rectangle(0, 0, 100, 100);
        var tree = new QuadTree<string>(bounds);
        var point = new Point(50, 50);

        tree.Insert(point, "existing");
        var removed = tree.Remove(point, "non-existing");

        removed.Should().BeFalse();
        tree.Count.Should().Be(1);
        var results = tree.Query(bounds);
        results.Should().ContainSingle().Which.Should().Be("existing");
    }

    /// <summary>
    /// Tests that removing one of multiple items at same point works correctly.
    /// The tree should remove only the specified item while preserving others.
    /// </summary>
    [Fact]
    public void Remove_OneOfMultipleAtSamePoint_RemovesOnlySpecifiedItem()
    {
        var bounds = new Rectangle(0, 0, 100, 100);
        var tree = new QuadTree<string>(bounds);
        var point = new Point(50, 50);

        tree.Insert(point, "first");
        tree.Insert(point, "second");
        tree.Insert(point, "third");

        var removed = tree.Remove(point, "second");

        removed.Should().BeTrue();
        tree.Count.Should().Be(2);
        var results = tree.Query(new Rectangle(49, 49, 2, 2));
        results.Should().HaveCount(2);
        results.Should().Contain("first");
        results.Should().Contain("third");
        results.Should().NotContain("second");
    }

    /// <summary>
    /// Tests removing items in both linear and spatial modes.
    /// The tree should handle removal correctly regardless of internal mode.
    /// </summary>
    [Fact]
    public void Remove_ItemsInBothModes_WorksCorrectly()
    {
        var bounds = new Rectangle(0, 0, 1000, 1000);
        var tree = new QuadTree<int>(bounds, spatialThreshold: 5);

        // Insert items in linear mode
        tree.Insert(new Point(10, 10), 1);
        tree.Insert(new Point(20, 20), 2);

        // Remove in linear mode
        var removed1 = tree.Remove(new Point(10, 10), 1);
        removed1.Should().BeTrue();
        tree.Count.Should().Be(1);

        // Add more items to trigger spatial mode
        for (int i = 3; i <= 10; i++)
        {
            tree.Insert(new Point(i * 10, i * 10), i);
        }

        // Remove in spatial mode
        var removed2 = tree.Remove(new Point(20, 20), 2);
        removed2.Should().BeTrue();
        tree.Count.Should().Be(8);
    }

    #endregion

    #region Query Tests

    /// <summary>
    /// Tests basic rectangular region query functionality.
    /// The tree should return all items within the specified rectangle.
    /// </summary>
    [Fact]
    public void Query_RectangularRegion_ReturnsItemsWithinBounds()
    {
        var bounds = new Rectangle(0, 0, 100, 100);
        var tree = new QuadTree<int>(bounds);

        // Insert items in different quadrants
        tree.Insert(new Point(25, 25), 1); // NW
        tree.Insert(new Point(75, 25), 2); // NE
        tree.Insert(new Point(25, 75), 3); // SW
        tree.Insert(new Point(75, 75), 4); // SE

        // Query NW quadrant
        var results = tree.Query(new Rectangle(0, 0, 50, 50));

        results.Should().ContainSingle().Which.Should().Be(1);
    }

    /// <summary>
    /// Tests query with overlapping rectangular regions.
    /// The tree should return items that fall within the query rectangle boundaries.
    /// </summary>
    [Fact]
    public void Query_OverlappingRegions_ReturnsCorrectItems()
    {
        var bounds = new Rectangle(0, 0, 100, 100);
        var tree = new QuadTree<string>(bounds);

        tree.Insert(new Point(45, 45), "center-left");
        tree.Insert(new Point(55, 45), "center-right");
        tree.Insert(new Point(50, 50), "center");

        // Query region overlapping multiple quadrants
        var results = tree.Query(new Rectangle(40, 40, 20, 20));

        results.Should().HaveCount(3);
        results.Should().Contain("center-left");
        results.Should().Contain("center-right");
        results.Should().Contain("center");
    }

    /// <summary>
    /// Tests query with empty region returns no results.
    /// The tree should return empty collection when no items fall within query bounds.
    /// </summary>
    [Fact]
    public void Query_EmptyRegion_ReturnsEmptyCollection()
    {
        var bounds = new Rectangle(0, 0, 100, 100);
        var tree = new QuadTree<int>(bounds);

        tree.Insert(new Point(50, 50), 42);

        // Query region with no items
        var results = tree.Query(new Rectangle(80, 80, 10, 10));

        results.Should().BeEmpty();
    }

    /// <summary>
    /// Tests query with region outside tree bounds.
    /// The tree should handle queries that don't intersect with tree boundaries.
    /// </summary>
    [Fact]
    public void Query_RegionOutsideBounds_ReturnsEmptyCollection()
    {
        var bounds = new Rectangle(0, 0, 100, 100);
        var tree = new QuadTree<int>(bounds);

        tree.Insert(new Point(50, 50), 42);

        // Query completely outside tree bounds
        var results = tree.Query(new Rectangle(200, 200, 50, 50));

        results.Should().BeEmpty();
    }

    /// <summary>
    /// Tests query with callback action functionality.
    /// The tree should invoke the callback for each item found within the query region.
    /// </summary>
    [Fact]
    public void Query_WithCallback_InvokesCallbackForFoundItems()
    {
        var bounds = new Rectangle(0, 0, 100, 100);
        var tree = new QuadTree<int>(bounds);
        var collectedItems = new List<int>();

        tree.Insert(new Point(25, 25), 1);
        tree.Insert(new Point(75, 75), 2);

        tree.Query(new Rectangle(0, 0, 50, 50), item => collectedItems.Add(item));

        collectedItems.Should().ContainSingle().Which.Should().Be(1);
    }

    /// <summary>
    /// Tests query with pre-allocated results list functionality.
    /// The tree should clear the provided list and populate it with query results.
    /// </summary>
    [Fact]
    public void Query_WithPreallocatedList_ClearsAndPopulatesList()
    {
        var bounds = new Rectangle(0, 0, 100, 100);
        var tree = new QuadTree<string>(bounds);
        var results = new List<string> { "existing item" };

        tree.Insert(new Point(25, 25), "found item");

        tree.Query(new Rectangle(0, 0, 50, 50), results);

        results.Should().ContainSingle().Which.Should().Be("found item");
        results.Should().NotContain("existing item");
    }

    /// <summary>
    /// Tests query performance with large number of items.
    /// The tree should efficiently handle queries on datasets with many spatial items.
    /// </summary>
    [Fact]
    public void Query_WithLargeDataset_PerformsEfficiently()
    {
        var bounds = new Rectangle(0, 0, 1000, 1000);
        var tree = new QuadTree<int>(bounds, spatialThreshold: 100);

        // Insert 1000 items distributed across the space
        for (int i = 0; i < 1000; i++)
        {
            var x = (i % 100) * 10;
            var y = (i / 100) * 100;
            tree.Insert(new Point(x, y), i);
        }

        // Query a small region
        var results = tree.Query(new Rectangle(100, 100, 100, 100));

        // Should find items in that region efficiently
        results.Should().NotBeEmpty();
        results.Should().HaveCountLessThan(200); // Much less than total items
    }

    #endregion

    #region FindNearest Tests

    /// <summary>
    /// Tests finding nearest item to a query point.
    /// The tree should return the spatially closest item to the specified point.
    /// </summary>
    [Fact]
    public void FindNearest_WithMultipleItems_ReturnsClosestItem()
    {
        var bounds = new Rectangle(0, 0, 100, 100);
        var tree = new QuadTree<string>(bounds);

        tree.Insert(new Point(10, 10), "far");
        tree.Insert(new Point(50, 50), "near");
        tree.Insert(new Point(90, 90), "farther");

        var nearest = tree.FindNearest(new Point(55, 55));

        nearest.Should().Be("near");
    }

    /// <summary>
    /// Tests finding nearest item when tree is empty throws exception.
    /// The tree should throw InvalidOperationException when no items are present.
    /// </summary>
    [Fact]
    public void FindNearest_EmptyTree_ThrowsInvalidOperationException()
    {
        var bounds = new Rectangle(0, 0, 100, 100);
        var tree = new QuadTree<int>(bounds);

        var act = () => tree.FindNearest(new Point(50, 50));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("QuadTree is empty");
    }

    /// <summary>
    /// Tests finding nearest item with single item returns that item.
    /// The tree should return the only available item regardless of distance.
    /// </summary>
    [Fact]
    public void FindNearest_SingleItem_ReturnsThatItem()
    {
        var bounds = new Rectangle(0, 0, 100, 100);
        var tree = new QuadTree<int>(bounds);

        tree.Insert(new Point(10, 10), 42);

        var nearest = tree.FindNearest(new Point(90, 90));

        nearest.Should().Be(42);
    }

    /// <summary>
    /// Tests finding nearest item with identical distances chooses one consistently.
    /// The tree should handle tie-breaking when multiple items are equidistant.
    /// </summary>
    [Fact]
    public void FindNearest_IdenticalDistances_ReturnsOneItem()
    {
        var bounds = new Rectangle(0, 0, 100, 100);
        var tree = new QuadTree<string>(bounds);

        tree.Insert(new Point(40, 50), "left");
        tree.Insert(new Point(60, 50), "right");

        var nearest = tree.FindNearest(new Point(50, 50));

        nearest.Should().BeOneOf("left", "right");
    }

    /// <summary>
    /// Tests finding nearest item works correctly in both linear and spatial modes.
    /// The tree should return accurate nearest neighbor regardless of internal mode.
    /// </summary>
    [Fact]
    public void FindNearest_InBothModes_ReturnsCorrectItem()
    {
        var bounds = new Rectangle(0, 0, 1000, 1000);
        var tree = new QuadTree<int>(bounds, spatialThreshold: 10);

        // Test in linear mode
        tree.Insert(new Point(100, 100), 1);
        tree.Insert(new Point(200, 200), 2);
        
        var nearestLinear = tree.FindNearest(new Point(150, 150));
        nearestLinear.Should().Be(1);

        // Add more items to trigger spatial mode
        for (int i = 3; i <= 15; i++)
        {
            tree.Insert(new Point(i * 50, i * 50), i);
        }

        // Test in spatial mode
        var nearestSpatial = tree.FindNearest(new Point(150, 150));
        nearestSpatial.Should().Be(3); // Item 3 is at exact position (150,150) - closer than item 1
    }

    #endregion

    #region Edge Cases and Boundary Tests

    /// <summary>
    /// Tests tree behavior with zero-sized rectangle bounds.
    /// The tree should handle degenerate bounds gracefully without errors.
    /// </summary>
    [Fact]
    public void Constructor_ZeroSizedBounds_CreatesValidTree()
    {
        var bounds = new Rectangle(50, 50, 0, 0);
        var tree = new QuadTree<int>(bounds);

        tree.Bounds.Should().Be(bounds);
        tree.Count.Should().Be(0);
    }

    /// <summary>
    /// Tests tree behavior with very large bounds.
    /// The tree should handle large coordinate systems without precision issues.
    /// </summary>
    [Fact]
    public void Constructor_VeryLargeBounds_CreatesValidTree()
    {
        var bounds = new Rectangle(-1e6, -1e6, 2e6, 2e6);
        var tree = new QuadTree<int>(bounds);

        tree.Insert(new Point(0, 0), 42);
        tree.Count.Should().Be(1);

        var results = tree.Query(new Rectangle(-100, -100, 200, 200));
        results.Should().ContainSingle().Which.Should().Be(42);
    }

    /// <summary>
    /// Tests tree subdivision with maximum depth limitation.
    /// The tree should stop subdividing when maximum depth is reached.
    /// </summary>
    [Fact]
    public void Insert_ExceedsMaxDepth_StopsSubdividing()
    {
        var bounds = new Rectangle(0, 0, 100, 100);
        var tree = new QuadTree<int>(bounds, maxDepth: 1, maxItemsPerNode: 1, spatialThreshold: 0);

        // Insert enough items to exceed depth limit
        for (int i = 0; i < 10; i++)
        {
            tree.Insert(new Point(i, i), i);
        }

        tree.Count.Should().Be(10);
        var results = tree.Query(bounds);
        results.Should().HaveCount(10);
    }

    /// <summary>
    /// Tests query with very small rectangular region.
    /// The tree should handle precision requirements for small query areas.
    /// </summary>
    [Fact]
    public void Query_VerySmallRegion_HandlesCorrectly()
    {
        var bounds = new Rectangle(0, 0, 100, 100);
        var tree = new QuadTree<string>(bounds);

        tree.Insert(new Point(50.0001, 50.0001), "inside");
        tree.Insert(new Point(50.0002, 50.0002), "outside");

        // Very small query region
        var results = tree.Query(new Rectangle(50, 50, 0.00015, 0.00015));

        results.Should().ContainSingle().Which.Should().Be("inside");
    }

    /// <summary>
    /// Tests handling of null reference type items.
    /// The tree should support null values as valid items when T allows nulls.
    /// </summary>
    [Fact]
    public void Insert_NullReferenceType_HandlesCorrectly()
    {
        var bounds = new Rectangle(0, 0, 100, 100);
        var tree = new QuadTree<string?>(bounds);

        tree.Insert(new Point(50, 50), null);

        tree.Count.Should().Be(1);
        var results = tree.Query(bounds);
        results.Should().ContainSingle().Which.Should().BeNull();
    }

    #endregion
}
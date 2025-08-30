using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Omni.Collections.Spatial;
using Xunit;

namespace Omni.Collections.Tests.Spatial;

public class OctTreeTests : IDisposable
{
    #region Test Data Classes

    private class Point3D
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public string Name { get; set; } = "";

        public Point3D(float x, float y, float z, string name = "")
        {
            X = x;
            Y = y;
            Z = z;
            Name = name;
        }
    }

    private class TestPointProvider : IOctPointProvider<Point3D>
    {
        public Vector3 GetPosition(Point3D item)
        {
            return new Vector3(item.X, item.Y, item.Z);
        }
    }

    #endregion

    #region Constructor Tests

    /// <summary>
    /// Tests that an OctTree can be constructed with valid point provider and default minSize.
    /// The tree should have zero count initially and accept the provided point provider.
    /// </summary>
    [Fact]
    public void Constructor_WithValidPointProvider_CreatesTreeWithCorrectProperties()
    {
        var pointProvider = new TestPointProvider();
        var tree = new OctTree<Point3D>(pointProvider);

        tree.Count.Should().Be(0);
        tree.MinSize.Should().Be(1.0f);
    }

    /// <summary>
    /// Tests that an OctTree can be constructed with custom minSize parameter.
    /// The tree should accept valid positive minSize values.
    /// </summary>
    [Theory]
    [InlineData(0.1f)]
    [InlineData(5.0f)]
    [InlineData(100.0f)]
    public void Constructor_WithCustomMinSize_CreatesTreeWithValidSettings(float minSize)
    {
        var pointProvider = new TestPointProvider();
        var tree = new OctTree<Point3D>(pointProvider, minSize);

        tree.Count.Should().Be(0);
        tree.MinSize.Should().Be(minSize);
    }

    /// <summary>
    /// Tests that constructing an OctTree with invalid minSize throws exception.
    /// The constructor should reject zero or negative minSize values.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-5.5f)]
    public void Constructor_WithInvalidMinSize_ThrowsArgumentOutOfRangeException(float minSize)
    {
        var pointProvider = new TestPointProvider();
        var act = () => new OctTree<Point3D>(pointProvider, minSize);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("minSize");
    }

    /// <summary>
    /// Tests that constructing an OctTree with null point provider throws exception.
    /// The constructor should reject null point provider references.
    /// </summary>
    [Fact]
    public void Constructor_WithNullPointProvider_ThrowsArgumentNullException()
    {
        var act = () => new OctTree<Point3D>(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("pointProvider");
    }

    /// <summary>
    /// Tests the factory method Create3D with coordinate accessors.
    /// The factory should create a properly configured OctTree with 3D point provider.
    /// </summary>
    [Fact]
    public void Create3D_WithCoordinateAccessors_CreatesConfiguredTree()
    {
        var tree = OctTree<Point3D>.Create3D(p => p.X, p => p.Y, p => p.Z, 2.0f);

        tree.Count.Should().Be(0);
        tree.MinSize.Should().Be(2.0f);
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
        var tree = OctTree<Point3D>.Create3D(p => p.X, p => p.Y, p => p.Z);
        var item = new Point3D(50, 50, 50, "test");

        tree.Insert(item);

        tree.Count.Should().Be(1);
        var bounds = new OctBounds(0, 0, 0, 100, 100, 100);
        var results = tree.FindInBounds(bounds);
        results.Should().ContainSingle().Which.Should().Be(item);
    }

    /// <summary>
    /// Tests that inserting multiple items with automatic bounds expansion works correctly.
    /// The tree should expand its bounds to accommodate items outside initial bounds.
    /// </summary>
    [Fact]
    public void Insert_MultipleItemsWithBoundsExpansion_StoresAllItems()
    {
        var tree = OctTree<Point3D>.Create3D(p => p.X, p => p.Y, p => p.Z);

        var items = new[]
        {
            new Point3D(10, 10, 10, "item1"),
            new Point3D(-10, -10, -10, "item2"),
            new Point3D(100, 100, 100, "item3")
        };

        foreach (var item in items)
        {
            tree.Insert(item);
        }

        tree.Count.Should().Be(3);
        var results = tree.GetAllItems().ToList();
        results.Should().HaveCount(3);
        results.Should().BeEquivalentTo(items);
    }

    /// <summary>
    /// Tests that inserting items with predefined bounds works correctly.
    /// The tree should use the provided bounds and handle items within those bounds.
    /// </summary>
    [Fact]
    public void Insert_WithPredefinedBounds_UsesProvidedBounds()
    {
        var tree = OctTree<Point3D>.Create3D(p => p.X, p => p.Y, p => p.Z);
        var bounds = new OctBounds(-50, -50, -50, 50, 50, 50);
        var item = new Point3D(25, 25, 25, "test");

        tree.Insert(item, bounds);

        tree.Count.Should().Be(1);
        var results = tree.FindInBounds(bounds);
        results.Should().ContainSingle().Which.Should().Be(item);
    }

    /// <summary>
    /// Tests that the tree subdivides correctly when item limit is exceeded.
    /// The tree should automatically create child nodes when too many items are in one node.
    /// </summary>
    [Fact]
    public void Insert_ExceedsItemLimit_SubdividesCorrectly()
    {
        var tree = OctTree<Point3D>.Create3D(p => p.X, p => p.Y, p => p.Z, 1.0f);

        // Insert more than 10 items in same region to trigger subdivision
        for (int i = 0; i < 15; i++)
        {
            tree.Insert(new Point3D(i * 0.1f, i * 0.1f, i * 0.1f, $"item{i}"));
        }

        tree.Count.Should().Be(15);
        var results = tree.GetAllItems().ToList();
        results.Should().HaveCount(15);
    }

    /// <summary>
    /// Tests InsertRange with multiple items and automatic bounds calculation.
    /// The tree should efficiently handle bulk insertion with optimized bounds.
    /// </summary>
    [Fact]
    public void InsertRange_WithMultipleItems_InsertsAllItemsEfficiently()
    {
        var tree = OctTree<Point3D>.Create3D(p => p.X, p => p.Y, p => p.Z);

        var items = Enumerable.Range(0, 100)
            .Select(i => new Point3D(i, i, i, $"item{i}"))
            .ToArray();

        tree.InsertRange(items);

        tree.Count.Should().Be(100);
        var results = tree.GetAllItems().ToList();
        results.Should().HaveCount(100);
        results.Should().BeEquivalentTo(items);
    }

    /// <summary>
    /// Tests InsertRange with empty collection handles gracefully.
    /// The tree should remain unchanged when inserting empty collections.
    /// </summary>
    [Fact]
    public void InsertRange_WithEmptyCollection_LeavesTreeUnchanged()
    {
        var tree = OctTree<Point3D>.Create3D(p => p.X, p => p.Y, p => p.Z);
        var emptyItems = new Point3D[0];

        tree.InsertRange(emptyItems);

        tree.Count.Should().Be(0);
    }

    #endregion

    #region Sphere Query Tests

    /// <summary>
    /// Tests finding items within a spherical region.
    /// The tree should return all items that fall within the specified sphere.
    /// </summary>
    [Fact]
    public void FindInSphere_WithItemsInside_ReturnsCorrectItems()
    {
        var tree = OctTree<Point3D>.Create3D(p => p.X, p => p.Y, p => p.Z);

        tree.Insert(new Point3D(0, 0, 0, "center"));
        tree.Insert(new Point3D(5, 0, 0, "near"));
        tree.Insert(new Point3D(15, 0, 0, "far"));

        var results = tree.FindInSphere(new Vector3(0, 0, 0), 10f);

        results.Should().HaveCount(2);
        results.Should().Contain(item => item.Name == "center");
        results.Should().Contain(item => item.Name == "near");
        results.Should().NotContain(item => item.Name == "far");
    }

    /// <summary>
    /// Tests finding items in sphere when no items are present.
    /// The tree should return empty collection when no items exist.
    /// </summary>
    [Fact]
    public void FindInSphere_EmptyTree_ReturnsEmptyCollection()
    {
        var tree = OctTree<Point3D>.Create3D(p => p.X, p => p.Y, p => p.Z);

        var results = tree.FindInSphere(new Vector3(0, 0, 0), 10f);

        results.Should().BeEmpty();
    }

    /// <summary>
    /// Tests spherical query with zero radius finds only exact matches.
    /// The tree should return items at exactly the center point when radius is zero.
    /// </summary>
    [Fact]
    public void FindInSphere_ZeroRadius_FindsOnlyExactMatches()
    {
        var tree = OctTree<Point3D>.Create3D(p => p.X, p => p.Y, p => p.Z);

        tree.Insert(new Point3D(0, 0, 0, "exact"));
        tree.Insert(new Point3D(0.1f, 0, 0, "close"));

        var results = tree.FindInSphere(new Vector3(0, 0, 0), 0f);

        results.Should().ContainSingle().Which.Name.Should().Be("exact");
    }

    /// <summary>
    /// Tests spherical query with very large radius includes all items.
    /// The tree should return all items when sphere encompasses entire tree.
    /// </summary>
    [Fact]
    public void FindInSphere_VeryLargeRadius_ReturnsAllItems()
    {
        var tree = OctTree<Point3D>.Create3D(p => p.X, p => p.Y, p => p.Z);

        for (int i = 0; i < 10; i++)
        {
            tree.Insert(new Point3D(i * 10, i * 10, i * 10, $"item{i}"));
        }

        var results = tree.FindInSphere(new Vector3(50, 50, 50), 1000f);

        results.Should().HaveCount(10);
    }

    #endregion

    #region Bounds Query Tests

    /// <summary>
    /// Tests finding items within rectangular bounds.
    /// The tree should return all items that fall within the specified 3D bounds.
    /// </summary>
    [Fact]
    public void FindInBounds_WithItemsInside_ReturnsCorrectItems()
    {
        var tree = OctTree<Point3D>.Create3D(p => p.X, p => p.Y, p => p.Z);

        tree.Insert(new Point3D(25, 25, 25, "inside"));
        tree.Insert(new Point3D(75, 75, 75, "outside"));
        tree.Insert(new Point3D(0, 0, 0, "boundary"));

        var bounds = new OctBounds(0, 0, 0, 50, 50, 50);
        var results = tree.FindInBounds(bounds);

        results.Should().HaveCount(2);
        results.Should().Contain(item => item.Name == "inside");
        results.Should().Contain(item => item.Name == "boundary");
        results.Should().NotContain(item => item.Name == "outside");
    }

    /// <summary>
    /// Tests bounds query when no items are present.
    /// The tree should return empty collection when no items exist.
    /// </summary>
    [Fact]
    public void FindInBounds_EmptyTree_ReturnsEmptyCollection()
    {
        var tree = OctTree<Point3D>.Create3D(p => p.X, p => p.Y, p => p.Z);

        var bounds = new OctBounds(0, 0, 0, 100, 100, 100);
        var results = tree.FindInBounds(bounds);

        results.Should().BeEmpty();
    }

    /// <summary>
    /// Tests bounds query with very small bounds.
    /// The tree should handle precision requirements for small query volumes.
    /// </summary>
    [Fact]
    public void FindInBounds_VerySmallBounds_HandlesCorrectly()
    {
        var tree = OctTree<Point3D>.Create3D(p => p.X, p => p.Y, p => p.Z);

        tree.Insert(new Point3D(50.001f, 50.001f, 50.001f, "inside"));
        tree.Insert(new Point3D(50.002f, 50.002f, 50.002f, "outside"));

        var bounds = new OctBounds(50, 50, 50, 50.0015f, 50.0015f, 50.0015f);
        var results = tree.FindInBounds(bounds);

        results.Should().ContainSingle().Which.Name.Should().Be("inside");
    }

    #endregion

    #region Nearest Neighbor Tests

    /// <summary>
    /// Tests finding the nearest item to a target point.
    /// The tree should return the spatially closest item to the specified 3D point.
    /// </summary>
    [Fact]
    public void FindNearest_WithMultipleItems_ReturnsClosestItem()
    {
        var tree = OctTree<Point3D>.Create3D(p => p.X, p => p.Y, p => p.Z);

        tree.Insert(new Point3D(10, 10, 10, "far"));
        tree.Insert(new Point3D(52, 53, 51, "near"));
        tree.Insert(new Point3D(90, 90, 90, "farther"));

        var nearest = tree.FindNearest(new Vector3(50, 50, 50));

        nearest?.Name.Should().Be("near");
    }

    /// <summary>
    /// Tests finding nearest item when tree is empty returns null.
    /// The tree should return null when no items are present.
    /// </summary>
    [Fact]
    public void FindNearest_EmptyTree_ReturnsNull()
    {
        var tree = OctTree<Point3D>.Create3D(p => p.X, p => p.Y, p => p.Z);

        var nearest = tree.FindNearest(new Vector3(50, 50, 50));

        nearest.Should().BeNull();
    }

    /// <summary>
    /// Tests finding nearest item with single item returns that item.
    /// The tree should return the only available item regardless of distance.
    /// </summary>
    [Fact]
    public void FindNearest_SingleItem_ReturnsThatItem()
    {
        var tree = OctTree<Point3D>.Create3D(p => p.X, p => p.Y, p => p.Z);

        var item = new Point3D(100, 100, 100, "only");
        tree.Insert(item);

        var nearest = tree.FindNearest(new Vector3(0, 0, 0));

        nearest.Should().Be(item);
    }

    /// <summary>
    /// Tests finding nearest item with identical distances chooses one consistently.
    /// The tree should handle tie-breaking when multiple items are equidistant.
    /// </summary>
    [Fact]
    public void FindNearest_IdenticalDistances_ReturnsOneItem()
    {
        var tree = OctTree<Point3D>.Create3D(p => p.X, p => p.Y, p => p.Z);

        tree.Insert(new Point3D(45, 50, 50, "left"));
        tree.Insert(new Point3D(55, 50, 50, "right"));

        var nearest = tree.FindNearest(new Vector3(50, 50, 50));

        nearest?.Name.Should().BeOneOf("left", "right");
    }

    #endregion

    #region Collision Detection Tests

    /// <summary>
    /// Tests finding collisions for an item within a specified radius.
    /// The tree should return nearby items excluding the query item itself.
    /// </summary>
    [Fact]
    public void FindCollisions_WithNearbyItems_ReturnsColliders()
    {
        var tree = OctTree<Point3D>.Create3D(p => p.X, p => p.Y, p => p.Z);

        var queryItem = new Point3D(50, 50, 50, "query");
        tree.Insert(queryItem);
        tree.Insert(new Point3D(52, 52, 52, "close"));
        tree.Insert(new Point3D(70, 70, 70, "far"));

        var collisions = tree.FindCollisions(queryItem, 10f);

        collisions.Should().ContainSingle().Which.Name.Should().Be("close");
        collisions.Should().NotContain(queryItem);
    }

    /// <summary>
    /// Tests collision detection when no nearby items exist.
    /// The tree should return empty collection when no items are within collision radius.
    /// </summary>
    [Fact]
    public void FindCollisions_NoNearbyItems_ReturnsEmptyCollection()
    {
        var tree = OctTree<Point3D>.Create3D(p => p.X, p => p.Y, p => p.Z);

        var queryItem = new Point3D(50, 50, 50, "query");
        tree.Insert(queryItem);
        tree.Insert(new Point3D(100, 100, 100, "far"));

        var collisions = tree.FindCollisions(queryItem, 10f);

        collisions.Should().BeEmpty();
    }

    #endregion

    #region Frustum Culling Tests

    /// <summary>
    /// Tests finding items within a view frustum.
    /// The tree should return all items that fall within the specified viewing frustum.
    /// </summary>
    [Fact]
    public void FindInFrustum_WithItemsInside_ReturnsCorrectItems()
    {
        var tree = OctTree<Point3D>.Create3D(p => p.X, p => p.Y, p => p.Z);

        tree.Insert(new Point3D(0, 0, 0, "inside"));
        tree.Insert(new Point3D(100, 100, 100, "outside"));

        // Create a simple frustum (6 planes forming a box)
        var planes = new[]
        {
            new Plane(new Vector3(1, 0, 0), 10),    // Right
            new Plane(new Vector3(-1, 0, 0), 10),   // Left
            new Plane(new Vector3(0, 1, 0), 10),    // Top
            new Plane(new Vector3(0, -1, 0), 10),   // Bottom
            new Plane(new Vector3(0, 0, 1), 10),    // Far
            new Plane(new Vector3(0, 0, -1), 10)    // Near
        };

        var frustum = new Frustum(planes);
        var results = tree.FindInFrustum(frustum);

        results.Should().ContainSingle().Which.Name.Should().Be("inside");
    }

    /// <summary>
    /// Tests frustum query when no items are present.
    /// The tree should return empty collection when no items exist.
    /// </summary>
    [Fact]
    public void FindInFrustum_EmptyTree_ReturnsEmptyCollection()
    {
        var tree = OctTree<Point3D>.Create3D(p => p.X, p => p.Y, p => p.Z);

        var planes = new[]
        {
            new Plane(new Vector3(1, 0, 0), 10),
            new Plane(new Vector3(-1, 0, 0), 10),
            new Plane(new Vector3(0, 1, 0), 10),
            new Plane(new Vector3(0, -1, 0), 10),
            new Plane(new Vector3(0, 0, 1), 10),
            new Plane(new Vector3(0, 0, -1), 10)
        };

        var frustum = new Frustum(planes);
        var results = tree.FindInFrustum(frustum);

        results.Should().BeEmpty();
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
        var tree = OctTree<Point3D>.Create3D(p => p.X, p => p.Y, p => p.Z);

        for (int i = 0; i < 10; i++)
        {
            tree.Insert(new Point3D(i, i, i, $"item{i}"));
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
        var tree = OctTree<Point3D>.Create3D(p => p.X, p => p.Y, p => p.Z);

        tree.Insert(new Point3D(50, 50, 50, "test"));
        tree.Dispose();

        tree.Count.Should().Be(0);
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
        var tree = OctTree<Point3D>.Create3D(p => p.X, p => p.Y, p => p.Z);

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
        var tree = OctTree<Point3D>.Create3D(p => p.X, p => p.Y, p => p.Z);

        var allItems = tree.GetAllItems().ToList();

        allItems.Should().BeEmpty();
    }

    #endregion

    #region Edge Cases Tests

    /// <summary>
    /// Tests tree behavior with items at extreme coordinates.
    /// The tree should handle very large coordinate values without precision issues.
    /// </summary>
    [Fact]
    public void Insert_ExtremeCoordinates_HandlesCorrectly()
    {
        var tree = OctTree<Point3D>.Create3D(p => p.X, p => p.Y, p => p.Z);

        var extremeItem = new Point3D(1e6f, -1e6f, 1e6f, "extreme");
        tree.Insert(extremeItem);

        tree.Count.Should().Be(1);
        var results = tree.GetAllItems().ToList();
        results.Should().ContainSingle().Which.Should().Be(extremeItem);
    }

    /// <summary>
    /// Tests tree subdivision with minimum size limitation.
    /// The tree should stop subdividing when minimum size is reached.
    /// </summary>
    [Fact]
    public void Insert_WithMinSizeLimit_StopsSubdividing()
    {
        var tree = OctTree<Point3D>.Create3D(p => p.X, p => p.Y, p => p.Z, 10.0f);

        // Insert many items in small area to test minimum size limit
        for (int i = 0; i < 20; i++)
        {
            tree.Insert(new Point3D(i * 0.1f, i * 0.1f, i * 0.1f, $"item{i}"));
        }

        tree.Count.Should().Be(20);
        var results = tree.GetAllItems().ToList();
        results.Should().HaveCount(20);
    }

    /// <summary>
    /// Tests handling of duplicate positions with different items.
    /// The tree should store multiple items at the same 3D coordinate.
    /// </summary>
    [Fact]
    public void Insert_DuplicatePositions_StoresBothItems()
    {
        var tree = OctTree<Point3D>.Create3D(p => p.X, p => p.Y, p => p.Z);

        tree.Insert(new Point3D(50, 50, 50, "first"));
        tree.Insert(new Point3D(50, 50, 50, "second"));

        tree.Count.Should().Be(2);
        var results = tree.GetAllItems().ToList();
        results.Should().HaveCount(2);
        results.Should().Contain(item => item.Name == "first");
        results.Should().Contain(item => item.Name == "second");
    }

    #endregion

    public void Dispose()
    {
        // Test cleanup if needed
    }
}
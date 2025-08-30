using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Omni.Collections.Linear;
using Omni.Collections.Spatial;
using Xunit;

namespace Omni.Collections.Tests.Spatial;

public class SpatialHashGridTests : IDisposable
{
    #region Constructor Tests

    /// <summary>
    /// Tests that a SpatialHashGrid can be constructed with default parameters.
    /// The grid should have default cell size and zero count initially.
    /// </summary>
    [Fact]
    public void Constructor_WithDefaults_CreatesGridWithCorrectProperties()
    {
        var grid = new SpatialHashGrid<string>();

        grid.Count.Should().Be(0);
        grid.CellSize.Should().Be(64.0f);
        grid.OccupiedCells.Should().Be(0);
    }

    /// <summary>
    /// Tests that a SpatialHashGrid can be constructed with custom parameters.
    /// The grid should accept valid cell size, spatial threshold, and expected item count.
    /// </summary>
    [Theory]
    [InlineData(16.0f, 1000, 100)]
    [InlineData(32.0f, 5000, 500)]
    [InlineData(128.0f, 10000, 1000)]
    public void Constructor_WithCustomParameters_CreatesGridWithValidSettings(float cellSize, int spatialThreshold, int expectedItems)
    {
        var grid = new SpatialHashGrid<int>(cellSize, spatialThreshold, expectedItems);

        grid.Count.Should().Be(0);
        grid.CellSize.Should().Be(cellSize);
        grid.OccupiedCells.Should().Be(0);
    }

    /// <summary>
    /// Tests that constructing a SpatialHashGrid with invalid cell size throws exception.
    /// The constructor should reject zero or negative cell size values.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10.5f)]
    public void Constructor_WithInvalidCellSize_ThrowsArgumentOutOfRangeException(float cellSize)
    {
        var act = () => new SpatialHashGrid<int>(cellSize);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("cellSize");
    }

    #endregion

    #region Insert Tests

    /// <summary>
    /// Tests that inserting a single item at a position works correctly.
    /// The grid count should increase and the item should be retrievable at that position.
    /// </summary>
    [Fact]
    public void Insert_SingleItem_IncreasesCountAndStoresItem()
    {
        var grid = new SpatialHashGrid<string>();

        grid.Insert(50.0f, 50.0f, "test item");

        grid.Count.Should().Be(1);
        var items = grid.GetObjectsAt(50.0f, 50.0f).ToList();
        items.Should().ContainSingle().Which.Should().Be("test item");
    }

    /// <summary>
    /// Tests that inserting multiple items at different positions works correctly.
    /// The grid should store all items at their respective positions.
    /// </summary>
    [Fact]
    public void Insert_MultipleItemsAtDifferentPositions_StoresAllItems()
    {
        var grid = new SpatialHashGrid<int>();

        grid.Insert(10.0f, 10.0f, 1);
        grid.Insert(50.0f, 50.0f, 2);
        grid.Insert(90.0f, 90.0f, 3);

        grid.Count.Should().Be(3);
        grid.GetObjectsAt(10.0f, 10.0f).Should().ContainSingle().Which.Should().Be(1);
        grid.GetObjectsAt(50.0f, 50.0f).Should().ContainSingle().Which.Should().Be(2);
        grid.GetObjectsAt(90.0f, 90.0f).Should().ContainSingle().Which.Should().Be(3);
    }

    /// <summary>
    /// Tests that inserting multiple items at the same position works correctly.
    /// The grid should allow multiple items at the same spatial location.
    /// </summary>
    [Fact]
    public void Insert_MultipleItemsAtSamePosition_StoresBothItems()
    {
        var grid = new SpatialHashGrid<string>();

        grid.Insert(50.0f, 50.0f, "first");
        grid.Insert(50.0f, 50.0f, "second");

        grid.Count.Should().Be(2);
        var items = grid.GetObjectsAt(50.0f, 50.0f).ToList();
        items.Should().HaveCount(2);
        items.Should().Contain("first");
        items.Should().Contain("second");
    }

    /// <summary>
    /// Tests mode transition from linear to spatial when threshold is exceeded.
    /// The grid should automatically convert to spatial mode for better performance.
    /// </summary>
    [Fact]
    public void Insert_ExceedsSpatialThreshold_ConvertsToSpatialMode()
    {
        var grid = new SpatialHashGrid<int>(64.0f, 10); // Low threshold for testing

        // Insert items to exceed threshold
        for (int i = 0; i < 15; i++)
        {
            grid.Insert(i * 10.0f, i * 10.0f, i);
        }

        grid.Count.Should().Be(15);
        grid.OccupiedCells.Should().BeGreaterThan(0); // Indicates spatial mode

        // Verify all items are still accessible
        for (int i = 0; i < 15; i++)
        {
            var items = grid.GetObjectsAt(i * 10.0f, i * 10.0f).ToList();
            items.Should().ContainSingle().Which.Should().Be(i);
        }
    }

    /// <summary>
    /// Tests inserting items with bounds (area insertion) works correctly.
    /// The grid should store items across multiple cells based on their bounds.
    /// </summary>
    [Fact]
    public void InsertBounds_WithAreaItem_StoresInMultipleCells()
    {
        var grid = new SpatialHashGrid<string>(32.0f, 0); // Force spatial mode

        // Insert item with bounds that spans multiple cells
        grid.InsertBounds(64.0f, 64.0f, 48.0f, 48.0f, "large item");

        grid.Count.Should().Be(1);
        grid.OccupiedCells.Should().BeGreaterThan(1); // Should occupy multiple cells

        // Item should be found in queries that overlap its bounds
        var items = grid.GetObjectsInRadius(64.0f, 64.0f, 10.0f).ToList();
        items.Should().Contain("large item");
    }

    #endregion

    #region Remove Tests

    /// <summary>
    /// Tests that removing an existing item works correctly.
    /// The grid count should decrease and the item should no longer be retrievable.
    /// </summary>
    [Fact]
    public void Remove_ExistingItem_DecreasesCountAndRemovesItem()
    {
        var grid = new SpatialHashGrid<string>();

        grid.Insert(50.0f, 50.0f, "test item");
        var removed = grid.Remove(50.0f, 50.0f, "test item");

        removed.Should().BeTrue();
        grid.Count.Should().Be(0);
        var items = grid.GetObjectsAt(50.0f, 50.0f).ToList();
        items.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that removing a non-existing item returns false.
    /// The grid should remain unchanged when attempting to remove items that don't exist.
    /// </summary>
    [Fact]
    public void Remove_NonExistingItem_ReturnsFalseAndLeavesGridUnchanged()
    {
        var grid = new SpatialHashGrid<string>();

        grid.Insert(50.0f, 50.0f, "existing");
        var removed = grid.Remove(50.0f, 50.0f, "non-existing");

        removed.Should().BeFalse();
        grid.Count.Should().Be(1);
        var items = grid.GetObjectsAt(50.0f, 50.0f).ToList();
        items.Should().ContainSingle().Which.Should().Be("existing");
    }

    /// <summary>
    /// Tests that removing one of multiple items at same position works correctly.
    /// The grid should remove only the specified item while preserving others.
    /// </summary>
    [Fact]
    public void Remove_OneOfMultipleAtSamePosition_RemovesOnlySpecifiedItem()
    {
        var grid = new SpatialHashGrid<string>();

        grid.Insert(50.0f, 50.0f, "first");
        grid.Insert(50.0f, 50.0f, "second");
        grid.Insert(50.0f, 50.0f, "third");

        var removed = grid.Remove(50.0f, 50.0f, "second");

        removed.Should().BeTrue();
        grid.Count.Should().Be(2);
        var items = grid.GetObjectsAt(50.0f, 50.0f).ToList();
        items.Should().HaveCount(2);
        items.Should().Contain("first");
        items.Should().Contain("third");
        items.Should().NotContain("second");
    }

    /// <summary>
    /// Tests removing items in both linear and spatial modes.
    /// The grid should handle removal correctly regardless of internal mode.
    /// </summary>
    [Fact]
    public void Remove_ItemsInBothModes_WorksCorrectly()
    {
        var grid = new SpatialHashGrid<int>(64.0f, 5);

        // Insert items in linear mode
        grid.Insert(10.0f, 10.0f, 1);
        grid.Insert(20.0f, 20.0f, 2);

        // Remove in linear mode
        var removed1 = grid.Remove(10.0f, 10.0f, 1);
        removed1.Should().BeTrue();
        grid.Count.Should().Be(1);

        // Add more items to trigger spatial mode
        for (int i = 3; i <= 10; i++)
        {
            grid.Insert(i * 10.0f, i * 10.0f, i);
        }

        // Remove in spatial mode
        var removed2 = grid.Remove(20.0f, 20.0f, 2);
        removed2.Should().BeTrue();
        grid.Count.Should().Be(8);
    }

    /// <summary>
    /// Tests that removing item requires exact position matching.
    /// The grid should require exact coordinates for removal operations.
    /// </summary>
    [Fact]
    public void Remove_WithExactPosition_FindsAndRemovesItem()
    {
        var grid = new SpatialHashGrid<string>();

        grid.Insert(50.0f, 50.0f, "test");

        // Remove with exact position
        var removed = grid.Remove(50.0f, 50.0f, "test");

        removed.Should().BeTrue();
        grid.Count.Should().Be(0);
    }

    #endregion

    #region Query Tests

    /// <summary>
    /// Tests getting objects at specific position returns correct items.
    /// The grid should return all items located at the exact position.
    /// </summary>
    [Fact]
    public void GetObjectsAt_SpecificPosition_ReturnsCorrectItems()
    {
        var grid = new SpatialHashGrid<string>();

        grid.Insert(50.0f, 50.0f, "at position");
        grid.Insert(60.0f, 60.0f, "elsewhere");

        var items = grid.GetObjectsAt(50.0f, 50.0f).ToList();

        items.Should().ContainSingle().Which.Should().Be("at position");
    }

    /// <summary>
    /// Tests getting objects in radius returns items within distance.
    /// The grid should return all items within the specified circular radius.
    /// </summary>
    [Fact]
    public void GetObjectsInRadius_WithItemsInRange_ReturnsCorrectItems()
    {
        var grid = new SpatialHashGrid<string>();

        grid.Insert(50.0f, 50.0f, "center");
        grid.Insert(55.0f, 55.0f, "near");  // ~7.07 units away
        grid.Insert(70.0f, 70.0f, "far");   // ~28.28 units away

        var items = grid.GetObjectsInRadius(50.0f, 50.0f, 10.0f).ToList();

        items.Should().HaveCount(2);
        items.Should().Contain("center");
        items.Should().Contain("near");
        items.Should().NotContain("far");
    }

    /// <summary>
    /// Tests getting objects in rectangular region returns correct items.
    /// The grid should return all items within the specified rectangle bounds.
    /// </summary>
    [Fact]
    public void GetObjectsInRectangle_WithItemsInside_ReturnsCorrectItems()
    {
        var grid = new SpatialHashGrid<string>();

        grid.Insert(25.0f, 25.0f, "inside");
        grid.Insert(75.0f, 75.0f, "outside");
        grid.Insert(0.0f, 0.0f, "boundary");

        var items = grid.GetObjectsInRectangle(0.0f, 0.0f, 50.0f, 50.0f).ToList();

        items.Should().HaveCount(2);
        items.Should().Contain("inside");
        items.Should().Contain("boundary");
        items.Should().NotContain("outside");
    }

    /// <summary>
    /// Tests query with empty grid returns empty collections.
    /// The grid should return no results when no items are present.
    /// </summary>
    [Fact]
    public void Queries_EmptyGrid_ReturnEmptyCollections()
    {
        var grid = new SpatialHashGrid<int>();

        grid.GetObjectsAt(50.0f, 50.0f).Should().BeEmpty();
        grid.GetObjectsInRadius(50.0f, 50.0f, 10.0f).Should().BeEmpty();
        grid.GetObjectsInRectangle(0.0f, 0.0f, 100.0f, 100.0f).Should().BeEmpty();
    }

    /// <summary>
    /// Tests queries work correctly in both linear and spatial modes.
    /// The grid should return consistent results regardless of internal mode.
    /// </summary>
    [Fact]
    public void Queries_WorkInBothModes_ReturnConsistentResults()
    {
        var gridLinear = new SpatialHashGrid<int>(64.0f, 1000); // High threshold, stays linear
        var gridSpatial = new SpatialHashGrid<int>(64.0f, 0);   // Force spatial mode

        // Insert same data in both grids
        var testData = new[] { (10.0f, 10.0f, 1), (20.0f, 20.0f, 2), (30.0f, 30.0f, 3) };
        foreach (var (x, y, item) in testData)
        {
            gridLinear.Insert(x, y, item);
            gridSpatial.Insert(x, y, item);
        }

        // Test radius query consistency
        var linearResults = gridLinear.GetObjectsInRadius(15.0f, 15.0f, 10.0f).ToList();
        var spatialResults = gridSpatial.GetObjectsInRadius(15.0f, 15.0f, 10.0f).ToList();

        linearResults.Should().BeEquivalentTo(spatialResults);
    }

    #endregion

    #region Collision Detection Tests

    /// <summary>
    /// Tests getting potential collisions returns all pairs of items in same cells.
    /// The grid should return pairs of items that could potentially collide.
    /// </summary>
    [Fact]
    public void GetPotentialCollisions_WithItemsInSameCells_ReturnsPairs()
    {
        var grid = new SpatialHashGrid<string>(64.0f, 0); // Force spatial mode

        // Insert items that will be in same or adjacent cells
        grid.Insert(32.0f, 32.0f, "item1");
        grid.Insert(33.0f, 33.0f, "item2");
        grid.Insert(150.0f, 150.0f, "item3"); // Different cell

        var collisions = grid.GetPotentialCollisions().ToList();

        // Should find collision between item1 and item2 (same cell)
        collisions.Should().Contain(pair => 
            (pair.first == "item1" && pair.second == "item2") ||
            (pair.first == "item2" && pair.second == "item1"));
    }

    /// <summary>
    /// Tests getting potential collisions with specific position and exclusion.
    /// The grid should return items at the position excluding the specified item.
    /// </summary>
    [Fact]
    public void GetPotentialCollisions_WithPositionAndExclusion_ReturnsCorrectItems()
    {
        var grid = new SpatialHashGrid<string>();

        grid.Insert(50.0f, 50.0f, "target");
        grid.Insert(50.0f, 50.0f, "collider");
        grid.Insert(100.0f, 100.0f, "distant");

        var collisions = grid.GetPotentialCollisions(50.0f, 50.0f, "target").ToList();

        // Note: GetPotentialCollisions returns ALL items in the cell, not just at exact position
        collisions.Should().Contain("collider");
        collisions.Should().NotContain("target");
        // "distant" might be included if in same cell, depending on cell size
    }

    /// <summary>
    /// Tests getting potential collisions using pooled list for efficiency.
    /// The grid should populate the provided pooled list with collision pairs.
    /// </summary>
    [Fact]
    public void GetPotentialCollisions_WithPooledList_PopulatesResults()
    {
        var grid = new SpatialHashGrid<int>(32.0f, 0); // Force spatial mode
        var results = new PooledList<(int, int)>();

        // Insert items in same cell
        grid.Insert(16.0f, 16.0f, 1);
        grid.Insert(17.0f, 17.0f, 2);
        grid.Insert(18.0f, 18.0f, 3);

        var count = grid.GetPotentialCollisions(results);

        count.Should().Be(3); // 3 items = 3 pairs (1,2), (1,3), (2,3)
        results.Count.Should().Be(3);
        results.Dispose();
    }

    #endregion

    #region Statistics and Information Tests

    /// <summary>
    /// Tests getting statistics provides correct information about grid state.
    /// The statistics should accurately reflect the current state of the grid.
    /// </summary>
    [Fact]
    public void GetStats_WithItems_ProvidesAccurateStatistics()
    {
        var grid = new SpatialHashGrid<int>(32.0f, 0); // Force spatial mode

        // Insert items distributed across cells
        for (int i = 0; i < 10; i++)
        {
            grid.Insert(i * 32.0f, 0.0f, i); // Each in different cell
        }

        var stats = grid.GetStats();

        stats.TotalObjects.Should().Be(10);
        stats.OccupiedCells.Should().Be(10);
        stats.AverageObjectsPerCell.Should().Be(1.0f);
        stats.MaxObjectsPerCell.Should().Be(1);
        stats.MedianObjectsPerCell.Should().Be(1);
    }

    /// <summary>
    /// Tests getting statistics in linear mode provides correct information.
    /// The statistics should handle linear mode appropriately.
    /// </summary>
    [Fact]
    public void GetStats_InLinearMode_ProvidesCorrectStatistics()
    {
        var grid = new SpatialHashGrid<int>(64.0f, 1000); // High threshold for linear mode

        for (int i = 0; i < 5; i++)
        {
            grid.Insert(i * 10.0f, 0.0f, i);
        }

        var stats = grid.GetStats();

        stats.TotalObjects.Should().Be(5);
        stats.OccupiedCells.Should().Be(1); // Linear mode shows as one "cell"
        stats.AverageObjectsPerCell.Should().Be(5.0f);
        stats.MaxObjectsPerCell.Should().Be(5);
    }

    /// <summary>
    /// Tests getting all objects returns complete item enumeration.
    /// The grid should return all items with their positions.
    /// </summary>
    [Fact]
    public void GetAllObjects_WithItems_ReturnsCompleteEnumeration()
    {
        var grid = new SpatialHashGrid<string>();

        var testData = new[]
        {
            (10.0f, 10.0f, "item1"),
            (20.0f, 20.0f, "item2"),
            (30.0f, 30.0f, "item3")
        };

        foreach (var (x, y, item) in testData)
        {
            grid.Insert(x, y, item);
        }

        var allObjects = grid.GetAllObjects().ToList();

        allObjects.Should().HaveCount(3);
        allObjects.Should().BeEquivalentTo(testData);
    }

    #endregion

    #region BuildFromItems and Clear Tests

    /// <summary>
    /// Tests building grid from collection of items works correctly.
    /// The grid should efficiently initialize from a collection of positioned items.
    /// </summary>
    [Fact]
    public void BuildFromItems_WithCollection_InitializesGridCorrectly()
    {
        var grid = new SpatialHashGrid<string>();

        var items = new[]
        {
            (10.0f, 10.0f, "item1"),
            (20.0f, 20.0f, "item2"),
            (30.0f, 30.0f, "item3")
        };

        grid.BuildFromItems(items);

        grid.Count.Should().Be(3);
        var allObjects = grid.GetAllObjects().ToList();
        allObjects.Should().BeEquivalentTo(items);
    }

    /// <summary>
    /// Tests building from large collection forces spatial mode.
    /// The grid should automatically use spatial mode for large datasets.
    /// </summary>
    [Fact]
    public void BuildFromItems_LargeCollection_ForcesSpatialMode()
    {
        var grid = new SpatialHashGrid<int>(32.0f, 100);

        var items = Enumerable.Range(0, 200)
            .Select(i => ((float)i, (float)i, i))
            .ToArray();

        grid.BuildFromItems(items);

        grid.Count.Should().Be(200);
        grid.OccupiedCells.Should().BeGreaterThan(1); // Should be in spatial mode
    }

    /// <summary>
    /// Tests building from empty collection handles gracefully.
    /// The grid should remain empty when built from empty collection.
    /// </summary>
    [Fact]
    public void BuildFromItems_EmptyCollection_LeavesGridEmpty()
    {
        var grid = new SpatialHashGrid<string>();

        grid.Insert(50.0f, 50.0f, "existing");
        grid.BuildFromItems(Array.Empty<(float, float, string)>());

        grid.Count.Should().Be(0);
    }

    /// <summary>
    /// Tests clearing the grid removes all items and resets state.
    /// The grid should be empty after clearing and ready for new items.
    /// </summary>
    [Fact]
    public void Clear_WithItems_RemovesAllItemsAndResetsState()
    {
        var grid = new SpatialHashGrid<int>();

        for (int i = 0; i < 10; i++)
        {
            grid.Insert(i * 10.0f, i * 10.0f, i);
        }

        grid.Clear();

        grid.Count.Should().Be(0);
        grid.OccupiedCells.Should().Be(0);
        grid.GetAllObjects().Should().BeEmpty();
    }

    /// <summary>
    /// Tests disposing the grid cleans up resources properly.
    /// The grid should be empty after disposal and have zero count.
    /// </summary>
    [Fact]
    public void Dispose_WithItems_CleansUpResources()
    {
        var grid = new SpatialHashGrid<string>();

        grid.Insert(50.0f, 50.0f, "test");
        grid.Dispose();

        grid.Count.Should().Be(0);
        grid.OccupiedCells.Should().Be(0);
    }

    #endregion

    #region Edge Cases and Performance Tests

    /// <summary>
    /// Tests grid behavior with negative coordinates.
    /// The grid should handle negative coordinate values correctly.
    /// </summary>
    [Fact]
    public void Insert_NegativeCoordinates_HandlesCorrectly()
    {
        var grid = new SpatialHashGrid<string>();

        grid.Insert(-50.0f, -50.0f, "negative");
        grid.Insert(50.0f, 50.0f, "positive");

        grid.Count.Should().Be(2);
        grid.GetObjectsAt(-50.0f, -50.0f).Should().ContainSingle().Which.Should().Be("negative");
        grid.GetObjectsAt(50.0f, 50.0f).Should().ContainSingle().Which.Should().Be("positive");
    }

    /// <summary>
    /// Tests grid behavior with very large coordinate values.
    /// The grid should handle extreme coordinate values without precision issues.
    /// </summary>
    [Fact]
    public void Insert_VeryLargeCoordinates_HandlesCorrectly()
    {
        var grid = new SpatialHashGrid<string>();

        var largeX = 1e6f;
        var largeY = 1e6f;

        grid.Insert(largeX, largeY, "large");

        grid.Count.Should().Be(1);
        grid.GetObjectsAt(largeX, largeY).Should().ContainSingle().Which.Should().Be("large");
    }

    /// <summary>
    /// Tests grid performance with large number of items.
    /// The grid should efficiently handle thousands of items.
    /// </summary>
    [Fact]
    public void Insert_LargeDataset_PerformsEfficiently()
    {
        var grid = new SpatialHashGrid<int>(64.0f, 1000);

        // Insert 5000 items
        for (int i = 0; i < 5000; i++)
        {
            grid.Insert(i % 100 * 10.0f, i / 100 * 10.0f, i);
        }

        grid.Count.Should().Be(5000);

        // Test query performance
        var items = grid.GetObjectsInRadius(500.0f, 500.0f, 100.0f).ToList();
        items.Should().NotBeEmpty();
    }

    /// <summary>
    /// Tests cell key calculation with boundary values.
    /// The grid should correctly calculate cell keys for items near cell boundaries.
    /// </summary>
    [Fact]
    public void Insert_CellBoundaryValues_CalculatesKeysCorrectly()
    {
        var grid = new SpatialHashGrid<string>(32.0f, 0); // Force spatial mode

        // Insert items at cell boundaries - ensuring they're in different cells
        grid.Insert(32.0f, 32.0f, "boundary1");    // Cell (1,1)
        grid.Insert(31.9f, 31.9f, "nearby1");      // Cell (0,0)
        grid.Insert(64.1f, 64.1f, "nearby2");      // Cell (2,2)

        grid.Count.Should().Be(3);

        // All should be retrievable from their respective positions
        var items1 = grid.GetObjectsAt(32.0f, 32.0f).ToList();
        var items2 = grid.GetObjectsAt(31.9f, 31.9f).ToList();
        var items3 = grid.GetObjectsAt(64.1f, 64.1f).ToList();

        items1.Should().ContainSingle().Which.Should().Be("boundary1");
        items2.Should().ContainSingle().Which.Should().Be("nearby1");
        items3.Should().ContainSingle().Which.Should().Be("nearby2");
    }

    /// <summary>
    /// Tests that null reference type items are handled correctly.
    /// The grid should support null values as valid items when T allows nulls.
    /// </summary>
    [Fact]
    public void Insert_NullReferenceType_HandlesCorrectly()
    {
        var grid = new SpatialHashGrid<string?>();

        grid.Insert(50.0f, 50.0f, null);

        grid.Count.Should().Be(1);
        var items = grid.GetObjectsAt(50.0f, 50.0f).ToList();
        items.Should().ContainSingle().Which.Should().BeNull();
    }

    #endregion

    public void Dispose()
    {
        // Test cleanup if needed
    }
}
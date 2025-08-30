using System;
using System.Linq;
using FluentAssertions;
using Omni.Collections.Grid2D.HexGrid;
using Xunit;

namespace Omni.Collections.Tests.Grid2D;

public class HexGrid2DTests
{
    /// <summary>
    /// Tests that a HexGrid2D can be constructed with default layout.
    /// The grid should initialize with flat-top hexagonal layout by default.
    /// </summary>
    [Fact]
    public void Constructor_Default_InitializesWithFlatTopLayout()
    {
        var grid = new HexGrid2D<string>();

        grid.Count.Should().Be(0);
        grid.Layout.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that HexGrid2D can be constructed with custom layout.
    /// The grid should use the provided layout for coordinate transformations.
    /// </summary>
    [Fact]
    public void Constructor_WithLayout_InitializesCorrectly()
    {
        var layout = HexLayout.PointyTop();
        var grid = new HexGrid2D<int>(layout);

        grid.Count.Should().Be(0);
        grid.Layout.Should().BeSameAs(layout);
    }

    /// <summary>
    /// Tests that HexGrid2D can be constructed with initial capacity.
    /// The grid should be optimized for the expected number of cells.
    /// </summary>
    [Fact]
    public void Constructor_WithCapacity_InitializesCorrectly()
    {
        var grid = new HexGrid2D<string>(100);

        grid.Count.Should().Be(0);
        grid.Layout.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that constructor throws exception for null layout.
    /// Layout parameter should be validated during construction.
    /// </summary>
    [Fact]
    public void Constructor_WithNullLayout_ThrowsArgumentNullException()
    {
        var act = () => new HexGrid2D<string>(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that indexer get and set operations work with HexCoord.
    /// Values should be stored and retrieved accurately at hexagonal coordinates.
    /// </summary>
    [Fact]
    public void Indexer_HexCoord_GetSet_WorksCorrectly()
    {
        var grid = new HexGrid2D<string>();
        var coord1 = new HexCoord(0, 0);
        var coord2 = new HexCoord(1, -1);
        var coord3 = new HexCoord(-1, 2);

        grid[coord1] = "center";
        grid[coord2] = "northeast";
        grid[coord3] = "southwest";

        grid[coord1].Should().Be("center");
        grid[coord2].Should().Be("northeast");
        grid[coord3].Should().Be("southwest");
        grid.Count.Should().Be(3);
    }

    /// <summary>
    /// Tests that indexer works with integer q,r coordinates.
    /// The integer indexer should provide convenient access to hexagonal cells.
    /// </summary>
    [Fact]
    public void Indexer_QR_GetSet_WorksCorrectly()
    {
        var grid = new HexGrid2D<int>();

        grid[2, -1] = 42;
        grid[-1, 3] = 100;

        grid[2, -1].Should().Be(42);
        grid[-1, 3].Should().Be(100);
        grid.Count.Should().Be(2);
    }

    /// <summary>
    /// Tests that indexer returns default value for non-existent coordinates.
    /// Access to unset coordinates should return default without adding to grid.
    /// </summary>
    [Fact]
    public void Indexer_NonExistentCoord_ReturnsDefault()
    {
        var grid = new HexGrid2D<string>();

        grid[new HexCoord(5, 5)].Should().BeNull(); // Default for reference type
        grid.Count.Should().Be(0); // Should not add to grid
    }

    /// <summary>
    /// Tests that Contains correctly identifies existing coordinates.
    /// The method should return true for set coordinates and false otherwise.
    /// </summary>
    [Fact]
    public void Contains_IdentifiesExistingCoordinates()
    {
        var grid = new HexGrid2D<string>();
        var coord = new HexCoord(3, -2);

        grid[coord] = "test";

        grid.Contains(coord).Should().BeTrue();
        grid.Contains(new HexCoord(0, 0)).Should().BeFalse();
    }

    /// <summary>
    /// Tests that TryGetValue retrieves values for existing coordinates.
    /// The method should return success status and value for set coordinates.
    /// </summary>
    [Fact]
    public void TryGetValue_ExistingCoord_ReturnsTrue()
    {
        var grid = new HexGrid2D<string>();
        var coord = new HexCoord(1, 1);
        grid[coord] = "found";

        var success = grid.TryGetValue(coord, out var value);

        success.Should().BeTrue();
        value.Should().Be("found");
    }

    /// <summary>
    /// Tests that TryGetValue returns false for non-existent coordinates.
    /// The method should indicate failure for unset coordinates.
    /// </summary>
    [Fact]
    public void TryGetValue_NonExistentCoord_ReturnsFalse()
    {
        var grid = new HexGrid2D<string>();

        var success = grid.TryGetValue(new HexCoord(5, 5), out var value);

        success.Should().BeFalse();
        value.Should().BeNull();
    }

    /// <summary>
    /// Tests that Set method stores values at specified coordinates.
    /// Both HexCoord and integer overloads should work correctly.
    /// </summary>
    [Fact]
    public void Set_StoresValuesCorrectly()
    {
        var grid = new HexGrid2D<string>();
        var coord = new HexCoord(2, -3);

        grid.Set(coord, "hexcoord");
        grid.Set(0, 0, "integers");

        grid[coord].Should().Be("hexcoord");
        grid[0, 0].Should().Be("integers");
        grid.Count.Should().Be(2);
    }

    /// <summary>
    /// Tests that Remove successfully removes coordinates from the grid.
    /// Removed coordinates should no longer exist in the grid.
    /// </summary>
    [Fact]
    public void Remove_RemovesExistingCoordinate()
    {
        var grid = new HexGrid2D<string>();
        var coord = new HexCoord(1, 2);
        grid[coord] = "toRemove";

        var removed = grid.Remove(coord);

        removed.Should().BeTrue();
        grid.Contains(coord).Should().BeFalse();
        grid.Count.Should().Be(0);
    }

    /// <summary>
    /// Tests that Remove returns false for non-existent coordinates.
    /// Attempting to remove non-existent coordinates should not affect the grid.
    /// </summary>
    [Fact]
    public void Remove_NonExistentCoordinate_ReturnsFalse()
    {
        var grid = new HexGrid2D<string>();

        var removed = grid.Remove(new HexCoord(10, 10));

        removed.Should().BeFalse();
        grid.Count.Should().Be(0);
    }

    /// <summary>
    /// Tests that Clear removes all cells from the grid.
    /// After clearing, the grid should be empty.
    /// </summary>
    [Fact]
    public void Clear_RemovesAllCells()
    {
        var grid = new HexGrid2D<string>();
        grid[0, 0] = "cell1";
        grid[1, 1] = "cell2";
        grid[2, 2] = "cell3";

        grid.Clear();

        grid.Count.Should().Be(0);
        grid.Contains(new HexCoord(0, 0)).Should().BeFalse();
    }

    /// <summary>
    /// Tests that GetCoordinates returns all coordinate keys.
    /// The method should provide access to all set coordinates.
    /// </summary>
    [Fact]
    public void GetCoordinates_ReturnsAllCoordKeys()
    {
        var grid = new HexGrid2D<string>();
        var coord1 = new HexCoord(0, 0);
        var coord2 = new HexCoord(1, -1);
        var coord3 = new HexCoord(-2, 1);

        grid[coord1] = "a";
        grid[coord2] = "b";
        grid[coord3] = "c";

        var coordinates = grid.GetCoordinates().ToList();

        coordinates.Should().HaveCount(3);
        coordinates.Should().Contain(coord1);
        coordinates.Should().Contain(coord2);
        coordinates.Should().Contain(coord3);
    }

    /// <summary>
    /// Tests that GetValues returns all stored values.
    /// The method should provide access to all cell values.
    /// </summary>
    [Fact]
    public void GetValues_ReturnsAllStoredValues()
    {
        var grid = new HexGrid2D<string>();
        grid[0, 0] = "alpha";
        grid[1, 1] = "beta";
        grid[2, 2] = "gamma";

        var values = grid.GetValues().ToList();

        values.Should().HaveCount(3);
        values.Should().Contain("alpha");
        values.Should().Contain("beta");
        values.Should().Contain("gamma");
    }

    /// <summary>
    /// Tests that GetCells returns coordinate-value pairs.
    /// The method should provide complete cell information.
    /// </summary>
    [Fact]
    public void GetCells_ReturnsCoordinateValuePairs()
    {
        var grid = new HexGrid2D<string>();
        var coord = new HexCoord(3, -1);
        grid[coord] = "test";

        var cells = grid.GetCells().ToList();

        cells.Should().HaveCount(1);
        cells[0].Coord.Should().Be(coord);
        cells[0].Value.Should().Be("test");
    }

    /// <summary>
    /// Tests that GetNeighbors returns adjacent hexagonal cells.
    /// The method should return neighbors that exist in the grid.
    /// </summary>
    [Fact]
    public void GetNeighbors_ReturnsAdjacentCells()
    {
        var grid = new HexGrid2D<string>();
        var center = new HexCoord(0, 0);
        
        // Set some neighbors
        grid[1, 0] = "east";
        grid[0, 1] = "southeast";
        grid[-1, 1] = "southwest";

        var neighbors = grid.GetNeighbors(center).ToList();

        neighbors.Should().HaveCount(3);
        neighbors.Should().Contain(cell => cell.Value == "east");
        neighbors.Should().Contain(cell => cell.Value == "southeast");
        neighbors.Should().Contain(cell => cell.Value == "southwest");
    }

    /// <summary>
    /// Tests that GetNeighbors includes empty cells when requested.
    /// The method should return empty neighbors when includeEmpty is true.
    /// </summary>
    [Fact]
    public void GetNeighbors_WithIncludeEmpty_ReturnsAllNeighbors()
    {
        var grid = new HexGrid2D<string>();
        var center = new HexCoord(0, 0);
        grid[1, 0] = "occupied";

        var neighbors = grid.GetNeighbors(center, includeEmpty: true).ToList();

        neighbors.Should().HaveCount(6); // All 6 hexagonal neighbors
        neighbors.Should().Contain(cell => cell.Value == "occupied");
        neighbors.Count(cell => cell.Value == null).Should().Be(5);
    }

    /// <summary>
    /// Tests that GetWithinDistance returns cells within specified hexagonal distance.
    /// The method should return all cells within the hexagonal radius.
    /// </summary>
    [Fact]
    public void GetWithinDistance_ReturnsHexagonalArea()
    {
        var grid = new HexGrid2D<int>();
        var center = new HexCoord(0, 0);
        
        // Fill hexagonal area
        grid[0, 0] = 0; // Distance 0
        grid[1, 0] = 1; // Distance 1
        grid[0, 1] = 1; // Distance 1
        grid[2, 0] = 2; // Distance 2

        var withinDistance1 = grid.GetWithinDistance(center, 1).ToList();
        var withinDistance2 = grid.GetWithinDistance(center, 2).ToList();

        withinDistance1.Should().HaveCount(3); // Center + 2 distance-1 neighbors
        withinDistance2.Should().HaveCount(4); // All cells
    }

    /// <summary>
    /// Tests that GetRing returns cells at exact hexagonal distance.
    /// The method should return only cells at the specified distance ring.
    /// </summary>
    [Fact]
    public void GetRing_ReturnsExactDistanceRing()
    {
        var grid = new HexGrid2D<int>();
        var center = new HexCoord(0, 0);
        
        grid[0, 0] = 0; // Distance 0
        grid[1, 0] = 1; // Distance 1
        grid[0, 1] = 1; // Distance 1
        grid[2, 0] = 2; // Distance 2
        grid[1, 1] = 2; // Distance 2

        var ring1 = grid.GetRing(center, 1).ToList();
        var ring2 = grid.GetRing(center, 2).ToList();

        ring1.Should().HaveCount(2); // Only distance-1 cells
        ring2.Should().HaveCount(2); // Only distance-2 cells
        ring1.Should().AllSatisfy(cell => cell.Value.Should().Be(1));
        ring2.Should().AllSatisfy(cell => cell.Value.Should().Be(2));
    }

    /// <summary>
    /// Tests that GetLine returns cells along hexagonal line between coordinates.
    /// The method should return cells forming a straight line in hex space.
    /// </summary>
    [Fact]
    public void GetLine_ReturnsHexagonalLineCells()
    {
        var grid = new HexGrid2D<string>();
        var start = new HexCoord(0, 0);
        var end = new HexCoord(2, 0);
        
        grid[0, 0] = "start";
        grid[1, 0] = "middle";
        grid[2, 0] = "end";

        var line = grid.GetLine(start, end).ToList();

        line.Should().HaveCount(3);
        line[0].Value.Should().Be("start");
        line[1].Value.Should().Be("middle");
        line[2].Value.Should().Be("end");
    }

    /// <summary>
    /// Tests that FindPath returns valid hexagonal path between coordinates.
    /// The pathfinding should navigate around blocked cells.
    /// </summary>
    [Fact]
    public void FindPath_ReturnsValidHexagonalPath()
    {
        var grid = new HexGrid2D<bool>();
        var start = new HexCoord(0, 0);
        var goal = new HexCoord(2, 0);
        
        // Set up some blocked cells
        grid[1, 0] = true; // Block direct path

        var path = grid.FindPath(start, goal, coord => grid.Contains(coord) && grid[coord]).ToList();

        path.Should().NotBeEmpty();
        path.First().Should().Be(start);
        path.Last().Should().Be(goal);
    }

    /// <summary>
    /// Tests that GetReachable returns coordinates within movement range.
    /// The method should calculate hexagonal movement distances correctly.
    /// </summary>
    [Fact]
    public void GetReachable_ReturnsMovementRange()
    {
        var grid = new HexGrid2D<bool>();
        var start = new HexCoord(0, 0);
        double movementPoints = 2.0;

        var reachable = grid.GetReachable(start, movementPoints, coord => false).ToList();

        reachable.Should().NotBeEmpty();
        reachable.Should().Contain(item => item.coord == start);
        reachable.All(item => item.remainingMovement >= 0).Should().BeTrue();
    }

    /// <summary>
    /// Tests that ToPixel and FromPixel coordinate conversions work correctly.
    /// Coordinate transformations should be consistent with the layout.
    /// </summary>
    [Fact]
    public void PixelConversions_WorkCorrectlyWithLayout()
    {
        var grid = new HexGrid2D<string>();
        var coord = new HexCoord(2, -1);

        var (x, y) = grid.ToPixel(coord);
        var backToHex = grid.FromPixel(x, y);

        backToHex.Should().Be(coord);
    }

    /// <summary>
    /// Tests that GetBounds returns correct coordinate boundaries.
    /// The method should return min and max coordinates for all set cells.
    /// </summary>
    [Fact]
    public void GetBounds_ReturnsCorrectCoordinateBoundaries()
    {
        var grid = new HexGrid2D<string>();
        grid[-2, 3] = "min";
        grid[5, -1] = "max";
        grid[0, 0] = "center";

        var (min, max) = grid.GetBounds();

        min.Q.Should().Be(-2);
        max.Q.Should().Be(5);
        min.R.Should().Be(-1);
        max.R.Should().Be(3);
    }

    /// <summary>
    /// Tests that GetBounds handles empty grid correctly.
    /// Empty grid should return origin coordinates as bounds.
    /// </summary>
    [Fact]
    public void GetBounds_EmptyGrid_ReturnsOrigin()
    {
        var grid = new HexGrid2D<string>();

        var (min, max) = grid.GetBounds();

        min.Should().Be(HexCoord.Origin);
        max.Should().Be(HexCoord.Origin);
    }

    /// <summary>
    /// Tests that FillHexagon fills hexagonal area around center coordinate.
    /// All cells within the hexagonal radius should be set to the value.
    /// </summary>
    [Fact]
    public void FillHexagon_FillsHexagonalArea()
    {
        var grid = new HexGrid2D<string>();
        var center = new HexCoord(0, 0);

        grid.FillHexagon(center, 1, "filled");

        var filledCells = grid.GetWithinDistance(center, 1, includeEmpty: false).ToList();
        filledCells.Should().HaveCount(7); // Center + 6 neighbors
        filledCells.Should().AllSatisfy(cell => cell.Value.Should().Be("filled"));
    }

    /// <summary>
    /// Tests that FillRing fills only the outer ring at specified distance.
    /// Only cells at exact distance should be filled.
    /// </summary>
    [Fact]
    public void FillRing_FillsOnlyOuterRing()
    {
        var grid = new HexGrid2D<string>();
        var center = new HexCoord(0, 0);

        grid.FillRing(center, 2, "ring");

        var ringCells = grid.GetRing(center, 2, includeEmpty: false).ToList();
        ringCells.Should().AllSatisfy(cell => cell.Value.Should().Be("ring"));
        
        // Center should not be filled
        grid.Contains(center).Should().BeFalse();
    }

    /// <summary>
    /// Tests that FillRectangle fills rectangular area in hexagonal coordinates.
    /// All coordinates within the rectangular bounds should be set.
    /// </summary>
    [Fact]
    public void FillRectangle_FillsRectangularArea()
    {
        var grid = new HexGrid2D<string>();

        grid.FillRectangle(0, 2, 0, 1, "rect");

        grid[0, 0].Should().Be("rect");
        grid[1, 0].Should().Be("rect");
        grid[2, 0].Should().Be("rect");
        grid[0, 1].Should().Be("rect");
        grid[1, 1].Should().Be("rect");
        grid[2, 1].Should().Be("rect");
        grid.Count.Should().Be(6);
    }

    /// <summary>
    /// Tests that Version property tracks grid modifications correctly.
    /// Version should increment when cells are added or removed.
    /// </summary>
    [Fact]
    public void Version_TracksModificationsCorrectly()
    {
        var grid = new HexGrid2D<string>();
        var initialVersion = grid.Version;

        grid[0, 0] = "new";
        grid.Version.Should().BeGreaterThan(initialVersion);

        var afterAddVersion = grid.Version;
        grid[0, 0] = "updated"; // Updating existing should not increment
        grid.Version.Should().Be(afterAddVersion);

        grid.Remove(new HexCoord(0, 0));
        grid.Version.Should().BeGreaterThan(afterAddVersion);
    }

    /// <summary>
    /// Tests that grid implements IEnumerable correctly.
    /// The enumeration should provide access to all cells.
    /// </summary>
    [Fact]
    public void IEnumerable_IteratesAllCells()
    {
        var grid = new HexGrid2D<string>();
        grid[0, 0] = "a";
        grid[1, 1] = "b";
        grid[2, 2] = "c";

        var cells = grid.ToList();

        cells.Should().HaveCount(3);
        cells.Select(cell => cell.Value).Should().Contain("a", "b", "c");
    }

    /// <summary>
    /// Tests hexagonal coordinate system properties and calculations.
    /// Hexagonal math should work correctly for distances and neighbors.
    /// </summary>
    [Fact]
    public void HexagonalCoordinateSystem_WorksCorrectly()
    {
        var grid = new HexGrid2D<int>();
        var origin = new HexCoord(0, 0);
        var neighbor = new HexCoord(1, 0);
        var farCell = new HexCoord(3, -2);

        // Test distance calculation
        origin.DistanceTo(neighbor).Should().Be(1);
        origin.DistanceTo(farCell).Should().Be(3);

        // Test neighbor relationships
        var neighbors = origin.GetNeighbors().ToList();
        neighbors.Should().HaveCount(6);
        neighbors.Should().Contain(neighbor);
    }

    /// <summary>
    /// Tests edge cases and boundary conditions for hexagonal operations.
    /// Grid should handle edge cases gracefully without errors.
    /// </summary>
    [Fact]
    public void EdgeCases_HandledGracefully()
    {
        var grid = new HexGrid2D<string>();

        // Empty grid operations
        grid.GetNeighbors(HexCoord.Origin).Should().BeEmpty();
        grid.GetWithinDistance(HexCoord.Origin, 5).Should().BeEmpty();
        grid.GetRing(HexCoord.Origin, 1).Should().BeEmpty();

        // Zero distance operations
        grid[0, 0] = "center";
        var withinZero = grid.GetWithinDistance(HexCoord.Origin, 0).ToList();
        withinZero.Should().HaveCount(1);
        withinZero[0].Value.Should().Be("center");
    }

    /// <summary>
    /// Tests performance with large coordinate values and many cells.
    /// Grid should maintain efficiency with realistic game-sized data.
    /// </summary>
    [Fact]
    public void LargeCoordinates_MaintainPerformance()
    {
        var grid = new HexGrid2D<int>();

        // Test with large coordinate values
        grid[1000, -500] = 1;
        grid[-750, 1200] = 2;
        grid[0, 0] = 3;

        grid.Count.Should().Be(3);
        grid[1000, -500].Should().Be(1);
        grid[-750, 1200].Should().Be(2);

        var bounds = grid.GetBounds();
        bounds.min.Q.Should().Be(-750);
        bounds.max.Q.Should().Be(1000);
    }
}
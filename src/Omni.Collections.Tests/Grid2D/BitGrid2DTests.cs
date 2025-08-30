using System;
using System.Linq;
using FluentAssertions;
using Omni.Collections.Grid2D;
using Xunit;

namespace Omni.Collections.Tests.Grid2D;

public class BitGrid2DTests
{
    /// <summary>
    /// Tests that a BitGrid2D can be constructed with valid dimensions.
    /// The grid should initialize with the specified width and height.
    /// </summary>
    [Theory]
    [InlineData(1, 1)]
    [InlineData(10, 10)]
    [InlineData(100, 50)]
    [InlineData(64, 64)]
    [InlineData(127, 63)]
    public void Constructor_WithValidDimensions_InitializesCorrectly(int width, int height)
    {
        var grid = new BitGrid2D(width, height);

        grid.Width.Should().Be(width);
        grid.Height.Should().Be(height);
        grid.Count.Should().Be(width * height);
    }

    /// <summary>
    /// Tests that constructor throws exception for invalid dimensions.
    /// The constructor should validate width and height parameters.
    /// </summary>
    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(10, 0)]
    [InlineData(10, -1)]
    public void Constructor_WithInvalidDimensions_ThrowsArgumentOutOfRangeException(int width, int height)
    {
        var act = () => new BitGrid2D(width, height);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Tests that CreateWithArrayPool creates grid with memory pooling enabled.
    /// The grid should use pooled arrays to reduce allocation pressure.
    /// </summary>
    [Fact]
    public void CreateWithArrayPool_CreatesGridWithPooling()
    {
        using var grid = BitGrid2D.CreateWithArrayPool(64, 64);

        grid.Width.Should().Be(64);
        grid.Height.Should().Be(64);
    }

    /// <summary>
    /// Tests that indexer get and set operations work correctly.
    /// Values should be stored and retrieved accurately at specific coordinates.
    /// </summary>
    [Fact]
    public void Indexer_GetSet_WorksCorrectly()
    {
        var grid = new BitGrid2D(10, 10);

        grid[5, 5] = true;
        grid[0, 0] = true;
        grid[9, 9] = true;

        grid[5, 5].Should().BeTrue();
        grid[0, 0].Should().BeTrue();
        grid[9, 9].Should().BeTrue();
        grid[1, 1].Should().BeFalse(); // Default value
    }

    /// <summary>
    /// Tests that indexer throws exception for out-of-bounds access.
    /// Access outside grid boundaries should throw IndexOutOfRangeException.
    /// </summary>
    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(10, 5)]
    [InlineData(5, 10)]
    public void Indexer_OutOfBounds_ThrowsException(int x, int y)
    {
        var grid = new BitGrid2D(10, 10);

        var getAct = () => _ = grid[x, y];
        var setAct = () => grid[x, y] = true;

        getAct.Should().Throw<IndexOutOfRangeException>();
        setAct.Should().Throw<IndexOutOfRangeException>();
    }

    /// <summary>
    /// Tests that IsInBounds correctly identifies valid coordinates.
    /// The method should return true for coordinates within grid boundaries.
    /// </summary>
    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(9, 9, true)]
    [InlineData(5, 5, true)]
    [InlineData(-1, 0, false)]
    [InlineData(0, -1, false)]
    [InlineData(10, 5, false)]
    [InlineData(5, 10, false)]
    public void IsInBounds_IdentifiesValidCoordinates(int x, int y, bool expected)
    {
        var grid = new BitGrid2D(10, 10);

        grid.IsInBounds(x, y).Should().Be(expected);
    }

    /// <summary>
    /// Tests that SwapElements exchanges values between two grid positions.
    /// Values at the specified coordinates should be swapped correctly.
    /// </summary>
    [Fact]
    public void SwapElements_ExchangesValues()
    {
        var grid = new BitGrid2D(10, 10);
        grid[2, 3] = true;
        grid[7, 8] = false;

        grid.SwapElements(2, 3, 7, 8);

        grid[2, 3].Should().BeFalse();
        grid[7, 8].Should().BeTrue();
    }

    /// <summary>
    /// Tests that GetNeighbors returns adjacent cell values correctly.
    /// The method should return neighbor values without including diagonals by default.
    /// </summary>
    [Fact]
    public void GetNeighbors_ReturnsAdjacentValues()
    {
        var grid = new BitGrid2D(5, 5);
        grid[1, 2] = true; // Left
        grid[3, 2] = true; // Right
        grid[2, 1] = true; // Up
        grid[2, 3] = true; // Down

        var neighbors = grid.GetNeighbors(2, 2, includeDiagonals: false).ToList();

        neighbors.Should().HaveCount(4);
        neighbors.Should().AllSatisfy(n => n.Should().BeTrue());
    }

    /// <summary>
    /// Tests that GetNeighbors includes diagonal neighbors when requested.
    /// The method should return all 8 surrounding neighbors when includeDiagonals is true.
    /// </summary>
    [Fact]
    public void GetNeighbors_WithDiagonals_ReturnsAllSurrounding()
    {
        var grid = new BitGrid2D(5, 5);
        // Set all surrounding cells to true
        for (int x = 1; x <= 3; x++)
        {
            for (int y = 1; y <= 3; y++)
            {
                if (x != 2 || y != 2) // Skip center
                    grid[x, y] = true;
            }
        }

        var neighbors = grid.GetNeighbors(2, 2, includeDiagonals: true).ToList();

        neighbors.Should().HaveCount(8);
        neighbors.Should().AllSatisfy(n => n.Should().BeTrue());
    }

    /// <summary>
    /// Tests that GetRow returns all values in a specified row.
    /// The method should return values from left to right for the given row.
    /// </summary>
    [Fact]
    public void GetRow_ReturnsRowValues()
    {
        var grid = new BitGrid2D(5, 3);
        grid[0, 1] = true;
        grid[2, 1] = true;
        grid[4, 1] = true;

        var row = grid.GetRow(1).ToList();

        row.Should().HaveCount(5);
        row[0].Should().BeTrue();
        row[1].Should().BeFalse();
        row[2].Should().BeTrue();
        row[3].Should().BeFalse();
        row[4].Should().BeTrue();
    }

    /// <summary>
    /// Tests that GetRow throws exception for invalid row index.
    /// Access to non-existent rows should throw ArgumentOutOfRangeException.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    public void GetRow_InvalidIndex_ThrowsException(int rowIndex)
    {
        var grid = new BitGrid2D(5, 5);

        var act = () => grid.GetRow(rowIndex).ToList();

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Tests that GetColumn returns all values in a specified column.
    /// The method should return values from top to bottom for the given column.
    /// </summary>
    [Fact]
    public void GetColumn_ReturnsColumnValues()
    {
        var grid = new BitGrid2D(3, 5);
        grid[1, 0] = true;
        grid[1, 2] = true;
        grid[1, 4] = true;

        var column = grid.GetColumn(1).ToList();

        column.Should().HaveCount(5);
        column[0].Should().BeTrue();
        column[1].Should().BeFalse();
        column[2].Should().BeTrue();
        column[3].Should().BeFalse();
        column[4].Should().BeTrue();
    }

    /// <summary>
    /// Tests that GetColumn throws exception for invalid column index.
    /// Access to non-existent columns should throw ArgumentOutOfRangeException.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    public void GetColumn_InvalidIndex_ThrowsException(int columnIndex)
    {
        var grid = new BitGrid2D(5, 5);

        var act = () => grid.GetColumn(columnIndex).ToList();

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Tests that FillArea sets all bits in specified rectangular region.
    /// All cells within the area should be set to the specified value.
    /// </summary>
    [Fact]
    public void FillArea_SetsRectangularRegion()
    {
        var grid = new BitGrid2D(10, 10);

        grid.FillArea(2, 3, 4, 3, true);

        // Check filled area
        for (int x = 2; x < 6; x++)
        {
            for (int y = 3; y < 6; y++)
            {
                grid[x, y].Should().BeTrue();
            }
        }

        // Check outside area remains false
        grid[1, 3].Should().BeFalse();
        grid[6, 3].Should().BeFalse();
        grid[2, 2].Should().BeFalse();
        grid[2, 6].Should().BeFalse();
    }

    /// <summary>
    /// Tests that FillArea handles out-of-bounds regions gracefully.
    /// Partial regions should be filled correctly without throwing exceptions.
    /// </summary>
    [Fact]
    public void FillArea_OutOfBounds_HandlesGracefully()
    {
        var grid = new BitGrid2D(5, 5);

        grid.FillArea(3, 3, 5, 5, true); // Extends beyond grid

        // Check that valid portion is filled
        grid[3, 3].Should().BeTrue();
        grid[4, 4].Should().BeTrue();
        
        // Grid should not throw or crash
        grid.Width.Should().Be(5);
        grid.Height.Should().Be(5);
    }

    /// <summary>
    /// Tests that ClearArea sets all bits in specified region to false.
    /// All cells within the area should be cleared.
    /// </summary>
    [Fact]
    public void ClearArea_ClearsRectangularRegion()
    {
        var grid = new BitGrid2D(10, 10);
        grid.SetAll(true); // Fill entire grid

        grid.ClearArea(2, 3, 4, 3);

        // Check cleared area
        for (int x = 2; x < 6; x++)
        {
            for (int y = 3; y < 6; y++)
            {
                grid[x, y].Should().BeFalse();
            }
        }

        // Check outside area remains true
        grid[1, 3].Should().BeTrue();
        grid[6, 3].Should().BeTrue();
    }

    /// <summary>
    /// Tests that SetAll fills entire grid with specified value.
    /// All cells should be set to the given value regardless of grid size.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SetAll_FillsEntireGrid(bool value)
    {
        var grid = new BitGrid2D(7, 11); // Non-power-of-2 dimensions

        grid.SetAll(value);

        var allValues = grid.EnumerateAll().Select(cell => cell.value).ToList();
        allValues.Should().AllSatisfy(v => v.Should().Be(value));
    }

    /// <summary>
    /// Tests that CountSetBits returns accurate count of true bits.
    /// The count should match the number of cells set to true.
    /// </summary>
    [Fact]
    public void CountSetBits_ReturnsAccurateCount()
    {
        var grid = new BitGrid2D(10, 10);
        
        // Set specific pattern
        grid[0, 0] = true;
        grid[5, 5] = true;
        grid[9, 9] = true;
        grid[2, 7] = true;

        grid.CountSetBits().Should().Be(4);
    }

    /// <summary>
    /// Tests that CountSetBits works correctly with edge cases.
    /// The method should handle empty grids and full grids correctly.
    /// </summary>
    [Fact]
    public void CountSetBits_HandlesEdgeCases()
    {
        var grid = new BitGrid2D(63, 63); // Test non-64-bit-aligned size

        grid.CountSetBits().Should().Be(0); // Empty grid

        grid.SetAll(true);
        grid.CountSetBits().Should().Be(63 * 63); // Full grid
    }

    /// <summary>
    /// Tests that Toggle flips bit values correctly.
    /// The method should change true to false and false to true.
    /// </summary>
    [Fact]
    public void Toggle_FlipsBitValues()
    {
        var grid = new BitGrid2D(5, 5);
        
        grid[2, 2] = true;
        grid.Toggle(2, 2);
        grid[2, 2].Should().BeFalse();

        grid.Toggle(2, 2);
        grid[2, 2].Should().BeTrue();
    }

    /// <summary>
    /// Tests that And operation performs bitwise AND with another grid.
    /// Result should contain true only where both grids have true.
    /// </summary>
    [Fact]
    public void And_PerformsBitwiseAnd()
    {
        var grid1 = new BitGrid2D(3, 3);
        var grid2 = new BitGrid2D(3, 3);

        grid1[0, 0] = true;
        grid1[1, 1] = true;
        grid1[2, 2] = true;

        grid2[0, 0] = true;
        grid2[1, 1] = false;
        grid2[2, 0] = true;

        grid1.And(grid2);

        grid1[0, 0].Should().BeTrue();  // true & true = true
        grid1[1, 1].Should().BeFalse(); // true & false = false
        grid1[2, 2].Should().BeFalse(); // true & false = false
        grid1[2, 0].Should().BeFalse(); // false & true = false
    }

    /// <summary>
    /// Tests that Or operation performs bitwise OR with another grid.
    /// Result should contain true where either grid has true.
    /// </summary>
    [Fact]
    public void Or_PerformsBitwiseOr()
    {
        var grid1 = new BitGrid2D(3, 3);
        var grid2 = new BitGrid2D(3, 3);

        grid1[0, 0] = true;
        grid1[1, 1] = false;

        grid2[0, 0] = false;
        grid2[1, 1] = true;
        grid2[2, 2] = true;

        grid1.Or(grid2);

        grid1[0, 0].Should().BeTrue();  // true | false = true
        grid1[1, 1].Should().BeTrue();  // false | true = true
        grid1[2, 2].Should().BeTrue();  // false | true = true
    }

    /// <summary>
    /// Tests that Xor operation performs bitwise XOR with another grid.
    /// Result should contain true only where grids differ.
    /// </summary>
    [Fact]
    public void Xor_PerformsBitwiseXor()
    {
        var grid1 = new BitGrid2D(3, 3);
        var grid2 = new BitGrid2D(3, 3);

        grid1[0, 0] = true;
        grid1[1, 1] = true;

        grid2[0, 0] = true;
        grid2[1, 1] = false;
        grid2[2, 2] = true;

        grid1.Xor(grid2);

        grid1[0, 0].Should().BeFalse(); // true ^ true = false
        grid1[1, 1].Should().BeTrue();  // true ^ false = true
        grid1[2, 2].Should().BeTrue();  // false ^ true = true
    }

    /// <summary>
    /// Tests that bitwise operations throw exception for mismatched dimensions.
    /// Grids with different sizes should not be compatible for bitwise operations.
    /// </summary>
    [Fact]
    public void BitwiseOperations_MismatchedDimensions_ThrowException()
    {
        var grid1 = new BitGrid2D(5, 5);
        var grid2 = new BitGrid2D(3, 3);

        var andAct = () => grid1.And(grid2);
        var orAct = () => grid1.Or(grid2);
        var xorAct = () => grid1.Xor(grid2);

        andAct.Should().Throw<ArgumentException>();
        orAct.Should().Throw<ArgumentException>();
        xorAct.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Tests that GetRowSpan returns boolean data for a row.
    /// The span should contain accurate row data without side effects.
    /// </summary>
    [Fact]
    public void GetRowSpan_ReturnsRowData()
    {
        var grid = new BitGrid2D(4, 3);
        grid[0, 1] = true;
        grid[2, 1] = true;

        var span = grid.GetRowSpan(1);

        span.Length.Should().Be(4);
        span[0].Should().BeTrue();
        span[1].Should().BeFalse();
        span[2].Should().BeTrue();
        span[3].Should().BeFalse();
    }

    /// <summary>
    /// Tests that EnumerateAll returns all grid positions and values.
    /// The enumeration should cover every cell in the grid exactly once.
    /// </summary>
    [Fact]
    public void EnumerateAll_ReturnsAllPositionsAndValues()
    {
        var grid = new BitGrid2D(3, 2);
        grid[1, 0] = true;
        grid[2, 1] = true;

        var allCells = grid.EnumerateAll().ToList();

        allCells.Should().HaveCount(6); // 3 * 2
        allCells.Should().Contain((1, 0, true));
        allCells.Should().Contain((2, 1, true));
        allCells.Where(c => c.value).Should().HaveCount(2);
    }

    /// <summary>
    /// Tests that EnumerateArea returns cells within specified rectangular bounds.
    /// The enumeration should only include cells within the area boundaries.
    /// </summary>
    [Fact]
    public void EnumerateArea_ReturnsAreaCells()
    {
        var grid = new BitGrid2D(10, 10);
        grid[2, 2] = true;
        grid[3, 3] = true;
        grid[5, 5] = true; // Outside area

        var areaCells = grid.EnumerateArea(2, 2, 2, 2).ToList();

        areaCells.Should().HaveCount(4); // 2x2 area
        areaCells.Should().Contain((2, 2, true));
        areaCells.Should().Contain((3, 3, true));
        areaCells.Should().NotContain(cell => cell.x == 5 && cell.y == 5);
    }

    /// <summary>
    /// Tests that EnumerateSetBits returns only positions with true values.
    /// The enumeration should skip all false bits and return only set positions.
    /// </summary>
    [Fact]
    public void EnumerateSetBits_ReturnsOnlyTruePositions()
    {
        var grid = new BitGrid2D(5, 5);
        grid[1, 1] = true;
        grid[3, 2] = true;
        grid[4, 4] = true;

        var setBits = grid.EnumerateSetBits().ToList();

        setBits.Should().HaveCount(3);
        setBits.Should().Contain((1, 1));
        setBits.Should().Contain((3, 2));
        setBits.Should().Contain((4, 4));
    }

    /// <summary>
    /// Tests that GetNeighborsNonAlloc fills buffer with neighbor values efficiently.
    /// The method should populate the provided buffer without additional allocations.
    /// </summary>
    [Fact]
    public void GetNeighborsNonAlloc_FillsBufferEfficiently()
    {
        var grid = new BitGrid2D(5, 5);
        grid[1, 2] = true; // Left
        grid[3, 2] = true; // Right

        Span<bool> buffer = stackalloc bool[8];
        var count = grid.GetNeighborsNonAlloc(2, 2, buffer, includeDiagonals: false);

        count.Should().Be(4);
        buffer[0].Should().BeTrue(); // Left neighbor
        buffer[1].Should().BeTrue(); // Right neighbor
    }

    /// <summary>
    /// Tests that ProcessNeighbors executes action on all neighbor values.
    /// The processor should be called for each valid neighbor cell.
    /// </summary>
    [Fact]
    public void ProcessNeighbors_ExecutesActionOnNeighbors()
    {
        var grid = new BitGrid2D(5, 5);
        grid[1, 2] = true;
        grid[3, 2] = true;

        int trueCount = 0;
        grid.ProcessNeighbors(2, 2, value => { if (value) trueCount++; }, includeDiagonals: false);

        trueCount.Should().Be(2);
    }

    /// <summary>
    /// Tests that Dispose cleans up resources properly including pooled memory.
    /// The grid should release allocated resources when disposed.
    /// </summary>
    [Fact]
    public void Dispose_CleansUpResources()
    {
        var grid = BitGrid2D.CreateWithArrayPool(64, 64);
        grid[10, 10] = true;

        grid.Dispose();

        // Should not throw after disposal
        grid.Dispose(); // Multiple dispose calls should be safe
    }

    /// <summary>
    /// Tests bit packing efficiency across ulong boundaries.
    /// Grid operations should work correctly when data spans multiple ulong values.
    /// </summary>
    [Fact]
    public void BitPacking_WorksAcrossUlongBoundaries()
    {
        var grid = new BitGrid2D(65, 65); // Forces multiple ulongs

        // Set bits across boundaries
        for (int i = 60; i < 70; i++)
        {
            if (i < 65)
                grid[i, 0] = true;
        }

        grid.CountSetBits().Should().Be(5); // Bits 60, 61, 62, 63, 64
        
        // Verify specific bits
        grid[60, 0].Should().BeTrue();
        grid[64, 0].Should().BeTrue();
    }

    /// <summary>
    /// Tests grid operations with large dimensions for performance and correctness.
    /// Large grids should maintain functionality without performance degradation.
    /// </summary>
    [Fact]
    public void LargeGrid_MaintainsFunctionality()
    {
        var grid = new BitGrid2D(1000, 500);

        // Test corners and center
        grid[0, 0] = true;
        grid[999, 499] = true;
        grid[500, 250] = true;

        grid[0, 0].Should().BeTrue();
        grid[999, 499].Should().BeTrue();
        grid[500, 250].Should().BeTrue();
        grid.CountSetBits().Should().Be(3);
    }

    /// <summary>
    /// Tests edge cases with minimal grid dimensions.
    /// Single-cell grids should work correctly for all operations.
    /// </summary>
    [Fact]
    public void SingleCellGrid_HandlesAllOperations()
    {
        var grid = new BitGrid2D(1, 1);

        grid[0, 0] = true;
        grid[0, 0].Should().BeTrue();
        grid.CountSetBits().Should().Be(1);

        grid.Toggle(0, 0);
        grid[0, 0].Should().BeFalse();
        grid.CountSetBits().Should().Be(0);

        var neighbors = grid.GetNeighbors(0, 0).ToList();
        neighbors.Should().BeEmpty(); // No neighbors for single cell
    }

    /// <summary>
    /// Tests coordinate boundary validation across all public methods.
    /// Methods should consistently validate coordinates and handle boundaries.
    /// </summary>
    [Theory]
    [InlineData(5, 5)]
    [InlineData(64, 32)]
    [InlineData(100, 100)]
    public void CoordinateValidation_ConsistentAcrossMethods(int width, int height)
    {
        var grid = new BitGrid2D(width, height);

        // Valid coordinates
        grid.IsInBounds(0, 0).Should().BeTrue();
        grid.IsInBounds(width - 1, height - 1).Should().BeTrue();

        // Invalid coordinates
        grid.IsInBounds(-1, 0).Should().BeFalse();
        grid.IsInBounds(0, -1).Should().BeFalse();
        grid.IsInBounds(width, 0).Should().BeFalse();
        grid.IsInBounds(0, height).Should().BeFalse();
    }
}
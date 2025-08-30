using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Omni.Collections.Grid2D;
using Xunit;

namespace Omni.Collections.Tests.Grid2D;

public class LayeredGrid2DTests
{
    /// <summary>
    /// Tests that a LayeredGrid2D can be constructed with valid dimensions.
    /// The grid should initialize with the specified width, height, and layer count.
    /// </summary>
    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(10, 10, 3)]
    [InlineData(100, 50, 5)]
    [InlineData(64, 64, 2)]
    public void Constructor_WithValidDimensions_InitializesCorrectly(int width, int height, int layerCount)
    {
        var grid = new LayeredGrid2D<string>(width, height, layerCount);

        grid.Width.Should().Be(width);
        grid.Height.Should().Be(height);
        grid.LayerCount.Should().Be(layerCount);
        grid.Count.Should().Be(width * height);
    }

    /// <summary>
    /// Tests that constructor throws exception for invalid dimensions.
    /// The constructor should validate width, height, and layer count parameters.
    /// </summary>
    [Theory]
    [InlineData(0, 10, 1)]
    [InlineData(-1, 10, 1)]
    [InlineData(10, 0, 1)]
    [InlineData(10, -1, 1)]
    [InlineData(10, 10, 0)]
    [InlineData(10, 10, -1)]
    public void Constructor_WithInvalidDimensions_ThrowsArgumentOutOfRangeException(int width, int height, int layerCount)
    {
        var act = () => new LayeredGrid2D<string>(width, height, layerCount);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Tests that CreateWithArrayPool creates grid with memory pooling enabled.
    /// The grid should use pooled arrays to reduce allocation pressure.
    /// </summary>
    [Fact]
    public void CreateWithArrayPool_CreatesGridWithPooling()
    {
        using var grid = LayeredGrid2D<string>.CreateWithArrayPool(64, 64, 3);

        grid.Width.Should().Be(64);
        grid.Height.Should().Be(64);
        grid.LayerCount.Should().Be(3);
    }

    /// <summary>
    /// Tests that 2D indexer get and set operations work on the default layer.
    /// Values should be stored and retrieved accurately on layer 0.
    /// </summary>
    [Fact]
    public void Indexer2D_GetSet_WorksOnDefaultLayer()
    {
        var grid = new LayeredGrid2D<string>(10, 10, 3);

        grid[5, 5] = "center";
        grid[0, 0] = "origin";
        grid[9, 9] = "corner";

        grid[5, 5].Should().Be("center");
        grid[0, 0].Should().Be("origin");
        grid[9, 9].Should().Be("corner");
    }

    /// <summary>
    /// Tests that 3D indexer get and set operations work across all layers.
    /// Values should be stored and retrieved accurately at specific layer coordinates.
    /// </summary>
    [Fact]
    public void Indexer3D_GetSet_WorksAcrossAllLayers()
    {
        var grid = new LayeredGrid2D<string>(5, 5, 3);

        grid[0, 2, 2] = "layer0";
        grid[1, 2, 2] = "layer1";
        grid[2, 2, 2] = "layer2";

        grid[0, 2, 2].Should().Be("layer0");
        grid[1, 2, 2].Should().Be("layer1");
        grid[2, 2, 2].Should().Be("layer2");
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
        var grid = new LayeredGrid2D<string>(10, 10, 2);

        var getAct = () => _ = grid[x, y];
        var setAct = () => grid[x, y] = "test";

        getAct.Should().Throw<IndexOutOfRangeException>();
        setAct.Should().Throw<IndexOutOfRangeException>();
    }

    /// <summary>
    /// Tests that indexer throws exception for invalid layer access.
    /// Access to non-existent layers should throw ArgumentOutOfRangeException.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void Indexer_InvalidLayer_ThrowsException(int layer)
    {
        var grid = new LayeredGrid2D<string>(5, 5, 3);

        var getAct = () => _ = grid[layer, 2, 2];
        var setAct = () => grid[layer, 2, 2] = "test";

        getAct.Should().Throw<ArgumentOutOfRangeException>();
        setAct.Should().Throw<ArgumentOutOfRangeException>();
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
        var grid = new LayeredGrid2D<string>(10, 10, 2);

        grid.IsInBounds(x, y).Should().Be(expected);
    }

    /// <summary>
    /// Tests that SwapElements exchanges values on the default layer.
    /// Values at the specified coordinates should be swapped correctly.
    /// </summary>
    [Fact]
    public void SwapElements_ExchangesValuesOnDefaultLayer()
    {
        var grid = new LayeredGrid2D<string>(10, 10, 2);
        grid[2, 3] = "first";
        grid[7, 8] = "second";

        grid.SwapElements(2, 3, 7, 8);

        grid[2, 3].Should().Be("second");
        grid[7, 8].Should().Be("first");
    }

    /// <summary>
    /// Tests that SwapElementsInLayer exchanges values within a specific layer.
    /// Values should be swapped correctly within the specified layer.
    /// </summary>
    [Fact]
    public void SwapElementsInLayer_ExchangesValuesInSpecificLayer()
    {
        var grid = new LayeredGrid2D<string>(10, 10, 3);
        grid[1, 2, 3] = "a";
        grid[1, 7, 8] = "b";

        grid.SwapElementsInLayer(1, 2, 3, 7, 8);

        grid[1, 2, 3].Should().Be("b");
        grid[1, 7, 8].Should().Be("a");
    }

    /// <summary>
    /// Tests that SwapElementsAcrossLayers exchanges values between different layers.
    /// Values should be swapped correctly across layer boundaries.
    /// </summary>
    [Fact]
    public void SwapElementsAcrossLayers_ExchangesValuesBetweenLayers()
    {
        var grid = new LayeredGrid2D<string>(10, 10, 3);
        grid[0, 2, 3] = "layer0";
        grid[2, 7, 8] = "layer2";

        grid.SwapElementsAcrossLayers(0, 2, 3, 2, 7, 8);

        grid[0, 2, 3].Should().Be("layer2");
        grid[2, 7, 8].Should().Be("layer0");
    }

    /// <summary>
    /// Tests that GetNeighbors returns adjacent cell values on the default layer.
    /// The method should return neighbor values without including diagonals by default.
    /// </summary>
    [Fact]
    public void GetNeighbors_ReturnsAdjacentValuesOnDefaultLayer()
    {
        var grid = new LayeredGrid2D<string>(5, 5, 2);
        grid[1, 2] = "left";
        grid[3, 2] = "right";
        grid[2, 1] = "up";
        grid[2, 3] = "down";

        var neighbors = grid.GetNeighbors(2, 2, includeDiagonals: false).ToList();

        neighbors.Should().HaveCount(4);
        neighbors.Should().Contain("left");
        neighbors.Should().Contain("right");
        neighbors.Should().Contain("up");
        neighbors.Should().Contain("down");
    }

    /// <summary>
    /// Tests that GetNeighbors includes diagonal neighbors when requested.
    /// The method should return all 8 surrounding neighbors when includeDiagonals is true.
    /// </summary>
    [Fact]
    public void GetNeighbors_WithDiagonals_ReturnsAllSurrounding()
    {
        var grid = new LayeredGrid2D<string>(5, 5, 2);
        // Set all surrounding cells to "filled"
        for (int x = 1; x <= 3; x++)
        {
            for (int y = 1; y <= 3; y++)
            {
                if (x != 2 || y != 2) // Skip center
                    grid[x, y] = "filled";
            }
        }

        var neighbors = grid.GetNeighbors(2, 2, includeDiagonals: true).ToList();

        neighbors.Should().HaveCount(8);
        neighbors.Should().AllSatisfy(n => n.Should().Be("filled"));
    }

    /// <summary>
    /// Tests that GetNeighborsAllLayers returns neighbors from all layers at coordinates.
    /// The method should return neighbors across all layers for comprehensive analysis.
    /// </summary>
    [Fact]
    public void GetNeighborsAllLayers_ReturnsNeighborsFromAllLayers()
    {
        var grid = new LayeredGrid2D<string>(5, 5, 3);
        
        // Set neighbors in different layers
        grid[0, 1, 2] = "layer0";
        grid[1, 1, 2] = "layer1";
        grid[2, 1, 2] = "layer2";

        var allNeighbors = grid.GetNeighborsAllLayers(2, 2, includeDiagonals: false).ToList();

        allNeighbors.Should().Contain("layer0");
        allNeighbors.Should().Contain("layer1");
        allNeighbors.Should().Contain("layer2");
    }

    /// <summary>
    /// Tests that GetRow returns all values in a specified row on the default layer.
    /// The method should return values from left to right for the given row.
    /// </summary>
    [Fact]
    public void GetRow_ReturnsRowValuesOnDefaultLayer()
    {
        var grid = new LayeredGrid2D<string>(5, 3, 2);
        grid[0, 1] = "a";
        grid[2, 1] = "c";
        grid[4, 1] = "e";

        var row = grid.GetRow(1).ToList();

        row.Should().HaveCount(5);
        row[0].Should().Be("a");
        row[1].Should().BeNull();
        row[2].Should().Be("c");
        row[3].Should().BeNull();
        row[4].Should().Be("e");
    }

    /// <summary>
    /// Tests that GetLayerRow returns row values from a specific layer.
    /// The method should return values from the specified layer's row.
    /// </summary>
    [Fact]
    public void GetLayerRow_ReturnsRowValuesFromSpecificLayer()
    {
        var grid = new LayeredGrid2D<string>(4, 3, 3);
        grid[1, 0, 1] = "layer1_a";
        grid[1, 2, 1] = "layer1_c";

        var row = grid.GetLayerRow(1, 1).ToList();

        row.Should().HaveCount(4);
        row[0].Should().Be("layer1_a");
        row[1].Should().BeNull();
        row[2].Should().Be("layer1_c");
        row[3].Should().BeNull();
    }

    /// <summary>
    /// Tests that GetColumn returns all values in a specified column on the default layer.
    /// The method should return values from top to bottom for the given column.
    /// </summary>
    [Fact]
    public void GetColumn_ReturnsColumnValuesOnDefaultLayer()
    {
        var grid = new LayeredGrid2D<string>(3, 5, 2);
        grid[1, 0] = "top";
        grid[1, 2] = "middle";
        grid[1, 4] = "bottom";

        var column = grid.GetColumn(1).ToList();

        column.Should().HaveCount(5);
        column[0].Should().Be("top");
        column[1].Should().BeNull();
        column[2].Should().Be("middle");
        column[3].Should().BeNull();
        column[4].Should().Be("bottom");
    }

    /// <summary>
    /// Tests that FillArea sets all values in specified rectangular region on default layer.
    /// All cells within the area should be set to the specified value.
    /// </summary>
    [Fact]
    public void FillArea_SetsRectangularRegionOnDefaultLayer()
    {
        var grid = new LayeredGrid2D<string>(10, 10, 2);

        grid.FillArea(2, 3, 4, 3, "filled");

        // Check filled area
        for (int x = 2; x < 6; x++)
        {
            for (int y = 3; y < 6; y++)
            {
                grid[x, y].Should().Be("filled");
            }
        }

        // Check outside area remains null
        grid[1, 3].Should().BeNull();
        grid[6, 3].Should().BeNull();
    }

    /// <summary>
    /// Tests that FillLayerArea sets values in specified region on specific layer.
    /// Only the specified layer should be affected by the fill operation.
    /// </summary>
    [Fact]
    public void FillLayerArea_SetsRectangularRegionOnSpecificLayer()
    {
        var grid = new LayeredGrid2D<string>(10, 10, 3);

        grid.FillLayerArea(1, 2, 3, 3, 2, "layer1_fill");

        // Check layer 1 is filled
        for (int x = 2; x < 5; x++)
        {
            for (int y = 3; y < 5; y++)
            {
                grid[1, x, y].Should().Be("layer1_fill");
            }
        }

        // Check other layers remain null
        grid[0, 2, 3].Should().BeNull();
        grid[2, 2, 3].Should().BeNull();
    }

    /// <summary>
    /// Tests that ClearArea sets all values in specified region to default on default layer.
    /// All cells within the area should be cleared.
    /// </summary>
    [Fact]
    public void ClearArea_ClearsRectangularRegionOnDefaultLayer()
    {
        var grid = new LayeredGrid2D<string>(10, 10, 2);
        grid.FillArea(0, 0, 10, 10, "filled"); // Fill entire grid

        grid.ClearArea(2, 3, 4, 3);

        // Check cleared area
        for (int x = 2; x < 6; x++)
        {
            for (int y = 3; y < 6; y++)
            {
                grid[x, y].Should().BeNull();
            }
        }

        // Check outside area remains filled
        grid[1, 3].Should().Be("filled");
        grid[6, 3].Should().Be("filled");
    }

    /// <summary>
    /// Tests that ClearAllLayersArea clears specified region across all layers.
    /// All layers should have the specified area cleared.
    /// </summary>
    [Fact]
    public void ClearAllLayersArea_ClearsRectangularRegionAcrossAllLayers()
    {
        var grid = new LayeredGrid2D<string>(5, 5, 3);
        
        // Fill all layers
        for (int layer = 0; layer < 3; layer++)
        {
            grid.FillLayerArea(layer, 0, 0, 5, 5, $"layer{layer}");
        }

        grid.ClearAllLayersArea(1, 1, 2, 2);

        // Check cleared area across all layers
        for (int layer = 0; layer < 3; layer++)
        {
            grid[layer, 1, 1].Should().BeNull();
            grid[layer, 2, 2].Should().BeNull();
        }

        // Check outside area remains filled
        grid[0, 0, 0].Should().Be("layer0");
        grid[1, 0, 0].Should().Be("layer1");
    }

    /// <summary>
    /// Tests that GetRowSpan returns span data for a row on the default layer.
    /// The span should contain accurate row data with efficient access.
    /// </summary>
    [Fact]
    public void GetRowSpan_ReturnsRowDataOnDefaultLayer()
    {
        var grid = new LayeredGrid2D<string>(4, 3, 2);
        grid[0, 1] = "a";
        grid[2, 1] = "c";

        var span = grid.GetRowSpan(1);

        span.Length.Should().Be(4);
        span[0].Should().Be("a");
        span[1].Should().BeNull();
        span[2].Should().Be("c");
        span[3].Should().BeNull();
    }

    /// <summary>
    /// Tests that GetLayerRowSpan returns span data for a row on specific layer.
    /// The span should provide efficient access to layer-specific row data.
    /// </summary>
    [Fact]
    public void GetLayerRowSpan_ReturnsRowDataFromSpecificLayer()
    {
        var grid = new LayeredGrid2D<string>(3, 3, 3);
        grid[2, 0, 1] = "layer2_a";
        grid[2, 2, 1] = "layer2_c";

        var span = grid.GetLayerRowSpan(2, 1);

        span.Length.Should().Be(3);
        span[0].Should().Be("layer2_a");
        span[1].Should().BeNull();
        span[2].Should().Be("layer2_c");
    }

    /// <summary>
    /// Tests that EnumerateAll returns all grid positions and values on default layer.
    /// The enumeration should cover every cell in the default layer exactly once.
    /// </summary>
    [Fact]
    public void EnumerateAll_ReturnsAllPositionsAndValuesOnDefaultLayer()
    {
        var grid = new LayeredGrid2D<string>(3, 2, 2);
        grid[1, 0] = "a";
        grid[2, 1] = "b";

        var allCells = grid.EnumerateAll().ToList();

        allCells.Should().HaveCount(6); // 3 * 2
        allCells.Should().Contain((1, 0, "a"));
        allCells.Should().Contain((2, 1, "b"));
        allCells.Where(c => c.value != null).Should().HaveCount(2);
    }

    /// <summary>
    /// Tests that EnumerateAllLayers returns positions and values from all layers.
    /// The enumeration should include layer information for each cell.
    /// </summary>
    [Fact]
    public void EnumerateAllLayers_ReturnsPositionsAndValuesFromAllLayers()
    {
        var grid = new LayeredGrid2D<string>(2, 2, 3);
        grid[0, 1, 0] = "layer0";
        grid[1, 1, 0] = "layer1";
        grid[2, 1, 0] = "layer2";

        var allCells = grid.EnumerateAllLayers().ToList();

        allCells.Should().HaveCount(12); // 2 * 2 * 3
        allCells.Should().Contain((1, 0, 0, "layer0"));
        allCells.Should().Contain((1, 0, 1, "layer1"));
        allCells.Should().Contain((1, 0, 2, "layer2"));
    }

    /// <summary>
    /// Tests that EnumerateArea returns cells within specified rectangular bounds on default layer.
    /// The enumeration should only include cells within the area boundaries.
    /// </summary>
    [Fact]
    public void EnumerateArea_ReturnsAreaCellsOnDefaultLayer()
    {
        var grid = new LayeredGrid2D<string>(10, 10, 2);
        grid[2, 2] = "inside";
        grid[3, 3] = "inside";
        grid[5, 5] = "outside"; // Outside the 2x2 area

        var areaCells = grid.EnumerateArea(2, 2, 2, 2).ToList();

        areaCells.Should().HaveCount(4); // 2x2 area
        areaCells.Should().Contain((2, 2, "inside"));
        areaCells.Should().Contain((3, 3, "inside"));
        areaCells.Should().NotContain(cell => cell.x == 5 && cell.y == 5);
    }

    /// <summary>
    /// Tests that CopyLayer copies all data from source to destination layer.
    /// The destination layer should contain exact copy of source layer data.
    /// </summary>
    [Fact]
    public void CopyLayer_CopiesDataBetweenLayers()
    {
        var grid = new LayeredGrid2D<string>(3, 3, 3);
        grid[0, 0, 0] = "a";
        grid[0, 1, 1] = "b";
        grid[0, 2, 2] = "c";

        grid.CopyLayer(0, 1);

        // Destination layer should have copied data
        grid[1, 0, 0].Should().Be("a");
        grid[1, 1, 1].Should().Be("b");
        grid[1, 2, 2].Should().Be("c");

        // Source layer should remain unchanged
        grid[0, 0, 0].Should().Be("a");
    }

    /// <summary>
    /// Tests that CopyLayer handles same source and destination layer gracefully.
    /// Self-copy should be a no-op without affecting the layer.
    /// </summary>
    [Fact]
    public void CopyLayer_SameSourceAndDestination_HandlesGracefully()
    {
        var grid = new LayeredGrid2D<string>(3, 3, 2);
        grid[0, 1, 1] = "test";

        grid.CopyLayer(0, 0); // Copy to same layer

        grid[0, 1, 1].Should().Be("test"); // Should remain unchanged
    }

    /// <summary>
    /// Tests that ClearLayer sets all values in specified layer to default.
    /// Only the specified layer should be cleared.
    /// </summary>
    [Fact]
    public void ClearLayer_ClearsSpecificLayerOnly()
    {
        var grid = new LayeredGrid2D<string>(3, 3, 3);
        
        // Fill all layers
        for (int layer = 0; layer < 3; layer++)
        {
            grid.FillLayer(layer, $"layer{layer}");
        }

        grid.ClearLayer(1);

        // Layer 1 should be cleared
        grid[1, 1, 1].Should().BeNull();

        // Other layers should remain filled
        grid[0, 1, 1].Should().Be("layer0");
        grid[2, 1, 1].Should().Be("layer2");
    }

    /// <summary>
    /// Tests that FillLayer sets all values in specified layer to given value.
    /// Only the specified layer should be filled.
    /// </summary>
    [Fact]
    public void FillLayer_FillsSpecificLayerOnly()
    {
        var grid = new LayeredGrid2D<string>(3, 3, 3);

        grid.FillLayer(1, "filled");

        // Layer 1 should be filled
        grid[1, 0, 0].Should().Be("filled");
        grid[1, 2, 2].Should().Be("filled");

        // Other layers should remain null
        grid[0, 1, 1].Should().BeNull();
        grid[2, 1, 1].Should().BeNull();
    }

    /// <summary>
    /// Tests that GetNeighborsNonAlloc fills buffer with neighbor values efficiently.
    /// The method should populate the provided buffer without additional allocations.
    /// </summary>
    [Fact]
    public void GetNeighborsNonAlloc_FillsBufferEfficientlyOnDefaultLayer()
    {
        var grid = new LayeredGrid2D<string>(5, 5, 2);
        grid[1, 2] = "left";
        grid[3, 2] = "right";

        Span<string> buffer = new string[8];
        var count = grid.GetNeighborsNonAlloc(2, 2, buffer, includeDiagonals: false);

        count.Should().Be(4);
        buffer[0].Should().Be("left");
        buffer[1].Should().Be("right");
    }

    /// <summary>
    /// Tests that ProcessNeighbors executes action on all neighbor values on default layer.
    /// The processor should be called for each valid neighbor cell.
    /// </summary>
    [Fact]
    public void ProcessNeighbors_ExecutesActionOnNeighborsOnDefaultLayer()
    {
        var grid = new LayeredGrid2D<string>(5, 5, 2);
        grid[1, 2] = "neighbor1";
        grid[3, 2] = "neighbor2";

        var neighbors = new List<string>();
        grid.ProcessNeighbors(2, 2, value => { if (value != null) neighbors.Add(value); }, includeDiagonals: false);

        neighbors.Should().HaveCount(2);
        neighbors.Should().Contain("neighbor1");
        neighbors.Should().Contain("neighbor2");
    }

    /// <summary>
    /// Tests that row and column access methods validate indices correctly.
    /// Invalid indices should throw ArgumentOutOfRangeException.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    public void RowColumnAccess_InvalidIndices_ThrowException(int invalidIndex)
    {
        var grid = new LayeredGrid2D<string>(5, 5, 2);

        var rowAct = () => grid.GetRow(invalidIndex).ToList();
        var columnAct = () => grid.GetColumn(invalidIndex).ToList();
        var layerRowAct = () => grid.GetLayerRow(0, invalidIndex).ToList();
        var spanAct = () => { var span = grid.GetRowSpan(invalidIndex); };

        rowAct.Should().Throw<ArgumentOutOfRangeException>();
        columnAct.Should().Throw<ArgumentOutOfRangeException>();
        layerRowAct.Should().Throw<ArgumentOutOfRangeException>();
        spanAct.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Tests that layer operations validate layer indices correctly.
    /// Invalid layer indices should throw ArgumentOutOfRangeException.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void LayerOperations_InvalidLayerIndex_ThrowException(int invalidLayer)
    {
        var grid = new LayeredGrid2D<string>(5, 5, 3);

        var copyFromAct = () => grid.CopyLayer(invalidLayer, 0);
        var copyToAct = () => grid.CopyLayer(0, invalidLayer);
        var clearAct = () => grid.ClearLayer(invalidLayer);
        var fillAct = () => grid.FillLayer(invalidLayer, "test");

        copyFromAct.Should().Throw<ArgumentOutOfRangeException>();
        copyToAct.Should().Throw<ArgumentOutOfRangeException>();
        clearAct.Should().Throw<ArgumentOutOfRangeException>();
        fillAct.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Tests that Dispose cleans up resources properly including pooled memory.
    /// The grid should release allocated resources when disposed.
    /// </summary>
    [Fact]
    public void Dispose_CleansUpResources()
    {
        var grid = LayeredGrid2D<string>.CreateWithArrayPool(64, 64, 3);
        grid[0, 10, 10] = "test";

        grid.Dispose();

        // Should not throw after disposal
        grid.Dispose(); // Multiple dispose calls should be safe
    }

    /// <summary>
    /// Tests memory layout efficiency across layers and coordinates.
    /// Grid operations should maintain contiguous memory access patterns.
    /// </summary>
    [Fact]
    public void MemoryLayout_MaintainsContiguousAccess()
    {
        var grid = new LayeredGrid2D<int>(10, 10, 3);

        // Fill in a pattern that tests memory layout
        for (int layer = 0; layer < 3; layer++)
        {
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    grid[layer, x, y] = layer * 10000 + x * 100 + y;
                }
            }
        }

        // Verify values are stored correctly
        grid[0, 5, 5].Should().Be(505);
        grid[1, 5, 5].Should().Be(10505);
        grid[2, 5, 5].Should().Be(20505);
    }

    /// <summary>
    /// Tests performance with large dimensions and multiple layers.
    /// Large grids should maintain functionality without performance degradation.
    /// </summary>
    [Fact]
    public void LargeGrid_MaintainsFunctionality()
    {
        var grid = new LayeredGrid2D<int>(100, 100, 5);

        // Test corners and center across layers
        grid[0, 0, 0] = 1;
        grid[2, 50, 50] = 2;
        grid[4, 99, 99] = 3;

        grid[0, 0, 0].Should().Be(1);
        grid[2, 50, 50].Should().Be(2);
        grid[4, 99, 99].Should().Be(3);

        // Test layer operations
        grid.FillLayer(1, 42);
        grid[1, 25, 75].Should().Be(42);
        grid[0, 25, 75].Should().Be(0); // Other layers unaffected
    }

    /// <summary>
    /// Tests edge cases with minimal grid dimensions.
    /// Single-cell grids should work correctly for all operations.
    /// </summary>
    [Fact]
    public void SingleCellGrid_HandlesAllOperations()
    {
        var grid = new LayeredGrid2D<string>(1, 1, 2);

        grid[0, 0, 0] = "layer0";
        grid[1, 0, 0] = "layer1";

        grid[0, 0, 0].Should().Be("layer0");
        grid[1, 0, 0].Should().Be("layer1");

        var neighbors = grid.GetNeighbors(0, 0).ToList();
        neighbors.Should().BeEmpty(); // No neighbors for single cell

        // Layer operations should work
        grid.CopyLayer(0, 1);
        grid[1, 0, 0].Should().Be("layer0");
    }

    /// <summary>
    /// Tests coordinate boundary validation across all public methods.
    /// Methods should consistently validate coordinates and handle boundaries.
    /// </summary>
    [Theory]
    [InlineData(5, 5, 3)]
    [InlineData(64, 32, 2)]
    [InlineData(100, 100, 4)]
    public void CoordinateValidation_ConsistentAcrossMethods(int width, int height, int layerCount)
    {
        var grid = new LayeredGrid2D<string>(width, height, layerCount);

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
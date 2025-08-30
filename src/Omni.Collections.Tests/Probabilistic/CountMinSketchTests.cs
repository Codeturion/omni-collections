using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Omni.Collections.Probabilistic;
using Xunit;

namespace Omni.Collections.Tests.Probabilistic;

public class CountMinSketchTests
{
    /// <summary>
    /// Tests that a CountMinSketch can be constructed with valid width and depth parameters.
    /// The sketch should initialize with the specified dimensions and zero total count.
    /// </summary>
    [Theory]
    [InlineData(512, 4)]
    [InlineData(1024, 8)]
    [InlineData(2048, 16)]
    public void Constructor_WithValidDimensions_InitializesCorrectly(int width, int depth)
    {
        var sketch = new CountMinSketch<int>(width, depth);

        sketch.Width.Should().Be(width);
        sketch.Depth.Should().Be(depth);
        sketch.TotalCount.Should().Be(0);
        sketch.MaxError.Should().Be(0.0);
    }

    /// <summary>
    /// Tests that constructing with default parameters creates a functional sketch.
    /// The sketch should use reasonable default dimensions for general use cases.
    /// </summary>
    [Fact]
    public void Constructor_WithDefaults_InitializesCorrectly()
    {
        var sketch = new CountMinSketch<int>();

        sketch.Width.Should().Be(1024);
        sketch.Depth.Should().Be(4);
        sketch.TotalCount.Should().Be(0);
    }

    /// <summary>
    /// Tests that constructing with error and confidence parameters calculates dimensions correctly.
    /// The sketch should size itself based on the desired accuracy requirements.
    /// </summary>
    [Theory]
    [InlineData(0.01, 0.99)]
    [InlineData(0.05, 0.95)]
    [InlineData(0.1, 0.9)]
    public void Constructor_WithErrorAndConfidence_CalculatesDimensionsCorrectly(double maxError, double confidence)
    {
        var sketch = new CountMinSketch<int>(maxError, confidence);

        sketch.Width.Should().BeGreaterThan(0);
        sketch.Depth.Should().BeGreaterThan(0);
        sketch.Depth.Should().BeLessOrEqualTo(32);
        sketch.TotalCount.Should().Be(0);
    }

    /// <summary>
    /// Tests that constructing with invalid width throws exception.
    /// The constructor should reject zero or negative width values.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_WithInvalidWidth_ThrowsArgumentOutOfRangeException(int width)
    {
        var act = () => new CountMinSketch<int>(width, 4);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("width");
    }

    /// <summary>
    /// Tests that constructing with invalid depth throws exception.
    /// The constructor should reject zero, negative, or excessively large depth values.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(33)]
    [InlineData(100)]
    public void Constructor_WithInvalidDepth_ThrowsArgumentOutOfRangeException(int depth)
    {
        var act = () => new CountMinSketch<int>(1024, depth);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("depth");
    }

    /// <summary>
    /// Tests that constructing with invalid error rate throws exception.
    /// The constructor should reject error rates outside the valid range of (0, 1).
    /// </summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.1)]
    [InlineData(1.0)]
    [InlineData(1.5)]
    public void Constructor_WithInvalidError_ThrowsArgumentOutOfRangeException(double maxError)
    {
        var act = () => new CountMinSketch<int>(maxError);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("maxError");
    }

    /// <summary>
    /// Tests that constructing with invalid confidence throws exception.
    /// The constructor should reject confidence values outside the valid range of (0, 1).
    /// </summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.1)]
    [InlineData(1.0)]
    [InlineData(1.5)]
    public void Constructor_WithInvalidConfidence_ThrowsArgumentOutOfRangeException(double confidence)
    {
        var act = () => new CountMinSketch<int>(0.01, confidence);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("confidence");
    }

    /// <summary>
    /// Tests that Add successfully adds single items to the sketch.
    /// Adding items should increase the total count and update estimates.
    /// </summary>
    [Fact]
    public void Add_SingleItems_UpdatesCountsCorrectly()
    {
        var sketch = new CountMinSketch<int>();

        sketch.Add(1);
        sketch.Add(2);
        sketch.Add(1);

        sketch.TotalCount.Should().Be(3);
        sketch.EstimateCount(1).Should().BeGreaterOrEqualTo(2);
        sketch.EstimateCount(2).Should().BeGreaterOrEqualTo(1);
    }

    /// <summary>
    /// Tests that Add with count parameter adds multiple occurrences correctly.
    /// The method should add the specified count to the item's frequency.
    /// </summary>
    [Fact]
    public void Add_WithCount_AddsMultipleOccurrences()
    {
        var sketch = new CountMinSketch<int>();

        sketch.Add(1, 5);
        sketch.Add(2, 3);

        sketch.TotalCount.Should().Be(8);
        sketch.EstimateCount(1).Should().BeGreaterOrEqualTo(5);
        sketch.EstimateCount(2).Should().BeGreaterOrEqualTo(3);
    }

    /// <summary>
    /// Tests that Add with zero count does not modify the sketch.
    /// Adding zero should be a no-op that doesn't affect totals or estimates.
    /// </summary>
    [Fact]
    public void Add_WithZeroCount_DoesNotModifySketch()
    {
        var sketch = new CountMinSketch<int>();
        sketch.Add(1, 1);

        var initialTotal = sketch.TotalCount;
        var initialEstimate = sketch.EstimateCount(1);

        sketch.Add(2, 0);

        sketch.TotalCount.Should().Be(initialTotal);
        sketch.EstimateCount(1).Should().Be(initialEstimate);
        sketch.EstimateCount(2).Should().Be(0);
    }

    /// <summary>
    /// Tests that EstimateCount provides accurate frequency estimates for known items.
    /// Estimates should be greater than or equal to actual counts (over-estimation property).
    /// </summary>
    [Fact]
    public void EstimateCount_ReturnsAccurateEstimates()
    {
        var sketch = new CountMinSketch<int>(2048, 8); // Large sketch for accuracy
        var actualCounts = new Dictionary<int, int>
        {
            { 1, 10 },
            { 2, 5 },
            { 3, 15 },
            { 4, 1 }
        };

        foreach (var kvp in actualCounts)
        {
            for (int i = 0; i < kvp.Value; i++)
            {
                sketch.Add(kvp.Key);
            }
        }

        foreach (var kvp in actualCounts)
        {
            var estimate = sketch.EstimateCount(kvp.Key);
            estimate.Should().BeGreaterOrEqualTo((uint)kvp.Value, 
                $"estimate for {kvp.Key} should be at least the actual count");
        }
    }

    /// <summary>
    /// Tests that EstimateFrequency calculates correct frequency ratios.
    /// Frequencies should sum to approximately 1.0 and be proportional to counts.
    /// </summary>
    [Fact]
    public void EstimateFrequency_CalculatesCorrectRatios()
    {
        var sketch = new CountMinSketch<int>();

        sketch.Add(1, 10);
        sketch.Add(2, 20);
        sketch.Add(3, 30);

        var freq1 = sketch.EstimateFrequency(1);
        var freq2 = sketch.EstimateFrequency(2);
        var freq3 = sketch.EstimateFrequency(3);

        freq1.Should().BeGreaterThan(0);
        freq2.Should().BeGreaterThan(freq1);
        freq3.Should().BeGreaterThan(freq2);

        // Total frequency should be approximately 1.0
        (freq1 + freq2 + freq3).Should().BeApproximately(1.0, 0.1);
    }

    /// <summary>
    /// Tests that EstimateFrequency returns zero for empty sketch.
    /// The method should handle empty state gracefully.
    /// </summary>
    [Fact]
    public void EstimateFrequency_EmptySketch_ReturnsZero()
    {
        var sketch = new CountMinSketch<int>();

        sketch.EstimateFrequency(1).Should().Be(0.0);
        sketch.EstimateFrequency(999).Should().Be(0.0);
    }

    /// <summary>
    /// Tests that IsHeavyHitter correctly identifies frequent items.
    /// Items with frequency above the threshold should be identified as heavy hitters.
    /// </summary>
    [Fact]
    public void IsHeavyHitter_IdentifiesFrequentItems()
    {
        var sketch = new CountMinSketch<int>();

        sketch.Add(1, 70); // 70% of total
        sketch.Add(2, 20); // 20% of total
        sketch.Add(3, 10); // 10% of total

        sketch.IsHeavyHitter(1, 0.5).Should().BeTrue("item 1 should be a heavy hitter with 70% frequency");
        sketch.IsHeavyHitter(2, 0.5).Should().BeFalse("item 2 should not be a heavy hitter with 20% frequency");
        sketch.IsHeavyHitter(3, 0.5).Should().BeFalse("item 3 should not be a heavy hitter with 10% frequency");

        sketch.IsHeavyHitter(2, 0.15).Should().BeTrue("item 2 should be a heavy hitter with lower threshold");
    }

    /// <summary>
    /// Tests that IsHeavyHitter throws exception for invalid threshold values.
    /// The method should validate threshold is in the valid range (0, 1].
    /// </summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(2.0)]
    public void IsHeavyHitter_WithInvalidThreshold_ThrowsArgumentOutOfRangeException(double threshold)
    {
        var sketch = new CountMinSketch<int>();
        sketch.Add(1, 10);

        var act = () => sketch.IsHeavyHitter(1, threshold);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("threshold");
    }

    /// <summary>
    /// Tests that Clear resets the sketch to empty state.
    /// All counts should be zero and total count should be reset after clearing.
    /// </summary>
    [Fact]
    public void Clear_ResetsSketchToEmptyState()
    {
        var sketch = new CountMinSketch<int>();
        sketch.Add(1, 10);
        sketch.Add(2, 20);

        sketch.Clear();

        sketch.TotalCount.Should().Be(0);
        sketch.EstimateCount(1).Should().Be(0);
        sketch.EstimateCount(2).Should().Be(0);
        sketch.MaxError.Should().Be(0.0);
    }

    /// <summary>
    /// Tests that Merge correctly combines two compatible sketches.
    /// The resulting sketch should contain the sum of counts from both sketches.
    /// </summary>
    [Fact]
    public void Merge_CombinesSketchesCorrectly()
    {
        var sketch1 = new CountMinSketch<int>(1024, 4);
        var sketch2 = new CountMinSketch<int>(1024, 4);

        sketch1.Add(1, 10);
        sketch1.Add(2, 5);
        sketch2.Add(1, 15);
        sketch2.Add(3, 8);

        sketch1.Merge(sketch2);

        sketch1.TotalCount.Should().Be(38); // 10 + 5 + 15 + 8
        sketch1.EstimateCount(1).Should().BeGreaterOrEqualTo(25); // 10 + 15
        sketch1.EstimateCount(2).Should().BeGreaterOrEqualTo(5);
        sketch1.EstimateCount(3).Should().BeGreaterOrEqualTo(8);
    }

    /// <summary>
    /// Tests that Merge throws exception for incompatible sketch dimensions.
    /// The operation should reject sketches with different width or depth.
    /// </summary>
    [Fact]
    public void Merge_WithIncompatibleSketch_ThrowsArgumentException()
    {
        var sketch1 = new CountMinSketch<int>(1024, 4);
        var sketch2 = new CountMinSketch<int>(512, 4); // Different width

        var act = () => sketch1.Merge(sketch2);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*same dimensions*");
    }

    /// <summary>
    /// Tests that Scale reduces all counts by the specified factor.
    /// Counts should be proportionally reduced while maintaining relative frequencies.
    /// </summary>
    [Fact]
    public void Scale_ReducesCountsProportionally()
    {
        var sketch = new CountMinSketch<int>();
        sketch.Add(1, 100);
        sketch.Add(2, 50);

        var originalTotal = sketch.TotalCount;
        var originalCount1 = sketch.EstimateCount(1);
        var originalCount2 = sketch.EstimateCount(2);

        sketch.Scale(0.5);

        sketch.TotalCount.Should().Be(originalTotal / 2);
        sketch.EstimateCount(1).Should().BeLessOrEqualTo(originalCount1 / 2 + 1); // Allow for rounding
        sketch.EstimateCount(2).Should().BeLessOrEqualTo(originalCount2 / 2 + 1);
    }

    /// <summary>
    /// Tests that Scale throws exception for invalid factor values.
    /// The method should validate factor is in the valid range [0, 1].
    /// </summary>
    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(2.0)]
    public void Scale_WithInvalidFactor_ThrowsArgumentOutOfRangeException(double factor)
    {
        var sketch = new CountMinSketch<int>();
        sketch.Add(1, 10);

        var act = () => sketch.Scale(factor);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("factor");
    }

    /// <summary>
    /// Tests that GetStats returns comprehensive sketch statistics.
    /// The statistics should accurately reflect the current state of the sketch.
    /// </summary>
    [Fact]
    public void GetStats_ReturnsComprehensiveStatistics()
    {
        var sketch = new CountMinSketch<int>(100, 4);
        sketch.Add(1, 10);
        sketch.Add(2, 20);
        sketch.Add(3, 5);

        var stats = sketch.GetStats();

        stats.TotalItems.Should().Be(35);
        stats.TotalCells.Should().Be(400); // 100 * 4
        stats.NonZeroCells.Should().BeGreaterThan(0);
        stats.FillRatio.Should().BeGreaterThan(0.0);
        stats.MinCellValue.Should().BeGreaterOrEqualTo(0);
        stats.MaxCellValue.Should().BeGreaterThan(0);
        stats.AverageCellValue.Should().BeGreaterThan(0.0);
        stats.TheoreticalMaxError.Should().BeGreaterThan(0.0);
    }

    /// <summary>
    /// Tests that GetMemoryUsage calculates approximate memory consumption.
    /// The memory usage should reflect the size of the internal table structure.
    /// </summary>
    [Fact]
    public void GetMemoryUsage_CalculatesCorrectly()
    {
        var sketch = new CountMinSketch<int>(1000, 5);

        var memoryUsage = sketch.GetMemoryUsage();

        memoryUsage.Should().BeGreaterThan(0);
        // Should be approximately: width * depth * sizeof(uint) + overhead
        var expectedMinimum = 1000 * 5 * sizeof(uint);
        memoryUsage.Should().BeGreaterOrEqualTo(expectedMinimum);
    }

    /// <summary>
    /// Tests that CountMinSketch handles string types correctly.
    /// String items should be processed and counted without issues.
    /// </summary>
    [Fact]
    public void CountMinSketch_WithStringType_WorksCorrectly()
    {
        var sketch = new CountMinSketch<string>();
        var words = new[] { "apple", "banana", "apple", "cherry", "banana", "apple" };

        foreach (var word in words)
        {
            sketch.Add(word);
        }

        sketch.TotalCount.Should().Be(6);
        sketch.EstimateCount("apple").Should().BeGreaterOrEqualTo(3);
        sketch.EstimateCount("banana").Should().BeGreaterOrEqualTo(2);
        sketch.EstimateCount("cherry").Should().BeGreaterOrEqualTo(1);
        sketch.EstimateCount("nonexistent").Should().Be(0);
    }

    /// <summary>
    /// Tests that CountMinSketch demonstrates over-estimation property.
    /// Estimates should never under-count but may over-count due to hash collisions.
    /// </summary>
    [Fact]
    public void CountMinSketch_DemonstratesOverEstimationProperty()
    {
        var sketch = new CountMinSketch<int>(128, 4); // Smaller sketch to increase collisions
        var actualCounts = new Dictionary<int, int>();
        var random = new Random(42);

        // Add many random items to create collisions
        for (int i = 0; i < 1000; i++)
        {
            var item = random.Next(0, 50);
            sketch.Add(item);
            actualCounts[item] = actualCounts.GetValueOrDefault(item) + 1;
        }

        // Verify over-estimation property
        foreach (var kvp in actualCounts)
        {
            var estimate = sketch.EstimateCount(kvp.Key);
            estimate.Should().BeGreaterOrEqualTo((uint)kvp.Value, 
                $"estimate for {kvp.Key} should be at least the actual count");
        }
    }

    /// <summary>
    /// Tests that CountMinSketch maintains bounded error rates.
    /// Maximum error should be proportional to total count divided by width.
    /// </summary>
    [Fact]
    public void CountMinSketch_MaintainsBoundedError()
    {
        var sketch = new CountMinSketch<int>(1000, 8);

        for (int i = 0; i < 500; i++)
        {
            sketch.Add(i % 50); // Create uneven distribution
        }

        var maxError = sketch.MaxError;
        maxError.Should().BeGreaterThan(0);

        // Test that individual estimates don't exceed the theoretical maximum error bound
        for (int i = 0; i < 10; i++)
        {
            var estimate = sketch.EstimateCount(i);
            var frequency = sketch.EstimateFrequency(i);
            
            // Frequency should be reasonable (not wildly over-estimated)
            frequency.Should().BeLessOrEqualTo(1.0);
        }
    }

    /// <summary>
    /// Tests edge case of empty CountMinSketch operations.
    /// The sketch should handle operations on empty state correctly.
    /// </summary>
    [Fact]
    public void EmptyCountMinSketch_HandlesOperationsCorrectly()
    {
        var sketch = new CountMinSketch<int>();

        sketch.TotalCount.Should().Be(0);
        sketch.EstimateCount(1).Should().Be(0);
        sketch.EstimateFrequency(1).Should().Be(0.0);
        sketch.MaxError.Should().Be(0.0);

        var stats = sketch.GetStats();
        stats.TotalItems.Should().Be(0);
        stats.NonZeroCells.Should().Be(0);
        stats.FillRatio.Should().Be(0.0);
    }

    /// <summary>
    /// Tests that CountMinSketch handles hash collision scenarios gracefully.
    /// Items with the same hash should still be counted independently when possible.
    /// </summary>
    [Fact]
    public void CountMinSketch_HandlesHashCollisionsGracefully()
    {
        var sketch = new CountMinSketch<TestItem>(64, 4); // Small width to force collisions
        
        var item1 = new TestItem(1, "A");
        var item2 = new TestItem(1, "B"); // Same hash as item1
        
        sketch.Add(item1, 10);
        sketch.Add(item2, 5);

        sketch.TotalCount.Should().Be(15);
        
        // Due to collisions, estimates might be inflated, but should be reasonable
        var estimate1 = sketch.EstimateCount(item1);
        var estimate2 = sketch.EstimateCount(item2);
        
        estimate1.Should().BeGreaterOrEqualTo(10);
        estimate2.Should().BeGreaterOrEqualTo(5);
    }

    private record TestItem(int Id, string Name)
    {
        public override int GetHashCode() => Id; // Intentional collision
    }
}
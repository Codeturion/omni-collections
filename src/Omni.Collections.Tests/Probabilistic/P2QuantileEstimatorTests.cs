using System;
using System.Linq;
using FluentAssertions;
using Omni.Collections.Probabilistic;
using Xunit;

namespace Omni.Collections.Tests.Probabilistic;

public class P2QuantileEstimatorTests
{
    /// <summary>
    /// Tests that a P2QuantileEstimator can be constructed with valid target percentile.
    /// The estimator should initialize with the specified percentile and zero count.
    /// </summary>
    [Theory]
    [InlineData(0.1)]
    [InlineData(0.5)]
    [InlineData(0.9)]
    [InlineData(0.95)]
    [InlineData(0.99)]
    public void Constructor_WithValidPercentile_InitializesCorrectly(double targetPercentile)
    {
        var estimator = new P2QuantileEstimator<double>(targetPercentile);

        estimator.TargetPercentile.Should().Be(targetPercentile);
        estimator.Count.Should().Be(0);
        estimator.AccuracyEstimate.Should().BeGreaterThan(0);
        estimator.MemoryUsage.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Tests that constructing with default parameters creates a 95th percentile estimator.
    /// The estimator should use reasonable default percentile for general monitoring use cases.
    /// </summary>
    [Fact]
    public void Constructor_WithDefaults_InitializesTo95thPercentile()
    {
        var estimator = new P2QuantileEstimator<double>();

        estimator.TargetPercentile.Should().Be(0.95);
        estimator.Count.Should().Be(0);
    }

    /// <summary>
    /// Tests that constructing with invalid target percentile throws exception.
    /// The constructor should reject percentiles outside the valid range of [0, 1].
    /// </summary>
    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(2.0)]
    [InlineData(-1.0)]
    public void Constructor_WithInvalidPercentile_ThrowsArgumentOutOfRangeException(double targetPercentile)
    {
        var act = () => new P2QuantileEstimator<double>(targetPercentile);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("targetPercentile");
    }

    /// <summary>
    /// Tests that Add successfully adds values and updates count.
    /// Adding values should increase the count and prepare the estimator for quantile calculation.
    /// </summary>
    [Fact]
    public void Add_AddsValuesSuccessfully()
    {
        var estimator = new P2QuantileEstimator<double>();

        estimator.Add(1.0);
        estimator.Add(2.0);
        estimator.Add(3.0);

        estimator.Count.Should().Be(3);
    }

    /// <summary>
    /// Tests that Add throws exception for null values.
    /// The method should validate that values are not null.
    /// </summary>
    [Fact]
    public void Add_WithNullValue_ThrowsArgumentNullException()
    {
        var estimator = new P2QuantileEstimator<string>();

        var act = () => estimator.Add(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("value");
    }

    /// <summary>
    /// Tests that GetPercentile returns accurate estimates for small datasets.
    /// With few samples, the estimator should provide exact results using full sorting.
    /// </summary>
    [Fact]
    public void GetPercentile_SmallDataset_ProvidesAccurateEstimate()
    {
        var estimator = new P2QuantileEstimator<double>(0.5); // Median
        var values = new double[] { 1.0, 2.0, 3.0, 4.0 };

        foreach (var value in values)
        {
            estimator.Add(value);
        }

        var median = estimator.GetPercentile(0.5);
        median.Should().BeOneOf(2.0, 3.0); // Either is acceptable for median of 4 values
    }

    /// <summary>
    /// Tests that GetPercentile maintains accuracy with larger streaming datasets.
    /// The P-squared algorithm should provide reasonable estimates for the target percentile.
    /// </summary>
    [Fact]
    public void GetPercentile_LargeDataset_MaintainsAccuracy()
    {
        var estimator = new P2QuantileEstimator<double>(0.9);
        var random = new Random(42);
        var values = Enumerable.Range(0, 1000)
            .Select(_ => random.NextDouble() * 100)
            .OrderBy(x => x)
            .ToArray();

        // Add values in streaming fashion
        foreach (var value in values.OrderBy(_ => random.Next()))
        {
            estimator.Add(value);
        }

        var estimate = estimator.GetPercentile(0.9);
        var exactP90 = values[(int)(values.Length * 0.9)];

        var relativeError = Math.Abs(estimate - exactP90) / exactP90;
        relativeError.Should().BeLessThan(0.1, "P-squared algorithm should maintain reasonable accuracy");
    }

    /// <summary>
    /// Tests that GetPercentile throws exception for invalid percentile values.
    /// The method should validate percentile is in the valid range [0, 1].
    /// </summary>
    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(2.0)]
    public void GetPercentile_WithInvalidPercentile_ThrowsArgumentOutOfRangeException(double percentile)
    {
        var estimator = new P2QuantileEstimator<double>();
        estimator.Add(1.0);

        var act = () => estimator.GetPercentile(percentile);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("percentile");
    }

    /// <summary>
    /// Tests that GetPercentile throws exception for empty estimator.
    /// The method should reject requests when no data has been added.
    /// </summary>
    [Fact]
    public void GetPercentile_EmptyEstimator_ThrowsInvalidOperationException()
    {
        var estimator = new P2QuantileEstimator<double>();

        var act = () => estimator.GetPercentile(0.5);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*empty estimator*");
    }

    /// <summary>
    /// Tests that GetPercentile handles extreme percentiles correctly.
    /// The method should return appropriate values for 0th and 100th percentiles.
    /// </summary>
    [Fact]
    public void GetPercentile_ExtremePercentiles_ReturnsCorrectValues()
    {
        var estimator = new P2QuantileEstimator<double>();
        var values = new double[] { 10.0, 20.0, 30.0, 40.0, 50.0, 60.0, 70.0, 80.0, 90.0, 100.0 };

        foreach (var value in values)
        {
            estimator.Add(value);
        }

        var min = estimator.GetPercentile(0.0);
        var max = estimator.GetPercentile(1.0);

        min.Should().Be(10.0, "0th percentile should be minimum value");
        max.Should().Be(100.0, "100th percentile should be maximum value");
    }

    /// <summary>
    /// Tests that GetPercentiles returns multiple percentile estimates efficiently.
    /// The method should calculate all requested percentiles in a single call.
    /// </summary>
    [Fact]
    public void GetPercentiles_ReturnsMultipleEstimates()
    {
        var estimator = new P2QuantileEstimator<double>();
        var values = Enumerable.Range(1, 100).Select(x => (double)x).ToArray();

        foreach (var value in values)
        {
            estimator.Add(value);
        }

        var percentiles = new double[] { 0.1, 0.25, 0.5, 0.75, 0.9 };
        var results = estimator.GetPercentiles(percentiles);

        results.Length.Should().Be(5);
        results[0].Should().BeLessThan(results[1]); // 10th < 25th
        results[1].Should().BeLessThan(results[2]); // 25th < 50th
        results[2].Should().BeLessThan(results[3]); // 50th < 75th
        results[3].Should().BeLessThan(results[4]); // 75th < 90th
    }

    /// <summary>
    /// Tests that GetPercentiles throws exception for null or empty arrays.
    /// The method should validate the percentiles parameter.
    /// </summary>
    [Fact]
    public void GetPercentiles_WithInvalidInput_ThrowsException()
    {
        var estimator = new P2QuantileEstimator<double>();
        estimator.Add(1.0);

        var actNull = () => estimator.GetPercentiles(null!);
        var actEmpty = () => estimator.GetPercentiles(Array.Empty<double>());

        actNull.Should().Throw<ArgumentNullException>();
        actEmpty.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be empty*");
    }

    /// <summary>
    /// Tests that Clear resets the estimator to empty state.
    /// The estimator should have zero count and reset internal markers after clearing.
    /// </summary>
    [Fact]
    public void Clear_ResetsEstimatorToEmptyState()
    {
        var estimator = new P2QuantileEstimator<double>();
        estimator.Add(1.0);
        estimator.Add(2.0);
        estimator.Add(3.0);

        estimator.Clear();

        estimator.Count.Should().Be(0);
        
        var act = () => estimator.GetPercentile(0.5);
        act.Should().Throw<InvalidOperationException>();
    }

    /// <summary>
    /// Tests that ValidateAccuracy compares estimator results against exact calculations.
    /// The method should determine if the estimator meets the specified accuracy tolerance.
    /// </summary>
    [Fact]
    public void ValidateAccuracy_ComparesAgainstExactData()
    {
        var estimator = new P2QuantileEstimator<double>(0.95);
        var testData = Enumerable.Range(1, 1000).Select(x => (double)x).ToArray();

        foreach (var value in testData)
        {
            estimator.Add(value);
        }

        var isAccurate = estimator.ValidateAccuracy(testData, tolerance: 0.05);
        isAccurate.Should().BeTrue("estimator should be reasonably accurate for large uniform dataset");
    }

    /// <summary>
    /// Tests that ValidateAccuracy returns false for null or empty exact data.
    /// The method should handle invalid validation data gracefully.
    /// </summary>
    [Fact]
    public void ValidateAccuracy_WithInvalidData_ReturnsFalse()
    {
        var estimator = new P2QuantileEstimator<double>();
        estimator.Add(1.0);

        var resultNull = estimator.ValidateAccuracy(null!);
        var resultEmpty = estimator.ValidateAccuracy(Array.Empty<double>());

        resultNull.Should().BeFalse();
        resultEmpty.Should().BeFalse();
    }

    /// <summary>
    /// Tests that GetStats returns comprehensive estimator statistics.
    /// The statistics should accurately reflect the current state of the estimator.
    /// </summary>
    [Fact]
    public void GetStats_ReturnsComprehensiveStatistics()
    {
        var estimator = new P2QuantileEstimator<double>(0.9);
        for (int i = 1; i <= 10; i++)
        {
            estimator.Add(i * 10.0);
        }

        var stats = estimator.GetStats();

        stats.Count.Should().Be(10);
        stats.TargetPercentile.Should().Be(0.9);
        stats.AccuracyEstimate.Should().BeGreaterThan(0);
        stats.MemoryUsage.Should().BeGreaterThan(0);
        stats.Markers.Should().NotBeNull();
        stats.MarkerPositions.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that AccuracyEstimate reflects the configured target percentile.
    /// Extreme percentiles should have lower accuracy estimates than middle percentiles.
    /// </summary>
    [Theory]
    [InlineData(0.01, true)]  // Extreme percentile - lower accuracy
    [InlineData(0.5, false)]  // Middle percentile - higher accuracy  
    [InlineData(0.99, true)]  // Extreme percentile - lower accuracy
    public void AccuracyEstimate_ReflectsPercentileExtremeness(double percentile, bool shouldBeLowAccuracy)
    {
        var estimator = new P2QuantileEstimator<double>(percentile);
        
        // Add enough data for meaningful accuracy estimate
        for (int i = 0; i < 1000; i++)
        {
            estimator.Add(i);
        }

        var accuracy = estimator.AccuracyEstimate;
        
        if (shouldBeLowAccuracy)
        {
            accuracy.Should().BeGreaterThan(0.03, "extreme percentiles should have lower accuracy");
        }
        else
        {
            accuracy.Should().BeLessThan(0.05, "middle percentiles should have higher accuracy");
        }
    }

    /// <summary>
    /// Tests that P2QuantileEstimator works correctly with integer types.
    /// Integer values should be handled without loss of precision in calculations.
    /// </summary>
    [Fact]
    public void P2QuantileEstimator_WithIntegerType_WorksCorrectly()
    {
        var estimator = new P2QuantileEstimator<int>(0.5);
        var values = Enumerable.Range(1, 100).ToArray();

        foreach (var value in values)
        {
            estimator.Add(value);
        }

        var median = estimator.GetPercentile(0.5);
        median.Should().BeInRange(45, 55, "median of 1-100 should be around 50");
    }

    /// <summary>
    /// Tests that P2QuantileEstimator handles floating-point precision correctly.
    /// Small differences in floating-point values should not cause calculation errors.
    /// </summary>
    [Fact]
    public void P2QuantileEstimator_HandlesFloatingPointPrecision()
    {
        var estimator = new P2QuantileEstimator<double>(0.95);
        
        // Add values with small differences
        for (int i = 0; i < 100; i++)
        {
            estimator.Add(1.0 + i * 0.001);
        }

        var p95 = estimator.GetPercentile(0.95);
        p95.Should().BeGreaterThan(1.09, "95th percentile should be near the high end");
        p95.Should().BeLessThan(1.11, "95th percentile should not exceed reasonable bounds");
    }

    /// <summary>
    /// Tests that P2QuantileEstimator maintains accuracy across different data distributions.
    /// The algorithm should work reasonably well with various statistical distributions.
    /// </summary>
    [Fact]
    public void P2QuantileEstimator_VariousDistributions_MaintainsAccuracy()
    {
        var estimator = new P2QuantileEstimator<double>(0.9);
        var random = new Random(42);

        // Generate data with normal-like distribution (using Box-Muller transform approximation)
        var values = new double[1000];
        for (int i = 0; i < 1000; i++)
        {
            var u1 = random.NextDouble();
            var u2 = random.NextDouble();
            var normal = Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);
            values[i] = normal;
            estimator.Add(normal);
        }

        Array.Sort(values);
        var exactP90 = values[899]; // 90th percentile index
        var estimatedP90 = estimator.GetPercentile(0.9);

        var relativeError = Math.Abs(estimatedP90 - exactP90) / Math.Max(Math.Abs(exactP90), 0.001);
        relativeError.Should().BeLessThan(0.15, "should maintain reasonable accuracy with normal distribution");
    }

    /// <summary>
    /// Tests that P2QuantileEstimator demonstrates consistent behavior across multiple runs.
    /// Results should be deterministic for the same input sequence.
    /// </summary>
    [Fact]
    public void P2QuantileEstimator_DeterministicBehavior()
    {
        var estimator1 = new P2QuantileEstimator<double>(0.95);
        var estimator2 = new P2QuantileEstimator<double>(0.95);
        var values = new double[] { 1, 5, 3, 9, 2, 8, 4, 7, 6, 10 };

        foreach (var value in values)
        {
            estimator1.Add(value);
            estimator2.Add(value);
        }

        var result1 = estimator1.GetPercentile(0.95);
        var result2 = estimator2.GetPercentile(0.95);

        result1.Should().Be(result2, "identical input sequences should produce identical results");
    }

    /// <summary>
    /// Tests that P2QuantileEstimator handles edge case of single value correctly.
    /// A single value should be returned for all percentile queries.
    /// </summary>
    [Fact]
    public void P2QuantileEstimator_SingleValue_HandlesAllPercentiles()
    {
        var estimator = new P2QuantileEstimator<double>();
        estimator.Add(42.0);

        var p10 = estimator.GetPercentile(0.1);
        var p50 = estimator.GetPercentile(0.5);
        var p90 = estimator.GetPercentile(0.9);

        p10.Should().Be(42.0);
        p50.Should().Be(42.0);
        p90.Should().Be(42.0);
    }

    /// <summary>
    /// Tests that P2QuantileEstimator's memory usage calculation is reasonable.
    /// Memory usage should be constant regardless of the number of values added.
    /// </summary>
    [Fact]
    public void MemoryUsage_RemainsConstant()
    {
        var estimator = new P2QuantileEstimator<double>();
        
        var initialMemory = estimator.MemoryUsage;
        
        for (int i = 0; i < 1000; i++)
        {
            estimator.Add(i);
        }
        
        var finalMemory = estimator.MemoryUsage;
        
        finalMemory.Should().Be(initialMemory, "memory usage should remain constant for streaming algorithm");
    }

    /// <summary>
    /// Tests edge case of P2QuantileEstimator with identical values.
    /// All percentiles should return the same value when all inputs are identical.
    /// </summary>
    [Fact]
    public void P2QuantileEstimator_IdenticalValues_ReturnsConstantPercentiles()
    {
        var estimator = new P2QuantileEstimator<double>();
        
        for (int i = 0; i < 100; i++)
        {
            estimator.Add(7.0);
        }

        var p25 = estimator.GetPercentile(0.25);
        var p50 = estimator.GetPercentile(0.5);
        var p75 = estimator.GetPercentile(0.75);

        p25.Should().Be(7.0);
        p50.Should().Be(7.0);
        p75.Should().Be(7.0);
    }

    /// <summary>
    /// Tests that P2QuantileEstimator handles very large datasets efficiently.
    /// Performance should remain good even with many values added.
    /// </summary>
    [Fact]
    public void P2QuantileEstimator_LargeDataset_MaintainsPerformance()
    {
        var estimator = new P2QuantileEstimator<double>(0.99);
        var random = new Random(42);

        // Add many values
        for (int i = 0; i < 10000; i++)
        {
            estimator.Add(random.NextDouble() * 1000);
        }

        estimator.Count.Should().Be(10000);
        
        var p99 = estimator.GetPercentile(0.99);
        p99.Should().BeGreaterThan(0, "99th percentile should be meaningful");
        
        var stats = estimator.GetStats();
        stats.AccuracyEstimate.Should().BeLessThan(0.1, "accuracy should improve with larger datasets");
    }
}
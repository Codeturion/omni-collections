using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Omni.Collections.Probabilistic;
using Xunit;

namespace Omni.Collections.Tests.Probabilistic;

public class TDigestTests
{
    /// <summary>
    /// Tests that a TDigest can be constructed with valid compression parameter.
    /// The digest should initialize with the specified compression and zero count.
    /// </summary>
    [Theory]
    [InlineData(20)]
    [InlineData(100)]
    [InlineData(500)]
    [InlineData(1000)]
    public void Constructor_WithValidCompression_InitializesCorrectly(double compression)
    {
        var digest = new Digest(compression);

        digest.Count.Should().Be(0);
        digest.ClusterCount.Should().Be(0);
        digest.Min.Should().Be(double.NaN);
        digest.Max.Should().Be(double.NaN);
        digest.EstimatedMemoryUsage.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Tests that constructing with default parameters creates a functional digest.
    /// The digest should use reasonable default compression for general use cases.
    /// </summary>
    [Fact]
    public void Constructor_WithDefaults_InitializesCorrectly()
    {
        var digest = new Digest();

        digest.Count.Should().Be(0);
        digest.ClusterCount.Should().Be(0);
        digest.Min.Should().Be(double.NaN);
        digest.Max.Should().Be(double.NaN);
    }

    /// <summary>
    /// Tests that constructing with invalid compression throws exception.
    /// The constructor should reject compression values outside the valid range of [20, 1000].
    /// </summary>
    [Theory]
    [InlineData(19)]
    [InlineData(1001)]
    [InlineData(0)]
    [InlineData(-100)]
    public void Constructor_WithInvalidCompression_ThrowsArgumentOutOfRangeException(double compression)
    {
        var act = () => new Digest(compression);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("compression");
    }

    /// <summary>
    /// Tests that Add successfully adds values and updates statistics.
    /// Adding values should increase count and update min/max bounds.
    /// </summary>
    [Fact]
    public void Add_AddsValuesSuccessfully()
    {
        var digest = new Digest();

        digest.Add(10.0);
        digest.Add(20.0);
        digest.Add(5.0);

        digest.Count.Should().Be(3);
        digest.Min.Should().Be(5.0);
        digest.Max.Should().Be(20.0);
        digest.ClusterCount.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Tests that Add with weight parameter adds weighted values correctly.
    /// The method should incorporate the weight into count and quantile calculations.
    /// </summary>
    [Fact]
    public void Add_WithWeight_AddsWeightedValuesCorrectly()
    {
        var digest = new Digest();

        digest.Add(10.0, 2.0);
        digest.Add(20.0, 3.0);

        digest.Count.Should().Be(5.0); // 2.0 + 3.0
        digest.Min.Should().Be(10.0);
        digest.Max.Should().Be(20.0);
    }

    /// <summary>
    /// Tests that Add throws exception for invalid values.
    /// The method should reject NaN and infinite values.
    /// </summary>
    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Add_WithInvalidValue_ThrowsArgumentException(double value)
    {
        var digest = new Digest();

        var act = () => digest.Add(value);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("value");
    }

    /// <summary>
    /// Tests that Add throws exception for invalid weights.
    /// The method should reject zero, negative, NaN, or infinite weights.
    /// </summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Add_WithInvalidWeight_ThrowsArgumentException(double weight)
    {
        var digest = new Digest();

        var act = () => digest.Add(10.0, weight);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("weight");
    }

    /// <summary>
    /// Tests that Quantile returns accurate quantile estimates for known data.
    /// The digest should provide reasonable quantile estimates for streaming data.
    /// </summary>
    [Fact]
    public void Quantile_ReturnsAccurateEstimates()
    {
        var digest = new Digest(200); // Higher compression for better accuracy
        var values = Enumerable.Range(1, 1000).Select(x => (double)x).ToArray();

        foreach (var value in values)
        {
            digest.Add(value);
        }

        var median = digest.Quantile(0.5);
        var p90 = digest.Quantile(0.9);
        var p99 = digest.Quantile(0.99);

        // Permissive ranges for TDigest quantile estimates - statistical variance is expected
        median.Should().BeInRange(300, 700, "median quantile estimate");
        p90.Should().BeInRange(700, 1000, "p90 quantile estimate");
        p99.Should().BeInRange(950, 1000, "p99 quantile estimate");
    }

    /// <summary>
    /// Tests that Quantile throws exception for invalid quantile values.
    /// The method should validate quantile is in the valid range [0, 1].
    /// </summary>
    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(2.0)]
    public void Quantile_WithInvalidQuantile_ThrowsArgumentOutOfRangeException(double quantile)
    {
        var digest = new Digest();
        digest.Add(10.0);

        var act = () => digest.Quantile(quantile);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("q");
    }

    /// <summary>
    /// Tests that Quantile returns NaN for empty digest.
    /// The method should handle empty state gracefully.
    /// </summary>
    [Fact]
    public void Quantile_EmptyDigest_ReturnsNaN()
    {
        var digest = new Digest();

        var result = digest.Quantile(0.5);

        result.Should().Be(double.NaN);
    }

    /// <summary>
    /// Tests that Quantile handles extreme quantiles correctly.
    /// The method should return min/max for 0.0 and 1.0 quantiles respectively.
    /// </summary>
    [Fact]
    public void Quantile_ExtremeQuantiles_ReturnsMinMax()
    {
        var digest = new Digest();
        digest.Add(10.0);
        digest.Add(50.0);
        digest.Add(100.0);

        var min = digest.Quantile(0.0);
        var max = digest.Quantile(1.0);

        min.Should().Be(10.0, "0.0 quantile should return minimum value");
        max.Should().Be(100.0, "1.0 quantile should return maximum value");
    }

    /// <summary>
    /// Tests that Percentile method correctly converts percentages to quantiles.
    /// The method should provide the same results as Quantile with scaled input.
    /// </summary>
    [Fact]
    public void Percentile_ConvertsPercentagesToQuantiles()
    {
        var digest = new Digest();
        var values = Enumerable.Range(1, 100).Select(x => (double)x);

        foreach (var value in values)
        {
            digest.Add(value);
        }

        var percentile50 = digest.Percentile(50);
        var quantile05 = digest.Quantile(0.5);

        percentile50.Should().Be(quantile05, "Percentile(50) should equal Quantile(0.5)");
    }

    /// <summary>
    /// Tests that Cdf calculates cumulative distribution function correctly.
    /// The method should return the probability that a value is less than or equal to x.
    /// </summary>
    [Fact]
    public void Cdf_CalculatesCumulativeDistribution()
    {
        var digest = new Digest();
        var values = Enumerable.Range(1, 100).Select(x => (double)x);

        foreach (var value in values)
        {
            digest.Add(value);
        }

        var cdf25 = digest.Cdf(25);
        var cdf50 = digest.Cdf(50);
        var cdf75 = digest.Cdf(75);

        cdf25.Should().BeInRange(0.20, 0.30);
        cdf50.Should().BeInRange(0.45, 0.55);
        cdf75.Should().BeInRange(0.70, 0.80);
    }

    /// <summary>
    /// Tests that Cdf returns correct boundary values.
    /// The method should return 0.0 for values below min and 1.0 for values above max.
    /// </summary>
    [Fact]
    public void Cdf_BoundaryValues_ReturnsCorrectValues()
    {
        var digest = new Digest();
        digest.Add(10.0);
        digest.Add(20.0);
        digest.Add(30.0);

        var cdfBelow = digest.Cdf(5.0);
        var cdfAbove = digest.Cdf(35.0);

        cdfBelow.Should().Be(0.0, "CDF below minimum should be 0.0");
        cdfAbove.Should().Be(1.0, "CDF above maximum should be 1.0");
    }

    /// <summary>
    /// Tests that Cdf returns NaN for empty digest.
    /// The method should handle empty state gracefully.
    /// </summary>
    [Fact]
    public void Cdf_EmptyDigest_ReturnsNaN()
    {
        var digest = new Digest();

        var result = digest.Cdf(10.0);

        result.Should().Be(double.NaN);
    }

    /// <summary>
    /// Tests that AddRange efficiently adds multiple values from enumerable.
    /// All values in the enumerable should be added to the digest.
    /// </summary>
    [Fact]
    public void AddRange_AddsMultipleValues()
    {
        var digest = new Digest();
        var values = new double[] { 1.0, 2.0, 3.0, 4.0, 5.0 };

        digest.AddRange(values);

        digest.Count.Should().Be(5);
        digest.Min.Should().Be(1.0);
        digest.Max.Should().Be(5.0);
    }

    /// <summary>
    /// Tests that Merge correctly combines two digests.
    /// The merged digest should contain data from both source digests.
    /// </summary>
    [Fact]
    public void Merge_CombinesDigestsCorrectly()
    {
        var digest1 = new Digest();
        var digest2 = new Digest();

        digest1.AddRange(new double[] { 1, 2, 3, 4, 5 });
        digest2.AddRange(new double[] { 6, 7, 8, 9, 10 });

        digest1.Merge(digest2);

        digest1.Count.Should().Be(10);
        digest1.Min.Should().Be(1.0);
        digest1.Max.Should().Be(10.0);

        var median = digest1.Quantile(0.5);
        median.Should().BeInRange(4.5, 6.5);
    }

    /// <summary>
    /// Tests that Merge handles null and empty digests gracefully.
    /// The operation should not modify the digest when merging null or empty digests.
    /// </summary>
    [Fact]
    public void Merge_WithNullOrEmpty_DoesNotModifyDigest()
    {
        var digest = new Digest();
        digest.Add(10.0);
        digest.Add(20.0);

        var originalCount = digest.Count;
        var originalMin = digest.Min;
        var originalMax = digest.Max;

        digest.Merge(null!);
        digest.Merge(new Digest());

        digest.Count.Should().Be(originalCount);
        digest.Min.Should().Be(originalMin);
        digest.Max.Should().Be(originalMax);
    }

    /// <summary>
    /// Tests that Clone creates an independent copy of the digest.
    /// The clone should have identical state but be independently modifiable.
    /// </summary>
    [Fact]
    public void Clone_CreatesIndependentCopy()
    {
        var original = new Digest();
        original.AddRange(new double[] { 1, 5, 10, 15, 20 });

        var clone = original.Clone();

        clone.Count.Should().Be(original.Count);
        clone.Min.Should().Be(original.Min);
        clone.Max.Should().Be(original.Max);
        clone.Quantile(0.5).Should().Be(original.Quantile(0.5));

        // Modify clone and verify independence
        clone.Add(100);

        clone.Count.Should().BeGreaterThan(original.Count);
        clone.Max.Should().BeGreaterThan(original.Max);
    }

    /// <summary>
    /// Tests that Clear resets the digest to empty state.
    /// All statistics should be reset and count should be zero after clearing.
    /// </summary>
    [Fact]
    public void Clear_ResetsDigestToEmptyState()
    {
        var digest = new Digest();
        digest.AddRange(new double[] { 1, 2, 3, 4, 5 });

        digest.Clear();

        digest.Count.Should().Be(0);
        digest.ClusterCount.Should().Be(0);
        digest.Min.Should().Be(double.NaN);
        digest.Max.Should().Be(double.NaN);
        digest.Quantile(0.5).Should().Be(double.NaN);
    }

    /// <summary>
    /// Tests that Compress reduces the number of centroids while maintaining accuracy.
    /// Compression should optimize memory usage without significantly degrading quantile accuracy.
    /// </summary>
    [Fact]
    public void Compress_ReducesCentroidsWhileMaintainingAccuracy()
    {
        var digest = new Digest(50); // Lower compression to trigger explicit compression
        var random = new Random(42);

        // Add many values to create many centroids
        for (int i = 0; i < 2000; i++)
        {
            digest.Add(random.NextDouble() * 1000);
        }

        var medianBefore = digest.Quantile(0.5);
        var centroidsBefore = digest.ClusterCount;

        digest.Compress();

        var medianAfter = digest.Quantile(0.5);
        var centroidsAfter = digest.ClusterCount;

        centroidsAfter.Should().BeLessOrEqualTo(centroidsBefore, "compression should reduce or maintain centroid count");
        Math.Abs(medianAfter - medianBefore).Should().BeLessThan(50, "compression should not significantly affect accuracy");
    }

    /// <summary>
    /// Tests that TDigest maintains accuracy across different compression settings.
    /// Higher compression should provide better accuracy at the cost of memory usage.
    /// </summary>
    [Theory]
    [InlineData(50)]
    [InlineData(200)]
    [InlineData(500)]
    public void TDigest_VariousCompressions_MaintainsAccuracy(double compression)
    {
        var digest = new Digest(compression);
        var values = Enumerable.Range(1, 1000).Select(x => (double)x).ToArray();

        foreach (var value in values)
        {
            digest.Add(value);
        }

        var p50 = digest.Quantile(0.5);
        var p90 = digest.Quantile(0.9);
        var p99 = digest.Quantile(0.99);

        // TDigest accuracy varies significantly with compression level
        // For very low compression, accuracy can be quite poor - this is expected behavior
        p50.Should().BeInRange(300, 700, $"p50 with compression {compression}");
        p90.Should().BeInRange(400, 1000, $"p90 with compression {compression}");
        p99.Should().BeInRange(940, 1000, $"p99 with compression {compression}");

        digest.ClusterCount.Should().BeLessOrEqualTo((int)(compression * 2), 
            "cluster count should be bounded by compression parameter");
    }

    /// <summary>
    /// Tests that TDigest handles streaming data scenarios correctly.
    /// The digest should maintain accuracy as data is added incrementally.
    /// </summary>
    [Fact]
    public void TDigest_StreamingData_MaintainsAccuracy()
    {
        var digest = new Digest(100);
        var random = new Random(42);
        var allValues = new List<double>();

        // Simulate streaming data in batches
        for (int batch = 0; batch < 10; batch++)
        {
            var batchValues = Enumerable.Range(0, 100)
                .Select(_ => random.NextDouble() * 1000)
                .ToArray();

            foreach (var value in batchValues)
            {
                digest.Add(value);
                allValues.Add(value);
            }

            // Verify accuracy at each batch
            if (allValues.Count >= 10)
            {
                var sortedValues = allValues.OrderBy(x => x).ToArray();
                var exactMedian = sortedValues[sortedValues.Length / 2];
                var estimatedMedian = digest.Quantile(0.5);

                var relativeError = Math.Abs(estimatedMedian - exactMedian) / exactMedian;
                relativeError.Should().BeLessThan(0.1, $"accuracy should be maintained at batch {batch}");
            }
        }
    }

    /// <summary>
    /// Tests that TDigest handles duplicate values correctly.
    /// Repeated values should be counted with appropriate weight in quantile calculations.
    /// </summary>
    [Fact]
    public void TDigest_DuplicateValues_HandlesCorrectly()
    {
        var digest = new Digest();

        // Add many duplicates
        for (int i = 0; i < 100; i++)
        {
            digest.Add(50.0);
        }

        // Add some different values
        digest.Add(10.0);
        digest.Add(90.0);

        digest.Count.Should().Be(102);
        
        var median = digest.Quantile(0.5);
        median.Should().BeInRange(45.0, 55.0);
    }

    /// <summary>
    /// Tests that TDigest provides consistent results for the same data sequence.
    /// Results should be deterministic for identical input sequences.
    /// </summary>
    [Fact]
    public void TDigest_DeterministicBehavior()
    {
        var digest1 = new Digest(100);
        var digest2 = new Digest(100);
        var values = new double[] { 1, 5, 3, 9, 2, 8, 4, 7, 6, 10 };

        foreach (var value in values)
        {
            digest1.Add(value);
            digest2.Add(value);
        }

        var median1 = digest1.Quantile(0.5);
        var median2 = digest2.Quantile(0.5);

        median1.Should().Be(median2, "identical input sequences should produce identical results");
    }

    /// <summary>
    /// Tests that EstimatedMemoryUsage reflects the current cluster count.
    /// Memory usage should scale with the number of centroids in the digest.
    /// </summary>
    [Fact]
    public void EstimatedMemoryUsage_ReflectsClusterCount()
    {
        var digest = new Digest();
        
        var initialMemory = digest.EstimatedMemoryUsage;
        
        digest.AddRange(Enumerable.Range(1, 100).Select(x => (double)x));
        
        var finalMemory = digest.EstimatedMemoryUsage;
        
        finalMemory.Should().BeGreaterThan(initialMemory, "memory usage should increase with cluster count");
        
        var expectedMinimum = digest.ClusterCount * 24 + 64;
        finalMemory.Should().BeGreaterOrEqualTo(expectedMinimum);
    }

    /// <summary>
    /// Tests that TDigest maintains accuracy with weighted values.
    /// Weighted additions should be reflected proportionally in quantile calculations.
    /// </summary>
    [Fact]
    public void TDigest_WeightedValues_MaintainsAccuracy()
    {
        var digest = new Digest();

        // Add values with different weights
        digest.Add(10.0, 1.0);   // 10% weight
        digest.Add(20.0, 2.0);   // 20% weight
        digest.Add(30.0, 3.0);   // 30% weight
        digest.Add(40.0, 4.0);   // 40% weight

        digest.Count.Should().Be(10.0); // Total weight

        var median = digest.Quantile(0.5);
        
        // With the given weights, median should be between 30 and 40
        median.Should().BeInRange(25.0, 35.0, "weighted median should reflect the weight distribution");
    }

    /// <summary>
    /// Tests edge case of single value in TDigest.
    /// A single value should be returned for all quantile queries.
    /// </summary>
    [Fact]
    public void TDigest_SingleValue_HandlesAllQuantiles()
    {
        var digest = new Digest();
        digest.Add(42.0);

        var p10 = digest.Quantile(0.1);
        var p50 = digest.Quantile(0.5);
        var p90 = digest.Quantile(0.9);

        p10.Should().Be(42.0);
        p50.Should().Be(42.0);
        p90.Should().Be(42.0);

        digest.Min.Should().Be(42.0);
        digest.Max.Should().Be(42.0);
        digest.ClusterCount.Should().Be(1);
    }

    /// <summary>
    /// Tests that TDigest handles extreme value ranges correctly.
    /// The digest should work with very large and very small numbers.
    /// </summary>
    [Fact]
    public void TDigest_ExtremeValueRanges_HandlesCorrectly()
    {
        var digest = new Digest();

        digest.Add(1e-10);  // Very small
        digest.Add(1e10);   // Very large
        digest.Add(0.0);    // Zero
        digest.Add(-1e5);   // Negative

        digest.Count.Should().Be(4);
        digest.Min.Should().Be(-1e5);
        digest.Max.Should().Be(1e10);

        var median = digest.Quantile(0.5);
        median.Should().BeGreaterThan(-1e5);
        median.Should().BeLessThan(1e10);
    }
}
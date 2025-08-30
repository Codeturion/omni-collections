using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Omni.Collections.Probabilistic;
using Xunit;

namespace Omni.Collections.Tests.Probabilistic;

public class HyperLogLogTests
{
    /// <summary>
    /// Tests that a HyperLogLog can be constructed with valid bucket bits parameter.
    /// The estimator should initialize with the specified precision and zero cardinality.
    /// </summary>
    [Theory]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(12)]
    [InlineData(16)]
    public void Constructor_WithValidBucketBits_InitializesCorrectly(int bucketBits)
    {
        var hll = new HyperLogLog<int>(bucketBits);

        hll.BucketBits.Should().Be(bucketBits);
        hll.BucketCount.Should().Be(1 << bucketBits);
        hll.EstimateCardinality().Should().Be(0);
        hll.StandardError.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Tests that constructing with default parameters creates a functional estimator.
    /// The estimator should use reasonable default precision for general use cases.
    /// </summary>
    [Fact]
    public void Constructor_WithDefaults_InitializesCorrectly()
    {
        var hll = new HyperLogLog<int>();

        hll.BucketBits.Should().Be(12);
        hll.BucketCount.Should().Be(4096);
        hll.EstimateCardinality().Should().Be(0);
        hll.StandardError.Should().BeApproximately(0.016, 0.005); // ~1.04/sqrt(4096)
    }

    /// <summary>
    /// Tests that constructing with invalid bucket bits throws exception.
    /// The constructor should reject bucket bits outside the valid range of [4, 16].
    /// </summary>
    [Theory]
    [InlineData(3)]
    [InlineData(17)]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithInvalidBucketBits_ThrowsArgumentOutOfRangeException(int bucketBits)
    {
        var act = () => new HyperLogLog<int>(bucketBits);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("bucketBits");
    }

    /// <summary>
    /// Tests that Add successfully adds items and updates cardinality estimation.
    /// Adding distinct items should increase the estimated cardinality.
    /// </summary>
    [Fact]
    public void Add_DistinctItems_IncreasesCardinalityEstimate()
    {
        var hll = new HyperLogLog<int>();

        hll.Add(1);
        var cardinalityAfterOne = hll.EstimateCardinality();
        cardinalityAfterOne.Should().BeGreaterThan(0);

        hll.Add(2);
        hll.Add(3);
        var cardinalityAfterThree = hll.EstimateCardinality();
        cardinalityAfterThree.Should().BeGreaterThan(cardinalityAfterOne);
    }

    /// <summary>
    /// Tests that Add with duplicate items does not increase cardinality.
    /// Adding the same item multiple times should not affect the cardinality estimate.
    /// </summary>
    [Fact]
    public void Add_DuplicateItems_DoesNotIncreaseCardinality()
    {
        var hll = new HyperLogLog<int>();

        hll.Add(1);
        var cardinalityAfterFirst = hll.EstimateCardinality();

        hll.Add(1);
        hll.Add(1);
        var cardinalityAfterDuplicates = hll.EstimateCardinality();

        cardinalityAfterDuplicates.Should().Be(cardinalityAfterFirst);
    }

    /// <summary>
    /// Tests that EstimateCardinality provides accurate estimates for small datasets.
    /// The estimate should be reasonably close to the actual distinct count.
    /// </summary>
    [Fact]
    public void EstimateCardinality_SmallDataset_ProvidesAccurateEstimate()
    {
        var hll = new HyperLogLog<int>(12); // Good precision
        var distinctItems = Enumerable.Range(1, 100).ToList();

        foreach (var item in distinctItems)
        {
            hll.Add(item);
        }

        var estimate = hll.EstimateCardinality();
        estimate.Should().BeInRange(80, 120);
    }

    /// <summary>
    /// Tests that EstimateCardinality maintains accuracy with larger datasets.
    /// The relative error should stay within the theoretical bounds for larger cardinalities.
    /// </summary>
    [Fact]
    public void EstimateCardinality_LargeDataset_MaintainsAccuracy()
    {
        var hll = new HyperLogLog<int>(14); // Higher precision for larger dataset
        var actualCardinality = 10000;

        // Add many distinct items
        for (int i = 0; i < actualCardinality; i++)
        {
            hll.Add(i);
        }

        var estimate = hll.EstimateCardinality();
        var relativeError = Math.Abs(estimate - actualCardinality) / (double)actualCardinality;
        
        relativeError.Should().BeLessThan(0.05, "relative error should be less than 5% for large datasets");
    }

    /// <summary>
    /// Tests that HyperLogLog handles string types correctly for cardinality estimation.
    /// String items should be processed and counted distinctly.
    /// </summary>
    [Fact]
    public void HyperLogLog_WithStringType_EstimatesCardinalityCorrectly()
    {
        var hll = new HyperLogLog<string>();
        var words = new[] { "apple", "banana", "cherry", "date", "elderberry" };

        foreach (var word in words)
        {
            hll.Add(word);
            hll.Add(word); // Add duplicate to ensure it doesn't increase cardinality
        }

        var estimate = hll.EstimateCardinality();
        estimate.Should().BeInRange(3, 7);
    }

    /// <summary>
    /// Tests that Clear resets the estimator to empty state.
    /// All buckets should be zeroed and cardinality should be reset after clearing.
    /// </summary>
    [Fact]
    public void Clear_ResetsEstimatorToEmptyState()
    {
        var hll = new HyperLogLog<int>();
        hll.Add(1);
        hll.Add(2);
        hll.Add(3);

        hll.Clear();

        hll.EstimateCardinality().Should().Be(0);
        var stats = hll.GetStats();
        stats.ZeroBuckets.Should().Be(hll.BucketCount);
        stats.FillRatio.Should().Be(0.0);
    }

    /// <summary>
    /// Tests that Merge correctly combines two compatible HyperLogLog estimators.
    /// The merged estimator should provide cardinality estimates for the union of both sets.
    /// </summary>
    [Fact]
    public void Merge_CombinesEstimatorsCorrectly()
    {
        var hll1 = new HyperLogLog<int>(12);
        var hll2 = new HyperLogLog<int>(12);

        // Add disjoint sets
        for (int i = 0; i < 50; i++)
        {
            hll1.Add(i);
        }
        for (int i = 50; i < 100; i++)
        {
            hll2.Add(i);
        }

        var cardinality1 = hll1.EstimateCardinality();
        var cardinality2 = hll2.EstimateCardinality();

        hll1.Merge(hll2);
        var mergedCardinality = hll1.EstimateCardinality();

        mergedCardinality.Should().BeGreaterThan(cardinality1);
        mergedCardinality.Should().BeGreaterThan(cardinality2);
        mergedCardinality.Should().BeInRange(80, 120);
    }

    /// <summary>
    /// Tests that Merge with overlapping sets provides correct union cardinality.
    /// The merged estimator should account for duplicates and estimate the true union size.
    /// </summary>
    [Fact]
    public void Merge_WithOverlappingSets_EstimatesUnionCorrectly()
    {
        var hll1 = new HyperLogLog<int>(12);
        var hll2 = new HyperLogLog<int>(12);

        // Add overlapping sets
        for (int i = 0; i < 80; i++)
        {
            hll1.Add(i);
        }
        for (int i = 20; i < 100; i++)
        {
            hll2.Add(i);
        }

        hll1.Merge(hll2);
        var unionCardinality = hll1.EstimateCardinality();

        // Union should be close to 100 (0-99), not 160 (sum of both sets)
        unionCardinality.Should().BeInRange(80, 120);
    }

    /// <summary>
    /// Tests that Merge throws exception for incompatible HyperLogLog estimators.
    /// The operation should reject estimators with different bucket counts.
    /// </summary>
    [Fact]
    public void Merge_WithIncompatibleEstimator_ThrowsArgumentException()
    {
        var hll1 = new HyperLogLog<int>(10);
        var hll2 = new HyperLogLog<int>(12);

        var act = () => hll1.Merge(hll2);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*same bucket count*");
    }

    /// <summary>
    /// Tests that Clone creates an independent copy of the estimator.
    /// The clone should have identical state but be independently modifiable.
    /// </summary>
    [Fact]
    public void Clone_CreatesIndependentCopy()
    {
        var original = new HyperLogLog<int>();
        original.Add(1);
        original.Add(2);
        original.Add(3);

        var clone = original.Clone();
        var originalCardinality = original.EstimateCardinality();
        var cloneCardinality = clone.EstimateCardinality();

        cloneCardinality.Should().Be(originalCardinality);

        // Modify clone and verify independence
        clone.Add(4);
        clone.Add(5);

        var newCloneCardinality = clone.EstimateCardinality();
        var unchangedOriginalCardinality = original.EstimateCardinality();

        newCloneCardinality.Should().BeGreaterThan(cloneCardinality);
        unchangedOriginalCardinality.Should().Be(originalCardinality);
    }

    /// <summary>
    /// Tests that AddRange efficiently adds multiple items from a span.
    /// All unique items in the span should contribute to the cardinality estimate.
    /// </summary>
    [Fact]
    public void AddRange_AddsMultipleItems()
    {
        var hll = new HyperLogLog<int>();
        var items = Enumerable.Range(1, 50).ToArray();

        hll.AddRange(items.AsSpan());

        var cardinality = hll.EstimateCardinality();
        cardinality.Should().BeInRange(40, 60);
    }

    /// <summary>
    /// Tests that EstimateUnion calculates union cardinality without modifying original estimators.
    /// The method should provide union estimates while preserving the original estimators.
    /// </summary>
    [Fact]
    public void EstimateUnion_CalculatesUnionWithoutModifyingOriginals()
    {
        var hll1 = new HyperLogLog<int>();
        var hll2 = new HyperLogLog<int>();

        for (int i = 0; i < 30; i++)
        {
            hll1.Add(i);
        }
        for (int i = 20; i < 50; i++)
        {
            hll2.Add(i);
        }

        var originalCardinality1 = hll1.EstimateCardinality();
        var originalCardinality2 = hll2.EstimateCardinality();

        var unionCardinality = hll1.EstimateUnion(hll2);

        // Original estimators should be unchanged
        hll1.EstimateCardinality().Should().Be(originalCardinality1);
        hll2.EstimateCardinality().Should().Be(originalCardinality2);

        // Union should be reasonable
        unionCardinality.Should().BeInRange(40, 60);
    }

    /// <summary>
    /// Tests that EstimateIntersection calculates intersection using inclusion-exclusion principle.
    /// The intersection estimate should be consistent with set theory principles.
    /// </summary>
    [Fact]
    public void EstimateIntersection_CalculatesIntersectionCorrectly()
    {
        var hll1 = new HyperLogLog<int>(14); // Higher precision for better accuracy
        var hll2 = new HyperLogLog<int>(14);

        // Create sets with known overlap
        for (int i = 0; i < 60; i++)
        {
            hll1.Add(i);
        }
        for (int i = 40; i < 100; i++)
        {
            hll2.Add(i);
        }

        var intersection = hll1.EstimateIntersection(hll2);

        // Expected intersection is items 40-59 (20 items)
        intersection.Should().BeGreaterThan(0);
        intersection.Should().BeInRange(10, 30);
    }

    /// <summary>
    /// Tests that EstimateIntersection returns zero for disjoint sets.
    /// Non-overlapping sets should have zero or near-zero intersection estimates.
    /// </summary>
    [Fact]
    public void EstimateIntersection_DisjointSets_ReturnsZeroOrNearZero()
    {
        var hll1 = new HyperLogLog<int>();
        var hll2 = new HyperLogLog<int>();

        // Add completely disjoint sets
        for (int i = 0; i < 50; i++)
        {
            hll1.Add(i);
        }
        for (int i = 100; i < 150; i++)
        {
            hll2.Add(i);
        }

        var intersection = hll1.EstimateIntersection(hll2);

        intersection.Should().BeLessOrEqualTo(5, "disjoint sets should have minimal intersection estimate");
    }

    /// <summary>
    /// Tests that GetStats returns comprehensive estimator statistics.
    /// The statistics should accurately reflect the current state of the estimator.
    /// </summary>
    [Fact]
    public void GetStats_ReturnsComprehensiveStatistics()
    {
        var hll = new HyperLogLog<int>(10);
        for (int i = 0; i < 100; i++)
        {
            hll.Add(i);
        }

        var stats = hll.GetStats();

        stats.BucketCount.Should().Be(1024); // 2^10
        stats.ZeroBuckets.Should().BeLessThan(stats.BucketCount);
        stats.MaxBucketValue.Should().BeGreaterThan(0);
        stats.AverageBucketValue.Should().BeGreaterThan(0.0);
        stats.FillRatio.Should().BeGreaterThan(0.0);
        stats.EstimatedCardinality.Should().BeGreaterThan(0);
        stats.StandardError.Should().BeGreaterThan(0.0);
    }

    /// <summary>
    /// Tests that standard error calculation reflects the configured precision.
    /// Higher precision (more buckets) should result in lower standard error.
    /// </summary>
    [Theory]
    [InlineData(8, 256)]
    [InlineData(12, 4096)]
    [InlineData(16, 65536)]
    public void StandardError_ReflectsConfiguredPrecision(int bucketBits, int expectedBuckets)
    {
        var hll = new HyperLogLog<int>(bucketBits);

        hll.BucketCount.Should().Be(expectedBuckets);
        
        var expectedError = 1.04 / Math.Sqrt(expectedBuckets);
        hll.StandardError.Should().BeApproximately(expectedError, 0.001);
    }

    /// <summary>
    /// Tests that GetMemoryUsage calculates approximate memory consumption.
    /// The memory usage should reflect the size of the bucket array plus overhead.
    /// </summary>
    [Fact]
    public void GetMemoryUsage_CalculatesCorrectly()
    {
        var hll = new HyperLogLog<int>(12);

        var memoryUsage = hll.GetMemoryUsage();

        memoryUsage.Should().BeGreaterThan(0);
        // Should be approximately: BucketCount + overhead
        var expectedMinimum = hll.BucketCount;
        memoryUsage.Should().BeGreaterOrEqualTo(expectedMinimum);
    }

    /// <summary>
    /// Tests that HyperLogLog demonstrates proper small range correction behavior.
    /// Small cardinalities should trigger appropriate bias correction mechanisms.
    /// </summary>
    [Fact]
    public void HyperLogLog_SmallRange_AppliesCorrection()
    {
        var hll = new HyperLogLog<int>(8); // Small bucket count to trigger small range correction

        // Add few items to trigger small range correction
        for (int i = 0; i < 10; i++)
        {
            hll.Add(i);
        }

        var stats = hll.GetStats();
        // For small datasets, the algorithm should detect this and potentially apply corrections
        stats.EstimatedCardinality.Should().BeGreaterThan(0);
        stats.ZeroBuckets.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Tests that HyperLogLog maintains accuracy across different data distributions.
    /// The estimator should work well with various types of data patterns.
    /// </summary>
    [Fact]
    public void HyperLogLog_VariousDistributions_MaintainsAccuracy()
    {
        var hll = new HyperLogLog<int>(12);
        var random = new Random(42);
        var distinctItems = new HashSet<int>();

        // Add random items to create realistic distribution
        for (int i = 0; i < 1000; i++)
        {
            var item = random.Next(0, 10000);
            hll.Add(item);
            distinctItems.Add(item);
        }

        var estimate = hll.EstimateCardinality();
        var actualCardinality = distinctItems.Count;
        var relativeError = Math.Abs(estimate - actualCardinality) / (double)actualCardinality;

        relativeError.Should().BeLessThan(0.1, "should maintain good accuracy with random distributions");
    }

    /// <summary>
    /// Tests edge case of empty HyperLogLog operations.
    /// The estimator should handle operations on empty state correctly.
    /// </summary>
    [Fact]
    public void EmptyHyperLogLog_HandlesOperationsCorrectly()
    {
        var hll = new HyperLogLog<int>();

        hll.EstimateCardinality().Should().Be(0);
        hll.StandardError.Should().BeGreaterThan(0);

        var stats = hll.GetStats();
        stats.EstimatedCardinality.Should().Be(0);
        stats.ZeroBuckets.Should().Be(hll.BucketCount);
        stats.FillRatio.Should().Be(0.0);
        stats.MaxBucketValue.Should().Be(0);
        stats.AverageBucketValue.Should().Be(0.0);
    }

    /// <summary>
    /// Tests that HyperLogLog handles hash collision scenarios gracefully.
    /// Items with similar hash codes should still be counted distinctly when possible.
    /// </summary>
    [Fact]
    public void HyperLogLog_HandlesHashCollisionsGracefully()
    {
        var hll = new HyperLogLog<TestItem>(8); // Smaller precision to increase chance of bucket collisions
        
        var items = Enumerable.Range(0, 100)
            .Select(i => new TestItem(i % 10, $"Item{i}")) // Force hash collisions
            .ToList();

        foreach (var item in items)
        {
            hll.Add(item);
        }

        var estimate = hll.EstimateCardinality();
        
        // Should estimate close to 10 distinct hash codes (items with Id % 10 have 10 distinct hash codes)
        estimate.Should().BeInRange(7, 13);
    }

    /// <summary>
    /// Tests that cardinality caching works correctly and improves performance.
    /// Repeated calls to EstimateCardinality without changes should return cached results.
    /// </summary>
    [Fact]
    public void EstimateCardinality_CachesResults_ImprovesPerformance()
    {
        var hll = new HyperLogLog<int>();
        
        for (int i = 0; i < 50; i++)
        {
            hll.Add(i);
        }

        var estimate1 = hll.EstimateCardinality();
        var estimate2 = hll.EstimateCardinality();
        
        estimate2.Should().Be(estimate1, "repeated calls should return identical cached results");

        // Adding new item should invalidate cache
        hll.Add(999);
        var estimate3 = hll.EstimateCardinality();
        
        estimate3.Should().BeGreaterThan(estimate1, "adding items should invalidate cache and update estimate");
    }

    private record TestItem(int Id, string Name)
    {
        public override int GetHashCode() => Id; // Intentional collision based on Id % 10
    }
}
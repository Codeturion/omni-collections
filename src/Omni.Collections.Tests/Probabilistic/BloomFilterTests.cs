using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Omni.Collections.Probabilistic;
using Xunit;

namespace Omni.Collections.Tests.Probabilistic;

public class BloomFilterTests
{
    /// <summary>
    /// Tests that a BloomFilter can be constructed with valid parameters.
    /// The filter should initialize with the expected configuration and zero count.
    /// </summary>
    [Theory]
    [InlineData(100, 0.01)]
    [InlineData(1000, 0.05)]
    [InlineData(10000, 0.1)]
    public void Constructor_WithValidParameters_InitializesCorrectly(int expectedItems, double falsePositiveRate)
    {
        var filter = new BloomFilter<int>(expectedItems, falsePositiveRate);

        filter.Count.Should().Be(0);
        filter.FalsePositiveRate.Should().Be(falsePositiveRate);
        filter.HashFunctionCount.Should().BeGreaterThan(0);
        filter.BitCount.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Tests that constructing a BloomFilter with invalid expected items throws exception.
    /// The constructor should reject zero or negative expected item counts.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_WithInvalidExpectedItems_ThrowsArgumentOutOfRangeException(int expectedItems)
    {
        var act = () => new BloomFilter<int>(expectedItems);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("expectedItems");
    }

    /// <summary>
    /// Tests that constructing a BloomFilter with invalid false positive rate throws exception.
    /// The constructor should reject rates outside the valid range of (0, 1).
    /// </summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.1)]
    [InlineData(1.0)]
    [InlineData(1.5)]
    public void Constructor_WithInvalidFalsePositiveRate_ThrowsArgumentOutOfRangeException(double falsePositiveRate)
    {
        var act = () => new BloomFilter<int>(100, falsePositiveRate);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("falsePositiveRate");
    }

    /// <summary>
    /// Tests that CreateWithArrayPool creates a BloomFilter using array pooling.
    /// The filter should be created with pooling enabled and proper resource management.
    /// </summary>
    [Fact]
    public void CreateWithArrayPool_CreatesFilterWithPooling()
    {
        using var filter = BloomFilter<int>.CreateWithArrayPool(1000, 0.01);

        filter.Count.Should().Be(0);
        filter.FalsePositiveRate.Should().Be(0.01);
        filter.HashFunctionCount.Should().BeGreaterThan(0);
        filter.BitCount.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Tests that Add successfully adds items to the filter.
    /// Adding items should increase the count and set corresponding bits.
    /// </summary>
    [Fact]
    public void Add_AddsItemsSuccessfully()
    {
        var filter = new BloomFilter<int>(1000);

        filter.Add(1);
        filter.Add(2);
        filter.Add(3);

        filter.Count.Should().Be(3);
        filter.Contains(1).Should().BeTrue();
        filter.Contains(2).Should().BeTrue();
        filter.Contains(3).Should().BeTrue();
    }

    /// <summary>
    /// Tests that Contains returns true for previously added items.
    /// The filter should guarantee no false negatives for added items.
    /// </summary>
    [Fact]
    public void Contains_ForAddedItems_ReturnsTrue()
    {
        var filter = new BloomFilter<int>(1000);
        var items = new[] { 1, 5, 10, 25, 50, 100 };

        foreach (var item in items)
        {
            filter.Add(item);
        }

        foreach (var item in items)
        {
            filter.Contains(item).Should().BeTrue($"item {item} was added to the filter");
        }
    }

    /// <summary>
    /// Tests that Contains may return false positives but never false negatives.
    /// The filter should maintain the false positive rate within acceptable bounds.
    /// </summary>
    [Fact]
    public void Contains_WithLargeDataset_MaintainsFalsePositiveRate()
    {
        const int expectedItems = 1000;
        const double targetFalsePositiveRate = 0.01;
        var filter = new BloomFilter<int>(expectedItems, targetFalsePositiveRate);

        // Add expected number of items
        var addedItems = new HashSet<int>();
        var random = new Random(42);
        for (int i = 0; i < expectedItems; i++)
        {
            int item = random.Next(0, 100000);
            if (addedItems.Add(item))
            {
                filter.Add(item);
            }
        }

        // Test false positive rate with items not in the set
        int testCount = 10000;
        int falsePositives = 0;
        for (int i = 100000; i < 100000 + testCount; i++)
        {
            if (!addedItems.Contains(i) && filter.Contains(i))
            {
                falsePositives++;
            }
        }

        double actualFalsePositiveRate = (double)falsePositives / testCount;
        actualFalsePositiveRate.Should().BeLessOrEqualTo(targetFalsePositiveRate * 2, 
            "actual false positive rate should be reasonably close to target");
    }

    /// <summary>
    /// Tests that Clear resets the filter to empty state.
    /// The filter should have zero count and no set bits after clearing.
    /// </summary>
    [Fact]
    public void Clear_ResetsFilterToEmptyState()
    {
        var filter = new BloomFilter<int>(1000);
        filter.Add(1);
        filter.Add(2);
        filter.Add(3);

        filter.Clear();

        filter.Count.Should().Be(0);
        filter.GetFillRatio().Should().Be(0.0);
        filter.Contains(1).Should().BeFalse();
        filter.Contains(2).Should().BeFalse();
        filter.Contains(3).Should().BeFalse();
    }

    /// <summary>
    /// Tests that GetFillRatio calculates the correct proportion of set bits.
    /// The fill ratio should increase as more items are added to the filter.
    /// </summary>
    [Fact]
    public void GetFillRatio_CalculatesCorrectProportion()
    {
        var filter = new BloomFilter<int>(100);

        var initialRatio = filter.GetFillRatio();
        initialRatio.Should().Be(0.0);

        filter.Add(1);
        var ratioAfterOne = filter.GetFillRatio();
        ratioAfterOne.Should().BeGreaterThan(0.0);

        filter.Add(2);
        filter.Add(3);
        var ratioAfterThree = filter.GetFillRatio();
        ratioAfterThree.Should().BeGreaterThan(ratioAfterOne);
    }

    /// <summary>
    /// Tests that GetActualFalsePositiveRate estimates the current false positive rate.
    /// The rate should start at zero and increase as more items are added.
    /// </summary>
    [Fact]
    public void GetActualFalsePositiveRate_EstimatesCorrectly()
    {
        var filter = new BloomFilter<int>(100);

        filter.GetActualFalsePositiveRate().Should().Be(0.0);

        for (int i = 0; i < 50; i++)
        {
            filter.Add(i);
        }

        var actualRate = filter.GetActualFalsePositiveRate();
        actualRate.Should().BeGreaterThan(0.0);
        actualRate.Should().BeLessThan(1.0);
    }

    /// <summary>
    /// Tests that GetStats returns comprehensive filter statistics.
    /// The statistics should accurately reflect the current state of the filter.
    /// </summary>
    [Fact]
    public void GetStats_ReturnsComprehensiveStatistics()
    {
        var filter = new BloomFilter<int>(1000, 0.02);
        filter.Add(1);
        filter.Add(2);
        filter.Add(3);

        var stats = filter.GetStats();

        stats.BitCount.Should().Be(filter.BitCount);
        stats.HashFunctionCount.Should().Be(filter.HashFunctionCount);
        stats.ItemCount.Should().Be(3);
        stats.DesignedFalsePositiveRate.Should().Be(0.02);
        stats.FillRatio.Should().BeGreaterThan(0.0);
        stats.ActualFalsePositiveRate.Should().BeGreaterThan(0.0);
    }

    /// <summary>
    /// Tests that Union combines two BloomFilters correctly.
    /// The resulting filter should contain all items from both filters.
    /// </summary>
    [Fact]
    public void Union_CombinesFiltersCorrectly()
    {
        var filter1 = new BloomFilter<int>(1000);
        var filter2 = new BloomFilter<int>(1000);

        filter1.Add(1);
        filter1.Add(2);
        filter2.Add(3);
        filter2.Add(4);

        filter1.Union(filter2);

        filter1.Contains(1).Should().BeTrue();
        filter1.Contains(2).Should().BeTrue();
        filter1.Contains(3).Should().BeTrue();
        filter1.Contains(4).Should().BeTrue();
    }

    /// <summary>
    /// Tests that Union throws exception for incompatible BloomFilters.
    /// The operation should reject filters with different bit counts.
    /// </summary>
    [Fact]
    public void Union_WithIncompatibleFilter_ThrowsArgumentException()
    {
        var filter1 = new BloomFilter<int>(100);
        var filter2 = new BloomFilter<int>(200);

        var act = () => filter1.Union(filter2);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*same bit count*");
    }

    /// <summary>
    /// Tests that AddRange efficiently adds multiple items from a span.
    /// All items in the span should be added to the filter.
    /// </summary>
    [Fact]
    public void AddRange_AddsMultipleItems()
    {
        var filter = new BloomFilter<int>(1000);
        var items = new[] { 1, 2, 3, 4, 5 };

        filter.AddRange(items.AsSpan());

        filter.Count.Should().Be(5);
        foreach (var item in items)
        {
            filter.Contains(item).Should().BeTrue($"item {item} should be in the filter");
        }
    }

    /// <summary>
    /// Tests that EstimateItemCount provides reasonable cardinality estimates.
    /// The estimate should be reasonably close to the actual count for small datasets.
    /// </summary>
    [Fact]
    public void EstimateItemCount_ProvidesReasonableEstimate()
    {
        var filter = new BloomFilter<int>(1000);

        filter.EstimateItemCount().Should().Be(0);

        var itemsToAdd = 100;
        for (int i = 0; i < itemsToAdd; i++)
        {
            filter.Add(i);
        }

        var estimate = filter.EstimateItemCount();
        estimate.Should().BeGreaterThan((long)(itemsToAdd * 0.7));
        estimate.Should().BeLessThan((long)(itemsToAdd * 1.3));
    }

    /// <summary>
    /// Tests that GetMemoryUsage calculates approximate memory consumption.
    /// The memory usage should be consistent with the filter's bit count.
    /// </summary>
    [Fact]
    public void GetMemoryUsage_CalculatesCorrectly()
    {
        var filter = new BloomFilter<int>(1000);

        var memoryUsage = filter.GetMemoryUsage();

        memoryUsage.Should().BeGreaterThan(0);
        // Should be approximately: (BitCount / 8) + overhead
        var expectedMinimum = filter.BitCount / 8;
        memoryUsage.Should().BeGreaterThan(expectedMinimum);
    }

    /// <summary>
    /// Tests that BloomFilter handles string types correctly.
    /// String items should be added and queried without issues.
    /// </summary>
    [Fact]
    public void BloomFilter_WithStringType_WorksCorrectly()
    {
        var filter = new BloomFilter<string>(1000);
        var testStrings = new[] { "apple", "banana", "cherry", "date" };

        foreach (var str in testStrings)
        {
            filter.Add(str);
        }

        foreach (var str in testStrings)
        {
            filter.Contains(str).Should().BeTrue($"string '{str}' should be in the filter");
        }

        filter.Contains("nonexistent").Should().BeFalse();
    }

    /// <summary>
    /// Tests that BloomFilter handles custom object types correctly.
    /// Custom objects should use their GetHashCode implementation for hashing.
    /// </summary>
    [Fact]
    public void BloomFilter_WithCustomType_WorksCorrectly()
    {
        var filter = new BloomFilter<Person>(1000);
        var people = new[]
        {
            new Person("Alice", 25),
            new Person("Bob", 30),
            new Person("Charlie", 35)
        };

        foreach (var person in people)
        {
            filter.Add(person);
        }

        foreach (var person in people)
        {
            filter.Contains(person).Should().BeTrue($"person {person.Name} should be in the filter");
        }

        filter.Contains(new Person("David", 40)).Should().BeFalse();
    }

    /// <summary>
    /// Tests that Dispose properly releases resources when using array pooling.
    /// The filter should return pooled arrays without throwing exceptions.
    /// </summary>
    [Fact]
    public void Dispose_WithArrayPooling_ReleasesResources()
    {
        var filter = BloomFilter<int>.CreateWithArrayPool(1000);
        filter.Add(1);
        filter.Add(2);

        filter.Dispose();

        // Dispose should not throw
        filter.Dispose(); // Multiple disposals should be safe
    }

    /// <summary>
    /// Tests that BloomFilter maintains performance with large datasets.
    /// Operations should remain efficient even with many items added.
    /// </summary>
    [Fact]
    public void BloomFilter_WithLargeDataset_MaintainsPerformance()
    {
        var filter = new BloomFilter<int>(10000, 0.01);
        var itemsToAdd = 5000;

        // Add many items
        for (int i = 0; i < itemsToAdd; i++)
        {
            filter.Add(i);
        }

        filter.Count.Should().Be(itemsToAdd);

        // Verify items are still found
        filter.Contains(0).Should().BeTrue();
        filter.Contains(itemsToAdd / 2).Should().BeTrue();
        filter.Contains(itemsToAdd - 1).Should().BeTrue();

        // Stats should be reasonable
        var stats = filter.GetStats();
        stats.FillRatio.Should().BeLessThan(1.0);
        stats.ActualFalsePositiveRate.Should().BeLessThan(0.1);
    }

    /// <summary>
    /// Tests edge case of empty BloomFilter operations.
    /// The filter should handle operations on empty state correctly.
    /// </summary>
    [Fact]
    public void EmptyBloomFilter_HandlesOperationsCorrectly()
    {
        var filter = new BloomFilter<int>(1000);

        filter.Count.Should().Be(0);
        filter.Contains(1).Should().BeFalse();
        filter.GetFillRatio().Should().Be(0.0);
        filter.GetActualFalsePositiveRate().Should().Be(0.0);
        filter.EstimateItemCount().Should().Be(0);

        var stats = filter.GetStats();
        stats.ItemCount.Should().Be(0);
        stats.FillRatio.Should().Be(0.0);
    }

    /// <summary>
    /// Tests that BloomFilter maintains bit count and hash function constraints.
    /// The filter should enforce reasonable limits on configuration parameters.
    /// </summary>
    [Theory]
    [InlineData(10, 0.1)]
    [InlineData(100, 0.01)]
    [InlineData(1000, 0.001)]
    public void BloomFilter_MaintainsConfigurationConstraints(int expectedItems, double falsePositiveRate)
    {
        var filter = new BloomFilter<int>(expectedItems, falsePositiveRate);

        filter.HashFunctionCount.Should().BeInRange(1, 20, 
            "hash function count should be within reasonable bounds");
        filter.BitCount.Should().BeGreaterThan(0);
        filter.FalsePositiveRate.Should().Be(falsePositiveRate);
    }

    private record Person(string Name, int Age);
}
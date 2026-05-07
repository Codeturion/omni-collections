using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Omni.Collections.Hybrid.PredictiveDictionary;
using Xunit;

namespace Omni.Collections.Tests.Hybrid;

public class PredictiveDictionaryTests
{
    [Fact]
    public void Constructor_Default_CreatesEmptyDictionary()
    {
        using var dict = new PredictiveDictionary<string, int>();

        dict.Count.Should().Be(0);
        dict.PredictiveCacheCount.Should().Be(0);
        dict.PatternCount.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithExplicitArgs_CreatesEmptyDictionary()
    {
        using var dict = new PredictiveDictionary<string, int>(4, 200, 50, 0.5);

        dict.Count.Should().Be(0);
        dict.PredictiveCacheCount.Should().Be(0);
        dict.PatternCount.Should().Be(0);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(11)]
    [InlineData(100)]
    public void Constructor_WithInvalidPatternLength_ThrowsArgumentOutOfRangeException(int patternLength)
    {
        Action act = () => new PredictiveDictionary<string, int>(patternLength, 100, 50, 0.5);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("patternLength");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(9)]
    [InlineData(-100)]
    public void Constructor_WithMaxPatternsBelowMinimum_ThrowsArgumentOutOfRangeException(int maxPatterns)
    {
        Action act = () => new PredictiveDictionary<string, int>(3, maxPatterns, 50, 0.5);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("maxPatterns");
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(2.0)]
    public void Constructor_WithInvalidConfidenceThreshold_ThrowsArgumentOutOfRangeException(double threshold)
    {
        Action act = () => new PredictiveDictionary<string, int>(3, 100, 50, threshold);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("confidenceThreshold");
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(10)]
    public void Constructor_WithValidPatternLength_DoesNotThrow(int patternLength)
    {
        Action act = () => new PredictiveDictionary<string, int>(patternLength, 100, 50, 0.5).Dispose();

        act.Should().NotThrow();
    }

    [Fact]
    public void Indexer_Set_AddsItem_CountIncreases()
    {
        using var dict = new PredictiveDictionary<string, int>();

        dict["a"] = 1;

        dict.Count.Should().Be(1);
    }

    [Fact]
    public void Indexer_Set_OverwritesExistingValue()
    {
        using var dict = new PredictiveDictionary<string, int>();

        dict["a"] = 1;
        dict["a"] = 2;

        dict.Count.Should().Be(1);
        dict["a"].Should().Be(2);
    }

    [Fact]
    public void Indexer_Get_ExistingKey_ReturnsValue()
    {
        using var dict = new PredictiveDictionary<string, int>();
        dict["a"] = 42;

        var v = dict["a"];

        v.Should().Be(42);
    }

    [Fact]
    public void Indexer_Get_MissingKey_ThrowsKeyNotFoundException()
    {
        using var dict = new PredictiveDictionary<string, int>();

        Action act = () => { var _ = dict["missing"]; };

        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void AddOrUpdate_AddsNewKey()
    {
        using var dict = new PredictiveDictionary<int, string>();

        dict.AddOrUpdate(1, "one");

        dict.Count.Should().Be(1);
        dict.TryGetValue(1, out var value).Should().BeTrue();
        value.Should().Be("one");
    }

    [Fact]
    public void AddOrUpdate_UpdatesExistingKey()
    {
        using var dict = new PredictiveDictionary<int, string>();
        dict.AddOrUpdate(1, "one");

        dict.AddOrUpdate(1, "ONE");

        dict.Count.Should().Be(1);
        dict.TryGetValue(1, out var value).Should().BeTrue();
        value.Should().Be("ONE");
    }

    [Fact]
    public void TryGetValue_ExistingKey_ReturnsTrueAndValue()
    {
        using var dict = new PredictiveDictionary<string, int>();
        dict["x"] = 42;

        var result = dict.TryGetValue("x", out var value);

        result.Should().BeTrue();
        value.Should().Be(42);
    }

    [Fact]
    public void TryGetValue_MissingKey_ReturnsFalseAndDefault()
    {
        using var dict = new PredictiveDictionary<string, int>();

        var result = dict.TryGetValue("missing", out var value);

        result.Should().BeFalse();
        value.Should().Be(0);
    }

    [Fact]
    public void TryGetValue_MissingReferenceKey_ReturnsFalseAndNull()
    {
        using var dict = new PredictiveDictionary<int, string>();

        var result = dict.TryGetValue(99, out var value);

        result.Should().BeFalse();
        value.Should().BeNull();
    }

    [Fact]
    public void Remove_ExistingKey_ReturnsTrueAndDecrementsCount()
    {
        using var dict = new PredictiveDictionary<string, int>();
        dict["a"] = 1;
        dict["b"] = 2;

        var removed = dict.Remove("a");

        removed.Should().BeTrue();
        dict.Count.Should().Be(1);
        dict.TryGetValue("a", out _).Should().BeFalse();
    }

    [Fact]
    public void Remove_MissingKey_ReturnsFalse()
    {
        using var dict = new PredictiveDictionary<string, int>();

        var removed = dict.Remove("ghost");

        removed.Should().BeFalse();
        dict.Count.Should().Be(0);
    }

    [Fact]
    public void Clear_RemovesAllKeysAndState()
    {
        using var dict = new PredictiveDictionary<string, int>(2, 100, 50, 0.05);
        dict["a"] = 1;
        dict["b"] = 2;
        dict["c"] = 3;
        for (int i = 0; i < 4; i++)
        {
            dict.TryGetValue("a", out _);
            dict.TryGetValue("b", out _);
            dict.TryGetValue("c", out _);
        }

        dict.Clear();

        dict.Count.Should().Be(0);
        dict.PredictiveCacheCount.Should().Be(0);
        dict.PatternCount.Should().Be(0);
    }

    [Fact]
    public void Clear_AfterClear_LearningStillWorks()
    {
        using var dict = new PredictiveDictionary<string, int>(2, 100, 50, 0.05);
        dict["a"] = 1;
        dict["b"] = 2;
        dict["c"] = 3;
        for (int i = 0; i < 4; i++)
        {
            dict.TryGetValue("a", out _);
            dict.TryGetValue("b", out _);
            dict.TryGetValue("c", out _);
        }

        dict.Clear();

        for (int i = 0; i < 4; i++)
        {
            dict.TryGetValue("a", out _);
            dict.TryGetValue("b", out _);
            dict.TryGetValue("c", out _);
        }

        dict.PatternCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Count_ReflectsAddRemoveOperations()
    {
        using var dict = new PredictiveDictionary<int, int>();

        dict[1] = 1;
        dict[2] = 2;
        dict[3] = 3;
        dict.Count.Should().Be(3);

        dict.Remove(2);
        dict.Count.Should().Be(2);

        dict.Clear();
        dict.Count.Should().Be(0);
    }

    [Fact]
    public void GetPredictions_EmptyContext_ReturnsEmpty()
    {
        using var dict = new PredictiveDictionary<string, int>();

        var predictions = dict.GetPredictions(Array.Empty<string>());

        predictions.Should().BeEmpty();
    }

    [Fact]
    public void GetPredictions_FreshDictionary_ReturnsEmpty()
    {
        using var dict = new PredictiveDictionary<string, int>();

        var predictions = dict.GetPredictions(new[] { "a", "b", "c" });

        predictions.Should().BeEmpty();
    }

    [Fact]
    public void GetPredictions_AfterFrequencyAccess_ReturnsFrequencyBasedFallback()
    {
        // Use confidence threshold 0 so frequency hits surface (frequency confidence is capped at 0.8).
        using var dict = new PredictiveDictionary<string, int>(2, 100, 50, 0.0);
        dict["popular"] = 1;
        for (int i = 0; i < 5; i++)
            dict.TryGetValue("popular", out _);

        // Context with no learned pattern → frequency fallback should kick in.
        var predictions = dict.GetPredictions(new[] { "unseen-context" }).ToList();

        predictions.Should().Contain(p => p.Key == "popular");
        predictions.First(p => p.Key == "popular").Reason.Should().Contain("Frequency");
    }

    [Fact]
    public void GetPredictions_AfterRepeatedSequence_SurfacesNextKeyWithHighConfidence()
    {
        using var dict = new PredictiveDictionary<string, int>(3, 100, 50, 0.5);
        dict["A"] = 1;
        dict["B"] = 2;
        dict["C"] = 3;
        dict["D"] = 4;

        // Train: A → B → C → D, repeated.
        for (int i = 0; i < 10; i++)
        {
            dict.TryGetValue("A", out _);
            dict.TryGetValue("B", out _);
            dict.TryGetValue("C", out _);
            dict.TryGetValue("D", out _);
        }

        var predictions = dict.GetPredictions(new[] { "A", "B", "C" }).ToList();

        predictions.Should().Contain(p => p.Key == "D");
        var d = predictions.First(p => p.Key == "D");
        d.Confidence.Should().BeGreaterThanOrEqualTo(0.5);
        d.Reason.Should().Contain("Pattern");
    }

    [Fact]
    public void GetPredictions_ConfidenceAlwaysWithinValidRange()
    {
        using var dict = new PredictiveDictionary<string, int>(2, 100, 50, 0.0);
        for (int i = 0; i < 6; i++)
        {
            dict.TryGetValue("x", out _);
            dict.TryGetValue("y", out _);
            dict.TryGetValue("z", out _);
        }

        var predictions = dict.GetPredictions(new[] { "x", "y" }).ToList();

        predictions.Should().NotBeEmpty();
        predictions.All(p => p.Confidence >= 0.0 && p.Confidence <= 1.0).Should().BeTrue();
    }

    [Fact]
    public void GetPredictions_LimitsResultsToTopFive()
    {
        using var dict = new PredictiveDictionary<string, int>(2, 100, 50, 0.0);
        // Generate enough distinct accessed keys so frequency fallback yields >5 candidates.
        for (int n = 0; n < 10; n++)
        {
            for (int i = 0; i < 3; i++)
                dict.TryGetValue($"k{n}", out _);
        }

        var predictions = dict.GetPredictions(new[] { "ctx" }).ToList();

        predictions.Count.Should().BeLessThanOrEqualTo(5);
    }

    [Fact]
    public void GetPredictions_SortedByConfidenceDescending()
    {
        using var dict = new PredictiveDictionary<string, int>(2, 100, 50, 0.0);
        // Skewed frequency distribution.
        for (int i = 0; i < 10; i++) dict.TryGetValue("hot", out _);
        for (int i = 0; i < 5; i++) dict.TryGetValue("warm", out _);
        for (int i = 0; i < 1; i++) dict.TryGetValue("cold", out _);

        var predictions = dict.GetPredictions(new[] { "no-pattern-context" }).ToList();

        predictions.Should().NotBeEmpty();
        for (int i = 1; i < predictions.Count; i++)
        {
            predictions[i - 1].Confidence.Should().BeGreaterThanOrEqualTo(predictions[i].Confidence);
        }
    }

    [Fact]
    public void PrefetchLikely_LoadsPredictedItemsIntoCache_AndReturnsCount()
    {
        using var dict = new PredictiveDictionary<string, int>(2, 100, 50, 0.0);
        // Train a strong pattern A → B → target so target surfaces from a known pattern.
        for (int i = 0; i < 8; i++)
        {
            dict.TryGetValue("A", out _);
            dict.TryGetValue("B", out _);
            dict.TryGetValue("target", out _);
        }

        var loaded = dict.PrefetchLikely(new[] { "A", "B" }, k => k.Length);

        loaded.Should().BeGreaterThan(0);
        dict.PredictiveCacheCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void PrefetchLikely_PromotedKeyIsRetrievableViaTryGetValue()
    {
        using var dict = new PredictiveDictionary<string, int>(2, 100, 50, 0.0);
        for (int i = 0; i < 8; i++)
        {
            dict.TryGetValue("A", out _);
            dict.TryGetValue("B", out _);
            dict.TryGetValue("payload", out _);
        }

        var loaded = dict.PrefetchLikely(new[] { "A", "B" }, k => 999);
        loaded.Should().BeGreaterThan(0);

        // Pull a prefetched key and ensure it promotes to main dict.
        var preCacheCount = dict.PredictiveCacheCount;
        var preMainCount = dict.Count;
        dict.TryGetValue("payload", out var v).Should().BeTrue();

        v.Should().Be(999);
        dict.Count.Should().Be(preMainCount + 1);
        dict.PredictiveCacheCount.Should().Be(preCacheCount - 1);
    }

    [Fact]
    public void PrefetchLikely_ValueFactoryThatThrows_DoesNotPropagate_AndSkipsKey()
    {
        using var dict = new PredictiveDictionary<string, int>(2, 100, 50, 0.0);
        for (int i = 0; i < 8; i++)
        {
            dict.TryGetValue("A", out _);
            dict.TryGetValue("B", out _);
            dict.TryGetValue("victim", out _);
        }

        Action act = () => dict.PrefetchLikely(new[] { "A", "B" }, _ => throw new InvalidOperationException("boom"));

        act.Should().NotThrow();
        // Nothing should have been loaded since the factory failed.
        dict.PredictiveCacheCount.Should().Be(0);
    }

    [Fact]
    public void PrefetchLikely_DoesNotReloadKeysAlreadyInMainDict()
    {
        using var dict = new PredictiveDictionary<string, int>(2, 100, 50, 0.0);
        dict["target"] = 99;
        for (int i = 0; i < 5; i++)
            dict.TryGetValue("target", out _);

        int factoryCalls = 0;
        var loaded = dict.PrefetchLikely(new[] { "ctx" }, k => { factoryCalls++; return -1; });

        // target is already in the main dict — it should not be reloaded into the predictive cache.
        loaded.Should().Be(0);
        dict.PredictiveCacheCount.Should().Be(0);
    }

    [Fact]
    public void PrefetchLikely_HonorsMaxCacheSize()
    {
        // Tiny cache so we hit the cap quickly.
        using var dict = new PredictiveDictionary<string, int>(2, 100, maxCacheSize: 2, confidenceThreshold: 0.0);
        // Generate many frequency-fallback candidates that aren't in the main dict.
        for (int n = 0; n < 10; n++)
        {
            for (int i = 0; i < 3; i++)
                dict.TryGetValue($"k{n}", out _);
        }

        var loaded = dict.PrefetchLikely(new[] { "no-pattern" }, k => 0);

        dict.PredictiveCacheCount.Should().BeLessThanOrEqualTo(2);
        loaded.Should().BeLessThanOrEqualTo(2);
    }

    [Fact]
    public void PrefetchLikely_EmptyContext_ReturnsZero()
    {
        using var dict = new PredictiveDictionary<string, int>();

        var loaded = dict.PrefetchLikely(Array.Empty<string>(), _ => 1);

        loaded.Should().Be(0);
        dict.PredictiveCacheCount.Should().Be(0);
    }

    [Fact]
    public void Remove_DropsPredictiveCacheEntry()
    {
        using var dict = new PredictiveDictionary<string, int>(2, 100, 50, 0.0);
        for (int i = 0; i < 8; i++)
        {
            dict.TryGetValue("A", out _);
            dict.TryGetValue("B", out _);
            dict.TryGetValue("freq", out _);
        }
        dict.PrefetchLikely(new[] { "A", "B" }, k => 7);
        var cacheCountBefore = dict.PredictiveCacheCount;
        cacheCountBefore.Should().BeGreaterThan(0);

        dict.Remove("freq");

        // Removing the prefetched key should evict it specifically — TryGetValue must report it gone.
        dict.TryGetValue("freq", out _).Should().BeFalse();
        // And the cache size should have shrunk by exactly the removed entry (or stayed the same if it
        // wasn't in the cache to begin with). Either way, "freq" itself must not be retrievable.
        dict.PredictiveCacheCount.Should().BeLessThanOrEqualTo(cacheCountBefore);
    }

    [Fact]
    public void AddOrUpdate_RefreshesValueInPredictiveCache()
    {
        using var dict = new PredictiveDictionary<string, int>(2, 100, 50, 0.0);
        for (int i = 0; i < 8; i++)
        {
            dict.TryGetValue("A", out _);
            dict.TryGetValue("B", out _);
            dict.TryGetValue("entry", out _);
        }
        dict.PrefetchLikely(new[] { "A", "B" }, k => 1);
        dict.PredictiveCacheCount.Should().BeGreaterThan(0);

        // AddOrUpdate should sync the predictive cache copy with the main dict copy.
        dict.AddOrUpdate("entry", 555);

        dict.TryGetValue("entry", out var v).Should().BeTrue();
        v.Should().Be(555);
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var dict = new PredictiveDictionary<string, int>();

        dict.Dispose();
        Action act = () => dict.Dispose();

        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_PrefetchLikely_ReturnsZero()
    {
        var dict = new PredictiveDictionary<string, int>();
        dict.Dispose();

        var loaded = dict.PrefetchLikely(new[] { "x" }, _ => 1);

        loaded.Should().Be(0);
    }

    [Fact]
    public void Dispose_ClearsState()
    {
        var dict = new PredictiveDictionary<string, int>();
        dict["a"] = 1;
        dict["b"] = 2;

        dict.Dispose();

        dict.Count.Should().Be(0);
    }

    [Fact]
    public void PredictionResult_ExposesKeyConfidenceAndReason()
    {
        using var dict = new PredictiveDictionary<string, int>(2, 100, 50, 0.0);
        for (int i = 0; i < 6; i++)
            dict.TryGetValue("hit", out _);

        var prediction = dict.GetPredictions(new[] { "ctx" }).First();

        prediction.Key.Should().NotBeNull();
        prediction.Confidence.Should().BeGreaterThanOrEqualTo(0.0);
        prediction.Reason.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void PatternCount_IncreasesAsNewSequencesAreSeen()
    {
        using var dict = new PredictiveDictionary<string, int>(2, 100, 50, 0.0);

        dict.PatternCount.Should().Be(0);

        // A 2-pattern dict needs 3 accesses (2 context + 1 next) before learning kicks in.
        dict.TryGetValue("a", out _);
        dict.TryGetValue("b", out _);
        dict.TryGetValue("c", out _);

        dict.PatternCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void MaxPatterns_HonoredByEvictingOldestPatternOnOverflow()
    {
        // Minimum allowed maxPatterns is 10 — feed enough varied sequences to exceed the cap.
        using var dict = new PredictiveDictionary<string, int>(2, maxPatterns: 10, maxCacheSize: 50, confidenceThreshold: 0.0);

        for (int i = 0; i < 50; i++)
        {
            dict.TryGetValue($"k{i}_x", out _);
            dict.TryGetValue($"k{i}_y", out _);
            dict.TryGetValue($"k{i}_z", out _);
        }

        dict.PatternCount.Should().BeLessThanOrEqualTo(10);
    }
}

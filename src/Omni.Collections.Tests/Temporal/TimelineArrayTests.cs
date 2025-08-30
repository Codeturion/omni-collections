using System;
using System.Linq;
using FluentAssertions;
using Omni.Collections.Temporal;
using Xunit;

namespace Omni.Collections.Tests.Temporal;

public class TimelineArrayTests
{
    /// <summary>
    /// Tests that a TimelineArray can be constructed with valid parameters.
    /// The timeline should initialize with the specified capacity and frame duration.
    /// </summary>
    [Theory]
    [InlineData(100, 16)]
    [InlineData(1000, 33)]
    [InlineData(3600, 8)]
    public void Constructor_WithValidParameters_InitializesCorrectly(int capacity, int frameDuration)
    {
        var timeline = new TimelineArray<string>(capacity, frameDuration);

        timeline.Capacity.Should().Be(capacity);
        timeline.Count.Should().Be(0);
        timeline.CurrentTime.Should().Be(0);
        timeline.StartTime.Should().Be(0);
        timeline.EndTime.Should().Be(0);
    }

    /// <summary>
    /// Tests that constructor with default frame duration creates a functional timeline.
    /// The timeline should use reasonable default frame duration for general use cases.
    /// </summary>
    [Fact]
    public void Constructor_WithDefaultFrameDuration_InitializesCorrectly()
    {
        var timeline = new TimelineArray<int>(100);

        timeline.Capacity.Should().Be(100);
        timeline.Count.Should().Be(0);
    }

    /// <summary>
    /// Tests that constructor throws exception for invalid parameters.
    /// The constructor should validate capacity and frame duration parameters.
    /// </summary>
    [Theory]
    [InlineData(0, 16)]
    [InlineData(-1, 16)]
    [InlineData(100, 0)]
    [InlineData(100, -1)]
    public void Constructor_WithInvalidParameters_ThrowsArgumentOutOfRangeException(int capacity, int frameDuration)
    {
        var act = () => new TimelineArray<string>(capacity, frameDuration);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Tests that CreateWithArrayPool creates timeline with memory pooling enabled.
    /// The timeline should use pooled arrays to reduce allocation pressure.
    /// </summary>
    [Fact]
    public void CreateWithArrayPool_CreatesTimelineWithPooling()
    {
        using var timeline = TimelineArray<string>.CreateWithArrayPool(100);

        timeline.Capacity.Should().BeGreaterOrEqualTo(100);
        timeline.Count.Should().Be(0);
    }

    /// <summary>
    /// Tests that Record successfully stores values at specified timestamps.
    /// Values should be indexed by time and retrievable with GetAtTime.
    /// </summary>
    [Fact]
    public void Record_StoresValuesAtTimestamps()
    {
        var timeline = new TimelineArray<string>(100);
        var baseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        timeline.Record("value1", baseTime);
        timeline.Record("value2", baseTime + 100);
        timeline.Record("value3", baseTime + 200);

        timeline.Count.Should().Be(3);
        timeline.StartTime.Should().Be(baseTime);
        timeline.CurrentTime.Should().Be(baseTime + 200);
    }

    /// <summary>
    /// Tests that GetAtTime retrieves values stored at specific timestamps.
    /// The method should return the exact value stored at the given time.
    /// </summary>
    [Fact]
    public void GetAtTime_RetrievesValuesAtTimestamps()
    {
        var timeline = new TimelineArray<string>(100);
        var baseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        timeline.Record("first", baseTime);
        timeline.Record("second", baseTime + 100);
        timeline.Record("third", baseTime + 200);

        timeline.GetAtTime(baseTime).Should().Be("first");
        timeline.GetAtTime(baseTime + 100).Should().Be("second");
        timeline.GetAtTime(baseTime + 200).Should().Be("third");
    }

    /// <summary>
    /// Tests that GetAtTime returns default value for non-existent timestamps.
    /// The method should handle missing timestamps gracefully.
    /// </summary>
    [Fact]
    public void GetAtTime_NonExistentTimestamp_ReturnsDefault()
    {
        var timeline = new TimelineArray<string>(100);
        var baseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        timeline.Record("existing", baseTime);

        timeline.GetAtTime(baseTime + 1000).Should().BeNull(); // Default for reference type
    }

    /// <summary>
    /// Tests that GetAtTime works correctly for interpolated values.
    /// The method should return the closest available value for timestamps between records.
    /// </summary>
    [Fact]
    public void GetAtTime_InterpolatedValue_ReturnsClosest()
    {
        var timeline = new TimelineArray<string>(100);
        var baseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        timeline.Record("first", baseTime);
        timeline.Record("second", baseTime + 200);

        // Request value between recorded timestamps
        var result = timeline.GetAtTime(baseTime + 100);

        result.Should().Be("first"); // Should return previous value
    }

    /// <summary>
    /// Tests that Record method works with current timestamp when no timestamp provided.
    /// The method should use current time for recording when timestamp is omitted.
    /// </summary>
    [Fact]
    public void Record_WithoutTimestamp_UsesCurrentTime()
    {
        var timeline = new TimelineArray<string>(100);
        var countBefore = timeline.Count;

        timeline.Record("test"); // Uses current time

        timeline.Count.Should().Be(countBefore + 1);
        timeline.Current.Should().Be("test");
    }

    /// <summary>
    /// Tests that RewindTo positions timeline at specified timestamp.
    /// The method should allow temporal navigation to any recorded time.
    /// </summary>
    [Fact]
    public void RewindTo_PositionsAtTimestamp()
    {
        var timeline = new TimelineArray<string>(100);
        var baseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        timeline.Record("early", baseTime);
        timeline.Record("late", baseTime + 1000);

        var rewound = timeline.RewindTo(baseTime);

        rewound.Should().BeTrue();
        timeline.CurrentTime.Should().Be(baseTime);
    }

    /// <summary>
    /// Tests that NextFrame and PreviousFrame allow frame-by-frame navigation.
    /// The methods should allow stepping through recorded timeline data.
    /// </summary>
    [Fact]
    public void NextPreviousFrame_AllowsFrameNavigation()
    {
        var timeline = new TimelineArray<string>(100);
        var baseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        timeline.Record("first", baseTime);
        timeline.Record("second", baseTime + 100);
        timeline.Record("third", baseTime + 200);

        timeline.RewindTo(baseTime);
        
        timeline.NextFrame().Should().BeTrue();
        timeline.Current.Should().Be("second");
        
        timeline.PreviousFrame().Should().BeTrue();
        timeline.Current.Should().Be("first");
    }

    /// <summary>
    /// Tests that Replay retrieves all values within a time range.
    /// The method should return values between start and end timestamps inclusive.
    /// </summary>
    [Fact]
    public void Replay_RetrievesValuesInTimeRange()
    {
        var timeline = new TimelineArray<string>(100);
        var baseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        timeline.Record("before", baseTime);
        timeline.Record("during1", baseTime + 500);
        timeline.Record("during2", baseTime + 800);
        timeline.Record("after", baseTime + 1500);

        var rangeValues = timeline.Replay(baseTime + 400, baseTime + 1000).ToList();

        rangeValues.Should().HaveCount(2);
        rangeValues.Should().Contain("during1");
        rangeValues.Should().Contain("during2");
        rangeValues.Should().NotContain("before");
        rangeValues.Should().NotContain("after");
    }

    /// <summary>
    /// Tests that timeline entries can be checked for existence at timestamps.
    /// The method should properly identify existing and non-existing entries.
    /// </summary>
    [Fact]
    public void GetAtTime_ChecksForEntriesAtTimestamps()
    {
        var timeline = new TimelineArray<string>(100);
        var baseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        timeline.Record("existing", baseTime);
        timeline.Record("toKeep", baseTime + 100);

        timeline.Count.Should().Be(2);
        timeline.GetAtTime(baseTime).Should().Be("existing");
        timeline.GetAtTime(baseTime + 100).Should().Be("toKeep");
        timeline.GetAtTime(baseTime + 200).Should().BeNull(); // Non-existent
    }

    /// <summary>
    /// Tests that timeline maintains consistency with non-existent timestamp queries.
    /// Querying non-existent entries should not affect the timeline state.
    /// </summary>
    [Fact]
    public void GetAtTime_NonExistentTimestamp_MaintainsConsistency()
    {
        var timeline = new TimelineArray<string>(100);
        var baseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        timeline.Record("existing", baseTime);

        var result = timeline.GetAtTime(baseTime + 1000);

        result.Should().BeNull();
        timeline.Count.Should().Be(1);
    }

    /// <summary>
    /// Tests that Clear removes all entries and resets timeline state.
    /// After clearing, the timeline should be empty with reset timestamps.
    /// </summary>
    [Fact]
    public void Clear_RemovesAllEntries()
    {
        var timeline = new TimelineArray<string>(100);
        var baseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        timeline.Record("entry1", baseTime);
        timeline.Record("entry2", baseTime + 100);

        timeline.Clear();

        timeline.Count.Should().Be(0);
        timeline.StartTime.Should().Be(0);
        timeline.EndTime.Should().Be(0);
        timeline.CurrentTime.Should().Be(0);
    }

    /// <summary>
    /// Tests that circular buffer behavior works correctly when capacity is exceeded.
    /// Old entries should be overwritten when the buffer reaches capacity.
    /// </summary>
    [Fact]
    public void CircularBuffer_OverwritesOldEntriesWhenFull()
    {
        var timeline = new TimelineArray<string>(3); // Small capacity
        var baseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        timeline.Record("first", baseTime);
        timeline.Record("second", baseTime + 100);
        timeline.Record("third", baseTime + 200);
        timeline.Record("fourth", baseTime + 300); // Should overwrite first

        timeline.Count.Should().Be(3);
        timeline.GetAtTime(baseTime).Should().BeNull(); // First should be gone
        timeline.GetAtTime(baseTime + 100).Should().Be("second");
        timeline.GetAtTime(baseTime + 200).Should().Be("third");
        timeline.GetAtTime(baseTime + 300).Should().Be("fourth");
    }

    /// <summary>
    /// Tests that ReplayAtFps provides frame-rate controlled playback.
    /// The method should return snapshots at specified frames per second.
    /// </summary>
    [Fact]
    public void ReplayAtFps_ProvidesFrameControlledPlayback()
    {
        var timeline = new TimelineArray<string>(100);
        var baseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        timeline.Record("frame1", baseTime);
        timeline.Record("frame2", baseTime + 500);
        timeline.Record("frame3", baseTime + 1000);

        var fpsReplay = timeline.ReplayAtFps(baseTime, fps: 2).ToList(); // 2 FPS = 500ms intervals

        fpsReplay.Should().HaveCountGreaterThan(0);
        fpsReplay.All(frame => frame.timestamp >= baseTime).Should().BeTrue();
    }

    /// <summary>
    /// Tests that ToArray converts timeline to array in chronological order.
    /// The method should return all values in the order they were recorded.
    /// </summary>
    [Fact]
    public void ToArray_ReturnsChronologicalValues()
    {
        var timeline = new TimelineArray<string>(100);
        var baseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        timeline.Record("first", baseTime);
        timeline.Record("second", baseTime + 100);
        timeline.Record("third", baseTime + 200);

        var array = timeline.ToArray();

        array.Should().HaveCount(3);
        array[0].Should().Be("first");
        array[1].Should().Be("second");
        array[2].Should().Be("third");
    }

    /// <summary>
    /// Tests that GetStats returns comprehensive timeline statistics.
    /// The method should provide detailed information about timeline state and performance.
    /// </summary>
    [Fact]
    public void GetStats_ReturnsComprehensiveStatistics()
    {
        var timeline = new TimelineArray<string>(100);
        var baseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        timeline.Record("first", baseTime);
        timeline.Record("second", baseTime + 100);
        timeline.Record("third", baseTime + 200);

        var stats = timeline.GetStats();

        stats.SnapshotCount.Should().Be(3);
        stats.Capacity.Should().Be(100);
        stats.StartTime.Should().Be(baseTime);
        stats.EndTime.Should().Be(baseTime + 200);
        stats.Duration.Should().Be(200);
        stats.MemoryUsage.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Tests that timeline handles edge cases gracefully.
    /// Empty timeline and boundary conditions should be handled correctly.
    /// </summary>
    [Fact]
    public void EdgeCases_HandledGracefully()
    {
        var timeline = new TimelineArray<string>(100);

        // Empty timeline operations
        timeline.Count.Should().Be(0);
        timeline.GetAtTime(12345).Should().BeNull();
        timeline.Current.Should().BeNull();
        
        // Boundary navigation
        timeline.NextFrame().Should().BeFalse();
        timeline.PreviousFrame().Should().BeFalse();
        timeline.RewindTo(12345).Should().BeFalse();
        
        // Edge case with single entry
        var baseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        timeline.Record("single", baseTime);
        timeline.RewindTo(baseTime);
        timeline.PreviousFrame().Should().BeFalse(); // No previous frame
    }

    /// <summary>
    /// Tests that ToArray returns all stored values in timestamp order.
    /// Values should be returned in the same order as their timestamps.
    /// </summary>
    [Fact]
    public void ToArray_ReturnsValuesInTimestampOrder()
    {
        var timeline = new TimelineArray<string>(100);
        var baseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        timeline.Record("first", baseTime);
        timeline.Record("second", baseTime + 100);
        timeline.Record("third", baseTime + 200);

        var values = timeline.ToArray();

        values.Should().HaveCount(3);
        values[0].Should().Be("first");
        values[1].Should().Be("second");
        values[2].Should().Be("third");
    }

    /// <summary>
    /// Tests that GetAtTime correctly identifies existing timestamps.
    /// The method should return stored values for existing timestamps and null otherwise.
    /// </summary>
    [Fact]
    public void GetAtTime_IdentifiesExistingTimestamps()
    {
        var timeline = new TimelineArray<string>(100);
        var baseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        timeline.Record("test", baseTime);

        // TimelineArray doesn't have ContainsTime, but we can test GetAtTime for existence
        timeline.GetAtTime(baseTime).Should().Be("test");
        timeline.GetAtTime(baseTime + 1000).Should().BeNull();
    }


    /// <summary>
    /// Tests that timeline works correctly with value types.
    /// Integer values should be stored and retrieved accurately.
    /// </summary>
    [Fact]
    public void TimelineArray_WithValueTypes_WorksCorrectly()
    {
        var timeline = new TimelineArray<int>(100);
        var baseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        timeline.Record(42, baseTime);
        timeline.Record(100, baseTime + 100);

        timeline.GetAtTime(baseTime).Should().Be(42);
        timeline.GetAtTime(baseTime + 100).Should().Be(100);
        timeline.GetAtTime(baseTime + 1000).Should().Be(0); // Default for value type
    }

    /// <summary>
    /// Tests that timeline works correctly with custom object types.
    /// Custom objects should be stored and retrieved with proper equality.
    /// </summary>
    [Fact]
    public void TimelineArray_WithCustomObjects_WorksCorrectly()
    {
        var timeline = new TimelineArray<GameState>(100);
        var baseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var state1 = new GameState("Player1", 100);
        var state2 = new GameState("Player2", 150);

        timeline.Record(state1, baseTime);
        timeline.Record(state2, baseTime + 100);

        timeline.GetAtTime(baseTime).Should().Be(state1);
        timeline.GetAtTime(baseTime + 100).Should().Be(state2);
    }

    /// <summary>
    /// Tests that Dispose cleans up resources properly including pooled arrays.
    /// The timeline should release all allocated resources when disposed.
    /// </summary>
    [Fact]
    public void Dispose_CleansUpResources()
    {
        var timeline = TimelineArray<string>.CreateWithArrayPool(100);
        timeline.Record("test", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        timeline.Dispose();

        // Should not throw after disposal
        timeline.Dispose(); // Multiple dispose calls should be safe
    }

    /// <summary>
    /// Tests edge case of empty timeline operations.
    /// Empty timeline operations should behave correctly without throwing exceptions.
    /// </summary>
    [Fact]
    public void EmptyTimeline_HandlesOperationsCorrectly()
    {
        var timeline = new TimelineArray<string>(100);

        timeline.Count.Should().Be(0);
        timeline.GetAtTime(12345).Should().BeNull();

        timeline.ToArray().Should().BeEmpty();
        timeline.Replay(0, 1000).Should().BeEmpty();

        // Should not throw
        timeline.Clear();
    }

    /// <summary>
    /// Tests that frame duration affects timeline behavior correctly.
    /// Different frame durations should not affect storage but may affect internal calculations.
    /// </summary>
    [Theory]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(33)]
    [InlineData(100)]
    public void FrameDuration_DoesNotAffectStorage(int frameDuration)
    {
        var timeline = new TimelineArray<string>(100, frameDuration);
        var baseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        timeline.Record("test1", baseTime);
        timeline.Record("test2", baseTime + 50);
        timeline.Record("test3", baseTime + 150);

        timeline.Count.Should().Be(3);
        timeline.GetAtTime(baseTime).Should().Be("test1");
        timeline.GetAtTime(baseTime + 50).Should().Be("test2");
        timeline.GetAtTime(baseTime + 150).Should().Be("test3");
    }

    /// <summary>
    /// Tests that timeline maintains data integrity with rapid timestamp updates.
    /// Frequent updates should not corrupt the internal state.
    /// </summary>
    [Fact]
    public void RapidTimestampUpdates_MaintainDataIntegrity()
    {
        var timeline = new TimelineArray<int>(1000);
        var baseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Rapidly add many timestamps
        for (int i = 0; i < 500; i++)
        {
            timeline.Record(i, baseTime + i * 10);
        }

        timeline.Count.Should().Be(500);

        // Verify random entries
        timeline.GetAtTime(baseTime + 100).Should().Be(10);
        timeline.GetAtTime(baseTime + 2500).Should().Be(250);
        timeline.GetAtTime(baseTime + 4990).Should().Be(499);
    }

    private record GameState(string PlayerName, int Score);
}
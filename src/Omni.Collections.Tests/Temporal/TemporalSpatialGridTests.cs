using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Omni.Collections.Temporal;
using Xunit;

namespace Omni.Collections.Tests.Temporal;

public class TemporalSpatialGridTests
{
    /// <summary>
    /// Tests that a TemporalSpatialGrid can be constructed with valid parameters.
    /// The grid should initialize with the specified capacity, cell size, and frame duration.
    /// </summary>
    [Theory]
    [InlineData(100, 32.0f, 16)]
    [InlineData(1000, 64.0f, 33)]
    [InlineData(3600, 128.0f, 8)]
    public void Constructor_WithValidParameters_InitializesCorrectly(int capacity, float cellSize, long frameDuration)
    {
        var grid = new TemporalSpatialGrid<string>(capacity, cellSize, frameDuration);

        grid.SnapshotCount.Should().Be(0);
        grid.CurrentObjectCount.Should().Be(0);
        grid.CellSize.Should().Be(cellSize);
        grid.TimeRange.Should().Be((0, 0));
    }

    /// <summary>
    /// Tests that constructor with default parameters creates a functional grid.
    /// The grid should use reasonable default values for general use cases.
    /// </summary>
    [Fact]
    public void Constructor_WithDefaults_InitializesCorrectly()
    {
        var grid = new TemporalSpatialGrid<int>();

        grid.SnapshotCount.Should().Be(0);
        grid.CurrentObjectCount.Should().Be(0);
        grid.CellSize.Should().Be(64.0f);
    }

    /// <summary>
    /// Tests that CreateWithPooling creates a grid with object pooling enabled.
    /// The grid should use pooled objects to reduce allocation pressure.
    /// </summary>
    [Fact]
    public void CreateWithPooling_CreatesGridWithPooling()
    {
        using var grid = TemporalSpatialGrid<string>.CreateWithArrayPool();

        grid.SnapshotCount.Should().Be(0);
        grid.CurrentObjectCount.Should().Be(0);
    }

    /// <summary>
    /// Tests that Insert adds objects at current time and spatial coordinates.
    /// Objects should be indexed both temporally and spatially for efficient retrieval.
    /// </summary>
    [Fact]
    public void Insert_AddsObjectsAtCurrentTime()
    {
        var grid = new TemporalSpatialGrid<string>(autoRecord: true);
        var timestampBefore = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        grid.Insert(10.0f, 20.0f, "object1");
        grid.Insert(50.0f, 60.0f, "object2");

        var timestampAfter = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        grid.CurrentObjectCount.Should().Be(2);
        grid.CurrentTime.Should().BeInRange(timestampBefore, timestampAfter);
    }

    /// <summary>
    /// Tests that Insert with auto-record creates snapshots automatically.
    /// Auto-recording should capture state changes at specified intervals.
    /// </summary>
    [Fact]
    public void Insert_WithAutoRecord_CreatesSnapshots()
    {
        var grid = new TemporalSpatialGrid<string>(capacity: 100, cellSize: 64.0f, frameDuration: 100, autoRecord: true);
        var baseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        grid.Insert(10.0f, 10.0f, "obj1");
        System.Threading.Thread.Sleep(150); // Wait longer than frameDuration (100ms)
        grid.Insert(20.0f, 20.0f, "obj2"); // Beyond frame duration

        grid.SnapshotCount.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Tests that GetObjectsInRectangle retrieves objects within specified spatial bounds at current time.
    /// The query should return objects that fall within the given coordinate range.
    /// </summary>
    [Fact]
    public void GetObjectsInRectangle_RetrievesObjectsInBounds()
    {
        var grid = new TemporalSpatialGrid<string>();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        grid.Insert(10.0f, 10.0f, "inside1");
        grid.Insert(15.0f, 15.0f, "inside2");
        grid.Insert(100.0f, 100.0f, "outside");

        var results = grid.GetObjectsInRectangle(5.0f, 5.0f, 20.0f, 20.0f);

        results.Should().HaveCount(2);
        results.Should().Contain("inside1");
        results.Should().Contain("inside2");
        results.Should().NotContain("outside");
    }

    /// <summary>
    /// Tests that GetObjectsInRectangleNear finds objects within specified radius of a point.
    /// The method should return objects within the circular search area.
    /// </summary>
    [Fact]
    public void GetObjectsInRectangleNear_FindsObjectsWithinRadius()
    {
        var grid = new TemporalSpatialGrid<string>();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        grid.Insert(10.0f, 10.0f, "near1");
        grid.Insert(12.0f, 12.0f, "near2");
        grid.Insert(50.0f, 50.0f, "far");

        var results = grid.GetObjectsInRadius(10.0f, 10.0f, 5.0f);

        results.Should().HaveCount(2);
        results.Should().Contain("near1");
        results.Should().Contain("near2");
        results.Should().NotContain("far");
    }

    /// <summary>
    /// Tests that RecordSnapshot manually captures current state at specified time.
    /// Manual snapshots should preserve the current spatial arrangement.
    /// </summary>
    [Fact]
    public void RecordSnapshot_CapturesCurrentState()
    {
        var grid = new TemporalSpatialGrid<string>();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        grid.Insert(10.0f, 10.0f, "obj1");
        grid.Insert(20.0f, 20.0f, "obj2");

        grid.RecordSnapshot(timestamp + 100);

        grid.SnapshotCount.Should().Be(1);
    }

    /// <summary>
    /// Tests that GetObjectsInRectangleAtTime retrieves historical spatial data at specified timestamp.
    /// The method should return objects that existed at the given point in time.
    /// </summary>
    [Fact]
    public void GetObjectsInRectangleAtTime_RetrievesHistoricalData()
    {
        var grid = new TemporalSpatialGrid<string>();
        var baseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Insert objects and record snapshot
        grid.Insert(10.0f, 10.0f, "historical1");
        grid.Insert(20.0f, 20.0f, "historical2");
        grid.RecordSnapshot(baseTime);

        // Add more objects after snapshot
        grid.Insert(30.0f, 30.0f, "current");

        var historicalResults = grid.GetObjectsInRectangleAtTime(5.0f, 5.0f, 25.0f, 25.0f, baseTime);

        historicalResults.Should().HaveCount(2);
        historicalResults.Should().Contain("historical1");
        historicalResults.Should().Contain("historical2");
        historicalResults.Should().NotContain("current");
    }

    /// <summary>
    /// Tests that GetObjectsInRadiusAtTime finds historical objects within radius at specified time.
    /// The method should perform temporal-spatial queries for past states.
    /// </summary>
    [Fact]
    public void GetObjectsInRadiusAtTime_FindsHistoricalObjectsWithinRadius()
    {
        var grid = new TemporalSpatialGrid<string>();
        var baseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        grid.Insert(10.0f, 10.0f, "old1");
        grid.Insert(12.0f, 12.0f, "old2");
        grid.RecordSnapshot(baseTime);

        // Add object after snapshot
        grid.Insert(11.0f, 11.0f, "new");

        var results = grid.GetObjectsInRadiusAtTime(10.0f, 10.0f, 5.0f, baseTime);

        results.Should().HaveCount(2);
        results.Should().Contain("old1");
        results.Should().Contain("old2");
        results.Should().NotContain("new");
    }

    /// <summary>
    /// Tests that Remove successfully removes objects from current spatial grid.
    /// Removed objects should no longer appear in current queries.
    /// </summary>
    [Fact]
    public void Remove_RemovesObjectsFromCurrentGrid()
    {
        var grid = new TemporalSpatialGrid<string>();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        grid.Insert(10.0f, 10.0f, "toRemove");
        grid.Insert(20.0f, 20.0f, "toKeep");

        var removed = grid.Remove(10.0f, 10.0f, "toRemove");

        removed.Should().BeTrue();
        grid.CurrentObjectCount.Should().Be(1);
        grid.GetObjectsInRectangle(5.0f, 5.0f, 15.0f, 15.0f).Should().NotContain("toRemove");
        grid.GetObjectsInRectangle(15.0f, 15.0f, 25.0f, 25.0f).Should().Contain("toKeep");
    }

    /// <summary>
    /// Tests that Remove returns false for non-existent objects.
    /// Attempting to remove non-existent objects should not affect the grid.
    /// </summary>
    [Fact]
    public void Remove_NonExistentObject_ReturnsFalse()
    {
        var grid = new TemporalSpatialGrid<string>();
        
        var removed = grid.Remove(0.0f, 0.0f, "nonExistent");

        removed.Should().BeFalse();
        grid.CurrentObjectCount.Should().Be(0);
    }

    /// <summary>
    /// Tests that objects can be moved by removing and re-inserting at new coordinates.
    /// This simulates object movement in the spatial grid.
    /// </summary>
    [Fact]
    public void MoveObject_ByRemoveAndInsert_UpdatesLocation()
    {
        var grid = new TemporalSpatialGrid<string>();

        grid.Insert(10.0f, 10.0f, "movable");
        
        // Move object by removing and re-inserting
        var removed = grid.Remove(10.0f, 10.0f, "movable");
        removed.Should().BeTrue();
        
        grid.Insert(50.0f, 60.0f, "movable");

        grid.GetObjectsInRectangle(5.0f, 5.0f, 15.0f, 15.0f).Should().NotContain("movable");
        grid.GetObjectsInRectangle(45.0f, 55.0f, 55.0f, 65.0f).Should().Contain("movable");
    }

    /// <summary>
    /// Tests that Clear removes all objects from current grid and resets state.
    /// Clearing should reset both current spatial data and snapshots.
    /// </summary>
    [Fact]
    public void Clear_RemovesAllObjectsFromCurrentGrid()
    {
        var grid = new TemporalSpatialGrid<string>();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        grid.Insert(10.0f, 10.0f, "obj1");
        grid.Insert(20.0f, 20.0f, "obj2");
        grid.RecordSnapshot(timestamp);

        grid.Clear();

        grid.CurrentObjectCount.Should().Be(0);
        grid.SnapshotCount.Should().Be(0);
        grid.GetObjectsInRectangle(0.0f, 0.0f, 100.0f, 100.0f).Should().BeEmpty();
    }

    /// <summary>
    /// Tests that TimeRange property returns correct temporal bounds of recorded data.
    /// The range should reflect the earliest and latest recorded timestamps.
    /// </summary>
    [Fact]
    public void TimeRange_ReturnsCorrectTemporalBounds()
    {
        var grid = new TemporalSpatialGrid<string>();
        var baseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        grid.Insert(10.0f, 10.0f, "obj1");
        grid.RecordSnapshot(baseTime);

        System.Threading.Thread.Sleep(100);
        grid.Insert(20.0f, 20.0f, "obj2");
        grid.RecordSnapshot(baseTime + 1000);

        var (start, end) = grid.TimeRange;
        start.Should().Be(baseTime);
        end.Should().Be(baseTime + 1000);
    }

    /// <summary>
    /// Tests that GetSpatialStateAtTime retrieves spatial grid state at specified timestamp.
    /// The method should return the spatial state from the given point in time.
    /// </summary>
    [Fact]
    public void GetSpatialStateAtTime_RetrievesTemporalSpatialState()
    {
        var grid = new TemporalSpatialGrid<string>();
        var baseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        grid.Insert(10.0f, 10.0f, "early");
        grid.RecordSnapshot(baseTime);

        // Add different items to current state without clearing timeline history
        grid.Insert(20.0f, 20.0f, "late");
        grid.RecordSnapshot(baseTime + 1000);

        // Get spatial state at first timestamp
        var spatialState = grid.GetSpatialStateAtTime(baseTime);

        spatialState.Should().NotBeNull();
        spatialState!.Count.Should().Be(1);
    }

    /// <summary>
    /// Tests that temporal grid handles concurrent access safely.
    /// Multiple threads should be able to insert and query data without corruption.
    /// </summary>
    [Fact]
    public void TemporalSpatialGrid_HandlesConcurrentAccess()
    {
        var grid = new TemporalSpatialGrid<int>();
        var tasks = new List<System.Threading.Tasks.Task>();

        // Start multiple threads inserting data
        for (int t = 0; t < 4; t++)
        {
            int threadId = t;
            tasks.Add(System.Threading.Tasks.Task.Run(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    grid.Insert(i * 10.0f, threadId * 50.0f, threadId * 100 + i);
                }
            }));
        }

        // Start a thread querying data
        tasks.Add(System.Threading.Tasks.Task.Run(() =>
        {
            for (int i = 0; i < 10; i++)
            {
                var results = grid.GetObjectsInRectangle(0.0f, 0.0f, 1000.0f, 1000.0f);
                System.Threading.Thread.Sleep(10);
            }
        }));

        System.Threading.Tasks.Task.WaitAll(tasks.ToArray());

        grid.CurrentObjectCount.Should().Be(400);
    }

    /// <summary>
    /// Tests that Dispose cleans up resources properly including pooled objects.
    /// The grid should release all allocated resources when disposed.
    /// </summary>
    [Fact]
    public void Dispose_CleansUpResources()
    {
        using var grid = TemporalSpatialGrid<string>.CreateWithArrayPool();
        grid.Insert(10.0f, 10.0f, "test");

        grid.Dispose();

        // Should not throw after disposal
        grid.Dispose(); // Multiple dispose calls should be safe
    }

    /// <summary>
    /// Tests that temporal queries work correctly with complex time-based scenarios.
    /// The grid should handle various temporal query patterns accurately.
    /// </summary>
    [Fact]
    public void ComplexTemporalQueries_WorkCorrectly()
    {
        var grid = new TemporalSpatialGrid<string>();
        var baseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Create a timeline with multiple snapshots
        grid.Insert(10.0f, 10.0f, "obj1");
        grid.RecordSnapshot(baseTime);

        grid.Remove(10.0f, 10.0f, "obj1");
        grid.Insert(20.0f, 20.0f, "obj1"); // Move obj1
        grid.Insert(30.0f, 30.0f, "obj2");
        grid.RecordSnapshot(baseTime + 500);

        grid.Remove(20.0f, 20.0f, "obj1");
        grid.Insert(40.0f, 40.0f, "obj3");
        grid.RecordSnapshot(baseTime + 1000);

        // GetObjectsInRectangle different time points
        var timePoint1 = grid.GetObjectsInRectangleAtTime(0.0f, 0.0f, 50.0f, 50.0f, baseTime);
        var timePoint2 = grid.GetObjectsInRectangleAtTime(0.0f, 0.0f, 50.0f, 50.0f, baseTime + 500);
        var timePoint3 = grid.GetObjectsInRectangleAtTime(0.0f, 0.0f, 50.0f, 50.0f, baseTime + 1000);

        timePoint1.Should().Contain("obj1").And.NotContain("obj2").And.NotContain("obj3");
        timePoint2.Should().Contain("obj1").And.Contain("obj2").And.NotContain("obj3");
        timePoint3.Should().NotContain("obj1").And.Contain("obj2").And.Contain("obj3");
    }

    /// <summary>
    /// Tests that spatial indexing maintains accuracy across different cell sizes.
    /// Different cell sizes should not affect the correctness of spatial queries.
    /// </summary>
    [Theory]
    [InlineData(16.0f)]
    [InlineData(64.0f)]
    [InlineData(128.0f)]
    public void SpatialIndexing_MaintainsAccuracyAcrossCellSizes(float cellSize)
    {
        var grid = new TemporalSpatialGrid<string>(cellSize: cellSize);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Insert objects in a pattern
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                grid.Insert(x * 10.0f, y * 10.0f, $"obj_{x}_{y}");
            }
        }

        // GetObjectsInRectangle specific region
        var results = grid.GetObjectsInRectangle(15.0f, 15.0f, 35.0f, 35.0f);

        results.Should().HaveCount(4); // Objects at (2,2), (2,3), (3,2), (3,3)
        results.Should().Contain("obj_2_2");
        results.Should().Contain("obj_2_3");
        results.Should().Contain("obj_3_2");
        results.Should().Contain("obj_3_3");
    }

    /// <summary>
    /// Tests edge case of empty temporal spatial grid operations.
    /// Empty grid operations should behave correctly without throwing exceptions.
    /// </summary>
    [Fact]
    public void EmptyGrid_HandlesOperationsCorrectly()
    {
        var grid = new TemporalSpatialGrid<string>();

        grid.SnapshotCount.Should().Be(0);
        grid.CurrentObjectCount.Should().Be(0);
        grid.TimeRange.Should().Be((0, 0));

        grid.GetObjectsInRectangle(0.0f, 0.0f, 100.0f, 100.0f).Should().BeEmpty();
        grid.GetObjectsInRadius(50.0f, 50.0f, 10.0f).Should().BeEmpty();
        grid.Remove(0.0f, 0.0f, "nonExistent").Should().BeFalse();

        // Should not throw
        grid.Clear();
        grid.RecordSnapshot(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }

    /// <summary>
    /// Tests that frame duration affects auto-recording behavior correctly.
    /// Different frame durations should control the frequency of automatic snapshots.
    /// </summary>
    [Theory]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(200)]
    public void FrameDuration_AffectsAutoRecording(long frameDuration)
    {
        var grid = new TemporalSpatialGrid<string>(capacity: 100, cellSize: 64.0f, frameDuration: frameDuration, autoRecord: true);

        grid.Insert(10.0f, 10.0f, "obj1");
        System.Threading.Thread.Sleep((int)frameDuration + 20); // Wait beyond frame duration
        grid.Insert(20.0f, 20.0f, "obj2"); // Should trigger auto-record

        grid.SnapshotCount.Should().BeGreaterOrEqualTo(1);
    }

    /// <summary>
    /// Tests the StartAutoRecording and StopAutoRecording methods.
    /// Auto-recording should be controllable at runtime.
    /// </summary>
    [Fact]
    public void AutoRecording_CanBeControlledAtRuntime()
    {
        var grid = new TemporalSpatialGrid<string>(capacity: 100, cellSize: 64.0f, frameDuration: 10, autoRecord: false);

        grid.Insert(10.0f, 10.0f, "obj1");
        System.Threading.Thread.Sleep(20);
        grid.Insert(20.0f, 20.0f, "obj2");
        
        // Should not have auto-recorded
        grid.SnapshotCount.Should().Be(0);

        // Start auto-recording
        grid.StartAutoRecording(10);
        System.Threading.Thread.Sleep(20);
        grid.Insert(30.0f, 30.0f, "obj3");
        
        // Should have auto-recorded
        grid.SnapshotCount.Should().BeGreaterOrEqualTo(1);

        // Stop auto-recording
        grid.StopAutoRecording();
        var countBeforeStop = grid.SnapshotCount;
        System.Threading.Thread.Sleep(20);
        grid.Insert(40.0f, 40.0f, "obj4");
        
        // Should not have recorded new snapshots
        grid.SnapshotCount.Should().Be(countBeforeStop);
    }

    /// <summary>
    /// Tests that GetStats returns comprehensive temporal-spatial statistics.
    /// Statistics should accurately reflect the current state of the grid.
    /// </summary>
    [Fact]
    public void GetStats_ReturnsComprehensiveStatistics()
    {
        var grid = new TemporalSpatialGrid<string>(capacity: 100, cellSize: 32.0f, frameDuration: 16, autoRecord: false);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        grid.Insert(10.0f, 10.0f, "obj1");
        grid.Insert(20.0f, 20.0f, "obj2");
        grid.RecordSnapshot(timestamp);

        var stats = grid.GetStats();

        stats.SnapshotCount.Should().Be(1);
        stats.SpatialCellSize.Should().Be(32.0f);
        stats.AutoRecording.Should().BeFalse();
        stats.TotalMemoryUsage.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Tests that StepInTime allows stepping through temporal snapshots.
    /// The method should allow navigation through recorded history.
    /// </summary>
    [Fact]
    public void StepInTime_AllowsTemporalNavigation()
    {
        var grid = new TemporalSpatialGrid<string>();
        var baseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Create multiple snapshots
        grid.Insert(10.0f, 10.0f, "obj1");
        grid.RecordSnapshot(baseTime);

        grid.Insert(20.0f, 20.0f, "obj2");
        grid.RecordSnapshot(baseTime + 100);

        grid.Insert(30.0f, 30.0f, "obj3");
        grid.RecordSnapshot(baseTime + 200);

        // Step through time
        var spatialState1 = grid.StepInTime(forward: false); // Go back
        spatialState1.Should().NotBeNull();

        var spatialState2 = grid.StepInTime(forward: true); // Go forward
        spatialState2.Should().NotBeNull();
    }

    /// <summary>
    /// Tests the ReplaySpatialHistory method for temporal-spatial playback.
    /// The method should return snapshots in chronological order.
    /// </summary>
    [Fact]
    public void ReplaySpatialHistory_ReturnsChronologicalSnapshots()
    {
        var grid = new TemporalSpatialGrid<string>();
        var baseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Create timeline
        grid.Insert(10.0f, 10.0f, "obj1");
        grid.RecordSnapshot(baseTime);

        grid.Insert(20.0f, 20.0f, "obj2");
        grid.RecordSnapshot(baseTime + 500);

        grid.Insert(30.0f, 30.0f, "obj3");
        grid.RecordSnapshot(baseTime + 1000);

        var replay = grid.ReplaySpatialHistory(baseTime, baseTime + 1000).ToList();

        replay.Should().HaveCount(3);
        replay[0].Timestamp.Should().Be(baseTime);
        replay[1].Timestamp.Should().Be(baseTime + 500);
        replay[2].Timestamp.Should().Be(baseTime + 1000);
    }
}
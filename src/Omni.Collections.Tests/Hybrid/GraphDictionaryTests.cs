using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Omni.Collections.Core.Time;
using Omni.Collections.Hybrid.GraphDictionary;
using Xunit;

namespace Omni.Collections.Tests.Hybrid;

public class GraphDictionaryTests
{
    private sealed class FakeClock : IClock
    {
        private DateTimeOffset _now;
        private long _timestamp;

        public FakeClock(DateTimeOffset start)
        {
            _now = start;
            _timestamp = 0;
        }

        public DateTimeOffset UtcNow => _now;
        public long GetTimestamp() => _timestamp;

        public void Advance(TimeSpan delta)
        {
            _now = _now.Add(delta);
            _timestamp += delta.Ticks;
        }

        public void SetUtcNow(DateTimeOffset value)
        {
            _now = value;
        }
    }

    [Fact]
    public void Constructor_Default_CreatesEmptyGraph()
    {
        using var graph = new GraphDictionary<string, int>();

        graph.Count.Should().Be(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(64)]
    [InlineData(1000)]
    public void Constructor_WithCapacity_CreatesEmptyGraph(int capacity)
    {
        using var graph = new GraphDictionary<string, int>(capacity);

        graph.Count.Should().Be(0);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_NegativeCapacity_Throws(int capacity)
    {
        Action act = () => new GraphDictionary<string, int>(capacity);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_NullClock_Throws()
    {
        Action act = () => new GraphDictionary<string, int>(0, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithClock_SucceedsAndUsesClockForEdges()
    {
        var clock = new FakeClock(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
        using var graph = new GraphDictionary<string, int>(8, clock);

        graph.Add("a", 1);
        graph.Add("b", 2);
        graph.AddEdge("a", "b").Should().BeTrue();
        graph.TryGetEdgeCreated("a", "b", out var created).Should().BeTrue();
        created.Should().Be(clock.UtcNow.UtcDateTime);
    }

    [Fact]
    public void AddVertex_NewKey_IncreasesCount()
    {
        using var graph = new GraphDictionary<string, int>();

        graph.Add("a", 1);
        graph.Add("b", 2);

        graph.Count.Should().Be(2);
        graph.ContainsKey("a").Should().BeTrue();
        graph.ContainsKey("b").Should().BeTrue();
    }

    [Fact]
    public void AddVertex_DuplicateKey_Throws()
    {
        using var graph = new GraphDictionary<string, int>();
        graph.Add("a", 1);

        Action act = () => graph.Add("a", 2);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Indexer_Set_OnNewKey_AddsVertex()
    {
        using var graph = new GraphDictionary<string, int>();

        graph["x"] = 5;

        graph.Count.Should().Be(1);
        graph["x"].Should().Be(5);
    }

    [Fact]
    public void Indexer_Set_OnExistingKey_Updates()
    {
        using var graph = new GraphDictionary<string, int>();
        graph.Add("x", 5);

        graph["x"] = 10;

        graph["x"].Should().Be(10);
        graph.Count.Should().Be(1);
    }

    [Fact]
    public void Indexer_Get_MissingKey_Throws()
    {
        using var graph = new GraphDictionary<string, int>();

        Action act = () => { var _ = graph["missing"]; };
        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void TryGetValue_PresentKey_ReturnsTrue()
    {
        using var graph = new GraphDictionary<string, int>();
        graph.Add("a", 42);

        graph.TryGetValue("a", out var value).Should().BeTrue();
        value.Should().Be(42);
    }

    [Fact]
    public void TryGetValue_MissingKey_ReturnsFalse()
    {
        using var graph = new GraphDictionary<string, int>();

        graph.TryGetValue("missing", out var value).Should().BeFalse();
        value.Should().Be(0);
    }

    [Fact]
    public void AddEdge_BetweenExistingVertices_ReturnsTrue()
    {
        using var graph = new GraphDictionary<string, int>();
        graph.Add("a", 1);
        graph.Add("b", 2);

        graph.AddEdge("a", "b").Should().BeTrue();
        graph.HasEdge("a", "b").Should().BeTrue();
        graph.HasEdge("b", "a").Should().BeFalse();
    }

    [Fact]
    public void AddEdge_MissingVertex_ReturnsFalse()
    {
        using var graph = new GraphDictionary<string, int>();
        graph.Add("a", 1);

        graph.AddEdge("a", "ghost").Should().BeFalse();
        graph.AddEdge("ghost", "a").Should().BeFalse();
    }

    [Fact]
    public void AddEdge_Duplicate_ReturnsFalse()
    {
        using var graph = new GraphDictionary<string, int>();
        graph.Add("a", 1);
        graph.Add("b", 2);
        graph.AddEdge("a", "b");

        graph.AddEdge("a", "b").Should().BeFalse();
    }

    [Fact]
    public void AddEdge_WithWeightAndMetadata_StoresValues()
    {
        using var graph = new GraphDictionary<string, int>();
        graph.Add("a", 1);
        graph.Add("b", 2);

        graph.AddEdge("a", "b", weight: 3.5, metadata: "label").Should().BeTrue();

        var edges = graph.GetNeighborsWithEdgeInfo("a").ToList();
        edges.Should().ContainSingle();
        edges[0].Key.Should().Be("b");
        edges[0].Weight.Should().Be(3.5);
        edges[0].Metadata.Should().Be("label");
    }

    [Fact]
    public void AddBidirectionalEdge_AddsBothDirections()
    {
        using var graph = new GraphDictionary<string, int>();
        graph.Add("a", 1);
        graph.Add("b", 2);

        graph.AddBidirectionalEdge("a", "b").Should().BeTrue();

        graph.HasEdge("a", "b").Should().BeTrue();
        graph.HasEdge("b", "a").Should().BeTrue();
    }

    [Fact]
    public void RemoveEdge_ExistingEdge_ReturnsTrue()
    {
        using var graph = new GraphDictionary<string, int>();
        graph.Add("a", 1);
        graph.Add("b", 2);
        graph.AddEdge("a", "b");

        graph.RemoveEdge("a", "b").Should().BeTrue();
        graph.HasEdge("a", "b").Should().BeFalse();
    }

    [Fact]
    public void RemoveEdge_MissingEdge_ReturnsFalse()
    {
        using var graph = new GraphDictionary<string, int>();
        graph.Add("a", 1);
        graph.Add("b", 2);

        graph.RemoveEdge("a", "b").Should().BeFalse();
    }

    [Fact]
    public void RemoveVertex_RemovesIncomingEdges()
    {
        using var graph = new GraphDictionary<string, int>();
        graph.Add("a", 1);
        graph.Add("b", 2);
        graph.Add("c", 3);
        graph.AddEdge("a", "b");
        graph.AddEdge("c", "b");

        graph.Remove("b").Should().BeTrue();

        graph.ContainsKey("b").Should().BeFalse();
        graph.HasEdge("a", "b").Should().BeFalse();
        graph.HasEdge("c", "b").Should().BeFalse();
        graph.GetNeighbors("a").Should().BeEmpty();
        graph.GetNeighbors("c").Should().BeEmpty();
    }

    [Fact]
    public void RemoveVertex_RemovesOutgoingEdges()
    {
        using var graph = new GraphDictionary<string, int>();
        graph.Add("a", 1);
        graph.Add("b", 2);
        graph.Add("c", 3);
        graph.AddEdge("a", "b");
        graph.AddEdge("a", "c");

        graph.Remove("a").Should().BeTrue();

        graph.ContainsKey("a").Should().BeFalse();
        graph.HasEdge("a", "b").Should().BeFalse();
        graph.HasEdge("a", "c").Should().BeFalse();
    }

    [Fact]
    public void Remove_MissingKey_ReturnsFalse()
    {
        using var graph = new GraphDictionary<string, int>();

        graph.Remove("missing").Should().BeFalse();
    }

    [Fact]
    public void GetNeighbors_VertexWithEdges_ReturnsAllOutgoing()
    {
        using var graph = new GraphDictionary<string, int>();
        graph.Add("a", 1);
        graph.Add("b", 2);
        graph.Add("c", 3);
        graph.AddEdge("a", "b");
        graph.AddEdge("a", "c");

        var neighbors = graph.GetNeighbors("a").OrderBy(n => n).ToList();

        neighbors.Should().Equal("b", "c");
    }

    [Fact]
    public void GetNeighbors_NoEdges_ReturnsEmpty()
    {
        using var graph = new GraphDictionary<string, int>();
        graph.Add("a", 1);

        graph.GetNeighbors("a").Should().BeEmpty();
    }

    [Fact]
    public void GetNeighbors_MissingVertex_ReturnsEmpty()
    {
        using var graph = new GraphDictionary<string, int>();

        graph.GetNeighbors("missing").Should().BeEmpty();
    }

    [Fact]
    public void Keys_ReturnsAllVertexKeys()
    {
        using var graph = new GraphDictionary<string, int>();
        graph.Add("a", 1);
        graph.Add("b", 2);

        graph.Keys.OrderBy(k => k).Should().Equal("a", "b");
    }

    [Fact]
    public void Values_ReturnsAllVertexValues()
    {
        using var graph = new GraphDictionary<string, int>();
        graph.Add("a", 1);
        graph.Add("b", 2);

        graph.Values.OrderBy(v => v).Should().Equal(1, 2);
    }

    [Fact]
    public void FindShortestPath_DirectlyConnected_ReturnsTwoStepPath()
    {
        using var graph = new GraphDictionary<string, int>();
        graph.Add("a", 1);
        graph.Add("b", 2);
        graph.AddEdge("a", "b", weight: 2.0);

        var path = graph.FindShortestPath("a", "b");

        path.Should().NotBeNull();
        path!.Path.Should().Equal("a", "b");
        path.TotalWeight.Should().Be(2.0);
    }

    [Fact]
    public void FindShortestPath_PrefersLowerWeight()
    {
        using var graph = new GraphDictionary<string, int>();
        graph.Add("a", 1);
        graph.Add("b", 2);
        graph.Add("c", 3);
        graph.AddEdge("a", "b", weight: 10.0);
        graph.AddEdge("a", "c", weight: 1.0);
        graph.AddEdge("c", "b", weight: 1.0);

        var path = graph.FindShortestPath("a", "b");

        path.Should().NotBeNull();
        path!.Path.Should().Equal("a", "c", "b");
        path.TotalWeight.Should().Be(2.0);
    }

    [Fact]
    public void FindShortestPath_Unreachable_ReturnsNull()
    {
        using var graph = new GraphDictionary<string, int>();
        graph.Add("a", 1);
        graph.Add("b", 2);

        graph.FindShortestPath("a", "b").Should().BeNull();
    }

    [Fact]
    public void FindShortestPath_MissingEndpoint_ReturnsNull()
    {
        using var graph = new GraphDictionary<string, int>();
        graph.Add("a", 1);

        graph.FindShortestPath("a", "ghost").Should().BeNull();
        graph.FindShortestPath("ghost", "a").Should().BeNull();
    }

    [Fact]
    public void FindNodesWithinDistance_ReturnsReachableWithinBudget()
    {
        using var graph = new GraphDictionary<string, int>();
        graph.Add("a", 1);
        graph.Add("b", 2);
        graph.Add("c", 3);
        graph.Add("d", 4);
        graph.AddEdge("a", "b", weight: 1.0);
        graph.AddEdge("b", "c", weight: 1.0);
        graph.AddEdge("c", "d", weight: 5.0);

        var reachable = graph.FindNodesWithinDistance("a", 2.0).ToDictionary(t => t.Key, t => t.Distance);

        reachable.Should().ContainKey("b");
        reachable.Should().ContainKey("c");
        reachable.Should().NotContainKey("d");
    }

    [Fact]
    public void FindStronglyConnectedComponents_DetectsCycle()
    {
        using var graph = new GraphDictionary<string, int>();
        graph.Add("a", 1);
        graph.Add("b", 2);
        graph.Add("c", 3);
        graph.Add("d", 4);
        graph.AddEdge("a", "b");
        graph.AddEdge("b", "c");
        graph.AddEdge("c", "a");
        graph.AddEdge("c", "d");

        var components = graph.FindStronglyConnectedComponents()
            .Select(c => c.OrderBy(k => k).ToList())
            .ToList();

        components.Should().Contain(comp => comp.SequenceEqual(new[] { "a", "b", "c" }));
        components.Should().Contain(comp => comp.SequenceEqual(new[] { "d" }));
    }

    [Fact]
    public void FindStronglyConnectedComponents_NoCycles_ReturnsAllSingletons()
    {
        using var graph = new GraphDictionary<string, int>();
        graph.Add("a", 1);
        graph.Add("b", 2);
        graph.Add("c", 3);
        graph.AddEdge("a", "b");
        graph.AddEdge("b", "c");

        var components = graph.FindStronglyConnectedComponents().ToList();

        components.Should().HaveCount(3);
        components.Should().AllSatisfy(comp => comp.Should().HaveCount(1));
    }

    [Fact]
    public void GetClusteringCoefficient_FullyConnectedNeighbors_ReturnsOne()
    {
        using var graph = new GraphDictionary<string, int>();
        graph.Add("a", 1);
        graph.Add("b", 2);
        graph.Add("c", 3);
        graph.AddBidirectionalEdge("a", "b");
        graph.AddBidirectionalEdge("a", "c");
        graph.AddBidirectionalEdge("b", "c");

        graph.GetClusteringCoefficient("a").Should().BeApproximately(1.0, 1e-9);
    }

    [Fact]
    public void GetClusteringCoefficient_DisconnectedNeighbors_ReturnsZero()
    {
        using var graph = new GraphDictionary<string, int>();
        graph.Add("a", 1);
        graph.Add("b", 2);
        graph.Add("c", 3);
        graph.AddEdge("a", "b");
        graph.AddEdge("a", "c");

        graph.GetClusteringCoefficient("a").Should().Be(0.0);
    }

    [Fact]
    public void GetStatistics_ReturnsAccurateCounts()
    {
        using var graph = new GraphDictionary<string, int>();
        graph.Add("a", 1);
        graph.Add("b", 2);
        graph.Add("c", 3);
        graph.AddEdge("a", "b");
        graph.AddEdge("b", "c");

        var stats = graph.GetStatistics();

        stats.NodeCount.Should().Be(3);
        stats.EdgeCount.Should().Be(2);
    }

    [Fact]
    public void Enumeration_YieldsAllKeyValuePairs()
    {
        using var graph = new GraphDictionary<string, int>();
        graph.Add("a", 1);
        graph.Add("b", 2);
        graph.Add("c", 3);

        var collected = graph.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        collected.Should().HaveCount(3);
        collected["a"].Should().Be(1);
        collected["b"].Should().Be(2);
        collected["c"].Should().Be(3);
    }

    [Fact]
    public void GetChunkedEnumerator_InvalidChunkSize_Throws()
    {
        using var graph = new GraphDictionary<string, int>();
        graph.Add("a", 1);

        Action act = () => graph.GetChunkedEnumerator(0).ToList();
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetChunkedEnumerator_YieldsAllElements()
    {
        using var graph = new GraphDictionary<string, int>();
        for (int i = 0; i < 50; i++)
            graph.Add(i.ToString(), i);

        var collected = graph.GetChunkedEnumerator(8).ToList();

        collected.Should().HaveCount(50);
        collected.Select(kvp => kvp.Value).OrderBy(v => v).Should().Equal(Enumerable.Range(0, 50));
    }

    [Fact]
    public void LargeGraph_InsertVerticesAndEdges_QueryConnectivity()
    {
        using var graph = new GraphDictionary<int, int>(1000);
        var rng = new Random(42);

        for (int i = 0; i < 1000; i++)
            graph.Add(i, i);

        var added = 0;
        var attempts = 0;
        while (added < 5000 && attempts < 50000)
        {
            attempts++;
            int from = rng.Next(1000);
            int to = rng.Next(1000);
            if (from == to) continue;
            if (graph.AddEdge(from, to))
                added++;
        }

        graph.Count.Should().Be(1000);
        added.Should().Be(5000);

        var stats = graph.GetStatistics();
        stats.NodeCount.Should().Be(1000);
        stats.EdgeCount.Should().Be(5000);
    }

    [Fact]
    public void LargeGraph_ChainPath_FindShortestPath_FindsLinearChain()
    {
        using var graph = new GraphDictionary<int, int>(200);
        for (int i = 0; i < 200; i++)
            graph.Add(i, i);
        for (int i = 0; i < 199; i++)
            graph.AddEdge(i, i + 1, weight: 1.0);

        var path = graph.FindShortestPath(0, 199);

        path.Should().NotBeNull();
        path!.Path.Should().HaveCount(200);
        path.TotalWeight.Should().Be(199.0);
    }

    [Fact]
    public void EdgeCreated_ReflectsClockAtAddTime()
    {
        var clock = new FakeClock(new DateTimeOffset(2024, 6, 1, 12, 0, 0, TimeSpan.Zero));
        using var graph = new GraphDictionary<string, int>(0, clock);
        graph.Add("a", 1);
        graph.Add("b", 2);
        graph.Add("c", 3);

        var t0 = clock.UtcNow.UtcDateTime;
        graph.AddEdge("a", "b").Should().BeTrue();
        graph.TryGetEdgeCreated("a", "b", out var createdAB).Should().BeTrue();
        createdAB.Should().Be(t0);

        clock.Advance(TimeSpan.FromMinutes(5));
        var t1 = clock.UtcNow.UtcDateTime;
        graph.AddEdge("b", "c").Should().BeTrue();
        graph.TryGetEdgeCreated("b", "c", out var createdBC).Should().BeTrue();
        createdBC.Should().Be(t1);
        createdBC.Should().BeAfter(createdAB);
    }

    [Fact]
    public void TryGetEdgeCreated_MissingEdge_ReturnsFalse()
    {
        using var graph = new GraphDictionary<string, int>();
        graph.Add("a", 1);

        graph.TryGetEdgeCreated("a", "b", out var created).Should().BeFalse();
        created.Should().Be(default);
    }

    [Fact]
    public void Dispose_PreventsFurtherUseOfLock()
    {
        var graph = new GraphDictionary<string, int>();
        graph.Add("a", 1);

        graph.Dispose();
        graph.Dispose();
    }

    [Fact]
    public void ClearCache_DoesNotAffectGraphState()
    {
        using var graph = new GraphDictionary<string, int>();
        graph.Add("a", 1);
        graph.Add("b", 2);
        graph.AddEdge("a", "b");

        graph.ClearCache();

        graph.Count.Should().Be(2);
        graph.HasEdge("a", "b").Should().BeTrue();
    }
}

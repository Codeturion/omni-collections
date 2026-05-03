#if NET8_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FluentAssertions;
using Omni.Collections.Grid2D.HexGrid;
using Xunit;

namespace Omni.Collections.Tests.Grid2D;

public class HexPathfindingTests
{
    private static readonly Func<HexCoord, bool> NoBlocked = _ => false;
    private static readonly Func<HexCoord, double> UnitCost = _ => 1.0;

    private static bool IsHexNeighbor(HexCoord a, HexCoord b)
    {
        return a != b && a.DistanceTo(b) == 1;
    }

    [Fact]
    public void FindPath_StartEqualsGoal_ReturnsSingleCoord()
    {
        var path = HexPathfinding.FindPath(HexCoord.Origin, HexCoord.Origin, NoBlocked, UnitCost).ToList();

        path.Should().Equal(HexCoord.Origin);
    }

    [Fact]
    public void FindPath_DirectNeighbor_ReturnsTwoStepPath()
    {
        var start = HexCoord.Origin;
        var goal = new HexCoord(1, 0);

        var path = HexPathfinding.FindPath(start, goal, NoBlocked, UnitCost).ToList();

        path.Should().HaveCount(2);
        path[0].Should().Be(start);
        path[1].Should().Be(goal);
    }

    [Fact]
    public void FindPath_GoalIsBlocked_ReturnsEmpty()
    {
        var start = HexCoord.Origin;
        var goal = new HexCoord(2, 0);

        var path = HexPathfinding.FindPath(start, goal, c => c == goal, UnitCost).ToList();

        path.Should().BeEmpty();
    }

    [Fact]
    public void FindPath_LongerStraightPath_ReturnsConsecutiveNeighbors()
    {
        var start = HexCoord.Origin;
        var goal = new HexCoord(5, 0);

        var path = HexPathfinding.FindPath(start, goal, NoBlocked, UnitCost).ToList();

        path.Should().NotBeEmpty();
        path.First().Should().Be(start);
        path.Last().Should().Be(goal);
        path.Should().HaveCount(start.DistanceTo(goal) + 1);
        for (int i = 1; i < path.Count; i++)
            IsHexNeighbor(path[i - 1], path[i]).Should().BeTrue($"step {i} from {path[i - 1]} to {path[i]} must be a hex neighbor");
    }

    [Fact]
    public void FindPath_ObstacleOnLine_RoutesAround()
    {
        var start = HexCoord.Origin;
        var goal = new HexCoord(3, 0);
        var obstacle = new HexCoord(1, 0);

        var path = HexPathfinding.FindPath(start, goal, c => c == obstacle, UnitCost).ToList();

        path.Should().NotBeEmpty();
        path.First().Should().Be(start);
        path.Last().Should().Be(goal);
        path.Should().NotContain(obstacle);
        for (int i = 1; i < path.Count; i++)
            IsHexNeighbor(path[i - 1], path[i]).Should().BeTrue();
    }

    [Fact]
    public void FindPath_TargetSurroundedByObstacles_ReturnsEmpty()
    {
        // Bound the search to a finite arena: target is encircled, and anything outside
        // distance 8 from start is also blocked. The A* explorer therefore exhausts the
        // open set and returns no path. (HexPathfinding has no built-in iteration cap.)
        var start = HexCoord.Origin;
        var target = new HexCoord(3, 0);
        var blockers = new HashSet<HexCoord>(target.GetNeighbors());
        Func<HexCoord, bool> isBlocked = c => blockers.Contains(c) || start.DistanceTo(c) > 8;

        var path = HexPathfinding.FindPath(start, target, isBlocked, UnitCost).ToList();

        path.Should().BeEmpty();
    }

    [Fact]
    public void FindPath_AllStepsAreValidHexNeighbors()
    {
        var start = HexCoord.Origin;
        var goal = new HexCoord(4, -2);

        var path = HexPathfinding.FindPath(start, goal, NoBlocked, UnitCost).ToList();

        path.Should().NotBeEmpty();
        for (int i = 1; i < path.Count; i++)
            IsHexNeighbor(path[i - 1], path[i]).Should().BeTrue();
    }

    [Fact]
    public void FindPath_HighCostCorridor_PrefersLowerCostRoute()
    {
        var start = HexCoord.Origin;
        var goal = new HexCoord(2, 0);
        var expensiveCell = new HexCoord(1, 0);

        Func<HexCoord, double> getCost = c => c == expensiveCell ? 100.0 : 1.0;

        var path = HexPathfinding.FindPath(start, goal, NoBlocked, getCost).ToList();

        path.Should().NotBeEmpty();
        path.First().Should().Be(start);
        path.Last().Should().Be(goal);
        path.Should().NotContain(expensiveCell);
    }

    [Fact]
    public void FindPath_LongPath_CompletesWithinTimeBudget()
    {
        var start = HexCoord.Origin;
        var goal = new HexCoord(60, 0);

        var sw = Stopwatch.StartNew();
        var path = HexPathfinding.FindPath(start, goal, NoBlocked, UnitCost).ToList();
        sw.Stop();

        sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
        path.Should().NotBeEmpty();
        path.Last().Should().Be(goal);
    }

    [Fact]
    public void GetReachable_ZeroBudget_YieldsOnlyStart()
    {
        var start = HexCoord.Origin;

        var reachable = HexPathfinding.GetReachable(start, 0.0, NoBlocked, UnitCost).ToList();

        reachable.Should().ContainSingle();
        reachable[0].coord.Should().Be(start);
        reachable[0].remainingMovement.Should().Be(0.0);
    }

    [Fact]
    public void GetReachable_BudgetTwo_IncludesAllCoordsWithinDistance()
    {
        var start = HexCoord.Origin;

        var reachable = HexPathfinding.GetReachable(start, 2.0, NoBlocked, UnitCost).Select(t => t.coord).ToHashSet();

        var expected = start.GetWithinDistance(2).ToHashSet();
        reachable.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void GetReachable_BlockedCells_AreExcluded()
    {
        var start = HexCoord.Origin;
        var blocked = new HexCoord(1, 0);

        var reachable = HexPathfinding.GetReachable(start, 3.0, c => c == blocked, UnitCost).Select(t => t.coord).ToList();

        reachable.Should().NotContain(blocked);
        reachable.Should().Contain(start);
    }

    [Fact]
    public void CalculatePathCost_EmptyPath_ReturnsZero()
    {
        HexPathfinding.CalculatePathCost(Array.Empty<HexCoord>(), UnitCost).Should().Be(0.0);
    }

    [Fact]
    public void CalculatePathCost_SingleStep_ReturnsZero()
    {
        var path = new[] { HexCoord.Origin };

        HexPathfinding.CalculatePathCost(path, UnitCost).Should().Be(0.0);
    }

    [Fact]
    public void CalculatePathCost_MultipleSteps_SkipsFirstCoord()
    {
        var path = new[]
        {
            HexCoord.Origin,
            new HexCoord(1, 0),
            new HexCoord(2, 0),
            new HexCoord(3, 0)
        };

        HexPathfinding.CalculatePathCost(path, c => c.Q * 1.0).Should().Be(1.0 + 2.0 + 3.0);
    }

    [Fact]
    public void HasLineOfSight_NoBlockers_ReturnsTrue()
    {
        var start = HexCoord.Origin;
        var end = new HexCoord(3, 0);

        HexPathfinding.HasLineOfSight(start, end, NoBlocked).Should().BeTrue();
    }

    [Fact]
    public void HasLineOfSight_BlockerOnLine_ReturnsFalse()
    {
        var start = HexCoord.Origin;
        var end = new HexCoord(4, 0);
        var blocker = new HexCoord(2, 0);

        HexPathfinding.HasLineOfSight(start, end, c => c == blocker).Should().BeFalse();
    }

    [Fact]
    public void HasLineOfSight_BlockerAtEndpoint_DoesNotBlock()
    {
        var start = HexCoord.Origin;
        var end = new HexCoord(3, 0);

        HexPathfinding.HasLineOfSight(start, end, c => c == end).Should().BeTrue();
        HexPathfinding.HasLineOfSight(start, end, c => c == start).Should().BeTrue();
    }

    [Fact]
    public void GetVisibleCoords_NoBlockers_ReturnsAllWithinRange()
    {
        var center = HexCoord.Origin;

        var visible = HexPathfinding.GetVisibleCoords(center, 2, NoBlocked).ToHashSet();

        var expected = center.GetWithinDistance(2).ToHashSet();
        visible.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void GetVisibleCoords_RangeZero_ReturnsCenterOnly()
    {
        var center = HexCoord.Origin;

        var visible = HexPathfinding.GetVisibleCoords(center, 0, NoBlocked).ToList();

        visible.Should().Equal(center);
    }

    [Fact]
    public void GetVisibleCoords_BlockerHidesFurtherCoords()
    {
        var center = HexCoord.Origin;
        var blocker = new HexCoord(1, 0);

        var visible = HexPathfinding.GetVisibleCoords(center, 3, c => c == blocker).ToHashSet();

        visible.Should().Contain(center);
        visible.Should().NotContain(new HexCoord(3, 0));
    }

    [Fact]
    public void FindAttackPositions_RangeOne_FindsAdjacentReachable()
    {
        var attacker = HexCoord.Origin;
        var target = new HexCoord(2, 0);

        var positions = HexPathfinding.FindAttackPositions(
            attacker, target, range: 1, movementPoints: 5,
            isBlocked: NoBlocked, blocksLineOfSight: NoBlocked, getCost: UnitCost).ToList();

        positions.Should().NotBeEmpty();
        positions.Should().AllSatisfy(p => p.position.DistanceTo(target).Should().BeLessThanOrEqualTo(1));
    }

    [Fact]
    public void FindAttackPositions_TargetOutOfReach_ReturnsEmpty()
    {
        var attacker = HexCoord.Origin;
        var target = new HexCoord(50, 0);

        var positions = HexPathfinding.FindAttackPositions(
            attacker, target, range: 1, movementPoints: 2,
            isBlocked: NoBlocked, blocksLineOfSight: NoBlocked, getCost: UnitCost).ToList();

        positions.Should().BeEmpty();
    }

    [Fact]
    public void CalculateThreatMap_NoEnemies_AllZero()
    {
        var area = HexCoord.Origin.GetWithinDistance(2).ToList();

        var threats = HexPathfinding.CalculateThreatMap(Array.Empty<HexCoord>(), 2, NoBlocked, area);

        threats.Values.Should().AllSatisfy(v => v.Should().Be(0));
    }

    [Fact]
    public void CalculateThreatMap_SingleEnemy_IncrementsVisibleCells()
    {
        var enemy = HexCoord.Origin;
        var area = enemy.GetWithinDistance(2).ToList();

        var threats = HexPathfinding.CalculateThreatMap(new[] { enemy }, 1, NoBlocked, area);

        threats[enemy].Should().Be(1);
        foreach (var neighbor in enemy.GetNeighbors())
            threats[neighbor].Should().Be(1);
    }

    [Fact]
    public void CalculateThreatMap_MultipleEnemies_StacksCounts()
    {
        var e1 = HexCoord.Origin;
        var e2 = new HexCoord(0, 0);
        var area = e1.GetWithinDistance(2).ToList();

        var threats = HexPathfinding.CalculateThreatMap(new[] { e1, e2 }, 1, NoBlocked, area);

        threats[e1].Should().Be(2);
    }

    [Fact]
    public void FindSafePositions_OrdersByThreatThenCost()
    {
        var start = HexCoord.Origin;
        var area = start.GetWithinDistance(2).ToList();
        var threatMap = new Dictionary<HexCoord, int>();
        foreach (var c in area)
            threatMap[c] = c.Q == 0 && c.R == 0 ? 5 : (c.DistanceTo(start) == 1 ? 1 : 0);

        var safe = HexPathfinding.FindSafePositions(start, 2.0, threatMap, NoBlocked, UnitCost).ToList();

        safe.Should().NotBeEmpty();
        for (int i = 1; i < safe.Count; i++)
        {
            (safe[i - 1].threatLevel <= safe[i].threatLevel).Should().BeTrue();
            if (safe[i - 1].threatLevel == safe[i].threatLevel)
                (safe[i - 1].movementCost <= safe[i].movementCost).Should().BeTrue();
        }
    }
}
#endif

using System;
using System.Collections.Generic;

namespace Omni.Collections.Grid2D.HexGrid;

public static class HexPathfinding
{
    public static IEnumerable<HexCoord> FindPath(HexCoord start, HexCoord goal,
        Func<HexCoord, bool> isBlocked,
        Func<HexCoord, double> getCost)
    {
        if (start == goal)
        {
            yield return start;
            yield break;
        }
        if (isBlocked(goal))
            yield break;
        var openSet = new PriorityQueue<HexCoord, double>();
        var cameFrom = new Dictionary<HexCoord, HexCoord>();
        var gScore = new Dictionary<HexCoord, double> { [start] = 0 };
        var fScore = new Dictionary<HexCoord, double> { [start] = start.DistanceTo(goal) };
        openSet.Enqueue(start, fScore[start]);
        while (openSet.Count > 0)
        {
            var current = openSet.Dequeue();
            if (current == goal)
            {
                var path = new List<HexCoord>();
                while (cameFrom.ContainsKey(current))
                {
                    path.Add(current);
                    current = cameFrom[current];
                }
                path.Add(start);
                path.Reverse();
                foreach (var coord in path)
                    yield return coord;
                yield break;
            }
            foreach (var neighbor in current.GetNeighbors())
            {
                if (isBlocked(neighbor))
                    continue;
                double tentativeGScore = gScore[current] + getCost(neighbor);
                if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = tentativeGScore + neighbor.DistanceTo(goal);
                    openSet.Enqueue(neighbor, fScore[neighbor]);
                }
            }
        }
    }

    public static IEnumerable<(HexCoord coord, double remainingMovement)> GetReachable(HexCoord start, double movementPoints,
        Func<HexCoord, bool> isBlocked,
        Func<HexCoord, double> getCost)
    {
        var distances = new Dictionary<HexCoord, double> { [start] = 0 };
        var queue = new PriorityQueue<HexCoord, double>();
        queue.Enqueue(start, 0);
        yield return (start, movementPoints);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var currentDistance = distances[current];
            if (currentDistance > movementPoints)
                continue;
            foreach (var neighbor in current.GetNeighbors())
            {
                if (isBlocked(neighbor))
                    continue;
                double newDistance = currentDistance + getCost(neighbor);
                if (newDistance <= movementPoints &&
                    (!distances.ContainsKey(neighbor) || newDistance < distances[neighbor]))
                {
                    distances[neighbor] = newDistance;
                    queue.Enqueue(neighbor, newDistance);
                    if (neighbor != start)
                        yield return (neighbor, movementPoints - newDistance);
                }
            }
        }
    }

    public static double CalculatePathCost(IEnumerable<HexCoord> path, Func<HexCoord, double> getCost)
    {
        double totalCost = 0;
        bool first = true;
        foreach (var coord in path)
        {
            if (first)
            {
                first = false;
                continue;
            }
            totalCost += getCost(coord);
        }
        return totalCost;
    }

    public static bool HasLineOfSight(HexCoord start, HexCoord end, Func<HexCoord, bool> isBlocked)
    {
        foreach (var coord in start.GetLineTo(end))
        {
            if (coord != start && coord != end && isBlocked(coord))
                return false;
        }
        return true;
    }

    public static IEnumerable<HexCoord> GetVisibleCoords(HexCoord center, int range, Func<HexCoord, bool> isBlocked)
    {
        yield return center;
        for (int distance = 1; distance <= range; distance++)
        {
            foreach (var coord in center.GetRing(distance))
            {
                if (HasLineOfSight(center, coord, isBlocked))
                    yield return coord;
            }
        }
    }

    public static IEnumerable<(HexCoord position, double movementCost)> FindAttackPositions(
        HexCoord attacker, HexCoord target, int range, double movementPoints,
        Func<HexCoord, bool> isBlocked,
        Func<HexCoord, bool> blocksLineOfSight,
        Func<HexCoord, double> getCost)
    {
        IEnumerable<(HexCoord coord, double remainingMovement)>? reachablePositions = GetReachable(attacker, movementPoints, isBlocked, getCost);
        foreach (var (position, remainingMovement) in reachablePositions)
        {
            if (position.DistanceTo(target) <= range)
            {
                if (HasLineOfSight(position, target, blocksLineOfSight))
                {
                    double movementCost = movementPoints - remainingMovement;
                    yield return (position, movementCost);
                }
            }
        }
    }

    public static Dictionary<HexCoord, int> CalculateThreatMap(
        IEnumerable<HexCoord> enemyPositions,
        int attackRange,
        Func<HexCoord, bool> blocksLineOfSight,
        IEnumerable<HexCoord> searchArea)
    {
        var threatMap = new Dictionary<HexCoord, int>();
        foreach (var coord in searchArea)
        {
            threatMap[coord] = 0;
        }
        foreach (var enemy in enemyPositions)
        {
            IEnumerable<HexCoord>? threatenedCoords = GetVisibleCoords(enemy, attackRange, blocksLineOfSight);
            foreach (var coord in threatenedCoords)
            {
                if (threatMap.ContainsKey(coord))
                {
                    threatMap[coord]++;
                }
            }
        }
        return threatMap;
    }

    public static IEnumerable<(HexCoord position, double movementCost, int threatLevel)> FindSafePositions(
        HexCoord start, double movementPoints,
        Dictionary<HexCoord, int> threatMap,
        Func<HexCoord, bool> isBlocked,
        Func<HexCoord, double> getCost)
    {
        IEnumerable<(HexCoord coord, double remainingMovement)>? reachablePositions = GetReachable(start, movementPoints, isBlocked, getCost);
        var safePositions = new List<(HexCoord position, double movementCost, int threatLevel)>();
        foreach (var (position, remainingMovement) in reachablePositions)
        {
            int threatLevel = threatMap.GetValueOrDefault(position, 0);
            double movementCost = movementPoints - remainingMovement;
            safePositions.Add((position, movementCost, threatLevel));
        }
        safePositions.Sort((a, b) =>
        {
            int threatComparison = a.threatLevel.CompareTo(b.threatLevel);
            return threatComparison != 0 ? threatComparison : a.movementCost.CompareTo(b.movementCost);
        });
        return safePositions;
    }
}
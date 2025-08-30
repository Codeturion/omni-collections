using System;

namespace Omni.Collections.Spatial.DistanceMetrics;

/// <summary>
/// Grid-based distance metric for axis-aligned movement with moderate computational overhead compared to Euclidean.
/// Calculates distance as sum of absolute differences, simulating movement restricted to horizontal and vertical directions.
/// Provides intuitive distance measurement for grid-based systems where diagonal movement is prohibited or costly.
/// Best suited for grid-based systems, urban navigation, and applications requiring axis-aligned movement patterns.
/// Gaming applications: tile-based strategy games, turn-based RPGs with grid movement, city-building games, puzzle games.
/// </summary>
public sealed class ManhattanDistance : IDistanceMetric
{
    public string Name => "Manhattan";
    public double CalculateDistance(double[] a, double[] b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException("Point dimensions must match");
        double sum = 0;
        for (int i = 0; i < a.Length; i++)
        {
            sum += Math.Abs(a[i] - b[i]);
        }
        return sum;
    }

    public double CalculateDistanceSquared(double[] a, double[] b)
    {
        double distance = CalculateDistance(a, b);
        return distance * distance;
    }
}
using System;

namespace Omni.Collections.Spatial.DistanceMetrics;

/// <summary>
/// Maximum-difference distance metric optimized for diagonal movement with competitive performance characteristics.
/// Calculates distance as maximum absolute difference across all dimensions, enabling uniform-cost diagonal movement.
/// Simulates movement where diagonal steps have the same cost as orthogonal steps (chess king movement pattern).
/// Best suited for grid-based games, image processing applications, and scenarios requiring uniform diagonal movement costs.
/// Gaming applications: chess-like movement, RTS unit pathfinding, hex-grid strategy games, image processing filters.
/// </summary>
public sealed class ChebyshevDistance : IDistanceMetric
{
    public string Name => "Chebyshev";
    public double CalculateDistance(double[] a, double[] b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException("Point dimensions must match");
        double max = 0;
        for (int i = 0; i < a.Length; i++)
        {
            double diff = Math.Abs(a[i] - b[i]);
            if (diff > max)
                max = diff;
        }
        return max;
    }

    public double CalculateDistanceSquared(double[] a, double[] b)
    {
        double distance = CalculateDistance(a, b);
        return distance * distance;
    }
}
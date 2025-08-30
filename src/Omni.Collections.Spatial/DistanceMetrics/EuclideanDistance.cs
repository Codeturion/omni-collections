using System;

namespace Omni.Collections.Spatial.DistanceMetrics;

/// <summary>
/// Standard geometric distance metric providing intuitive spatial measurements with optimal performance characteristics.
/// Calculates straight-line distance using the Pythagorean theorem, offering fastest computation across all KDTree operations
/// despite square root calculations. Provides natural distance representation matching human spatial perception.
/// Best suited for physics simulations, computer graphics, machine learning, and general spatial queries requiring realistic distances.
/// Gaming applications: collision detection, AI pathfinding with realistic movement, distance-based mechanics, physics engines.
/// </summary>
public sealed class EuclideanDistance : IDistanceMetric
{
    public string Name => "Euclidean";
    public double CalculateDistance(double[] a, double[] b)
    {
        return Math.Sqrt(CalculateDistanceSquared(a, b));
    }

    public double CalculateDistanceSquared(double[] a, double[] b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException("Point dimensions must match");
        double sum = 0;
        for (int i = 0; i < a.Length; i++)
        {
            double diff = a[i] - b[i];
            sum += diff * diff;
        }
        return sum;
    }
}
using System;

namespace Omni.Collections.Spatial.DistanceMetrics;

/// <summary>
/// Generalized distance metric with configurable p-parameter offering mathematical flexibility at computational cost.
/// Provides Manhattan (p=1), Euclidean (p=2), and higher-order distance calculations with increasing overhead for higher p-values.
/// Higher p-values require more expensive power calculations, making them suitable primarily for specialized mathematical applications.
/// Best suited for research scenarios, mathematical simulations, or when specific p-norm behavior is required.
/// Gaming applications: limited use cases, primarily for specialized physics calculations or mathematical simulations requiring specific norms.
/// </summary>
public sealed class MinkowskiDistance : IDistanceMetric
{
    private readonly double _p;
    private readonly double _invP;
    public string Name => $"Minkowski(p={_p})";
    public MinkowskiDistance(double p)
    {
        if (p < 1)
            throw new ArgumentException("Minkowski p parameter must be >= 1", nameof(p));
        _p = p;
        _invP = 1.0 / p;
    }

    public double CalculateDistance(double[] a, double[] b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException("Point dimensions must match");
        if (_p == 1)
        {
            double manhattanSum = 0;
            for (int i = 0; i < a.Length; i++)
            {
                manhattanSum += Math.Abs(a[i] - b[i]);
            }
            return manhattanSum;
        }
        if (_p == 2)
        {
            return Math.Sqrt(CalculateDistanceSquared(a, b));
        }
        double sum = 0;
        for (int i = 0; i < a.Length; i++)
        {
            sum += Math.Pow(Math.Abs(a[i] - b[i]), _p);
        }
        return Math.Pow(sum, _invP);
    }

    public double CalculateDistanceSquared(double[] a, double[] b)
    {
        if (_p == 2)
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
        double distance = CalculateDistance(a, b);
        return distance * distance;
    }
}
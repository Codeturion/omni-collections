namespace Omni.Collections.Spatial.DistanceMetrics;

public interface IDistanceMetric
{
    double CalculateDistance(double[] a, double[] b);
    double CalculateDistanceSquared(double[] a, double[] b);
    string Name { get; }
}
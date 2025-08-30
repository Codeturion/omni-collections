namespace Omni.Collections.Hybrid.GraphDictionary;

public class GraphStatistics
{
    public int NodeCount { get; set; }

    public long EdgeCount { get; set; }

    public double AverageDegree { get; set; }

    public int MaxDegree { get; set; }

    public int MinDegree { get; set; }

    public double Density { get; set; }

    public long LookupCount { get; set; }

    public long TraversalCount { get; set; }

    public int CacheHits { get; set; }

    public long MemoryUsageBytes { get; set; }

    public override string ToString()
    {
        return $"Nodes: {NodeCount}, Edges: {EdgeCount}, Avg Degree: {AverageDegree:F2}, " +
            $"Density: {Density:P2}, Lookups: {LookupCount}, Traversals: {TraversalCount}";
    }
}
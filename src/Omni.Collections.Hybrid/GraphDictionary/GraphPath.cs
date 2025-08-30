using System.Collections.Generic;
using System.Linq;

namespace Omni.Collections.Hybrid.GraphDictionary;

public class GraphPath<TKey>
{
    public IReadOnlyList<TKey> Path { get; }

    public double TotalWeight { get; }

    public int Length => Path.Count;
    public GraphPath(IEnumerable<TKey> path, double totalWeight)
    {
        Path = path.ToList();
        TotalWeight = totalWeight;
    }

    public override string ToString()
    {
        return $"Path: {string.Join(" ? ", Path)} (Weight: {TotalWeight:F2})";
    }
}
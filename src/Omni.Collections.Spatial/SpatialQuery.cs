using System;

namespace Omni.Collections.Spatial;

public readonly struct SpatialQuery : IEquatable<SpatialQuery>
{
    public readonly BoundingRectangle Bounds;
    public readonly SpatialQueryType QueryType;
    
    public SpatialQuery(BoundingRectangle bounds, SpatialQueryType queryType = SpatialQueryType.Intersection)
    {
        Bounds = bounds;
        QueryType = queryType;
    }

    public bool Equals(SpatialQuery other)
    {
        return Bounds.Equals(other.Bounds) && QueryType == other.QueryType;
    }

    public override bool Equals(object? obj) => obj is SpatialQuery other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Bounds, QueryType);
    public override string ToString() => $"{QueryType}: {Bounds}";
}
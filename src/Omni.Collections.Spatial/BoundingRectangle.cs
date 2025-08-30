using System;
using System.Runtime.CompilerServices;

namespace Omni.Collections.Spatial;

public readonly struct BoundingRectangle : IEquatable<BoundingRectangle>
{
    public readonly float MinX;
    public readonly float MinY;
    public readonly float MaxX;
    public readonly float MaxY;
    private readonly float _cachedArea;
    
    public BoundingRectangle(float minX, float minY, float maxX, float maxY)
    {
        MinX = minX;
        MinY = minY;
        MaxX = maxX;
        MaxY = maxY;
        _cachedArea = (maxX - minX) * (maxY - minY);
    }

    public BoundingRectangle(float x, float y) : this(x, y, x, y) { }

    public float Width => MaxX - MinX;
    public float Height => MaxY - MinY;
    public float Area => _cachedArea;
    public float CenterX => (MinX + MaxX) * 0.5f;
    public float CenterY => (MinY + MaxY) * 0.5f;
    public bool IsPoint => Width == 0 && Height == 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Intersects(in BoundingRectangle other)
    {
        return MinX <= other.MaxX && MaxX >= other.MinX &&
            MinY <= other.MaxY && MaxY >= other.MinY;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(in BoundingRectangle other)
    {
        return MinX <= other.MinX && MinY <= other.MinY &&
            MaxX >= other.MaxX && MaxY >= other.MaxY;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(float x, float y)
    {
        return x >= MinX && x <= MaxX && y >= MinY && y <= MaxY;
    }

    public BoundingRectangle Union(in BoundingRectangle other)
    {
        return new BoundingRectangle(
            Math.Min(MinX, other.MinX),
            Math.Min(MinY, other.MinY),
            Math.Max(MaxX, other.MaxX),
            Math.Max(MaxY, other.MaxY)
        );
    }

    public float CalculateAreaIncrease(in BoundingRectangle other)
    {
        // Calculate union bounds directly without creating intermediate struct
        var unionMinX = Math.Min(MinX, other.MinX);
        var unionMinY = Math.Min(MinY, other.MinY);
        var unionMaxX = Math.Max(MaxX, other.MaxX);
        var unionMaxY = Math.Max(MaxY, other.MaxY);
        
        // Calculate union area directly
        var unionArea = (unionMaxX - unionMinX) * (unionMaxY - unionMinY);
        return unionArea - _cachedArea;
    }

    public bool Equals(BoundingRectangle other)
    {
        return MinX.Equals(other.MinX) && MinY.Equals(other.MinY) &&
            MaxX.Equals(other.MaxX) && MaxY.Equals(other.MaxY);
    }

    public override bool Equals(object? obj) => obj is BoundingRectangle other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(MinX, MinY, MaxX, MaxY);
    public override string ToString() => $"[({MinX},{MinY}) - ({MaxX},{MaxY})]";
}
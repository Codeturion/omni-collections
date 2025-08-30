using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Omni.Collections.Grid2D.HexGrid;

public readonly struct HexCoord : IEquatable<HexCoord>
{
    public readonly int Q;
    public readonly int R;
    public int S => -Q - R;
    public static HexCoord Origin => new HexCoord(0, 0);
    public HexCoord(int q, int r)
    {
        Q = q;
        R = r;
    }

    public static HexCoord FromCube(int x, int y, int z)
    {
        if (x + y + z != 0)
            throw new ArgumentException("Cube coordinates must sum to zero");
        return new HexCoord(x, z);
    }

    public (int x, int y, int z) ToCube() => (Q, -Q - R, R);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int DistanceTo(HexCoord other)
    {
        return (Math.Abs(Q - other.Q) + Math.Abs(Q + R - other.Q - other.R) + Math.Abs(R - other.R)) / 2;
    }

    public IEnumerable<HexCoord> GetNeighbors()
    {
        yield return new HexCoord(Q + 1, R);
        yield return new HexCoord(Q + 1, R - 1);
        yield return new HexCoord(Q, R - 1);
        yield return new HexCoord(Q - 1, R);
        yield return new HexCoord(Q - 1, R + 1);
        yield return new HexCoord(Q, R + 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HexCoord GetNeighbor(int direction)
    {
        return direction switch
        {
            0 => new HexCoord(Q + 1, R),
            1 => new HexCoord(Q + 1, R - 1),
            2 => new HexCoord(Q, R - 1),
            3 => new HexCoord(Q - 1, R),
            4 => new HexCoord(Q - 1, R + 1),
            5 => new HexCoord(Q, R + 1),
            _ => throw new ArgumentOutOfRangeException(nameof(direction), "Direction must be 0-5")
        };
    }

    public IEnumerable<HexCoord> GetWithinDistance(int distance)
    {
        for (int dx = -distance; dx <= distance; dx++)
        {
            int dy1 = Math.Max(-distance, -dx - distance);
            int dy2 = Math.Min(distance, -dx + distance);
            for (int dy = dy1; dy <= dy2; dy++)
            {
                yield return new HexCoord(Q + dx, R + dy);
            }
        }
    }

    public IEnumerable<HexCoord> GetRing(int distance)
    {
        if (distance == 0)
        {
            yield return this;
            yield break;
        }
        var current = new HexCoord(Q - distance, R + distance);
        for (int direction = 0; direction < 6; direction++)
        {
            for (int step = 0; step < distance; step++)
            {
                yield return current;
                current = current.GetNeighbor(direction);
            }
        }
    }

    public IEnumerable<HexCoord> GetLineTo(HexCoord end)
    {
        int distance = DistanceTo(end);
        if (distance == 0)
        {
            yield return this;
            yield break;
        }
        for (int i = 0; i <= distance; i++)
        {
            double t = (double)i / distance;
            var cube = LerpCube(ToCube(), end.ToCube(), t);
            yield return RoundCube(cube);
        }
    }

    static private (double x, double y, double z) LerpCube((int x, int y, int z) a, (int x, int y, int z) b, double t)
    {
        return (a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t);
    }

    internal static HexCoord RoundCube((double x, double y, double z) cube)
    {
        int rx = (int)Math.Round(cube.x);
        int ry = (int)Math.Round(cube.y);
        int rz = (int)Math.Round(cube.z);
        double xDiff = Math.Abs(rx - cube.x);
        double yDiff = Math.Abs(ry - cube.y);
        double zDiff = Math.Abs(rz - cube.z);
        if (xDiff > yDiff && xDiff > zDiff)
            rx = -ry - rz;
        else if (yDiff > zDiff)
            ry = -rx - rz;
        else
            rz = -rx - ry;
        return new HexCoord(rx, rz);
    }

    public static HexCoord operator +(HexCoord a, HexCoord b) => new HexCoord(a.Q + b.Q, a.R + b.R);
    public static HexCoord operator -(HexCoord a, HexCoord b) => new HexCoord(a.Q - b.Q, a.R - b.R);
    public static HexCoord operator *(HexCoord coord, int scalar) => new HexCoord(coord.Q * scalar, coord.R * scalar);
    public bool Equals(HexCoord other) => Q == other.Q && R == other.R;
    public override bool Equals(object? obj) => obj is HexCoord other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Q, R);
    public override string ToString() => $"({Q}, {R})";
    public static bool operator ==(HexCoord left, HexCoord right) => left.Equals(right);
    public static bool operator !=(HexCoord left, HexCoord right) => !left.Equals(right);
}
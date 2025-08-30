using System;

namespace Omni.Collections.Grid2D.HexGrid;

public class HexOrientation
{
    public (double f0, double f1, double f2, double f3) ForwardMatrix { get; }

    public (double b0, double b1, double b2, double b3) InverseMatrix { get; }

    public double StartAngle { get; }

    private HexOrientation(double f0, double f1, double f2, double f3,
        double b0, double b1, double b2, double b3, double startAngle)
    {
        ForwardMatrix = (f0, f1, f2, f3);
        InverseMatrix = (b0, b1, b2, b3);
        StartAngle = startAngle;
    }

    public static HexOrientation FlatTop => new HexOrientation(
        3.0 / 2.0, 0.0, Math.Sqrt(3.0) / 2.0, Math.Sqrt(3.0),
        2.0 / 3.0, 0.0, -1.0 / 3.0, Math.Sqrt(3.0) / 3.0, 0.0);
    public static HexOrientation PointyTop => new HexOrientation(
        Math.Sqrt(3.0), Math.Sqrt(3.0) / 2.0, 0.0, 3.0 / 2.0,
        Math.Sqrt(3.0) / 3.0, -1.0 / 3.0, 0.0, 2.0 / 3.0, 0.5);
}
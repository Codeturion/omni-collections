namespace Omni.Collections.Grid2D.HexGrid;

public class HexLayout
{
    public HexOrientation Orientation { get; }

    public (double width, double height) Size { get; }

    public (double x, double y) Origin { get; }

    public HexLayout(HexOrientation orientation, (double width, double height) size, (double x, double y) origin)
    {
        Orientation = orientation;
        Size = size;
        Origin = origin;
    }

    public static HexLayout FlatTop(double size = 1.0) =>
        new HexLayout(HexOrientation.FlatTop, (size, size), (0, 0));
    public static HexLayout PointyTop(double size = 1.0) =>
        new HexLayout(HexOrientation.PointyTop, (size, size), (0, 0));
    public (double x, double y) ToPixel(HexCoord coord)
    {
        var m = Orientation.ForwardMatrix;
        double x = (m.f0 * coord.Q + m.f1 * coord.R) * Size.width + Origin.x;
        double y = (m.f2 * coord.Q + m.f3 * coord.R) * Size.height + Origin.y;
        return (x, y);
    }

    public HexCoord FromPixel(double x, double y)
    {
        var pt = ((x - Origin.x) / Size.width, (y - Origin.y) / Size.height);
        var m = Orientation.InverseMatrix;
        double q = m.b0 * pt.Item1 + m.b1 * pt.Item2;
        double r = m.b2 * pt.Item1 + m.b3 * pt.Item2;
        return HexCoord.RoundCube((q, -q - r, r));
    }
}
namespace Omni.Collections.Grid2D.HexGrid;

public readonly struct HexCell<T>
{
    public readonly HexCoord Coord;
    public readonly T Value;
    public HexCell(HexCoord coord, T value)
    {
        Coord = coord;
        Value = value;
    }

    public void Deconstruct(out HexCoord coord, out T value)
    {
        coord = Coord;
        value = Value;
    }

    public override string ToString() => $"{Coord}: {Value}";
}
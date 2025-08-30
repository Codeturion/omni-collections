using System;

namespace Omni.Collections.Spatial.KDTree;

public class KdPointProvider2D<T> : IKdPointProvider<T>
{
    private readonly Func<T, double> _getX;
    private readonly Func<T, double> _getY;
    public KdPointProvider2D(Func<T, double> getX, Func<T, double> getY)
    {
        _getX = getX ?? throw new ArgumentNullException(nameof(getX));
        _getY = getY ?? throw new ArgumentNullException(nameof(getY));
    }

    public double[] GetCoordinates(T item)
    {
        return [_getX(item), _getY(item)];
    }
}
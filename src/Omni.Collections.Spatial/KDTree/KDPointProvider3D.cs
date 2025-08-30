using System;

namespace Omni.Collections.Spatial.KDTree;

public class KdPointProvider3D<T> : IKdPointProvider<T>
{
    private readonly Func<T, double> _getX;
    private readonly Func<T, double> _getY;
    private readonly Func<T, double> _getZ;
    public KdPointProvider3D(Func<T, double> getX, Func<T, double> getY, Func<T, double> getZ)
    {
        _getX = getX ?? throw new ArgumentNullException(nameof(getX));
        _getY = getY ?? throw new ArgumentNullException(nameof(getY));
        _getZ = getZ ?? throw new ArgumentNullException(nameof(getZ));
    }

    public double[] GetCoordinates(T item)
    {
        return [_getX(item), _getY(item), _getZ(item)];
    }
}
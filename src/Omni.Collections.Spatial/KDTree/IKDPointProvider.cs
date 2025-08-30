namespace Omni.Collections.Spatial.KDTree;

public interface IKdPointProvider<T>
{
    double[] GetCoordinates(T item);
}
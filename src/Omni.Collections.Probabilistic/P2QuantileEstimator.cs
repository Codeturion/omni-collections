using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Omni.Collections.Probabilistic;

public class P2QuantileEstimator<T> where T : IComparable<T>
{
    private readonly T[] _q = new T[5];
    private readonly int[] _n = new int[5];
    private readonly double[] _np = new double[5];
    private readonly double[] _dnp = new double[5];
    private int _count;
    private readonly double _targetPercentile;
    public long Count => _count;
    public long MemoryUsage =>
        5 * GetSizeOfT() +
        5 * sizeof(int) +
        5 * sizeof(double) +
        5 * sizeof(double) +
        sizeof(int) +
        sizeof(double) +
        64;
    public double AccuracyEstimate
    {
        get
        {
            if (_count < 5) return 1.0;
            var extremenessPenalty = Math.Abs(0.5 - _targetPercentile) * 2;
            var sampleSizeFactor = Math.Min(1.0, _count / 1000.0);
            var baseAccuracy = 0.02;
            var extremeAccuracy = extremenessPenalty * 0.05;
            var adjustedAccuracy = (baseAccuracy + extremeAccuracy) / Math.Sqrt(sampleSizeFactor);
            return Math.Min(adjustedAccuracy, 0.15);
        }
    }

    public double TargetPercentile => _targetPercentile;
    public P2QuantileEstimator(double targetPercentile = 0.95)
    {
        if (targetPercentile < 0.0 || targetPercentile > 1.0)
            throw new ArgumentOutOfRangeException(nameof(targetPercentile),
                "Target percentile must be between 0.0 and 1.0");
        _targetPercentile = targetPercentile;
        _n[0] = 1; _n[1] = 2; _n[2] = 3; _n[3] = 4; _n[4] = 5;
        _np[0] = 1;
        _np[1] = 1 + 2 * _targetPercentile;
        _np[2] = 1 + 4 * _targetPercentile;
        _np[3] = 3 + 2 * _targetPercentile;
        _np[4] = 5;
        _dnp[0] = 0;
        _dnp[1] = _targetPercentile / 2;
        _dnp[2] = _targetPercentile;
        _dnp[3] = (1 + _targetPercentile) / 2;
        _dnp[4] = 1;
        _count = 0;
    }

    public void Add(T value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        if (_count < 5)
        {
            _q[_count] = value;
            _count++;
            if (_count == 5)
            {
                Array.Sort(_q);
            }
            return;
        }
        int k = FindCellIndex(value);
        if (k == 0 && value.CompareTo(_q[0]) < 0)
        {
            _q[0] = value;
        }
        else if (k == 3 && value.CompareTo(_q[4]) > 0)
        {
            _q[4] = value;
        }
        for (int i = k + 1; i < 5; i++)
        {
            _n[i]++;
        }
        for (int i = 0; i < 5; i++)
        {
            _np[i] += _dnp[i];
        }
        for (int i = 1; i < 4; i++)
        {
            double d = _np[i] - _n[i];
            if ((d >= 1 && _n[i + 1] - _n[i] > 1) || (d <= -1 && _n[i - 1] - _n[i] < -1))
            {
                int sign = d >= 0 ? 1 : -1;
                T parabolicEstimate = ParabolicInterpolation(i, sign);
                if (_q[i - 1].CompareTo(parabolicEstimate) < 0 && parabolicEstimate.CompareTo(_q[i + 1]) < 0)
                {
                    _q[i] = parabolicEstimate;
                }
                else
                {
                    _q[i] = LinearInterpolation(i, sign);
                }
                _n[i] += sign;
            }
        }
        _count++;
    }

    public T GetPercentile(double percentile)
    {
        if (percentile < 0.0 || percentile > 1.0)
            throw new ArgumentOutOfRangeException(nameof(percentile),
                "Percentile must be between 0.0 and 1.0");
        if (_count == 0)
            throw new InvalidOperationException("Cannot get percentile from empty estimator");
        if (_count < 5)
        {
            var sortedSample = new T[_count];
            Array.Copy(_q, sortedSample, _count);
            Array.Sort(sortedSample);
            var index = Math.Max(0, Math.Min(_count - 1, (int)Math.Ceiling(percentile * _count) - 1));
            return sortedSample[index];
        }
        if (Math.Abs(percentile - _targetPercentile) < 1e-10)
        {
            return _q[2];
        }
        if (percentile <= 0.0) return _q[0];
        if (percentile >= 1.0) return _q[4];
        double scaledP = percentile * 4;
        int lowerIndex = (int)Math.Floor(scaledP);
        int upperIndex = Math.Min(4, lowerIndex + 1);
        if (lowerIndex == upperIndex)
        {
            return _q[lowerIndex];
        }
        double fraction = scaledP - lowerIndex;
        return InterpolateValues(_q[lowerIndex], _q[upperIndex], fraction);
    }

    public T[] GetPercentiles(double[] percentiles)
    {
        if (percentiles == null)
            throw new ArgumentNullException(nameof(percentiles));
        if (percentiles.Length == 0)
            throw new ArgumentException("Percentiles array cannot be empty", nameof(percentiles));
        var results = new T[percentiles.Length];
        for (int i = 0; i < percentiles.Length; i++)
        {
            results[i] = GetPercentile(percentiles[i]);
        }
        return results;
    }

    public void Clear()
    {
        Array.Clear(_q, 0, 5);
        Array.Clear(_n, 0, 5);
        _n[0] = 1; _n[1] = 2; _n[2] = 3; _n[3] = 4; _n[4] = 5;
        _np[0] = 1;
        _np[1] = 1 + 2 * _targetPercentile;
        _np[2] = 1 + 4 * _targetPercentile;
        _np[3] = 3 + 2 * _targetPercentile;
        _np[4] = 5;
        _count = 0;
    }

    public bool ValidateAccuracy(T[] exactData, double tolerance = 0.05)
    {
        if (exactData == null || exactData.Length == 0)
            return false;
        var sortedData = exactData.ToArray();
        Array.Sort(sortedData);
        var exactIndex = Math.Max(0, Math.Min(sortedData.Length - 1,
            (int)Math.Ceiling(_targetPercentile * sortedData.Length) - 1));
        var exactValue = sortedData[exactIndex];
        var estimatedValue = GetPercentile(_targetPercentile);
        return CalculateRelativeError(exactValue, estimatedValue) <= tolerance;
    }

    public P2QuantileEstimatorStats GetStats()
    {
        return new P2QuantileEstimatorStats
        {
            Count = _count,
            TargetPercentile = _targetPercentile,
            AccuracyEstimate = AccuracyEstimate,
            MemoryUsage = MemoryUsage,
            Markers = _count >= 5 ? _q.ToArray() : _q.Take(_count).ToArray(),
            MarkerPositions = _count >= 5 ? _n.ToArray() : _n.Take(_count).ToArray()
        };
    }
    #region Private Helper Methods
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int FindCellIndex(T value)
    {
        if (value.CompareTo(_q[0]) < 0) return -1;
        if (value.CompareTo(_q[1]) < 0) return 0;
        if (value.CompareTo(_q[2]) < 0) return 1;
        if (value.CompareTo(_q[3]) < 0) return 2;
        return 3;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private T ParabolicInterpolation(int i, int d)
    {
        var qi = GetDoubleValue(_q[i]);
        var qiMinus1 = GetDoubleValue(_q[i - 1]);
        var qiPlus1 = GetDoubleValue(_q[i + 1]);
        var ni = _n[i];
        var niMinus1 = _n[i - 1];
        var niPlus1 = _n[i + 1];
        var numerator = d / (double)(niPlus1 - niMinus1);
        var term1 = (ni - niMinus1 + d) * (qiPlus1 - qi) / (niPlus1 - ni);
        var term2 = (niPlus1 - ni - d) * (qi - qiMinus1) / (ni - niMinus1);
        var result = qi + numerator * (term1 + term2);
        return ConvertFromDouble(result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private T LinearInterpolation(int i, int d)
    {
        var qi = GetDoubleValue(_q[i]);
        var qTarget = GetDoubleValue(_q[i + d]);
        var slope = (qTarget - qi) / (_n[i + d] - _n[i]);
        var result = qi + d * slope;
        return ConvertFromDouble(result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T InterpolateValues(T lower, T upper, double fraction)
    {
        var lowerVal = GetDoubleValue(lower);
        var upperVal = GetDoubleValue(upper);
        var result = lowerVal + fraction * (upperVal - lowerVal);
        return ConvertFromDouble(result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double GetDoubleValue(T value)
    {
        return value switch
        {
            double d => d,
            float f => f,
            int i => i,
            long l => l,
            decimal dec => (double)dec,
            _ => Convert.ToDouble(value)
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T ConvertFromDouble(double value)
    {
        return (T)Convert.ChangeType(value, typeof(T));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetSizeOfT()
    {
        return Type.GetTypeCode(typeof(T)) switch
        {
            TypeCode.Double => sizeof(double),
            TypeCode.Single => sizeof(float),
            TypeCode.Int32 => sizeof(int),
            TypeCode.Int64 => sizeof(long),
            TypeCode.Decimal => sizeof(decimal),
            _ => IntPtr.Size
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double CalculateRelativeError(T exactValue, T estimatedValue)
    {
        var exact = GetDoubleValue(exactValue);
        var estimated = GetDoubleValue(estimatedValue);
        if (Math.Abs(exact) < 1e-10)
            return Math.Abs(estimated);
        return Math.Abs(estimated - exact) / Math.Abs(exact);
    }
    #endregion
}

public readonly struct P2QuantileEstimatorStats
{
    public int Count { get; init; }

    public double TargetPercentile { get; init; }

    public double AccuracyEstimate { get; init; }

    public long MemoryUsage { get; init; }

    public Array Markers { get; init; }

    public int[] MarkerPositions { get; init; }
}
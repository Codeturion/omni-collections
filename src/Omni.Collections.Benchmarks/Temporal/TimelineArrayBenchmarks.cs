using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Omni.Collections.Benchmarks.Common;
using Omni.Collections.Temporal;

namespace Omni.Collections.Benchmarks.Temporal;

[BenchmarkCategory(Categories.Temporal)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[MemoryDiagnoser]
public class TimelineArrayBenchmarks
{
    private const int OpsPerIteration = 8192;

    [Params(Sizes.Small, Sizes.Medium, Sizes.Large)]
    public int N;

    private int[] _values = null!;
    private long[] _timestamps = null!;

    private TimelineArray<int> _omniFilled = null!;
    private List<(long t, int v)> _baselineFilled = null!;

    private TimelineArray<int> _omniMut = null!;
    private List<(long t, int v)> _baselineMut = null!;
    private int _addCounter;
    private const int ReadIndexMask = 1023;
    private int _readCounter;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _values = RandomData.Ints(N + OpsPerIteration);
        _timestamps = new long[N + OpsPerIteration];
        for (int i = 0; i < _timestamps.Length; i++)
            _timestamps[i] = i;

        _omniFilled = new TimelineArray<int>(N);
        _baselineFilled = new List<(long, int)>(N);
        for (int i = 0; i < N; i++)
        {
            _omniFilled.Record(_values[i], _timestamps[i]);
            _baselineFilled.Add((_timestamps[i], _values[i]));
        }
    }

    [IterationSetup(Targets = new[] { nameof(Omni_Record), nameof(Baseline_Record) })]
    public void ResetForRecord()
    {
        _omniMut = new TimelineArray<int>(N + OpsPerIteration);
        _baselineMut = new List<(long, int)>(N + OpsPerIteration);
        for (int i = 0; i < N; i++)
        {
            _omniMut.Record(_values[i], _timestamps[i]);
            _baselineMut.Add((_timestamps[i], _values[i]));
        }
        _addCounter = N;
    }

    /// Claim: TimelineArray.Record uses circular buffer; List.Add appends.
    [Benchmark, BenchmarkCategory("Record"), InvocationCount(OpsPerIteration)]
    public int Omni_Record()
    {
        _omniMut.Record(_values[_addCounter], _timestamps[_addCounter]);
        _addCounter++;
        return _omniMut.Count;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Record"), InvocationCount(OpsPerIteration)]
    public int Baseline_Record()
    {
        _baselineMut.Add((_timestamps[_addCounter], _values[_addCounter]));
        _addCounter++;
        return _baselineMut.Count;
    }

    /// Claim: TimelineArray.GetAtTime is O(log n) via binary search; List linear search is O(n).
    [Benchmark, BenchmarkCategory("GetAtTime")]
    public int Omni_GetAtTime()
    {
        var t = _timestamps[_readCounter++ & ReadIndexMask];
        return _omniFilled.GetAtTime(t);
    }

    [Benchmark(Baseline = true), BenchmarkCategory("GetAtTime")]
    public int Baseline_GetAtTime()
    {
        var t = _timestamps[_readCounter++ & ReadIndexMask];
        foreach (var (ts, v) in _baselineFilled)
            if (ts >= t) return v;
        return 0;
    }

    /// Claim: TimelineArray fixed-capacity ring buffer vs List<T> growing storage.
    [Benchmark, BenchmarkCategory("Fill"), InvocationCount(1)]
    public TimelineArray<int> Omni_Fill()
    {
        var c = new TimelineArray<int>(N);
        for (int i = 0; i < N; i++)
            c.Record(_values[i], _timestamps[i]);
        return c;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Fill"), InvocationCount(1)]
    public List<(long t, int v)> Baseline_Fill()
    {
        var c = new List<(long, int)>();
        for (int i = 0; i < N; i++)
            c.Add((_timestamps[i], _values[i]));
        return c;
    }
}

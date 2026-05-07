using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Omni.Collections.Benchmarks.Common;
using Omni.Collections.Probabilistic;

namespace Omni.Collections.Benchmarks.Probabilistic;

[BenchmarkCategory(Categories.Probabilistic)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[MemoryDiagnoser]
public class BloomFilterBenchmarks
{
    private const int OpsPerIteration = 32768;
    private const double FalsePositiveRate = 0.01;

    [Params(Sizes.Small, Sizes.Medium, Sizes.Large)]
    public int N;

    private string[] _values = null!;
    private string[] _missingValues = null!;
    private int _readCounter;
    private const int ReadIndexMask = 1023;

    private BloomFilter<string> _omniFilled = null!;
    private HashSet<string> _baselineFilled = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _values = RandomData.Strings(N);
        _missingValues = RandomData.Strings(ReadIndexMask + 1, seed: RandomData.Seed + 1);

        _omniFilled = new BloomFilter<string>(N, FalsePositiveRate);
        _baselineFilled = new HashSet<string>(N);
        for (int i = 0; i < N; i++)
        {
            _omniFilled.Add(_values[i]);
            _baselineFilled.Add(_values[i]);
        }
    }

    [GlobalCleanup]
    public void GlobalCleanup() => _omniFilled.Dispose();

    /// Claim: BloomFilter.Contains for definitely-NOT-present items is competitive with HashSet.Contains on a miss.
    [Benchmark, BenchmarkCategory("ContainsMiss")]
    public bool Omni_ContainsMiss()
    {
        return _omniFilled.Contains(_missingValues[_readCounter++ & ReadIndexMask]);
    }

    [Benchmark(Baseline = true), BenchmarkCategory("ContainsMiss")]
    public bool Baseline_ContainsMiss()
    {
        return _baselineFilled.Contains(_missingValues[_readCounter++ & ReadIndexMask]);
    }

    /// Claim: BloomFilter.Contains for present items returns quickly via bit checks.
    [Benchmark, BenchmarkCategory("ContainsHit")]
    public bool Omni_ContainsHit()
    {
        return _omniFilled.Contains(_values[_readCounter++ & (_values.Length - 1)]);
    }

    [Benchmark(Baseline = true), BenchmarkCategory("ContainsHit")]
    public bool Baseline_ContainsHit()
    {
        return _baselineFilled.Contains(_values[_readCounter++ & (_values.Length - 1)]);
    }

    /// Claim: BloomFilter allocates O(bits) bytes regardless of item size; HashSet stores full keys.
    [Benchmark, BenchmarkCategory("Fill"), InvocationCount(1)]
    public BloomFilter<string> Omni_Fill()
    {
        var c = new BloomFilter<string>(N, FalsePositiveRate);
        for (int i = 0; i < N; i++)
            c.Add(_values[i]);
        return c;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Fill"), InvocationCount(1)]
    public HashSet<string> Baseline_Fill()
    {
        var c = new HashSet<string>();
        for (int i = 0; i < N; i++)
            c.Add(_values[i]);
        return c;
    }
}

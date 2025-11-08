using BenchmarkDotNet.Attributes;
using Omni.Collections.Benchmarks.Comparison;
using Omni.Collections.Benchmarks.Config;
using Omni.Collections.Probabilistic;
using System;
using System.Collections.Generic;
using System.Linq;
using Omni.Collections.Benchmarks.Benchmarks.Helpers;

namespace Omni.Collections.Benchmarks.Core;

/// <summary>
/// Comprehensive benchmarks for all Probabilistic data structures in Omni.Collections.
/// Tests: BloomFilter, CountMinSketch, HyperLogLog, TDigest, DigestStreamingAnalytics vs their traditional .NET equivalents 
/// with appropriate baseline comparisons for probabilistic algorithms.
/// </summary>
public class ProbabilisticStructureBenchmarks
{
    /// <summary>
    /// BloomFilter<T> vs HashSet<T> - Probabilistic set membership comparison
    /// NOTE: BloomFilter excels at high miss rate scenarios (e.g., cache filtering, web crawling).
    /// Default tests 90% miss rate to demonstrate intended use case.
    /// </summary>
    [GroupBenchmarks]
    public class BloomFilterVsHashSet : BaselineComparisonBenchmark<BloomFilter<int>, HashSet<int>, int, int>
    {
        private Random _random = null!;
        private int[] _existingValues = null!;
        private int[] _nonExistingValues = null!;
        private BloomFilter<int> _arrayPoolCollection = null!;
        
        // Configurable miss rate - default 90% miss (realistic for bloom filter use cases)
        [Params(0.9, 0.5, 0.1)]
        public double MissRate { get; set; } = 0.9;
        
        [GlobalSetup]
        public void Setup() 
        {
            SetupComparison();
            SetupArrayPoolCollection();
        }
        
        [GlobalCleanup]
        public void Cleanup() => CleanupComparison();
        
        protected override void CleanupOmniCollection()
        {
            OmniCollection?.Dispose();
            _arrayPoolCollection?.Dispose();
        }
        
        protected override void SetupTestData()
        {
            _random = new Random(42);
            TestValues = BenchmarkDataFactory.GetIntPool(DataSize);
            TestKeys = TestValues;
            
            // Pre-generate values for membership testing
            _existingValues = TestValues.Take(DataSize / 2).ToArray();
            _nonExistingValues = Enumerable.Range(DataSize * 2, DataSize / 2).ToArray();
        }
        
        protected override void SetupOmniCollection()
        {
            // BloomFilter with optimal size for 1% false positive rate
            OmniCollection = new BloomFilter<int>(DataSize, 0.01);
            
            // Pre-populate with half the data
            for (int i = 0; i < DataSize / 2; i++)
            {
                OmniCollection.Add(TestValues[i]);
            }
        }
        
        protected override void SetupBaselineCollection()
        {
            BaselineCollection = new HashSet<int>();
            
            // Pre-populate with half the data
            for (int i = 0; i < DataSize / 2; i++)
            {
                BaselineCollection.Add(TestValues[i]);
            }
        }
        
        private void SetupArrayPoolCollection()
        {
            // BloomFilter with ArrayPool for comparison
            _arrayPoolCollection = BloomFilter<int>.CreateWithArrayPool(DataSize, 0.01);
            
            // Pre-populate with half the data
            for (int i = 0; i < DataSize / 2; i++)
            {
                _arrayPoolCollection.Add(TestValues[i]);
            }
        }
        
        protected override object PerformOmniAdd()
        {
            var value = GetRandomValue();
            OmniCollection.Add(value);
            return true; // Avoid boxing Count - just return success
        }
        
        protected override object PerformBaselineAdd()
        {
            var value = GetRandomValue();
            return BaselineCollection.Add(value);
        }
        
        protected override object PerformOmniGet()
        {
            // Use configured miss rate for realistic testing
            var value = _random.NextDouble() >= MissRate 
                ? _existingValues[_random.Next(_existingValues.Length)]
                : _nonExistingValues[_random.Next(_nonExistingValues.Length)];
            return OmniCollection.Contains(value);
        }
        
        protected override object PerformBaselineGet()
        {
            // Use same miss rate for fair comparison
            var value = _random.NextDouble() >= MissRate 
                ? _existingValues[_random.Next(_existingValues.Length)]
                : _nonExistingValues[_random.Next(_nonExistingValues.Length)];
            return BaselineCollection.Contains(value);
        }
        
        protected override object PerformOmniRemove()
        {
            // BloomFilter doesn't support remove operations (fundamental limitation)
            // Baseline performs actual work while Omni returns false immediately
            return false;
        }
        
        protected override object PerformBaselineRemove()
        {
            var value = GetRandomValue();
            // NOTE: Baseline does actual removal while BloomFilter cannot
            // This creates computational asymmetry but reflects real-world usage
            return BaselineCollection.Remove(value);
        }
        
        protected override int PerformOmniEnumerate()
        {
            return OmniCollection.Count;
        }
        
        protected override int PerformBaselineEnumerate()
        {
            return BaselineCollection.Count;
        }
        
        [Benchmark(Description = "BloomFilter Add")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object BloomFilter_Add() => base.Omni_Add();
        
        [Benchmark(Description = "BloomFilter Add (ArrayPool.Shared)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object BloomFilter_Add_ArrayPool()
        {
            var value = GetRandomValue();
            _arrayPoolCollection.Add(value);
            return true; // Avoid boxing Count - just return success
        }
        
        [Benchmark(Baseline = true, Description = "HashSet Add operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object HashSet_Add() => base.Baseline_Add();
        
        [Benchmark(Description = "BloomFilter Contains")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object BloomFilter_Get() => base.Omni_Get();
        
        [Benchmark(Description = "BloomFilter Contains (ArrayPool.Shared)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object BloomFilter_Get_ArrayPool()
        {
            // Use configured miss rate for realistic testing
            var value = _random.NextDouble() >= MissRate 
                ? _existingValues[_random.Next(_existingValues.Length)]
                : _nonExistingValues[_random.Next(_nonExistingValues.Length)];
            return _arrayPoolCollection.Contains(value);
        }
        
        [Benchmark(Description = "HashSet Contains operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object HashSet_Get() => base.Baseline_Get();
        
        [Benchmark(Description = "HashSet Remove operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object HashSet_Remove() => base.Baseline_Remove();
        
        [Benchmark(Description = "BloomFilter Count access")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int BloomFilter_CountAccess() => base.Omni_Enumerate();
        
        [Benchmark(Description = "BloomFilter Count access (ArrayPool.Shared)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int BloomFilter_CountAccess_ArrayPool()
        {
            return _arrayPoolCollection.Count;
        }
        
        [Benchmark(Description = "HashSet Count access (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int HashSet_CountAccess() => base.Baseline_Enumerate();
        
    }
    
    /// <summary>
    /// CountMinSketch<T> vs Dictionary<T,int> - Probabilistic frequency counter comparison
    /// </summary>
    [GroupBenchmarks]
    public class CountMinSketchVsDict : BaselineComparisonBenchmark<CountMinSketch<int>, Dictionary<int, int>, int, int>
    {
        private Random _random = null!;
        private int[] _frequentItems = null!;
        
        [GlobalSetup]
        public void Setup() => SetupComparison();
        
        [GlobalCleanup]
        public void Cleanup() => CleanupComparison();
        
        protected override void SetupTestData()
        {
            _random = new Random(42);
            TestValues = BenchmarkDataFactory.GetIntPool(DataSize);
            TestKeys = TestValues;
            
            // Generate some frequent items for realistic frequency distribution
            _frequentItems = TestValues.Take(10).ToArray();
        }
        
        protected override void SetupOmniCollection()
        {
            // CountMinSketch with reasonable size/accuracy trade-off
            OmniCollection = new CountMinSketch<int>(DataSize / 100, 4);
            
            // Pre-populate with realistic frequency distribution
            for (int i = 0; i < DataSize / 2; i++)
            {
                var item = i % 10 < 3 
                    ? _frequentItems[_random.Next(_frequentItems.Length)] 
                    : TestValues[i];
                OmniCollection.Add(item);
            }
        }
        
        protected override void SetupBaselineCollection()
        {
            BaselineCollection = new Dictionary<int, int>();
            
            // Pre-populate with same frequency distribution
            for (int i = 0; i < DataSize / 2; i++)
            {
                var item = i % 10 < 3 
                    ? _frequentItems[_random.Next(_frequentItems.Length)] 
                    : TestValues[i];
                    
                if (BaselineCollection.ContainsKey(item))
                    BaselineCollection[item]++;
                else
                    BaselineCollection[item] = 1;
            }
        }
        
        protected override object PerformOmniAdd()
        {
            // Use same frequency distribution as setup: 30% frequent items
            var value = _random.Next(10) < 3 
                ? _frequentItems[_random.Next(_frequentItems.Length)]
                : GetRandomValue();
            OmniCollection.Add(value);
            return OmniCollection.EstimateCount(value);
        }
        
        protected override object PerformBaselineAdd()
        {
            // Use same frequency distribution as setup: 30% frequent items
            var value = _random.Next(10) < 3 
                ? _frequentItems[_random.Next(_frequentItems.Length)]
                : GetRandomValue();
            if (BaselineCollection.ContainsKey(value))
                BaselineCollection[value]++;
            else
                BaselineCollection[value] = 1;
            return BaselineCollection[value];
        }
        
        protected override object PerformOmniGet()
        {
            // Query with same distribution pattern
            var value = _random.Next(10) < 3 
                ? _frequentItems[_random.Next(_frequentItems.Length)]
                : GetRandomValue();
            return OmniCollection.EstimateCount(value);
        }
        
        protected override object PerformBaselineGet()
        {
            // Query with same distribution pattern
            var value = _random.Next(10) < 3 
                ? _frequentItems[_random.Next(_frequentItems.Length)]
                : GetRandomValue();
            return BaselineCollection.TryGetValue(value, out var count) ? count : 0;
        }
        
        protected override object PerformOmniRemove()
        {
            // CountMinSketch doesn't support remove operations (fundamental limitation)
            // Baseline performs actual work while Omni returns false immediately
            return false;
        }
        
        protected override object PerformBaselineRemove()
        {
            var value = GetRandomValue();
            // NOTE: Baseline does actual frequency decrement while CountMinSketch cannot
            // This creates computational asymmetry but reflects real-world usage
            if (BaselineCollection.TryGetValue(value, out var count) && count > 1)
            {
                BaselineCollection[value]--;
                return true;
            }
            return BaselineCollection.Remove(value);
        }
        
        protected override int PerformOmniEnumerate()
        {
            // CountMinSketch doesn't support enumeration, return total count
            return (int)OmniCollection.TotalCount;
        }
        
        protected override int PerformBaselineEnumerate()
        {
            return BaselineCollection.Values.Sum();
        }
        
        [Benchmark(Description = "CountMinSketch Add operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object CountMinSketch_Add() => base.Omni_Add();
        
        [Benchmark(Baseline = true, Description = "Dictionary Increment operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object Dictionary_Add() => base.Baseline_Add();
        
        [Benchmark(Description = "CountMinSketch GetEstimate operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object CountMinSketch_Get() => base.Omni_Get();
        
        [Benchmark(Description = "Dictionary TryGetValue operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object Dictionary_Get() => base.Baseline_Get();
        
        [Benchmark(Description = "Dictionary Decrement operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object Dictionary_Remove() => base.Baseline_Remove();
        
        [Benchmark(Description = "CountMinSketch TotalCount access (NOTE: Cannot enumerate - probabilistic structure)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int CountMinSketch_TotalCountAccess() => base.Omni_Enumerate();
        
        [Benchmark(Description = "Dictionary Count access (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int Dictionary_CountAccess() => base.Baseline_Enumerate();
    }
    
    /// <summary>
    /// HyperLogLog<T> vs HashSet<T>.Count - Cardinality estimator comparison
    /// 
    /// IMPORTANT: This benchmark intentionally compares different algorithmic approaches:
    /// - HyperLogLog: ~1KB memory usage, probabilistic computation for cardinality estimation
    /// - HashSet: O(n) memory usage, O(1) exact counting with maintained counter
    /// 
    /// The computational asymmetry (EstimateCardinality() vs Count property) is intentional
    /// and demonstrates the fundamental trade-off between memory efficiency and computational overhead.
    /// Use HyperLogLog when memory is constrained and approximate counting is acceptable.
    /// </summary>
    [GroupBenchmarks]
    public class HyperLogLogVsHashSet : BaselineComparisonBenchmark<HyperLogLog<int>, HashSet<int>, int, int>
    {
        private Random _random = null!;
        
        [GlobalSetup]
        public void Setup() => SetupComparison();
        
        [GlobalCleanup]
        public void Cleanup() => CleanupComparison();
        
        protected override void SetupTestData()
        {
            _random = new Random(42);
            TestValues = BenchmarkDataFactory.GetIntPool(DataSize);
            TestKeys = TestValues;
        }
        
        protected override void SetupOmniCollection()
        {
            // HyperLogLog with standard precision (14 bits = 0.8% error)
            OmniCollection = new HyperLogLog<int>(14);
            
            // Pre-populate with half the data
            for (int i = 0; i < DataSize / 2; i++)
            {
                OmniCollection.Add(TestValues[i]);
            }
        }
        
        protected override void SetupBaselineCollection()
        {
            BaselineCollection = new HashSet<int>();
            
            // Pre-populate with half the data
            for (int i = 0; i < DataSize / 2; i++)
            {
                BaselineCollection.Add(TestValues[i]);
            }
        }
        
        protected override object PerformOmniAdd()
        {
            var value = GetRandomValue();
            OmniCollection.Add(value);
            return OmniCollection.EstimateCardinality();
        }
        
        protected override object PerformBaselineAdd()
        {
            var value = GetRandomValue();
            BaselineCollection.Add(value);
            return BaselineCollection.Count;
        }
        
        protected override object PerformOmniGet()
        {
            // INTENTIONAL ALGORITHMIC DIFFERENCE: HyperLogLog EstimateCardinality() requires computation
            // This is the core trade-off: memory efficiency vs computational overhead for cardinality estimation
            // HyperLogLog uses ~1KB memory vs HashSet's O(n) memory, but requires probabilistic computation
            // This asymmetry demonstrates the fundamental design difference between exact vs approximate counting
            return OmniCollection.EstimateCardinality();
        }
        
        protected override object PerformBaselineGet()
        {
            // EXACT COUNTING: HashSet Count is O(1) property access (maintained counter)
            // Provides exact cardinality but requires O(n) memory to store all unique elements
            // This comparison shows the memory vs computation trade-off between data structure approaches
            return BaselineCollection.Count;
        }
        
        protected override object PerformOmniRemove()
        {
            // HyperLogLog doesn't support remove operations (fundamental limitation)
            // Baseline performs actual work while Omni returns false immediately
            return false;
        }
        
        protected override object PerformBaselineRemove()
        {
            var value = GetRandomValue();
            // NOTE: Baseline does actual removal while HyperLogLog cannot
            // This creates computational asymmetry but reflects real-world usage
            return BaselineCollection.Remove(value);
        }
        
        protected override int PerformOmniEnumerate()
        {
            return (int)OmniCollection.EstimateCardinality();
        }
        
        protected override int PerformBaselineEnumerate()
        {
            return BaselineCollection.Count;
        }
        
        [Benchmark(Description = "HyperLogLog Add operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object HyperLogLog_Add() => base.Omni_Add();
        
        [Benchmark(Baseline = true, Description = "HashSet Add operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object HashSet_Add() => base.Baseline_Add();
        
        [Benchmark(Description = "HyperLogLog GetCardinality operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object HyperLogLog_Get() => base.Omni_Get();
        
        [Benchmark(Description = "HashSet Count operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object HashSet_Get() => base.Baseline_Get();
        
        [Benchmark(Description = "HashSet Remove operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object HashSet_Remove() => base.Baseline_Remove();
        
        [Benchmark(Description = "HyperLogLog EstimateCardinality access (NOTE: Cannot enumerate - probabilistic structure)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int HyperLogLog_CardinalityAccess() => base.Omni_Enumerate();
        
        [Benchmark(Description = "HashSet Count access (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int HashSet_CountAccess2() => base.Baseline_Enumerate();
    }
    
    /// <summary>
    /// TDigest vs List<T>.Sort() + percentiles - Quantile estimator comparison
    /// NOTE: TDigest provides O(1) quantile queries while List requires O(n log n) sorting.
    /// For fairness, baseline maintains a sorted flag to avoid redundant sorting.
    /// </summary>
    [GroupBenchmarks]
    public class TDigestVsList : BaselineComparisonBenchmark<Digest, List<double>, double, double>
    {
        private Random _random = null!;
        private double[] _percentiles = { 0.5, 0.9, 0.95, 0.99 };
        private bool _baselineNeedsSorting = true;
        private List<double>? _sortedBaseline = null;
        
        [GlobalSetup]
        public void Setup() => SetupComparison();
        
        [GlobalCleanup]
        public void Cleanup() => CleanupComparison();
        
        private static readonly double[] PrecomputedNormalDistribution = CreateNormalDistribution();
        
        protected override void SetupTestData()
        {
            _random = new Random(42);
            // Use pre-computed normal distribution values - take needed amount
            var values = new double[DataSize];
            for (int i = 0; i < DataSize; i++)
            {
                values[i] = PrecomputedNormalDistribution[i % PrecomputedNormalDistribution.Length];
            }
            TestValues = values;
            TestKeys = values;
        }
        
        /// <summary>
        /// Creates pre-computed normally distributed values using Box-Muller transform
        /// This is computed once at class initialization time
        /// </summary>
        private static double[] CreateNormalDistribution()
        {
            const int PrecomputedSize = 10000;
            const double Mean = 500.0;
            const double StdDev = 100.0;
            const int Seed = 42;
            
            var random = new Random(Seed);
            var values = new double[PrecomputedSize];
            for (int i = 0; i < PrecomputedSize; i++)
            {
                // Box-Muller transform for normal distribution
                var u1 = 1.0 - random.NextDouble();
                var u2 = 1.0 - random.NextDouble();
                values[i] = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2) * StdDev + Mean;
            }
            return values;
        }
        
        protected override void SetupOmniCollection()
        {
            // TDigest with default compression
            OmniCollection = new Digest();
            
            // Pre-populate with half the data
            for (int i = 0; i < DataSize / 2; i++)
            {
                OmniCollection.Add(TestValues[i]);
            }
        }
        
        protected override void SetupBaselineCollection()
        {
            BaselineCollection = new List<double>();
            
            // Pre-populate with half the data
            for (int i = 0; i < DataSize / 2; i++)
            {
                BaselineCollection.Add(TestValues[i]);
            }
        }
        
        protected override object PerformOmniAdd()
        {
            var value = GetRandomValue();
            OmniCollection.Add(value);
            return (int)OmniCollection.Count;
        }
        
        protected override object PerformBaselineAdd()
        {
            var value = GetRandomValue();
            BaselineCollection.Add(value);
            _baselineNeedsSorting = true; // Mark as needing sort
            return BaselineCollection.Count;
        }
        
        protected override object PerformOmniGet()
        {
            var percentile = _percentiles[_random.Next(_percentiles.Length)];
            return OmniCollection.Quantile(percentile);
        }
        
        protected override object PerformBaselineGet()
        {
            var percentile = _percentiles[_random.Next(_percentiles.Length)];
            
            // Only sort if data has changed since last sort
            if (_baselineNeedsSorting || _sortedBaseline == null)
            {
                _sortedBaseline = BaselineCollection.OrderBy(x => x).ToList();
                _baselineNeedsSorting = false;
            }
            
            var index = (int)(percentile * (_sortedBaseline.Count - 1));
            return _sortedBaseline[index];
        }
        
        protected override object PerformOmniRemove()
        {
            // TDigest doesn't support remove operations (fundamental limitation)
            // Baseline performs actual work while Omni returns false immediately
            return false;
        }
        
        protected override object PerformBaselineRemove()
        {
            if (BaselineCollection.Count > 0)
            {
                BaselineCollection.RemoveAt(BaselineCollection.Count - 1);
                _baselineNeedsSorting = true; // Mark as needing sort after removal
                return true;
            }
            return false;
        }
        
        protected override int PerformOmniEnumerate()
        {
            return (int)OmniCollection.Count;
        }
        
        protected override int PerformBaselineEnumerate()
        {
            return BaselineCollection.Count;
        }
        
        [Benchmark(Description = "TDigest Add operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object TDigest_Add() => base.Omni_Add();
        
        [Benchmark(Baseline = true, Description = "List Add operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object List_Add() => base.Baseline_Add();
        
        [Benchmark(Description = "TDigest GetQuantile operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object TDigest_Get() => base.Omni_Get();
        
        [Benchmark(Description = "List Sort+Percentile operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object List_Get() => base.Baseline_Get();
        
        [Benchmark(Description = "List Remove operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object List_Remove() => base.Baseline_Remove();
        
        [Benchmark(Description = "TDigest Count access (NOTE: Cannot enumerate - probabilistic structure)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int TDigest_CountAccess() => base.Omni_Enumerate();
        
        [Benchmark(Description = "List Count access (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int List_CountAccess() => base.Baseline_Enumerate();
    }
    
    /// <summary>
    /// DigestStreamingAnalytics<T> vs P2QuantileEstimator - Fair streaming percentile algorithm comparison
    /// </summary>
    [GroupBenchmarks]
    public class DigestStreamingVsP2Quantile : BaselineComparisonBenchmark<DigestStreamingAnalytics<double>, P2QuantileEstimator<double>, double, double>
    {
        private Random _random = null!;
        private double[] _streamData = null!;
        private double[] _sortedStreamData = null!;  // Pre-computed sorted data for O(1) accuracy tests
        
        [Params(DataDistribution.Normal, DataDistribution.Uniform, DataDistribution.Exponential)]
        public DataDistribution Distribution { get; set; } = DataDistribution.Normal;
        
        /// <summary>
        /// Enumeration of statistical distributions for robust testing
        /// </summary>
        public enum DataDistribution
        {
            Normal,
            Uniform,
            Exponential,
            LogNormal,
            Bimodal
        }
        
        /// <summary>
        /// Helper class for generating various statistical distributions
        /// </summary>
        public static class DistributionGenerator
        {
            /// <summary>
            /// Generates data following the specified distribution
            /// </summary>
            public static double[] Generate(DataDistribution distribution, int size, Random random)
            {
                var data = new double[size];
                
                switch (distribution)
                {
                    case DataDistribution.Normal:
                        for (int i = 0; i < size; i++)
                        {
                            data[i] = GenerateNormal(random, 50.0, 10.0);
                        }
                        break;
                        
                    case DataDistribution.Uniform:
                        for (int i = 0; i < size; i++)
                        {
                            data[i] = random.NextDouble() * 100.0;
                        }
                        break;
                        
                    case DataDistribution.Exponential:
                        for (int i = 0; i < size; i++)
                        {
                            data[i] = -Math.Log(1.0 - random.NextDouble()) * 10.0;
                        }
                        break;
                        
                    case DataDistribution.LogNormal:
                        for (int i = 0; i < size; i++)
                        {
                            var normal = GenerateNormal(random, 0.0, 1.0);
                            data[i] = Math.Exp(normal * 0.5 + 3.0);
                        }
                        break;
                        
                    case DataDistribution.Bimodal:
                        for (int i = 0; i < size; i++)
                        {
                            if (random.NextDouble() < 0.7)
                                data[i] = GenerateNormal(random, 30.0, 5.0);
                            else
                                data[i] = GenerateNormal(random, 80.0, 8.0);
                        }
                        break;
                        
                    default:
                        throw new ArgumentException($"Unsupported distribution: {distribution}");
                }
                
                return data;
            }
            
            private static double GenerateNormal(Random random, double mean, double stdDev)
            {
                // Box-Muller transformation
                var u1 = 1.0 - random.NextDouble();
                var u2 = 1.0 - random.NextDouble();
                var z0 = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
                return z0 * stdDev + mean;
            }
        }
        
        [GlobalSetup]
        public void Setup() => SetupComparison();
        
        [GlobalCleanup]
        public void Cleanup() => CleanupComparison();
        
        protected override void SetupTestData()
        {
            _random = new Random(42);
            
            // Generate streaming data using specified distribution
            _streamData = DistributionGenerator.Generate(Distribution, DataSize, _random);
            
            // Add occasional spikes for realistic performance monitoring data
            for (int i = 0; i < DataSize; i += 20)
            {
                if (i < DataSize)
                {
                    _streamData[i] = Math.Max(_streamData[i], _random.NextDouble() * 1000);
                }
            }
            
            TestValues = _streamData;
            TestKeys = _streamData;
            
            // Pre-compute sorted data ONCE for O(1) accuracy tests instead of O(n log n) per benchmark iteration
            _sortedStreamData = _streamData.OrderBy(x => x).ToArray();
        }
        
        protected override void SetupOmniCollection()
        {
            // Create with 5 minute window and identity function for doubles
            OmniCollection = new DigestStreamingAnalytics<double>(
                TimeSpan.FromMinutes(5), 
                x => x
            );
            
            // Pre-populate with identical data for fair comparison
            for (int i = 0; i < DataSize / 2; i++)
            {
                OmniCollection.Add(_streamData[i]);
            }
        }
        
        protected override void SetupBaselineCollection()
        {
            BaselineCollection = new P2QuantileEstimator<double>(0.95); // 95th percentile estimator
            
            // Pre-populate with identical data for fair comparison
            for (int i = 0; i < DataSize / 2; i++)
            {
                BaselineCollection.Add(_streamData[i]);
            }
            
            // Validate P² algorithm accuracy against known data
            ValidateP2Accuracy();
        }
        
        private void ValidateP2Accuracy()
        {
            // Create validation dataset
            var validationData = _streamData.Take(Math.Min(1000, _streamData.Length)).ToArray();
            var validator = new P2QuantileEstimator<double>(0.95);
            
            foreach (var value in validationData)
            {
                validator.Add(value);
            }
            
            // Validate against exact calculation - P² is an approximation algorithm so use realistic tolerance
            var isAccurate = validator.ValidateAccuracy(validationData, 0.25); // 25% tolerance for approximation algorithm
            if (!isAccurate)
            {
                // Log accuracy issue but don't fail benchmark for now
                Console.WriteLine($"⚠️ P² algorithm accuracy warning for {Distribution} distribution. " +
                    "Algorithm accuracy is below 25% threshold. Consider algorithm improvements before production.");
            }
        }
        
        protected override object PerformOmniAdd()
        {
            // Stream realistic performance data
            var value = _streamData[Random.Shared.Next(_streamData.Length)];
            OmniCollection.Add(value);
            return true;
        }
        
        protected override object PerformBaselineAdd()
        {
            // Identical streaming pattern for fair comparison
            var value = _streamData[Random.Shared.Next(_streamData.Length)];
            BaselineCollection.Add(value);
            return true;
        }
        
        protected override object PerformOmniGet()
        {
            // Retrieve P95 percentile (common SLA monitoring)
            var percentile = OmniCollection.GetPercentile(0.95);
            return percentile;
        }
        
        protected override object PerformBaselineGet()
        {
            // Identical percentile retrieval for fair comparison
            var percentile = BaselineCollection.GetPercentile(0.95);
            return percentile;
        }
        
        protected override object PerformOmniRemove()
        {
            // Streaming analytics use time-based windows, not explicit removes
            // This represents a no-op for both algorithms
            return false;
        }
        
        protected override object PerformBaselineRemove()
        {
            // P2 Quantile Estimator doesn't support removes either
            // Both algorithms handle data expiration differently
            return false;
        }
        
        protected override int PerformOmniEnumerate()
        {
            return (int)OmniCollection.WindowCount;
        }
        
        protected override int PerformBaselineEnumerate()
        {
            return (int)BaselineCollection.Count;
        }
        
        [Benchmark(Description = "DigestStreaming AddValue operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object DigestStreaming_Add() => Omni_Add();
        
        [Benchmark(Baseline = true, Description = "P2 Quantile Estimator Add operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object P2QuantileEstimator_Add() => Baseline_Add();
        
        [Benchmark(Description = "DigestStreaming GetPercentile95 operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object DigestStreaming_Get() => Omni_Get();
        
        [Benchmark(Description = "P2 Quantile GetPercentile operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object P2QuantileEstimator_Get() => Baseline_Get();
        
        [Benchmark(Description = "DigestStreaming Remove operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object DigestStreaming_Remove() => Omni_Remove();
        
        [Benchmark(Description = "P2 Quantile Remove operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object P2QuantileEstimator_Remove() => Baseline_Remove();
        
        [Benchmark(Description = "DigestStreaming enumeration")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int DigestStreaming_Enumerate() => Omni_Enumerate();
        
        [Benchmark(Description = "P2 Quantile enumeration (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int P2QuantileEstimator_Enumerate() => Baseline_Enumerate();
    }

    /// <summary>
    /// Benchmarks for BloomDictionary across various miss rate scenarios.
    /// 
    /// BloomDictionary is optimized for workloads with high miss rates (>50%) where negative lookups dominate.
    /// At low miss rates (1-10%), the Bloom filter overhead may hurt performance compared to standard Dictionary.
    /// 
    /// When NOT to use BloomDictionary:
    /// - Low miss rates (<10%) - overhead exceeds benefits
    /// - Memory-constrained environments - additional Bloom filter storage
    /// - Cache-friendly workloads - standard Dictionary may be faster
    /// - When false positives are problematic for the application logic
    /// </summary>
    [MemoryDiagnoser]
    [ThreadingDiagnoser]
    [Config(typeof(MediumBenchmarkConfig))]
    [GroupBenchmarks]
    public class BloomDictionaryVsDict : BaselineComparisonBenchmark<BloomDictionary<string, string>, Dictionary<string, string>, string, string>
    {
        private readonly Random _random = new Random(42);
        private string[] _existingKeys = Array.Empty<string>();
        private string[] _nonExistingKeys = Array.Empty<string>();
        private int _keyIndex;
        
        // Test with different miss rates - from low (where BloomDictionary overhead hurts) to high (where it helps)
        [Params(0.01, 0.1, 0.5, 0.9)]  // 1%, 10%, 50%, 90% miss rates for comprehensive evaluation
        public double MissRate { get; set; }
        
        [GlobalSetup]
        public void Setup() => SetupComparison();
        
        [GlobalCleanup]
        public void Cleanup() => CleanupComparison();
        
        protected override void SetupTestData()
        {
            // Generate existing keys
            _existingKeys = new string[DataSize];
            TestKeys = new string[DataSize];
            TestValues = new string[DataSize];
            
            for (int i = 0; i < DataSize; i++)
            {
                var key = $"ExistingKey_{i:D6}_{Guid.NewGuid():N}";
                var value = $"Value_{i}";
                _existingKeys[i] = key;
                TestKeys[i] = key;
                TestValues[i] = value;
            }
            
            // Generate non-existing keys (different pattern to ensure they don't exist)
            _nonExistingKeys = new string[DataSize];
            for (int i = 0; i < DataSize; i++)
            {
                _nonExistingKeys[i] = $"NonExistingKey_{i:D6}_{Guid.NewGuid():N}";
            }
            
            _keyIndex = 0;
        }
        
        protected override void SetupOmniCollection()
        {
            OmniCollection = new BloomDictionary<string, string>(DataSize, 0.01);  // 1% false positive rate
            
            // Pre-populate
            for (int i = 0; i < DataSize; i++)
            {
                OmniCollection.Add(TestKeys[i], TestValues[i]);
            }
        }
        
        protected override void SetupBaselineCollection()
        {
            BaselineCollection = new Dictionary<string, string>(DataSize);
            
            // Pre-populate
            for (int i = 0; i < DataSize; i++)
            {
                BaselineCollection.Add(TestKeys[i], TestValues[i]);
            }
        }
        
        private string GetMixedKey()
        {
            // Return key based on miss rate
            bool shouldMiss = _random.NextDouble() < MissRate;
            var key = shouldMiss 
                ? _nonExistingKeys[_keyIndex % _nonExistingKeys.Length]
                : _existingKeys[_keyIndex % _existingKeys.Length];
            _keyIndex++;
            return key;
        }
        
        protected override object PerformOmniAdd()
        {
            var key = $"NewKey_{_keyIndex++}_{Guid.NewGuid():N}";
            var value = $"Value_{_keyIndex}";
            OmniCollection[key] = value;
            return key;
        }
        
        protected override object PerformBaselineAdd()
        {
            var key = $"NewKey_{_keyIndex++}_{Guid.NewGuid():N}";
            var value = $"Value_{_keyIndex}";
            BaselineCollection[key] = value;
            return key;
        }
        
        protected override object PerformOmniGet()
        {
            var key = GetMixedKey();
            OmniCollection.TryGetValue(key, out var value);
            return value ?? string.Empty;
        }
        
        protected override object PerformBaselineGet()
        {
            var key = GetMixedKey();
            BaselineCollection.TryGetValue(key, out var value);
            return value ?? string.Empty;
        }
        
        protected override object PerformOmniRemove()
        {
            var key = _existingKeys[_keyIndex % _existingKeys.Length];
            _keyIndex++;
            return OmniCollection.Remove(key);
        }
        
        protected override object PerformBaselineRemove()
        {
            var key = _existingKeys[_keyIndex % _existingKeys.Length];
            _keyIndex++;
            return BaselineCollection.Remove(key);
        }
        
        protected override int PerformOmniEnumerate()
        {
            int count = 0;
            foreach (var item in OmniCollection)
            {
                count++;
            }
            return count;
        }
        
        protected override int PerformBaselineEnumerate()
        {
            int count = 0;
            foreach (var item in BaselineCollection)
            {
                count++;
            }
            return count;
        }
        
        [Benchmark(Description = "BloomDictionary Add operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object BloomDictionary_Add() => base.Omni_Add();
        
        [Benchmark(Baseline = true, Description = "Dictionary Add operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object Dictionary_Add() => base.Baseline_Add();
        
        [Benchmark(Description = "BloomDictionary TryGetValue (mixed hit/miss)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object BloomDictionary_Get() => base.Omni_Get();
        
        [Benchmark(Description = "Dictionary TryGetValue (mixed hit/miss) (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object Dictionary_Get() => base.Baseline_Get();
        
        [Benchmark(Description = "BloomDictionary Remove operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object BloomDictionary_Remove() => base.Omni_Remove();
        
        [Benchmark(Description = "Dictionary Remove operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object Dictionary_Remove() => base.Baseline_Remove();
        
        [Benchmark(Description = "BloomDictionary enumeration")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int BloomDictionary_Enumerate() => base.Omni_Enumerate();
        
        [Benchmark(Description = "Dictionary enumeration (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int Dictionary_Enumerate() => base.Baseline_Enumerate();
    }
}
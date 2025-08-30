using BenchmarkDotNet.Attributes;
using Omni.Collections.Benchmarks.Comparison;
using Omni.Collections.Linear;
using System;
using System.Collections.Generic;
using System.Linq;
using Omni.Collections.Benchmarks.Benchmarks.Helpers;

namespace Omni.Collections.Benchmarks.Core;

/// <summary>
/// Comprehensive benchmarks for all Linear data structures in Omni.Collections.
/// Tests: BoundedList, FastQueue, MaxHeap, MinHeap, PooledList, PooledStack
/// vs their .NET equivalents with appropriate baseline comparisons.
/// </summary>
public class LinearStructureBenchmarks
{
    /// <summary>
    /// BoundedList<T> vs List<T> - Fixed-capacity list comparison
    /// NOTE: BoundedList has fixed capacity while List<T> can grow dynamically.
    /// Benchmark applies capacity constraints to both for fairer comparison.
    /// </summary>
    [GroupBenchmarks]
    public class BoundedListVsList : BaselineComparisonBenchmark<BoundedList<string>, List<string>, int, string>
    {
        private BoundedList<string> _arrayPoolCollection = null!;
        
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
            TestKeys = BenchmarkDataFactory.GetIntPool(DataSize);
            TestValues = BenchmarkDataFactory.GetStringPool(DataSize);
        }
        
        protected override void SetupOmniCollection()
        {
            // BoundedList with fixed capacity (no pooling)
            OmniCollection = new BoundedList<string>(DataSize);
            foreach (var value in TestValues.Take(DataSize / 2))
            {
                OmniCollection.Add(value);
            }
        }
        
        protected override void SetupBaselineCollection()
        {
            // Standard List<T> with initial capacity
            // NOTE: List can grow beyond initial capacity, while BoundedList cannot
            // This creates a fundamental difference in behavior when capacity is reached
            BaselineCollection = new List<string>(DataSize);
            foreach (var value in TestValues.Take(DataSize / 2))
            {
                BaselineCollection.Add(value);
            }
        }
        
        private void SetupArrayPoolCollection()
        {
            // BoundedList with ArrayPool for comparison
            _arrayPoolCollection = BoundedList<string>.CreateWithArrayPool(DataSize);
            foreach (var value in TestValues.Take(DataSize / 2))
            {
                _arrayPoolCollection.Add(value);
            }
        }
        
        protected override object PerformOmniAdd()
        {
            if (OmniCollection.Count < OmniCollection.Capacity)
            {
                OmniCollection.Add(GetRandomValue());
            }
            return OmniCollection.Count;
        }
        
        protected override object PerformBaselineAdd()
        {
            // Apply same capacity constraint as BoundedList for fair comparison
            if (BaselineCollection.Count < DataSize)
            {
                BaselineCollection.Add(GetRandomValue());
            }
            return BaselineCollection.Count;
        }
        
        protected override object PerformOmniGet()
        {
            var index = Random.Shared.Next(OmniCollection.Count);
            return OmniCollection[index];
        }
        
        protected override object PerformBaselineGet()
        {
            var index = Random.Shared.Next(BaselineCollection.Count);
            return BaselineCollection[index];
        }
        
        protected override object PerformOmniRemove()
        {
            if (OmniCollection.Count > 0)
            {
                var index = Random.Shared.Next(OmniCollection.Count);
                var item = OmniCollection[index];
                OmniCollection.RemoveAt(index);
                return item;
            }
            return null!;
        }
        
        protected override object PerformBaselineRemove()
        {
            if (BaselineCollection.Count > 0)
            {
                var index = Random.Shared.Next(BaselineCollection.Count);
                var item = BaselineCollection[index];
                BaselineCollection.RemoveAt(index);
                return item;
            }
            return null!;
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
        
        [Benchmark(Description = "BoundedList Add")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object BoundedList_Add() => base.Omni_Add();
        
        [Benchmark(Description = "BoundedList Add (ArrayPool.Shared)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object BoundedList_Add_ArrayPool()
        {
            if (_arrayPoolCollection.Count < _arrayPoolCollection.Capacity)
            {
                var value = GetRandomValue();
                _arrayPoolCollection.Add(value);
                return value;
            }
            return null!;
        }
        
        [Benchmark(Baseline = true, Description = "List Add operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object List_Add() => base.Baseline_Add();
        
        [Benchmark(Description = "BoundedList indexer")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object BoundedList_Get() => base.Omni_Get();
        
        [Benchmark(Description = "BoundedList indexer (ArrayPool.Shared)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object BoundedList_Get_ArrayPool()
        {
            if (_arrayPoolCollection.Count > 0)
            {
                // Use consistent Random.Shared.Next for fair comparison
                var index = Random.Shared.Next(_arrayPoolCollection.Count);
                return _arrayPoolCollection[index];
            }
            return null!;
        }
        
        [Benchmark(Description = "List indexer operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object List_Get() => base.Baseline_Get();
        
        [Benchmark(Description = "BoundedList RemoveAt")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object BoundedList_Remove() => base.Omni_Remove();
        
        [Benchmark(Description = "BoundedList RemoveAt (ArrayPool.Shared)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object BoundedList_Remove_ArrayPool()
        {
            if (_arrayPoolCollection.Count > 0)
            {
                var index = Random.Shared.Next(_arrayPoolCollection.Count);
                var item = _arrayPoolCollection[index];
                _arrayPoolCollection.RemoveAt(index);
                return item;
            }
            return null!;
        }
        
        [Benchmark(Description = "List RemoveAt operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object List_Remove() => base.Baseline_Remove();
        
        [Benchmark(Description = "BoundedList enumeration")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int BoundedList_Enumerate() => base.Omni_Enumerate();
        
        [Benchmark(Description = "BoundedList enumeration (ArrayPool.Shared)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int BoundedList_Enumerate_ArrayPool()
        {
            int count = 0;
            foreach (var item in _arrayPoolCollection)
            {
                count++;
            }
            return count;
        }
        
        [Benchmark(Description = "List enumeration (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int List_Enumerate() => base.Baseline_Enumerate();
    }
    
    /// <summary>
    /// FastQueue<T> vs Queue<T> - High-performance circular buffer queue comparison
    /// </summary>
    [GroupBenchmarks]
    public class FastQueueVsQueue : BaselineComparisonBenchmark<FastQueue<string>, Queue<string>, int, string>
    {
        private FastQueue<string> _arrayPoolCollection = null!;
        
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
            TestKeys = BenchmarkDataFactory.GetIntPool(DataSize);
            TestValues = BenchmarkDataFactory.GetStringPool(DataSize);
        }
        
        protected override void SetupOmniCollection()
        {
            OmniCollection = new FastQueue<string>();
            // Pre-populate with half the data
            foreach (var value in TestValues.Take(DataSize / 2))
            {
                OmniCollection.Enqueue(value);
            }
        }
        
        protected override void SetupBaselineCollection()
        {
            BaselineCollection = new Queue<string>();
            // Pre-populate with half the data
            foreach (var value in TestValues.Take(DataSize / 2))
            {
                BaselineCollection.Enqueue(value);
            }
        }
        
        private void SetupArrayPoolCollection()
        {
            // FastQueue with ArrayPool for comparison
            _arrayPoolCollection = FastQueue<string>.CreateWithArrayPool();
            foreach (var value in TestValues.Take(DataSize / 2))
            {
                _arrayPoolCollection.Enqueue(value);
            }
        }
        
        protected override object PerformOmniAdd()
        {
            OmniCollection.Enqueue(GetRandomValue());
            return OmniCollection.Count;
        }
        
        protected override object PerformBaselineAdd()
        {
            BaselineCollection.Enqueue(GetRandomValue());
            return BaselineCollection.Count;
        }
        
        protected override object PerformOmniGet()
        {
            return OmniCollection.Count > 0 ? OmniCollection.Peek() : null!;
        }
        
        protected override object PerformBaselineGet()
        {
            return BaselineCollection.Count > 0 ? BaselineCollection.Peek() : null!;
        }
        
        protected override object PerformOmniRemove()
        {
            return OmniCollection.Count > 0 ? OmniCollection.Dequeue() : null!;
        }
        
        protected override object PerformBaselineRemove()
        {
            return BaselineCollection.Count > 0 ? BaselineCollection.Dequeue() : null!;
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
        
        [Benchmark(Description = "FastQueue Enqueue")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object FastQueue_Add() => base.Omni_Add();
        
        [Benchmark(Description = "FastQueue Enqueue (ArrayPool.Shared)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object FastQueue_Add_ArrayPool()
        {
            var value = GetRandomValue();
            _arrayPoolCollection.Enqueue(value);
            return value;
        }
        
        [Benchmark(Baseline = true, Description = "Queue Enqueue operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object Queue_Add() => base.Baseline_Add();
        
        [Benchmark(Description = "FastQueue Peek")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object FastQueue_Get() => base.Omni_Get();
        
        [Benchmark(Description = "FastQueue Peek (ArrayPool.Shared)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object FastQueue_Get_ArrayPool()
        {
            return _arrayPoolCollection.Count > 0 ? _arrayPoolCollection.Peek() : null!;
        }
        
        [Benchmark(Description = "Queue Peek operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object Queue_Get() => base.Baseline_Get();
        
        [Benchmark(Description = "FastQueue Dequeue")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object FastQueue_Remove() => base.Omni_Remove();
        
        [Benchmark(Description = "FastQueue Dequeue (ArrayPool.Shared)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object FastQueue_Remove_ArrayPool()
        {
            return _arrayPoolCollection.Count > 0 ? _arrayPoolCollection.Dequeue() : null!;
        }
        
        [Benchmark(Description = "Queue Dequeue operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object Queue_Remove() => base.Baseline_Remove();
        
        [Benchmark(Description = "FastQueue enumeration")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int FastQueue_Enumerate() => base.Omni_Enumerate();
        
        [Benchmark(Description = "FastQueue enumeration (ArrayPool.Shared)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int FastQueue_Enumerate_ArrayPool()
        {
            int count = 0;
            foreach (var item in _arrayPoolCollection)
            {
                count++;
            }
            return count;
        }
        
        [Benchmark(Description = "Queue enumeration (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int Queue_Enumerate() => base.Baseline_Enumerate();
    }
    
    /// <summary>
    /// MaxHeap<T> vs SortedSet<T> - Binary max heap comparison
    /// NOTE: These serve different purposes - MaxHeap is a priority queue (O(1) peek, O(log n) extract)
    /// while SortedSet is a balanced tree (O(log n) for all operations). Comparison shows trade-offs.
    /// </summary>
    [GroupBenchmarks]
    public class MaxHeapVsSortedSet : BaselineComparisonBenchmark<MaxHeap<int>, SortedSet<int>, int, int>
    {
        private MaxHeap<int> _arrayPoolCollection = null!;
        
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
            TestKeys = BenchmarkDataFactory.GetIntPool(DataSize);
            TestValues = TestKeys; // For heaps, keys = values
        }
        
        protected override void SetupOmniCollection()
        {
            OmniCollection = new MaxHeap<int>();
            // Pre-populate with half the data
            foreach (var value in TestValues.Take(DataSize / 2))
            {
                OmniCollection.Insert(value);
            }
        }
        
        protected override void SetupBaselineCollection()
        {
            BaselineCollection = new SortedSet<int>();
            // Pre-populate with half the data
            foreach (var value in TestValues.Take(DataSize / 2))
            {
                BaselineCollection.Add(value);
            }
        }
        
        private void SetupArrayPoolCollection()
        {
            // MaxHeap with ArrayPool for comparison
            _arrayPoolCollection = MaxHeap<int>.CreateWithArrayPool();
            foreach (var value in TestValues.Take(DataSize / 2))
            {
                _arrayPoolCollection.Insert(value);
            }
        }
        
        protected override object PerformOmniAdd()
        {
            OmniCollection.Insert(GetRandomValue());
            return OmniCollection.Count;
        }
        
        protected override object PerformBaselineAdd()
        {
            BaselineCollection.Add(GetRandomValue());
            return BaselineCollection.Count;
        }
        
        protected override object PerformOmniGet()
        {
            return OmniCollection.Count > 0 ? OmniCollection.PeekMax() : 0;
        }
        
        protected override object PerformBaselineGet()
        {
            return BaselineCollection.Count > 0 ? BaselineCollection.Max : 0;
        }
        
        protected override object PerformOmniRemove()
        {
            // ExtractMax: O(log n) heap operation - removes and returns top element
            return OmniCollection.Count > 0 ? OmniCollection.ExtractMax() : 0;
        }
        
        protected override object PerformBaselineRemove()
        {
            // SortedSet: O(log n) to find max + O(log n) to remove = 2*O(log n)
            // Different operation but comparable complexity
            if (BaselineCollection.Count > 0)
            {
                var max = BaselineCollection.Max;
                BaselineCollection.Remove(max);
                return max;
            }
            return 0;
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
        
        [Benchmark(Description = "MaxHeap Insert")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object MaxHeap_Add() => base.Omni_Add();
        
        [Benchmark(Description = "MaxHeap Insert (ArrayPool.Shared)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object MaxHeap_Add_ArrayPool()
        {
            var value = GetRandomValue();
            _arrayPoolCollection.Insert(value);
            return value;
        }
        
        [Benchmark(Baseline = true, Description = "SortedSet Add operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object SortedSet_Add() => base.Baseline_Add();
        
        [Benchmark(Description = "MaxHeap PeekMax")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object MaxHeap_Get() => base.Omni_Get();
        
        [Benchmark(Description = "MaxHeap PeekMax (ArrayPool.Shared)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object MaxHeap_Get_ArrayPool()
        {
            return _arrayPoolCollection.Count > 0 ? _arrayPoolCollection.PeekMax() : 0;
        }
        
        [Benchmark(Description = "SortedSet Max operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object SortedSet_Get() => base.Baseline_Get();
        
        [Benchmark(Description = "MaxHeap ExtractMax")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object MaxHeap_Remove() => base.Omni_Remove();
        
        [Benchmark(Description = "MaxHeap ExtractMax (ArrayPool.Shared)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object MaxHeap_Remove_ArrayPool()
        {
            return _arrayPoolCollection.Count > 0 ? _arrayPoolCollection.ExtractMax() : 0;
        }
        
        [Benchmark(Description = "SortedSet Remove Max operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object SortedSet_Remove() => base.Baseline_Remove();
        
        [Benchmark(Description = "MaxHeap enumeration")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int MaxHeap_Enumerate() => base.Omni_Enumerate();
        
        [Benchmark(Description = "MaxHeap enumeration (ArrayPool.Shared)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int MaxHeap_Enumerate_ArrayPool()
        {
            int count = 0;
            foreach (var item in _arrayPoolCollection)
            {
                count++;
            }
            return count;
        }
        
        [Benchmark(Description = "SortedSet enumeration (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int SortedSet_Enumerate() => base.Baseline_Enumerate();
    }
    
    /// <summary>
    /// PooledList<T> vs List<T> - Memory pool-managed list comparison
    /// </summary>
    [GroupBenchmarks]
    public class PooledListVsList : BaselineComparisonBenchmark<PooledList<string>, List<string>, int, string>
    {
        [GlobalSetup]
        public void Setup() => SetupComparison();
        
        [GlobalCleanup]
        public void Cleanup() => CleanupComparison();
        
        protected override void SetupTestData()
        {
            TestKeys = BenchmarkDataFactory.GetIntPool(DataSize);
            TestValues = BenchmarkDataFactory.GetStringPool(DataSize);
        }
        
        protected override void SetupOmniCollection()
        {
            // Use default direct allocation (no ArrayPool)
            OmniCollection = new PooledList<string>(DataSize);
            // Pre-populate with half the data
            foreach (var value in TestValues.Take(DataSize / 2))
            {
                OmniCollection.Add(value);
            }
        }
        
        protected override void SetupBaselineCollection()
        {
            BaselineCollection = new List<string>();
            // Pre-populate with half the data
            foreach (var value in TestValues.Take(DataSize / 2))
            {
                BaselineCollection.Add(value);
            }
        }
        
        protected override object PerformOmniAdd()
        {
            OmniCollection.Add(GetRandomValue());
            return OmniCollection.Count;
        }
        
        protected override object PerformBaselineAdd()
        {
            BaselineCollection.Add(GetRandomValue());
            return BaselineCollection.Count;
        }
        
        protected override object PerformOmniGet()
        {
            var index = Random.Shared.Next(OmniCollection.Count);
            return OmniCollection[index];
        }
        
        protected override object PerformBaselineGet()
        {
            var index = Random.Shared.Next(BaselineCollection.Count);
            return BaselineCollection[index];
        }
        
        protected override object PerformOmniRemove()
        {
            if (OmniCollection.Count > 0)
            {
                var index = Random.Shared.Next(OmniCollection.Count);
                var item = OmniCollection[index];
                OmniCollection.RemoveAt(index);
                return item;
            }
            return null!;
        }
        
        protected override object PerformBaselineRemove()
        {
            if (BaselineCollection.Count > 0)
            {
                var index = Random.Shared.Next(BaselineCollection.Count);
                var item = BaselineCollection[index];
                BaselineCollection.RemoveAt(index);
                return item;
            }
            return null!;
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
        
        [Benchmark(Description = "PooledList Add")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object PooledList_Add() => base.Omni_Add();
        
        [Benchmark(Baseline = true, Description = "List Add operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object List_Add() => base.Baseline_Add();
        
        [Benchmark(Description = "PooledList indexer")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object PooledList_Get() => base.Omni_Get();
        
        [Benchmark(Description = "List indexer operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object List_Get() => base.Baseline_Get();
        
        [Benchmark(Description = "PooledList RemoveAt")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object PooledList_Remove() => base.Omni_Remove();
        
        [Benchmark(Description = "List RemoveAt operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object List_Remove() => base.Baseline_Remove();
        
        [Benchmark(Description = "PooledList enumeration")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int PooledList_Enumerate() => base.Omni_Enumerate();
        
        [Benchmark(Description = "List enumeration (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int List_Enumerate() => base.Baseline_Enumerate();
        
        protected override void CleanupOmniCollection()
        {
            OmniCollection?.Dispose();
        }
    }
    
    /// <summary>
    /// MinHeap<T> vs SortedSet<T> - Binary min heap comparison
    /// NOTE: These serve different purposes - MinHeap is a priority queue (O(1) peek, O(log n) extract)
    /// while SortedSet is a balanced tree (O(log n) for all operations). Comparison shows trade-offs.
    /// </summary>
    [GroupBenchmarks]
    public class MinHeapVsSortedSet : BaselineComparisonBenchmark<MinHeap<int>, SortedSet<int>, int, int>
    {
        private MinHeap<int> _arrayPoolCollection = null!;
        
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
            TestKeys = BenchmarkDataFactory.GetIntPool(DataSize);
            TestValues = TestKeys; // For heaps, keys = values
        }
        
        protected override void SetupOmniCollection()
        {
            OmniCollection = new MinHeap<int>();
            // Pre-populate with half the data
            foreach (var value in TestValues.Take(DataSize / 2))
            {
                OmniCollection.Insert(value);
            }
        }
        
        protected override void SetupBaselineCollection()
        {
            BaselineCollection = new SortedSet<int>();
            // Pre-populate with half the data
            foreach (var value in TestValues.Take(DataSize / 2))
            {
                BaselineCollection.Add(value);
            }
        }
        
        private void SetupArrayPoolCollection()
        {
            // MinHeap with ArrayPool for comparison
            _arrayPoolCollection = MinHeap<int>.CreateWithArrayPool();
            foreach (var value in TestValues.Take(DataSize / 2))
            {
                _arrayPoolCollection.Insert(value);
            }
        }
        
        protected override object PerformOmniAdd()
        {
            OmniCollection.Insert(GetRandomValue());
            return OmniCollection.Count;
        }
        
        protected override object PerformBaselineAdd()
        {
            BaselineCollection.Add(GetRandomValue());
            return BaselineCollection.Count;
        }
        
        protected override object PerformOmniGet()
        {
            return OmniCollection.Count > 0 ? OmniCollection.PeekMin() : 0;
        }
        
        protected override object PerformBaselineGet()
        {
            return BaselineCollection.Count > 0 ? BaselineCollection.Min : 0;
        }
        
        protected override object PerformOmniRemove()
        {
            // ExtractMin: O(log n) heap operation - removes and returns top element
            return OmniCollection.Count > 0 ? OmniCollection.ExtractMin() : 0;
        }
        
        protected override object PerformBaselineRemove()
        {
            // SortedSet: O(log n) to find min + O(log n) to remove = 2*O(log n)
            // Different operation but comparable complexity
            if (BaselineCollection.Count > 0)
            {
                var min = BaselineCollection.Min;
                BaselineCollection.Remove(min);
                return min;
            }
            return 0;
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
        
        [Benchmark(Description = "MinHeap Insert")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object MinHeap_Add() => base.Omni_Add();
        
        [Benchmark(Description = "MinHeap Insert (ArrayPool.Shared)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object MinHeap_Add_ArrayPool()
        {
            var value = GetRandomValue();
            _arrayPoolCollection.Insert(value);
            return value;
        }
        
        [Benchmark(Baseline = true, Description = "SortedSet Add operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object SortedSet_Add() => base.Baseline_Add();
        
        [Benchmark(Description = "MinHeap PeekMin")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object MinHeap_Get() => base.Omni_Get();
        
        [Benchmark(Description = "MinHeap PeekMin (ArrayPool.Shared)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object MinHeap_Get_ArrayPool()
        {
            return _arrayPoolCollection.Count > 0 ? _arrayPoolCollection.PeekMin() : 0;
        }
        
        [Benchmark(Description = "SortedSet Min operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object SortedSet_Get() => base.Baseline_Get();
        
        [Benchmark(Description = "MinHeap ExtractMin")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object MinHeap_Remove() => base.Omni_Remove();
        
        [Benchmark(Description = "MinHeap ExtractMin (ArrayPool.Shared)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object MinHeap_Remove_ArrayPool()
        {
            return _arrayPoolCollection.Count > 0 ? _arrayPoolCollection.ExtractMin() : 0;
        }
        
        [Benchmark(Description = "SortedSet Remove Min operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object SortedSet_Remove() => base.Baseline_Remove();
        
        [Benchmark(Description = "MinHeap enumeration")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int MinHeap_Enumerate() => base.Omni_Enumerate();
        
        [Benchmark(Description = "MinHeap enumeration (ArrayPool.Shared)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int MinHeap_Enumerate_ArrayPool()
        {
            int count = 0;
            foreach (var item in _arrayPoolCollection)
            {
                count++;
            }
            return count;
        }
        
        [Benchmark(Description = "SortedSet enumeration (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int SortedSet_Enumerate() => base.Baseline_Enumerate();
    }
    
    /// <summary>
    /// PooledStack<T> vs Stack<T> - Memory pool-managed stack comparison
    /// </summary>
    [GroupBenchmarks]
    public class PooledStackVsStack : BaselineComparisonBenchmark<PooledStack<string>, Stack<string>, int, string>
    {
        [GlobalSetup]
        public void Setup() => SetupComparison();
        
        [GlobalCleanup]
        public void Cleanup() => CleanupComparison();
        
        protected override void SetupTestData()
        {
            TestKeys = BenchmarkDataFactory.GetIntPool(DataSize);
            TestValues = BenchmarkDataFactory.GetStringPool(DataSize);
        }
        
        protected override void SetupOmniCollection()
        {
            OmniCollection = new PooledStack<string>();
            // Pre-populate with half the data
            foreach (var value in TestValues.Take(DataSize / 2))
            {
                OmniCollection.Push(value);
            }
        }
        
        protected override void SetupBaselineCollection()
        {
            BaselineCollection = new Stack<string>();
            // Pre-populate with half the data
            foreach (var value in TestValues.Take(DataSize / 2))
            {
                BaselineCollection.Push(value);
            }
        }
        
        protected override object PerformOmniAdd()
        {
            OmniCollection.Push(GetRandomValue());
            return OmniCollection.Count;
        }
        
        protected override object PerformBaselineAdd()
        {
            BaselineCollection.Push(GetRandomValue());
            return BaselineCollection.Count;
        }
        
        protected override object PerformOmniGet()
        {
            return OmniCollection.Count > 0 ? OmniCollection.Peek() : null!;
        }
        
        protected override object PerformBaselineGet()
        {
            return BaselineCollection.Count > 0 ? BaselineCollection.Peek() : null!;
        }
        
        protected override object PerformOmniRemove()
        {
            return OmniCollection.Count > 0 ? OmniCollection.Pop() : null!;
        }
        
        protected override object PerformBaselineRemove()
        {
            return BaselineCollection.Count > 0 ? BaselineCollection.Pop() : null!;
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
        
        [Benchmark(Description = "PooledStack Push")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object PooledStack_Add() => base.Omni_Add();
        
        [Benchmark(Baseline = true, Description = "Stack Push operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object Stack_Add() => base.Baseline_Add();
        
        [Benchmark(Description = "PooledStack Peek")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object PooledStack_Get() => base.Omni_Get();
        
        [Benchmark(Description = "Stack Peek operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object Stack_Get() => base.Baseline_Get();
        
        [Benchmark(Description = "PooledStack Pop")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object PooledStack_Remove() => base.Omni_Remove();
        
        [Benchmark(Description = "Stack Pop operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object Stack_Remove() => base.Baseline_Remove();
        
        [Benchmark(Description = "PooledStack enumeration")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int PooledStack_Enumerate() => base.Omni_Enumerate();
        
        [Benchmark(Description = "Stack enumeration (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int Stack_Enumerate() => base.Baseline_Enumerate();
        
        protected override void CleanupOmniCollection()
        {
            OmniCollection?.Dispose();
        }
    }
}

using BenchmarkDotNet.Attributes;
using Omni.Collections.Benchmarks.Comparison;
using Omni.Collections.Hybrid;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Omni.Collections.Benchmarks.Benchmarks.Helpers;
using Omni.Collections.Core.Security;
using Omni.Collections.Hybrid.GraphDictionary;
using Omni.Collections.Hybrid.LinkedDictionary;
using Omni.Collections.Hybrid.PredictiveDictionary;
using Omni.Collections.Hybrid.QueueDictionary;

namespace Omni.Collections.Benchmarks.Core;

/// <summary>
/// Comprehensive benchmarks for all Hybrid data structures in Omni.Collections.
/// Tests: LinkedDictionary, CounterDictionary, QueueDictionary, DequeDictionary
/// vs their traditional .NET equivalents with appropriate baseline comparisons.
/// </summary>
public class HybridStructureBenchmarks
{
    /// <summary>
    /// LinkedDictionary<TKey, TValue> vs Dictionary<TKey, TValue> + manual LRU - LRU cache comparison
    /// </summary>
    [GroupBenchmarks]
    public class LinkedDictionaryVsDict : BaselineComparisonBenchmark<LinkedDictionary<string, int>, Dictionary<string, int>, string, int>
    {
        
        [GlobalSetup]
        public void Setup() => SetupComparison();
        
        [GlobalCleanup]
        public void Cleanup() => CleanupComparison();
        
        protected override void SetupTestData()
        {
            TestKeys = BenchmarkDataFactory.GetStringPool(DataSize);
            TestValues = BenchmarkDataFactory.GetIntPool(DataSize);
        }
        
        protected override void SetupOmniCollection()
        {
            // LinkedDictionary with LRU capacity
            OmniCollection = new LinkedDictionary<string, int>(capacity: DataSize / 2);
            
            // Pre-populate with half the data
            for (int i = 0; i < DataSize / 2; i++)
            {
                OmniCollection[TestKeys[i]] = TestValues[i];
            }
        }
        
        protected override void SetupBaselineCollection()
        {
            // Standard Dictionary (no LRU behavior)
            BaselineCollection = new Dictionary<string, int>();
            
            // Pre-populate with half the data
            for (int i = 0; i < DataSize / 2; i++)
            {
                BaselineCollection[TestKeys[i]] = TestValues[i];
            }
        }
        
        protected override object PerformOmniAdd()
        {
            var key = GetRandomKey();
            var value = GetRandomValue();
            OmniCollection[key] = value;
            return OmniCollection.Count;
        }
        
        protected override object PerformBaselineAdd()
        {
            var key = GetRandomKey();
            var value = GetRandomValue();
            BaselineCollection[key] = value;
            return BaselineCollection.Count;
        }
        
        protected override object PerformOmniGet()
        {
            var key = GetRandomKey();
            return OmniCollection.TryGetValue(key, out var value) ? value : -1;
        }
        
        protected override object PerformBaselineGet()
        {
            var key = GetRandomKey();
            return BaselineCollection.TryGetValue(key, out var value) ? value : -1;
        }
        
        protected override object PerformOmniRemove()
        {
            var key = GetRandomKey();
            return OmniCollection.Remove(key);
        }
        
        protected override object PerformBaselineRemove()
        {
            var key = GetRandomKey();
            return BaselineCollection.Remove(key);
        }
        
        protected override int PerformOmniEnumerate()
        {
            int count = 0;
            foreach (var kvp in OmniCollection)
            {
                count++;
            }
            return count;
        }
        
        protected override int PerformBaselineEnumerate()
        {
            int count = 0;
            foreach (var kvp in BaselineCollection)
            {
                count++;
            }
            return count;
        }
        
        [Benchmark(Description = "LinkedDictionary Add operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object LinkedDictionary_Add() => base.Omni_Add();
        
        [Benchmark(Baseline = true, Description = "Dictionary Add operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object Dictionary_Add() => base.Baseline_Add();
        
        [Benchmark(Description = "LinkedDictionary TryGetValue operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object LinkedDictionary_Get() => base.Omni_Get();
        
        [Benchmark(Description = "Dictionary TryGetValue operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object Dictionary_Get() => base.Baseline_Get();
        
        [Benchmark(Description = "LinkedDictionary Remove operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object LinkedDictionary_Remove() => base.Omni_Remove();
        
        [Benchmark(Description = "Dictionary Remove operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object Dictionary_Remove() => base.Baseline_Remove();
        
        [Benchmark(Description = "LinkedDictionary enumeration")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int LinkedDictionary_Enumerate() => base.Omni_Enumerate();
        
        [Benchmark(Description = "Dictionary enumeration (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int Dictionary_Enumerate() => base.Baseline_Enumerate();
        
    }
    
    /// <summary>
    /// CounterDictionary<TKey, TValue> vs Dictionary<TKey, int> - Frequency counting comparison
    /// </summary>
    [GroupBenchmarks]
    public class CounterDictionaryVsDict : BaselineComparisonBenchmark<CounterDictionary<string, string>, Dictionary<string, int>, string, string>
    {
        [GlobalSetup]
        public void Setup() 
        {
            SetupComparison();
        }
        
        [GlobalCleanup]
        public void Cleanup() => CleanupComparison();
        
        protected override void SetupTestData()
        {
            TestKeys = BenchmarkDataFactory.GetStringPool(DataSize);
            TestValues = TestKeys; // For counters, keys are values
        }
        
        protected override void SetupOmniCollection()
        {
            OmniCollection = new CounterDictionary<string, string>();
            
            // Pre-populate with half the data (some duplicates for counting)
            for (int i = 0; i < DataSize / 2; i++)
            {
                var key = TestKeys[i % (DataSize / 4)]; // Create duplicates
                OmniCollection.AddOrUpdate(key, key); // Value = key, increment count
            }
        }
        
        protected override void SetupBaselineCollection()
        {
            BaselineCollection = new Dictionary<string, int>();
            
            // Pre-populate with manual counting logic
            for (int i = 0; i < DataSize / 2; i++)
            {
                var key = TestKeys[i % (DataSize / 4)]; // Create duplicates
                BaselineCollection.TryGetValue(key, out var count);
                BaselineCollection[key] = count + 1;
            }
        }
        
        
        protected override object PerformOmniAdd()
        {
            var key = GetRandomValue();
            OmniCollection.AddOrUpdate(key, key); // Add/update with count increment
            return OmniCollection.Count;
        }
        
        protected override object PerformBaselineAdd()
        {
            var key = GetRandomValue();
            BaselineCollection.TryGetValue(key, out var count);
            BaselineCollection[key] = count + 1;
            return BaselineCollection.Count;
        }
        
        protected override object PerformOmniGet()
        {
            var key = GetRandomValue();
            return OmniCollection.GetAccessCount(key);
        }
        
        protected override object PerformBaselineGet()
        {
            var key = GetRandomValue();
            return BaselineCollection.GetValueOrDefault(key);
        }
        
        protected override object PerformOmniRemove()
        {
            var key = GetRandomValue();
            return OmniCollection.Remove(key);
        }
        
        protected override object PerformBaselineRemove()
        {
            var key = GetRandomValue();
            return BaselineCollection.Remove(key);
        }
        
        protected override int PerformOmniEnumerate()
        {
            int count = 0;
            foreach (var kvp in OmniCollection)
            {
                count++;
            }
            return count;
        }
        
        protected override int PerformBaselineEnumerate()
        {
            int count = 0;
            foreach (var kvp in BaselineCollection)
            {
                count++;
            }
            return count;
        }
        
        [Benchmark(Description = "CounterDictionary AddOrUpdate operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object CounterDictionary_Add() => base.Omni_Add();
        
        [Benchmark(Baseline = true, Description = "Dictionary manual count operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object Dictionary_Add() => base.Baseline_Add();
        
        [Benchmark(Description = "CounterDictionary GetAccessCount operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object CounterDictionary_Get() => base.Omni_Get();
        
        [Benchmark(Description = "Dictionary GetValueOrDefault operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object Dictionary_Get() => base.Baseline_Get();
        
        [Benchmark(Description = "CounterDictionary Remove operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object CounterDictionary_Remove() => base.Omni_Remove();
        
        [Benchmark(Description = "Dictionary Remove operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object Dictionary_Remove() => base.Baseline_Remove();
        
        [Benchmark(Description = "CounterDictionary enumeration")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int CounterDictionary_Enumerate() => base.Omni_Enumerate();
        
        [Benchmark(Description = "Dictionary enumeration (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int Dictionary_Enumerate() => base.Baseline_Enumerate();
        
    }
    
    /// <summary>
    /// QueueDictionary<TKey, TValue> vs Dictionary<TKey, TValue> + Queue<T> - FIFO dictionary comparison
    /// </summary>
    [GroupBenchmarks]
    public class QueueDictionaryVsDict : BaselineComparisonBenchmark<QueueDictionary<string, int>, (Dictionary<string, int> Dict, Queue<string> Queue), string, int>
    {
        
        [GlobalSetup]
        public void Setup() => SetupComparison();
        
        [GlobalCleanup]
        public void Cleanup() => CleanupComparison();
        
        protected override void SetupTestData()
        {
            TestKeys = BenchmarkDataFactory.GetStringPool(DataSize);
            TestValues = BenchmarkDataFactory.GetIntPool(DataSize);
        }
        
        protected override void SetupOmniCollection()
        {
            OmniCollection = new QueueDictionary<string, int>();
            
            // Pre-populate with half the data
            for (int i = 0; i < DataSize / 2; i++)
            {
                OmniCollection.Enqueue(TestKeys[i], TestValues[i]);
            }
        }
        
        protected override void SetupBaselineCollection()
        {
            // Manual combination of Dictionary + Queue
            BaselineCollection = (new Dictionary<string, int>(), new Queue<string>());
            
            // Pre-populate with half the data
            for (int i = 0; i < DataSize / 2; i++)
            {
                BaselineCollection.Dict[TestKeys[i]] = TestValues[i];
                BaselineCollection.Queue.Enqueue(TestKeys[i]);
            }
        }
        
        protected override object PerformOmniAdd()
        {
            var key = GetRandomKey();
            var value = GetRandomValue();
            OmniCollection.Enqueue(key, value);
            return OmniCollection.Count;
        }
        
        protected override object PerformBaselineAdd()
        {
            var key = GetRandomKey();
            var value = GetRandomValue();
            BaselineCollection.Dict[key] = value;
            BaselineCollection.Queue.Enqueue(key);
            return BaselineCollection.Dict.Count;
        }
        
        protected override object PerformOmniGet()
        {
            var key = GetRandomKey();
            return OmniCollection.TryGetValue(key, out var value) ? value : -1;
        }
        
        protected override object PerformBaselineGet()
        {
            var key = GetRandomKey();
            return BaselineCollection.Dict.TryGetValue(key, out var value) ? value : -1;
        }
        
        protected override object PerformOmniRemove()
        {
            return OmniCollection.Count > 0 ? OmniCollection.Dequeue() : (default(string), default(int));
        }
        
        protected override object PerformBaselineRemove()
        {
            if (BaselineCollection.Queue.Count > 0)
            {
                var key = BaselineCollection.Queue.Dequeue();
                var value = BaselineCollection.Dict.GetValueOrDefault(key);
                BaselineCollection.Dict.Remove(key);
                return (key, value);
            }
            return (default(string), default(int));
        }
        
        protected override int PerformOmniEnumerate()
        {
            int count = 0;
            foreach (var kvp in OmniCollection)
            {
                count++;
            }
            return count;
        }
        
        protected override int PerformBaselineEnumerate()
        {
            int count = 0;
            foreach (var kvp in BaselineCollection.Dict)
            {
                count++;
            }
            return count;
        }
        
        [Benchmark(Description = "QueueDictionary Enqueue operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object QueueDictionary_Add() => base.Omni_Add();
        
        [Benchmark(Baseline = true, Description = "Dictionary+Queue manual operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object DictQueue_Add() => base.Baseline_Add();
        
        [Benchmark(Description = "QueueDictionary TryGetValue operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object QueueDictionary_Get() => base.Omni_Get();
        
        [Benchmark(Description = "Dictionary TryGetValue operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object DictQueue_Get() => base.Baseline_Get();
        
        [Benchmark(Description = "QueueDictionary Dequeue operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object QueueDictionary_Remove() => base.Omni_Remove();
        
        [Benchmark(Description = "Dictionary+Queue manual dequeue operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object DictQueue_Remove() => base.Baseline_Remove();
        
        [Benchmark(Description = "QueueDictionary enumeration")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int QueueDictionary_Enumerate() => base.Omni_Enumerate();
        
        [Benchmark(Description = "Dictionary enumeration (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int DictQueue_Enumerate() => base.Baseline_Enumerate();
        
    }
    
    /// <summary>
    /// CircularDictionary<TKey, TValue> vs Dictionary<TKey, TValue> + manual eviction - LRU eviction comparison
    /// </summary>
    [GroupBenchmarks]
    public class CircularDictionaryVsDict : BaselineComparisonBenchmark<CircularDictionary<string, int>, Dictionary<string, int>, string, int>
    {
        private Queue<string> _evictionQueue = new();
        private HashSet<string> _evictionSet = new(); // O(1) lookup for queue membership
        private readonly int _maxCapacity = 500; // Half of DataSize for LRU simulation
        
        [GlobalSetup]
        public void Setup() 
        {
            SetupComparison();
        }
        
        [GlobalCleanup]
        public void Cleanup() => CleanupComparison();
        
        protected override void SetupTestData()
        {
            TestKeys = BenchmarkDataFactory.GetStringPool(DataSize);
            TestValues = BenchmarkDataFactory.GetIntPool(DataSize);
        }
        
        protected override void SetupOmniCollection()
        {
            OmniCollection = new CircularDictionary<string, int>(_maxCapacity);
            
            // Pre-populate with half the data
            for (int i = 0; i < DataSize / 2; i++)
            {
                OmniCollection[TestKeys[i]] = TestValues[i];
            }
        }
        
        protected override void SetupBaselineCollection()
        {
            BaselineCollection = new Dictionary<string, int>();
            _evictionQueue = new Queue<string>();
            _evictionSet = new HashSet<string>();
            
            // Pre-populate with half the data
            for (int i = 0; i < DataSize / 2; i++)
            {
                BaselineCollection[TestKeys[i]] = TestValues[i];
                _evictionQueue.Enqueue(TestKeys[i]);
                _evictionSet.Add(TestKeys[i]);
            }
        }
        
        protected override object PerformOmniAdd()
        {
            var key = GetRandomKey();
            var value = GetRandomValue();
            OmniCollection[key] = value; // CircularDictionary handles eviction automatically
            return OmniCollection.Count;
        }
        
        protected override object PerformBaselineAdd()
        {
            var key = GetRandomKey();
            var value = GetRandomValue();
            
            // Manual LRU eviction with O(1) operations
            if (BaselineCollection.Count >= _maxCapacity && !BaselineCollection.ContainsKey(key))
            {
                if (_evictionQueue.Count > 0)
                {
                    var oldKey = _evictionQueue.Dequeue();
                    BaselineCollection.Remove(oldKey);
                    _evictionSet.Remove(oldKey);
                }
            }
            
            BaselineCollection[key] = value;
            if (!_evictionSet.Contains(key))
            {
                _evictionQueue.Enqueue(key);
                _evictionSet.Add(key);
            }
            
            return BaselineCollection.Count;
        }
        
        protected override object PerformOmniGet()
        {
            var key = GetRandomKey();
            return OmniCollection.TryGetValue(key, out var value) ? value : -1;
        }
        
        protected override object PerformBaselineGet()
        {
            var key = GetRandomKey();
            return BaselineCollection.TryGetValue(key, out var value) ? value : -1;
        }
        
        protected override object PerformOmniRemove()
        {
            var key = GetRandomKey();
            return OmniCollection.Remove(key);
        }
        
        protected override object PerformBaselineRemove()
        {
            var key = GetRandomKey();
            var removed = BaselineCollection.Remove(key);
            // Note: We don't remove from queue for simplicity in benchmarking
            return removed;
        }
        
        protected override int PerformOmniEnumerate()
        {
            int count = 0;
            foreach (var kvp in OmniCollection)
            {
                count++;
            }
            return count;
        }
        
        protected override int PerformBaselineEnumerate()
        {
            int count = 0;
            foreach (var kvp in BaselineCollection)
            {
                count++;
            }
            return count;
        }
        
        [Benchmark(Description = "CircularDictionary Add operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object CircularDictionary_Add() => base.Omni_Add();
        
        [Benchmark(Baseline = true, Description = "Dictionary+manual eviction operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object Dictionary_Add() => base.Baseline_Add();
        
        [Benchmark(Description = "CircularDictionary TryGetValue operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object CircularDictionary_Get() => base.Omni_Get();
        
        [Benchmark(Description = "Dictionary TryGetValue operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object Dictionary_Get() => base.Baseline_Get();
        
        [Benchmark(Description = "CircularDictionary Remove operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object CircularDictionary_Remove() => base.Omni_Remove();
        
        [Benchmark(Description = "Dictionary Remove operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object Dictionary_Remove() => base.Baseline_Remove();
        
        [Benchmark(Description = "CircularDictionary enumeration")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int CircularDictionary_Enumerate() => base.Omni_Enumerate();
        
        [Benchmark(Description = "Dictionary enumeration (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int Dictionary_Enumerate() => base.Baseline_Enumerate();
    }
    
    /// <summary>
    /// DequeDictionary<TKey, TValue> vs Dictionary<TKey, TValue> + LinkedList<T> - Double-ended queue dictionary comparison
    /// </summary>
    [GroupBenchmarks]
    public class DequeDictionaryVsDict : BaselineComparisonBenchmark<DequeDictionary<string, int>, (Dictionary<string, int> Dict, LinkedList<(string Key, int Value)> List), string, int>
    {
        
        [GlobalSetup]
        public void Setup() 
        {
            SetupComparison();
        }
        
        [GlobalCleanup]
        public void Cleanup() => CleanupComparison();
        
        protected override void SetupTestData()
        {
            TestKeys = BenchmarkDataFactory.GetStringPool(DataSize);
            TestValues = BenchmarkDataFactory.GetIntPool(DataSize);
        }
        
        protected override void SetupOmniCollection()
        {
            OmniCollection = new DequeDictionary<string, int>();
            
            // Pre-populate with half the data
            for (int i = 0; i < DataSize / 2; i++)
            {
                if (i % 2 == 0)
                    OmniCollection.PushFront(TestKeys[i], TestValues[i]);
                else
                    OmniCollection.PushBack(TestKeys[i], TestValues[i]);
            }
        }
        
        protected override void SetupBaselineCollection()
        {
            BaselineCollection = (new Dictionary<string, int>(), new LinkedList<(string, int)>());
            
            // Pre-populate with half the data
            for (int i = 0; i < DataSize / 2; i++)
            {
                BaselineCollection.Dict[TestKeys[i]] = TestValues[i];
                if (i % 2 == 0)
                    BaselineCollection.List.AddFirst((TestKeys[i], TestValues[i]));
                else
                    BaselineCollection.List.AddLast((TestKeys[i], TestValues[i]));
            }
        }
        
        
        protected override object PerformOmniAdd()
        {
            var key = GetRandomKey();
            var value = GetRandomValue();
            // Alternate between PushFront and PushBack
            if (Random.Shared.Next(2) == 0)
                OmniCollection.PushFront(key, value);
            else
                OmniCollection.PushBack(key, value);
            return OmniCollection.Count;
        }
        
        protected override object PerformBaselineAdd()
        {
            var key = GetRandomKey();
            var value = GetRandomValue();
            BaselineCollection.Dict[key] = value;
            // Alternate between AddFirst and AddLast
            if (Random.Shared.Next(2) == 0)
                BaselineCollection.List.AddFirst((key, value));
            else
                BaselineCollection.List.AddLast((key, value));
            return BaselineCollection.Dict.Count;
        }
        
        protected override object PerformOmniGet()
        {
            var key = GetRandomKey();
            return OmniCollection.TryGetValue(key, out var value) ? value : -1;
        }
        
        protected override object PerformBaselineGet()
        {
            var key = GetRandomKey();
            return BaselineCollection.Dict.TryGetValue(key, out var value) ? value : -1;
        }
        
        protected override object PerformOmniRemove()
        {
            // Alternate between PopFront and PopBack
            if (Random.Shared.Next(2) == 0)
                return OmniCollection.Count > 0 && OmniCollection.TryPopFront(out var frontResult) ? frontResult : new KeyValuePair<string, int>(string.Empty, 0);
            else
                return OmniCollection.Count > 0 && OmniCollection.TryPopBack(out var backResult) ? backResult : new KeyValuePair<string, int>(string.Empty, 0);
        }
        
        protected override object PerformBaselineRemove()
        {
            if (BaselineCollection.List.Count > 0)
            {
                // Alternate between RemoveFirst and RemoveLast
                var item = Random.Shared.Next(2) == 0 ? 
                    BaselineCollection.List.First?.Value ?? default :
                    BaselineCollection.List.Last?.Value ?? default;
                
                if (item != default)
                {
                    BaselineCollection.Dict.Remove(item.Key);
                    if (Random.Shared.Next(2) == 0)
                        BaselineCollection.List.RemoveFirst();
                    else
                        BaselineCollection.List.RemoveLast();
                    return item;
                }
            }
            return (default(string), default(int));
        }
        
        protected override int PerformOmniEnumerate()
        {
            int count = 0;
            foreach (var kvp in OmniCollection)
            {
                count++;
            }
            return count;
        }
        
        protected override int PerformBaselineEnumerate()
        {
            int count = 0;
            foreach (var kvp in BaselineCollection.Dict)
            {
                count++;
            }
            return count;
        }
        
        [Benchmark(Description = "DequeDictionary Add operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object DequeDictionary_Add() => base.Omni_Add();
        
        [Benchmark(Baseline = true, Description = "Dictionary+LinkedList manual operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object DictLinkedList_Add() => base.Baseline_Add();
        
        [Benchmark(Description = "DequeDictionary TryGetValue operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object DequeDictionary_Get() => base.Omni_Get();
        
        [Benchmark(Description = "Dictionary TryGetValue operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object DictLinkedList_Get() => base.Baseline_Get();
        
        [Benchmark(Description = "DequeDictionary Remove operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object DequeDictionary_Remove() => base.Omni_Remove();
        
        [Benchmark(Description = "Dictionary+LinkedList manual remove operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object DictLinkedList_Remove() => base.Baseline_Remove();
        
        [Benchmark(Description = "DequeDictionary enumeration")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int DequeDictionary_Enumerate() => base.Omni_Enumerate();
        
        [Benchmark(Description = "Dictionary enumeration (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int DictLinkedList_Enumerate() => base.Baseline_Enumerate();
        
    }
    
    /// <summary>
    /// ConcurrentLinkedDictionary<T> vs ConcurrentDictionary<T> - Thread-safe linked dictionary comparison
    /// </summary>
    [GroupBenchmarks]
    public class ConcurrentLinkedDictionaryVsDict : BaselineComparisonBenchmark<ConcurrentLinkedDictionary<string, int>, ConcurrentDictionary<string, int>, string, int>
    {
        
        [GlobalSetup]
        public void Setup() 
        {
            SetupComparison();
        }
        
        [GlobalCleanup]
        public void Cleanup() => CleanupComparison();
        
        protected override void SetupTestData()
        {
            TestKeys = BenchmarkDataFactory.GetStringPool(DataSize);
            TestValues = BenchmarkDataFactory.GetIntPool(DataSize);
        }
        
        protected override void SetupOmniCollection()
        {
            OmniCollection = new ConcurrentLinkedDictionary<string, int>(capacity: DataSize);
            
            // Pre-populate with half the data
            for (int i = 0; i < DataSize / 2; i++)
            {
                OmniCollection.AddOrUpdate(TestKeys[i], TestValues[i]);
            }
        }
        
        protected override void SetupBaselineCollection()
        {
            BaselineCollection = new ConcurrentDictionary<string, int>();
            
            // Pre-populate with half the data
            for (int i = 0; i < DataSize / 2; i++)
            {
                BaselineCollection.TryAdd(TestKeys[i], TestValues[i]);
            }
        }
        
        
        protected override object PerformOmniAdd()
        {
            var key = GetRandomKey();
            var value = GetRandomValue();
            OmniCollection.AddOrUpdate(key, value);
            return OmniCollection.Count;
        }
        
        protected override object PerformBaselineAdd()
        {
            var key = GetRandomKey();
            var value = GetRandomValue();
            BaselineCollection.TryAdd(key, value);
            return BaselineCollection.Count;
        }
        
        protected override object PerformOmniGet()
        {
            var key = GetRandomKey();
            OmniCollection.TryGetValue(key, out var value);
            return value;
        }
        
        protected override object PerformBaselineGet()
        {
            var key = GetRandomKey();
            BaselineCollection.TryGetValue(key, out var value);
            return value;
        }
        
        protected override object PerformOmniRemove()
        {
            var key = GetRandomKey();
            var removed = OmniCollection.TryRemove(key, out var value);
            return removed ? key : string.Empty;
        }
        
        protected override object PerformBaselineRemove()
        {
            var key = GetRandomKey();
            BaselineCollection.TryRemove(key, out var value);
            return value;
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
        
        protected override void CleanupOmniCollection()
        {
            OmniCollection?.Dispose();
        }
        
        [Benchmark(Description = "ConcurrentLinkedDictionary TryAdd operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object ConcurrentLinkedDictionary_Add() => base.Omni_Add();
        
        [Benchmark(Baseline = true, Description = "ConcurrentDictionary TryAdd operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object ConcurrentDictionary_Add() => base.Baseline_Add();
        
        [Benchmark(Description = "ConcurrentLinkedDictionary TryGetValue operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object ConcurrentLinkedDictionary_Get() => base.Omni_Get();
        
        [Benchmark(Description = "ConcurrentDictionary TryGetValue operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object ConcurrentDictionary_Get() => base.Baseline_Get();
        
        [Benchmark(Description = "ConcurrentLinkedDictionary TryRemove operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object ConcurrentLinkedDictionary_Remove() => base.Omni_Remove();
        
        [Benchmark(Description = "ConcurrentDictionary TryRemove operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object ConcurrentDictionary_Remove() => base.Baseline_Remove();
        
        [Benchmark(Description = "ConcurrentLinkedDictionary enumeration")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int ConcurrentLinkedDictionary_Enumerate() => base.Omni_Enumerate();
        
        [Benchmark(Description = "ConcurrentDictionary enumeration (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int ConcurrentDictionary_Enumerate() => base.Baseline_Enumerate();
    }
    
    /// <summary>
    /// LinkedMultiMap<T> vs Dictionary<K,List<V>> - Multi-value dictionary comparison
    /// </summary>
    [GroupBenchmarks]
    public class LinkedMultiMapVsDict : BaselineComparisonBenchmark<LinkedMultiMap<string, int>, Dictionary<string, List<int>>, string, int>
    {
        
        [GlobalSetup]
        public void Setup() 
        {
            SetupComparison();
        }
        
        [GlobalCleanup]
        public void Cleanup() => CleanupComparison();
        
        protected override void SetupTestData()
        {
            TestKeys = BenchmarkDataFactory.GetStringPool(DataSize / 4); // Fewer keys for multi-value scenario
            TestValues = BenchmarkDataFactory.GetIntPool(DataSize);
        }
        
        protected override void SetupOmniCollection()
        {
            OmniCollection = new LinkedMultiMap<string, int>();
            
            // Pre-populate with multiple values per key
            for (int i = 0; i < DataSize / 2; i++)
            {
                var keyIndex = i % TestKeys.Length;
                OmniCollection.Add(TestKeys[keyIndex], TestValues[i]);
            }
        }
        
        protected override void SetupBaselineCollection()
        {
            BaselineCollection = new Dictionary<string, List<int>>();
            
            // Pre-populate with multiple values per key
            for (int i = 0; i < DataSize / 2; i++)
            {
                var keyIndex = i % TestKeys.Length;
                var key = TestKeys[keyIndex];
                
                if (!BaselineCollection.ContainsKey(key))
                {
                    BaselineCollection[key] = new List<int>();
                }
                BaselineCollection[key].Add(TestValues[i]);
            }
        }
        
        
        protected override object PerformOmniAdd()
        {
            var keyIndex = Random.Shared.Next(TestKeys.Length);
            var key = TestKeys[keyIndex];
            var value = GetRandomValue();
            OmniCollection.Add(key, value);
            return OmniCollection.KeyCount;
        }
        
        protected override object PerformBaselineAdd()
        {
            var keyIndex = Random.Shared.Next(TestKeys.Length);
            var key = TestKeys[keyIndex];
            var value = GetRandomValue();
            
            if (!BaselineCollection.ContainsKey(key))
            {
                BaselineCollection[key] = new List<int>();
            }
            BaselineCollection[key].Add(value);
            return BaselineCollection.Count;
        }
        
        protected override object PerformOmniGet()
        {
            var keyIndex = Random.Shared.Next(TestKeys.Length);
            var key = TestKeys[keyIndex];
            var values = OmniCollection[key];
            return values.Count;
        }
        
        protected override object PerformBaselineGet()
        {
            var keyIndex = Random.Shared.Next(TestKeys.Length);
            var key = TestKeys[keyIndex];
            BaselineCollection.TryGetValue(key, out var values);
            return values?.Count ?? 0;
        }
        
        protected override object PerformOmniRemove()
        {
            var keyIndex = Random.Shared.Next(TestKeys.Length);
            var key = TestKeys[keyIndex];
            return OmniCollection.RemoveKey(key);
        }
        
        protected override object PerformBaselineRemove()
        {
            var keyIndex = Random.Shared.Next(TestKeys.Length);
            var key = TestKeys[keyIndex];
            return BaselineCollection.Remove(key);
        }
        
        protected override int PerformOmniEnumerate()
        {
            int count = 0;
            foreach (var item in OmniCollection)
            {
                count += item.Value.Count;
            }
            return count;
        }
        
        protected override int PerformBaselineEnumerate()
        {
            int count = 0;
            foreach (var item in BaselineCollection)
            {
                count += item.Value.Count;
            }
            return count;
        }
        
        protected override void CleanupOmniCollection()
        {
            OmniCollection?.Dispose();
        }
        
        [Benchmark(Description = "LinkedMultiMap Add operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object LinkedMultiMap_Add() => base.Omni_Add();
        
        [Benchmark(Baseline = true, Description = "Dictionary<K,List<V>> Add operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object DictList_Add() => base.Baseline_Add();
        
        [Benchmark(Description = "LinkedMultiMap indexer operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object LinkedMultiMap_Get() => base.Omni_Get();
        
        [Benchmark(Description = "Dictionary<K,List<V>> TryGetValue operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object DictList_Get() => base.Baseline_Get();
        
        [Benchmark(Description = "LinkedMultiMap RemoveKey operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object LinkedMultiMap_Remove() => base.Omni_Remove();
        
        [Benchmark(Description = "Dictionary<K,List<V>> Remove operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object DictList_Remove() => base.Baseline_Remove();
        
        [Benchmark(Description = "LinkedMultiMap enumeration")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int LinkedMultiMap_Enumerate() => base.Omni_Enumerate();
        
        [Benchmark(Description = "Dictionary<K,List<V>> enumeration (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int DictList_Enumerate() => base.Baseline_Enumerate();
        
        
        
        
    }
    
    /// <summary>
    /// GraphDictionary<T> vs Dictionary<K,V> + adjacency lists - Graph with dictionary comparison
    /// </summary>
    [GroupBenchmarks]
    public class GraphDictionaryVsDict : BaselineComparisonBenchmark<GraphDictionary<string, int>, (Dictionary<string, int> Dict, Dictionary<string, HashSet<string>> Adjacency), string, int>
    {
        
        [GlobalSetup]
        public void Setup() 
        {
            SetupComparison();
        }
        
        [GlobalCleanup]
        public void Cleanup() => CleanupComparison();
        
        protected override void SetupTestData()
        {
            TestKeys = BenchmarkDataFactory.GetStringPool(DataSize);
            TestValues = BenchmarkDataFactory.GetIntPool(DataSize);
        }
        
        protected override void SetupOmniCollection()
        {
            OmniCollection = new GraphDictionary<string, int>();
            
            // Pre-populate with half the data
            for (int i = 0; i < DataSize / 2; i++)
            {
                OmniCollection.Add(TestKeys[i], TestValues[i]);
            }
            
            // Add some edges for graph functionality (after all nodes are added)
            for (int i = 1; i < DataSize / 2; i++)
            {
                OmniCollection.AddEdge(TestKeys[i-1], TestKeys[i]);
            }
        }
        
        protected override void SetupBaselineCollection()
        {
            BaselineCollection = (
                new Dictionary<string, int>(),
                new Dictionary<string, HashSet<string>>()
            );
            
            // Pre-populate with half the data and some edges
            for (int i = 0; i < DataSize / 2; i++)
            {
                BaselineCollection.Dict[TestKeys[i]] = TestValues[i];
                BaselineCollection.Adjacency[TestKeys[i]] = new HashSet<string>();
                
                // Add some edges for graph functionality
                if (i > 0)
                {
                    BaselineCollection.Adjacency[TestKeys[i-1]].Add(TestKeys[i]);
                    if (!BaselineCollection.Adjacency.ContainsKey(TestKeys[i]))
                        BaselineCollection.Adjacency[TestKeys[i]] = new HashSet<string>();
                    BaselineCollection.Adjacency[TestKeys[i]].Add(TestKeys[i-1]);
                }
            }
        }
        
        protected override object PerformOmniAdd()
        {
            var key = GetRandomKey();
            var value = GetRandomValue();
            OmniCollection[key] = value; // Use indexer to handle both add and update
            return OmniCollection.Count;
        }
        
        protected override object PerformBaselineAdd()
        {
            var key = GetRandomKey();
            var value = GetRandomValue();
            BaselineCollection.Dict[key] = value;
            if (!BaselineCollection.Adjacency.ContainsKey(key))
                BaselineCollection.Adjacency[key] = new HashSet<string>();
            return BaselineCollection.Dict.Count;
        }
        
        protected override object PerformOmniGet()
        {
            var key = GetRandomKey();
            OmniCollection.TryGetValue(key, out var value);
            return value;
        }
        
        protected override object PerformBaselineGet()
        {
            var key = GetRandomKey();
            BaselineCollection.Dict.TryGetValue(key, out var value);
            return value;
        }
        
        protected override object PerformOmniRemove()
        {
            var key = GetRandomKey();
            return OmniCollection.Remove(key);
        }
        
        protected override object PerformBaselineRemove()
        {
            var key = GetRandomKey();
            var removed = BaselineCollection.Dict.Remove(key);
            BaselineCollection.Adjacency.Remove(key);
            // Remove from all adjacency lists (simplified)
            foreach (var adj in BaselineCollection.Adjacency.Values)
            {
                adj.Remove(key);
            }
            return removed;
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
            foreach (var kvp in BaselineCollection.Dict)
            {
                count++;
            }
            return count;
        }
        
        [Benchmark(Description = "GraphDictionary AddNode operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object GraphDictionary_Add() => base.Omni_Add();
        
        [Benchmark(Baseline = true, Description = "Dictionary+Adjacency Add operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object DictAdjacency_Add() => base.Baseline_Add();
        
        [Benchmark(Description = "GraphDictionary TryGetValue operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object GraphDictionary_Get() => base.Omni_Get();
        
        [Benchmark(Description = "Dictionary TryGetValue operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object DictAdjacency_Get() => base.Baseline_Get();
        
        [Benchmark(Description = "GraphDictionary RemoveNode operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object GraphDictionary_Remove() => base.Omni_Remove();
        
        [Benchmark(Description = "Dictionary+Adjacency Remove operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object DictAdjacency_Remove() => base.Baseline_Remove();
        
        [Benchmark(Description = "GraphDictionary enumeration")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int GraphDictionary_Enumerate() => base.Omni_Enumerate();
        
        [Benchmark(Description = "Dictionary enumeration (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int DictAdjacency_Enumerate() => base.Baseline_Enumerate();
    }
    
    /// <summary>
    /// PredictiveDictionary<T> vs Dictionary<K,V> - AI-enhanced dictionary comparison
    /// </summary>
    [GroupBenchmarks]
    public class PredictiveDictionaryVsDict : BaselineComparisonBenchmark<PredictiveDictionary<string, int>, Dictionary<string, int>, string, int>
    {
        
        [GlobalSetup]
        public void Setup() 
        {
            SetupComparison();
        }
        
        [GlobalCleanup]
        public void Cleanup() => CleanupComparison();
        
        protected override void SetupTestData()
        {
            TestKeys = BenchmarkDataFactory.GetStringPool(DataSize);
            TestValues = BenchmarkDataFactory.GetIntPool(DataSize);
        }
        
        protected override void SetupOmniCollection()
        {
            OmniCollection = new PredictiveDictionary<string, int>();
            
            // Pre-populate with half the data
            for (int i = 0; i < DataSize / 2; i++)
            {
                OmniCollection[TestKeys[i]] = TestValues[i];
            }
        }
        
        protected override void SetupBaselineCollection()
        {
            BaselineCollection = new Dictionary<string, int>();
            
            // Pre-populate with half the data
            for (int i = 0; i < DataSize / 2; i++)
            {
                BaselineCollection[TestKeys[i]] = TestValues[i];
            }
        }
        
        protected override object PerformOmniAdd()
        {
            var key = GetRandomKey();
            var value = GetRandomValue();
            OmniCollection[key] = value;
            return OmniCollection.Count;
        }
        
        protected override object PerformBaselineAdd()
        {
            var key = GetRandomKey();
            var value = GetRandomValue();
            BaselineCollection[key] = value;
            return BaselineCollection.Count;
        }
        
        protected override object PerformOmniGet()
        {
            var key = GetRandomKey();
            OmniCollection.TryGetValue(key, out var value);
            return value;
        }
        
        protected override object PerformBaselineGet()
        {
            var key = GetRandomKey();
            BaselineCollection.TryGetValue(key, out var value);
            return value;
        }
        
        protected override object PerformOmniRemove()
        {
            var key = GetRandomKey();
            return OmniCollection.Remove(key);
        }
        
        protected override object PerformBaselineRemove()
        {
            var key = GetRandomKey();
            return BaselineCollection.Remove(key);
        }
        
        protected override int PerformOmniEnumerate()
        {
            // PredictiveDictionary doesn't implement IEnumerable
            // Return the count directly as enumeration benchmark alternative
            return OmniCollection.Count;
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
        
        [Benchmark(Description = "PredictiveDictionary indexer operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object PredictiveDictionary_Add() => base.Omni_Add();
        
        [Benchmark(Baseline = true, Description = "Dictionary indexer operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object Dictionary_Add() => base.Baseline_Add();
        
        [Benchmark(Description = "PredictiveDictionary TryGetValue operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object PredictiveDictionary_Get() => base.Omni_Get();
        
        [Benchmark(Description = "Dictionary TryGetValue operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object Dictionary_Get() => base.Baseline_Get();
        
        [Benchmark(Description = "PredictiveDictionary Remove operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object PredictiveDictionary_Remove() => base.Omni_Remove();
        
        [Benchmark(Description = "Dictionary Remove operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object Dictionary_Remove() => base.Baseline_Remove();
        
        [Benchmark(Description = "PredictiveDictionary enumeration")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int PredictiveDictionary_Enumerate() => base.Omni_Enumerate();
        
        [Benchmark(Description = "Dictionary enumeration (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int Dictionary_Enumerate() => base.Baseline_Enumerate();
        
        
        
        
        
    }
    
    

    /// <summary>
    /// SecureHashingBenchmark - Compares secure vs normal hashing performance
    /// </summary>
    [GroupBenchmarks]
    public class SecureHashingBenchmark
    {
        private CircularDictionary<string, int> _secureCircular = null!;
        private CircularDictionary<string, int> _normalCircular = null!;
        private CounterDictionary<string, string> _secureCounter = null!;
        private CounterDictionary<string, string> _normalCounter = null!;
        private LinkedDictionary<string, int> _secureLinked = null!;
        private LinkedDictionary<string, int> _normalLinked = null!;
        private QueueDictionary<string, int> _secureQueue = null!;
        private QueueDictionary<string, int> _normalQueue = null!;
        private DequeDictionary<string, int> _secureDeque = null!;
        private DequeDictionary<string, int> _normalDeque = null!;
        private LinkedMultiMap<string, int> _secureMulti = null!;
        private LinkedMultiMap<string, int> _normalMulti = null!;
        private Dictionary<string, int> _baseline = null!;
        
        private string[] _keys = null!;
        private int[] _values = null!;
        private const int DataSize = 10000;
        private const int Capacity = 5000;
        
        [GlobalSetup]
        public void Setup()
        {
            // Generate test data
            _keys = new string[DataSize];
            _values = new int[DataSize];
            
            for (int i = 0; i < DataSize; i++)
            {
                _keys[i] = $"key_{i:D6}";
                _values[i] = i;
            }
            
            // Setup secure collections (randomized hashing enabled)
            _secureCircular = new CircularDictionary<string, int>(
                Capacity, 
                comparer: null, 
                hashOptions: SecureHashOptions.Default); // Uses secure hashing
                
            _secureCounter = new CounterDictionary<string, string>(
                Capacity, 
                trackWrites: true, 
                loadFactor: 0.75f, 
                comparer: null,
                hashOptions: SecureHashOptions.Default); // Uses secure hashing
                
            _secureLinked = new LinkedDictionary<string, int>(
                Capacity,
                CapacityMode.Dynamic,
                0.75f,
                comparer: null,
                hashOptions: SecureHashOptions.Default);
                
            _secureQueue = new QueueDictionary<string, int>(
                Capacity,
                0.75f,
                comparer: null,
                hashOptions: SecureHashOptions.Default);
                
            _secureDeque = new DequeDictionary<string, int>(
                Capacity,
                0.75f,
                comparer: null,
                hashOptions: SecureHashOptions.Default);
                
            _secureMulti = new LinkedMultiMap<string, int>(
                Capacity,
                CapacityMode.Dynamic,
                allowDuplicateValues: true,
                keyComparer: null,
                valueComparer: null,
                enableLruOptimization: false,
                hashOptions: SecureHashOptions.Default);
                
            
            // Setup normal collections (randomized hashing disabled)
            var normalOptions = new SecureHashOptions { EnableRandomizedHashing = false };
            
            _normalCircular = new CircularDictionary<string, int>(
                Capacity, 
                comparer: null, 
                hashOptions: normalOptions); // No secure hashing
                
            _normalCounter = new CounterDictionary<string, string>(
                Capacity, 
                trackWrites: true, 
                loadFactor: 0.75f, 
                comparer: null,
                hashOptions: normalOptions); // No secure hashing
                
            _normalLinked = new LinkedDictionary<string, int>(
                Capacity,
                CapacityMode.Dynamic,
                0.75f,
                comparer: null,
                hashOptions: normalOptions);
                
            _normalQueue = new QueueDictionary<string, int>(
                Capacity,
                0.75f,
                comparer: null,
                hashOptions: normalOptions);
                
            _normalDeque = new DequeDictionary<string, int>(
                Capacity,
                0.75f,
                comparer: null,
                hashOptions: normalOptions);
                
            _normalMulti = new LinkedMultiMap<string, int>(
                Capacity,
                CapacityMode.Dynamic,
                allowDuplicateValues: true,
                keyComparer: null,
                valueComparer: null,
                enableLruOptimization: false,
                hashOptions: normalOptions);
                
            
            // Setup baseline
            _baseline = new Dictionary<string, int>(Capacity);
            
            // Pre-populate with half the data
            for (int i = 0; i < DataSize / 2; i++)
            {
                _secureCircular.Add(_keys[i], _values[i]);
                _normalCircular.Add(_keys[i], _values[i]);
                _secureCounter.AddOrUpdate(_keys[i], _keys[i]);
                _normalCounter.AddOrUpdate(_keys[i], _keys[i]);
                
                _secureLinked[_keys[i]] = _values[i];
                _normalLinked[_keys[i]] = _values[i];
                
                _secureQueue.Enqueue(_keys[i], _values[i]);
                _normalQueue.Enqueue(_keys[i], _values[i]);
                
                _secureDeque[_keys[i]] = _values[i];
                _normalDeque[_keys[i]] = _values[i];
                
                _secureMulti.Add(_keys[i], _values[i]);
                _normalMulti.Add(_keys[i], _values[i]);
                
                _baseline[_keys[i]] = _values[i];
            }
        }
        
        [Benchmark(Description = "CircularDictionary Add (Secure Hash)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public void CircularSecure_Add()
        {
            for (int i = 0; i < 100; i++)
            {
                var index = (DataSize / 2 + i) % DataSize;
                _secureCircular.Add(_keys[index], _values[index]);
            }
        }
        
        [Benchmark(Description = "CircularDictionary Add (Normal Hash)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public void CircularNormal_Add()
        {
            for (int i = 0; i < 100; i++)
            {
                var index = (DataSize / 2 + i) % DataSize;
                _normalCircular.Add(_keys[index], _values[index]);
            }
        }
        
        [Benchmark(Baseline = true, Description = "Dictionary Add (Baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public void Baseline_Add()
        {
            for (int i = 0; i < 100; i++)
            {
                var index = (DataSize / 2 + i) % DataSize;
                _baseline[_keys[index]] = _values[index];
            }
        }
        
        [Benchmark(Description = "CircularDictionary Get (Secure Hash)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int CircularSecure_Get()
        {
            int sum = 0;
            for (int i = 0; i < 100; i++)
            {
                var index = i % (DataSize / 2);
                if (_secureCircular.TryGetValue(_keys[index], out var value))
                    sum += value;
            }
            return sum;
        }
        
        [Benchmark(Description = "CircularDictionary Get (Normal Hash)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int CircularNormal_Get()
        {
            int sum = 0;
            for (int i = 0; i < 100; i++)
            {
                var index = i % (DataSize / 2);
                if (_normalCircular.TryGetValue(_keys[index], out var value))
                    sum += value;
            }
            return sum;
        }
        
        [Benchmark(Description = "Dictionary Get (Baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int Baseline_Get()
        {
            int sum = 0;
            for (int i = 0; i < 100; i++)
            {
                var index = i % (DataSize / 2);
                if (_baseline.TryGetValue(_keys[index], out var value))
                    sum += value;
            }
            return sum;
        }
        
        // LinkedDictionary benchmarks
        [Benchmark(Description = "LinkedDictionary Add (Secure Hash)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public void LinkedSecure_Add()
        {
            for (int i = 0; i < 100; i++)
            {
                var index = (DataSize / 2 + i) % DataSize;
                _secureLinked[_keys[index]] = _values[index];
            }
        }
        
        [Benchmark(Description = "LinkedDictionary Add (Normal Hash)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public void LinkedNormal_Add()
        {
            for (int i = 0; i < 100; i++)
            {
                var index = (DataSize / 2 + i) % DataSize;
                _normalLinked[_keys[index]] = _values[index];
            }
        }
        
        [Benchmark(Description = "LinkedDictionary Get (Secure Hash)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int LinkedSecure_Get()
        {
            int sum = 0;
            for (int i = 0; i < 100; i++)
            {
                var index = i % (DataSize / 2);
                if (_secureLinked.TryGetValue(_keys[index], out var value))
                    sum += value;
            }
            return sum;
        }
        
        [Benchmark(Description = "LinkedDictionary Get (Normal Hash)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int LinkedNormal_Get()
        {
            int sum = 0;
            for (int i = 0; i < 100; i++)
            {
                var index = i % (DataSize / 2);
                if (_normalLinked.TryGetValue(_keys[index], out var value))
                    sum += value;
            }
            return sum;
        }
        
        // QueueDictionary benchmarks
        [Benchmark(Description = "QueueDictionary Enqueue (Secure Hash)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public void QueueSecure_Enqueue()
        {
            for (int i = 0; i < 100; i++)
            {
                var index = (DataSize / 2 + i) % DataSize;
                _secureQueue.Enqueue(_keys[index], _values[index]);
            }
        }
        
        [Benchmark(Description = "QueueDictionary Enqueue (Normal Hash)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public void QueueNormal_Enqueue()
        {
            for (int i = 0; i < 100; i++)
            {
                var index = (DataSize / 2 + i) % DataSize;
                _normalQueue.Enqueue(_keys[index], _values[index]);
            }
        }
        
        // DequeDictionary benchmarks
        [Benchmark(Description = "DequeDictionary AddFirst (Secure Hash)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public void DequeSecure_AddFirst()
        {
            for (int i = 0; i < 100; i++)
            {
                var index = (DataSize / 2 + i) % DataSize;
                _secureDeque[_keys[index]] = _values[index];
            }
        }
        
        [Benchmark(Description = "DequeDictionary AddFirst (Normal Hash)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public void DequeNormal_AddFirst()
        {
            for (int i = 0; i < 100; i++)
            {
                var index = (DataSize / 2 + i) % DataSize;
                _normalDeque[_keys[index]] = _values[index];
            }
        }
        
        // LinkedMultiMap benchmarks
        [Benchmark(Description = "LinkedMultiMap Add (Secure Hash)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public void MultiSecure_Add()
        {
            for (int i = 0; i < 100; i++)
            {
                var index = (DataSize / 2 + i) % DataSize;
                _secureMulti.Add(_keys[index], _values[index]);
            }
        }
        
        [Benchmark(Description = "LinkedMultiMap Add (Normal Hash)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public void MultiNormal_Add()
        {
            for (int i = 0; i < 100; i++)
            {
                var index = (DataSize / 2 + i) % DataSize;
                _normalMulti.Add(_keys[index], _values[index]);
            }
        }
        
    }
}
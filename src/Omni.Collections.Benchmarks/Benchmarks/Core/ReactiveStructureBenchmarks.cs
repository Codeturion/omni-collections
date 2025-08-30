using BenchmarkDotNet.Attributes;
using Omni.Collections.Benchmarks.Comparison;
using Omni.Collections.Reactive;
using System;
using System.Collections.Generic;
using System.Linq;
using Omni.Collections.Benchmarks.Benchmarks.Helpers;
using System.Collections.Specialized;

namespace Omni.Collections.Benchmarks.Core;

/// <summary>
/// Comprehensive benchmarks for all Reactive data structures in Omni.Collections.
/// Tests: ObservableHashSet, ObservableList vs their traditional .NET equivalents 
/// with appropriate baseline comparisons for reactive/observable collections.
/// </summary>
public class ReactiveStructureBenchmarks
{
    /// <summary>
    /// ObservableHashSet<T> vs HashSet<T> - Reactive hash set comparison
    /// </summary>
    [GroupBenchmarks]
    public class ObservableHashSetVsHashSet : BaselineComparisonBenchmark<ObservableHashSet<int>, HashSet<int>, int, int>
    {
        private ObservableHashSet<int> _eventPooledCollection = null!;
        
        // Deterministic access patterns - no iteration state needed
        private int[] _addValues = null!;        // Values to add in sequence
        private int[] _queryValues = null!;      // Values to query (mix of existing/non-existing)
        private HashSet<int> _sustainableRemoveSet = null!; // Refillable pool for remove operations
        
        [GlobalSetup]
        public void Setup() 
        {
            SetupComparison();
            SetupEventPooledCollection();
        }
        
        [GlobalCleanup]
        public void Cleanup() => CleanupComparison();
        
        protected override void SetupTestData()
        {
            TestValues = BenchmarkDataFactory.GetIntPool(DataSize);
            TestKeys = TestValues;

            // Create deterministic access patterns
            _addValues = new int[DataSize];
            for (int i = 0; i < DataSize; i++)
            {
                _addValues[i] = DataSize + i; // Unique values for adding
            }
            
            // Query values: mix of existing and non-existing for realistic lookup patterns
            _queryValues = new int[DataSize];
            for (int i = 0; i < DataSize; i++)
            {
                _queryValues[i] = i < DataSize / 2 ? TestValues[i] : (DataSize * 2 + i); // 50% hits, 50% misses
            }
            
            // Sustainable remove set: will be refilled as needed
            _sustainableRemoveSet = new HashSet<int>();
            for (int i = 0; i < DataSize / 4; i++)
            {
                _sustainableRemoveSet.Add(DataSize + 10000 + i); // Separate range for remove operations
            }
        }
        
        protected override void SetupOmniCollection()
        {
            OmniCollection = new ObservableHashSet<int>();
            
            // Subscribe to events with comprehensive validation
            OmniCollection.CollectionChanged += (s, e) => {
                EventValidationHelpers.ValidateCollectionChangedEvent(s, e);
            };
            
            // Pre-populate with test data to establish baseline state
            for (int i = 0; i < DataSize / 2; i++)
            {
                OmniCollection.Add(TestValues[i]);
            }
        }
        
        protected override void SetupBaselineCollection()
        {
            BaselineCollection = new HashSet<int>();
            
            // Pre-populate with identical data to ensure fair comparison
            for (int i = 0; i < DataSize / 2; i++)
            {
                BaselineCollection.Add(TestValues[i]);
            }
        }
        
        private void SetupEventPooledCollection()
        {
            // ObservableHashSet with event pooling for algorithm comparison
            _eventPooledCollection = ObservableHashSet<int>.CreateWithEventPooling();
            
            // Subscribe to events with comprehensive validation
            _eventPooledCollection.CollectionChanged += (s, e) => {
                EventValidationHelpers.ValidateCollectionChangedEvent(s, e);
            };
            
            // Pre-populate with identical data for fair comparison
            for (int i = 0; i < DataSize / 2; i++)
            {
                _eventPooledCollection.Add(TestValues[i]);
            }
        }
        
        
        protected override object PerformOmniAdd()
        {
            // Use deterministic access pattern - let base class handle iteration lifecycle
            var value = _addValues[Random.Shared.Next(_addValues.Length)];
            return OmniCollection.Add(value);
        }
        
        protected override object PerformBaselineAdd()
        {
            // Identical access pattern for fair comparison
            var value = _addValues[Random.Shared.Next(_addValues.Length)];
            return BaselineCollection.Add(value);
        }
        
        protected override object PerformOmniGet()
        {
            // Realistic query pattern: mix of hits and misses
            var value = _queryValues[Random.Shared.Next(_queryValues.Length)];
            return OmniCollection.Contains(value);
        }
        
        protected override object PerformBaselineGet()
        {
            // Identical query pattern for fair comparison
            var value = _queryValues[Random.Shared.Next(_queryValues.Length)];
            return BaselineCollection.Contains(value);
        }
        
        protected override object PerformOmniRemove()
        {
            // Sustainable remove pattern: refill pool when needed
            if (_sustainableRemoveSet.Count < 10)
            {
                // Refill with fresh values
                for (int i = 0; i < 100; i++)
                {
                    var refillValue = Random.Shared.Next(100000, 200000);
                    _sustainableRemoveSet.Add(refillValue);
                    OmniCollection.Add(refillValue);
                }
            }
            
            var valueToRemove = _sustainableRemoveSet.First();
            _sustainableRemoveSet.Remove(valueToRemove);
            return OmniCollection.Remove(valueToRemove);
        }
        
        protected override object PerformBaselineRemove()
        {
            // Identical sustainable remove pattern
            if (_sustainableRemoveSet.Count < 10)
            {
                // Refill with fresh values
                for (int i = 0; i < 100; i++)
                {
                    var refillValue = Random.Shared.Next(100000, 200000);
                    _sustainableRemoveSet.Add(refillValue);
                    BaselineCollection.Add(refillValue);
                }
            }
            
            var valueToRemove = _sustainableRemoveSet.First();
            _sustainableRemoveSet.Remove(valueToRemove);
            return BaselineCollection.Remove(valueToRemove);
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
        
        [Benchmark(Description = "ObservableHashSet Add")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object ObservableHashSet_Add() => Omni_Add();
        
        [Benchmark(Description = "ObservableHashSet Add (EventPool.Shared)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public bool ObservableHashSet_Add_EventPooled()
        {
            var value = _addValues[Random.Shared.Next(_addValues.Length)];
            return _eventPooledCollection.Add(value);
        }
        
        [Benchmark(Baseline = true, Description = "HashSet Add operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object HashSet_Add() => Baseline_Add();
        
        [Benchmark(Description = "ObservableHashSet Contains")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object ObservableHashSet_Get() => Omni_Get();
        
        [Benchmark(Description = "ObservableHashSet Contains (EventPool.Shared)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public bool ObservableHashSet_Get_EventPooled()
        {
            var value = _queryValues[Random.Shared.Next(_queryValues.Length)];
            return _eventPooledCollection.Contains(value);
        }
        
        [Benchmark(Description = "HashSet Contains operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object HashSet_Get() => Baseline_Get();
        
        [Benchmark(Description = "ObservableHashSet Remove")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object ObservableHashSet_Remove() => Omni_Remove();
        
        [Benchmark(Description = "ObservableHashSet Remove (EventPool.Shared)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public bool ObservableHashSet_Remove_EventPooled()
        {
            // Use same sustainable pattern for event pooled collection
            if (_sustainableRemoveSet.Count < 10)
            {
                for (int i = 0; i < 100; i++)
                {
                    var refillValue = Random.Shared.Next(200000, 300000);
                    _sustainableRemoveSet.Add(refillValue);
                    _eventPooledCollection.Add(refillValue);
                }
            }
            
            var valueToRemove = _sustainableRemoveSet.First();
            _sustainableRemoveSet.Remove(valueToRemove);
            return _eventPooledCollection.Remove(valueToRemove);
        }
        
        [Benchmark(Description = "HashSet Remove operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object HashSet_Remove() => Baseline_Remove();
        
        [Benchmark(Description = "ObservableHashSet enumeration")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int ObservableHashSet_Enumerate() => Omni_Enumerate();
        
        [Benchmark(Description = "ObservableHashSet enumeration (EventPool.Shared)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int ObservableHashSet_Enumerate_EventPooled()
        {
            int count = 0;
            foreach (var item in _eventPooledCollection)
            {
                count++;
            }
            return count;
        }
        
        [Benchmark(Description = "HashSet enumeration (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int HashSet_Enumerate() => Baseline_Enumerate();
        
    }
    
    /// <summary>
    /// ObservableList<T> vs List<T> - Reactive list comparison
    /// </summary>
    [GroupBenchmarks]
    public class ObservableListVsList : BaselineComparisonBenchmark<ObservableList<int>, List<int>, int, int>
    {
        private ObservableList<int> _noSubscribersCollection = null!;
        
        // Deterministic access patterns
        private int[] _addValues = null!;        // Values to add in sequence
        private Queue<int> _sustainableRemoveQueue = null!; // Refillable remove queue
        
        [GlobalSetup]
        public void Setup() 
        {
            SetupComparison();
            SetupNonSubscribedCollection();
        }
        
        [GlobalCleanup]
        public void Cleanup() => CleanupComparison();
        
        protected override void SetupTestData()
        {
            TestValues = BenchmarkDataFactory.GetIntPool(DataSize);
            TestKeys = TestValues;

            // Create deterministic access patterns
            _addValues = new int[DataSize];
            for (int i = 0; i < DataSize; i++)
            {
                _addValues[i] = DataSize + i; // Unique values for adding
            }
            
            // Sustainable remove queue: will be refilled as needed
            _sustainableRemoveQueue = new Queue<int>();
            for (int i = 0; i < 1000; i++)
            {
                _sustainableRemoveQueue.Enqueue(DataSize + 20000 + i); // Separate range for remove operations
            }
        }
        
        protected override void SetupOmniCollection()
        {
            OmniCollection = new ObservableList<int>();
            
            // Subscribe to events with comprehensive validation
            OmniCollection.CollectionChanged += EventValidationHelpers.ValidateCollectionChangedEvent;
            
            // Pre-populate with test data
            for (int i = 0; i < DataSize / 2; i++)
            {
                OmniCollection.Add(TestValues[i]);
            }
        }
        
        protected override void SetupBaselineCollection()
        {
            BaselineCollection = new List<int>();
            
            // Pre-populate with half the data
            for (int i = 0; i < DataSize / 2; i++)
            {
                BaselineCollection.Add(TestValues[i]);
            }
        }
        
        private void SetupNonSubscribedCollection()
        {
            // ObservableList without event subscribers to measure baseline performance
            _noSubscribersCollection = new ObservableList<int>();
            
            // NO event subscribers - measure pure ObservableList performance
            // This shows the benefit of cached event args when no events are fired
            
            // Pre-populate with identical data for fair comparison
            for (int i = 0; i < DataSize / 2; i++)
            {
                _noSubscribersCollection.Add(TestValues[i]);
            }
        }
        
        protected override object PerformOmniAdd()
        {
            var value = _addValues[Random.Shared.Next(_addValues.Length)];
            OmniCollection.Add(value);
            return true;
        }
        
        protected override object PerformBaselineAdd()
        {
            var value = _addValues[Random.Shared.Next(_addValues.Length)];
            BaselineCollection.Add(value);
            return true;
        }
        
        protected override object PerformOmniGet()
        {
            if (OmniCollection.Count > 0)
            {
                var index = Random.Shared.Next(OmniCollection.Count);
                return OmniCollection[index];
            }
            return 0;
        }
        
        protected override object PerformBaselineGet()
        {
            if (BaselineCollection.Count > 0)
            {
                var index = Random.Shared.Next(BaselineCollection.Count);
                return BaselineCollection[index];
            }
            return 0;
        }
        
        protected override object PerformOmniRemove()
        {
            // Sustainable remove pattern: refill queue when needed
            if (_sustainableRemoveQueue.Count < 10)
            {
                // Refill with fresh values
                for (int i = 0; i < 100; i++)
                {
                    var refillValue = Random.Shared.Next(300000, 400000);
                    _sustainableRemoveQueue.Enqueue(refillValue);
                    OmniCollection.Add(refillValue);
                }
            }
            
            if (_sustainableRemoveQueue.Count > 0)
            {
                var valueToRemove = _sustainableRemoveQueue.Dequeue();
                return OmniCollection.Remove(valueToRemove);
            }
            return false;
        }
        
        protected override object PerformBaselineRemove()
        {
            // Identical sustainable remove pattern
            if (_sustainableRemoveQueue.Count < 10)
            {
                // Refill with fresh values
                for (int i = 0; i < 100; i++)
                {
                    var refillValue = Random.Shared.Next(300000, 400000);
                    _sustainableRemoveQueue.Enqueue(refillValue);
                    BaselineCollection.Add(refillValue);
                }
            }
            
            if (_sustainableRemoveQueue.Count > 0)
            {
                var valueToRemove = _sustainableRemoveQueue.Dequeue();
                return BaselineCollection.Remove(valueToRemove);
            }
            return false;
        }
        
        protected override int PerformOmniEnumerate()
        {
            int sum = 0;
            foreach (var item in OmniCollection)
            {
                sum += item;
            }
            return sum;
        }
        
        protected override int PerformBaselineEnumerate()
        {
            int sum = 0;
            foreach (var item in BaselineCollection)
            {
                sum += item;
            }
            return sum;
        }
        
        [Benchmark(Description = "ObservableList Add operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object ObservableList_Add() => Omni_Add();
        
        [Benchmark(Baseline = true, Description = "List Add operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object List_Add() => Baseline_Add();
        
        [Benchmark(Description = "ObservableList indexer operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object ObservableList_Get() => Omni_Get();
        
        [Benchmark(Description = "List indexer operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object List_Get() => Baseline_Get();
        
        [Benchmark(Description = "ObservableList RemoveAt operation")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object ObservableList_Remove() => Omni_Remove();
        
        [Benchmark(Description = "List RemoveAt operation (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object List_Remove() => Baseline_Remove();
        
        [Benchmark(Description = "ObservableList enumeration")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int ObservableList_Enumerate() => Omni_Enumerate();
        
        [Benchmark(Description = "List enumeration (baseline)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int List_Enumerate() => Baseline_Enumerate();
        
        [Benchmark(Description = "ObservableList Add (no event subscribers)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public bool ObservableList_Add_NoSubscribers()
        {
            var value = _addValues[Random.Shared.Next(_addValues.Length)];
            _noSubscribersCollection.Add(value);
            return true;
        }
        
        [Benchmark(Description = "ObservableList indexer (no event subscribers)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public object ObservableList_Get_NoSubscribers()
        {
            if (_noSubscribersCollection.Count > 0)
            {
                var index = Random.Shared.Next(_noSubscribersCollection.Count);
                return _noSubscribersCollection[index];
            }
            return 0;
        }
        
        [Benchmark(Description = "ObservableList RemoveAt (no event subscribers)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public bool ObservableList_Remove_NoSubscribers()
        {
            // Sustainable remove pattern for no-subscribers collection
            if (_sustainableRemoveQueue.Count < 10)
            {
                // Refill with fresh values
                for (int i = 0; i < 100; i++)
                {
                    var refillValue = Random.Shared.Next(400000, 500000);
                    _sustainableRemoveQueue.Enqueue(refillValue);
                    _noSubscribersCollection.Add(refillValue);
                }
            }
            
            if (_sustainableRemoveQueue.Count > 0)
            {
                var valueToRemove = _sustainableRemoveQueue.Dequeue();
                return _noSubscribersCollection.Remove(valueToRemove);
            }
            return false;
        }
        
        [Benchmark(Description = "ObservableList enumeration (no event subscribers)")]
        [BenchmarkCategory(nameof(BenchmarkCategory.Core))]
        public int ObservableList_Enumerate_NoSubscribers()
        {
            int sum = 0;
            foreach (var item in _noSubscribersCollection)
            {
                sum += item;
            }
            return sum;
        }
    }
    
    
    /// <summary>
    /// Event validation helper methods for ensuring correctness
    /// </summary>
    public static class EventValidationHelpers
    {
        /// <summary>
        /// Validates that CollectionChanged event arguments are mathematically correct
        /// </summary>
        public static void ValidateCollectionChangedEvent(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (sender == null)
                throw new InvalidOperationException("CollectionChanged event sender cannot be null");
                
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems == null || e.NewItems.Count == 0)
                        throw new InvalidOperationException("Add event must have NewItems");
                    // Note: HashSet operations may have NewStartingIndex = -1 since they're unordered
                    // Only validate index for ordered collections (List, etc.)
                    break;
                    
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems == null || e.OldItems.Count == 0)
                        throw new InvalidOperationException("Remove event must have OldItems");
                    // Note: HashSet operations may have OldStartingIndex = -1 since they're unordered
                    // Only validate index for ordered collections (List, etc.)
                    break;
                    
                case NotifyCollectionChangedAction.Replace:
                    if (e.NewItems == null || e.OldItems == null)
                        throw new InvalidOperationException("Replace event must have both NewItems and OldItems");
                    if (e.NewItems.Count != e.OldItems.Count)
                        throw new InvalidOperationException("Replace event must have equal count of new and old items");
                    break;
                    
                case NotifyCollectionChangedAction.Reset:
                    // Reset events should have no items
                    if (e.NewItems != null || e.OldItems != null)
                        throw new InvalidOperationException("Reset event should not have NewItems or OldItems");
                    break;
            }
        }
    }
}
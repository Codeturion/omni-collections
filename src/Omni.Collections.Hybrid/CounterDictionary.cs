using System;
using System.Collections;
using System.Collections.Generic;
using Omni.Collections.Core.Node;
using Omni.Collections.Core.Security;

namespace Omni.Collections.Hybrid
{
    /// <summary>
    /// A dictionary that automatically tracks access frequency with O(1) count retrieval and intelligent frequency grouping.
    /// Provides O(1) access/update operations while maintaining frequency statistics through optimized counter management.
    /// Essential for recommendation engines, hot-spot detection, and adaptive caching systems where O(1) frequency tracking
    /// drives intelligent decision-making and system optimization.
    /// </summary>
    public class CounterDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, (TValue value, long count)>>, IDisposable
        where TKey : notnull
    {
         #region FrequencyNode
        sealed private class FrequencyNode
        {
            public long Frequency;

            public UniversalDictionaryNode<TKey, TValue>? Head;
            public UniversalDictionaryNode<TKey, TValue>? Tail;
            public FrequencyNode? Next;
            public FrequencyNode? Prev;
            public int NodeCount;
            public FrequencyNode() { }

            public FrequencyNode(long frequency)
            {
                Frequency = frequency;
            }
        }
        #endregion
        private UniversalDictionaryNode<TKey, TValue>?[] _buckets;
        private readonly Dictionary<long, FrequencyNode> _frequencyMap;
        private readonly Dictionary<UniversalDictionaryNode<TKey, TValue>, FrequencyNode> _nodeToFrequency;
        private FrequencyNode? _minFrequency;
        private FrequencyNode? _maxFrequency;
        private int _count;
        private int _version;
        private long _totalAccessCount;
        private readonly IEqualityComparer<TKey> _comparer;
        private readonly SecureHashOptions _hashOptions;
        private readonly float _loadFactor;
        private readonly bool _trackWrites;
        private readonly object _frequencyLock = new object();
        public int Count => _count;
        public long TotalAccessCount => _totalAccessCount;
        public bool TrackWrites => _trackWrites;
        public CounterDictionary() : this(16, true, 0.75f, null, null)
        {
        }

        public CounterDictionary(int capacity, bool trackWrites = true)
            : this(capacity, trackWrites, 0.75f, null, null)
        {
        }

        public CounterDictionary(int capacity, bool trackWrites, float loadFactor, IEqualityComparer<TKey>? comparer, SecureHashOptions? hashOptions = null)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            if (loadFactor <= 0 || loadFactor >= 1)
                throw new ArgumentOutOfRangeException(nameof(loadFactor));
            _trackWrites = trackWrites;
            _loadFactor = loadFactor;
            _hashOptions = hashOptions ?? SecureHashOptions.Default;
            
            // Use secure comparer if randomized hashing is enabled and no custom comparer provided
            if (_hashOptions.EnableRandomizedHashing && comparer == null)
            {
                _comparer = SecureHashHelper.CreateSecureComparer<TKey>();
            }
            else
            {
                _comparer = comparer ?? EqualityComparer<TKey>.Default;
            }
            
            _frequencyMap = new Dictionary<long, FrequencyNode>();
            _nodeToFrequency = new Dictionary<UniversalDictionaryNode<TKey, TValue>, FrequencyNode>();
            var initialSize = HybridUtils.GetPrime(capacity);
            _buckets = new UniversalDictionaryNode<TKey, TValue>?[initialSize];
        }

        public TValue this[TKey key]
        {
            get
            {
                if (TryGetValue(key, out var value))
                    return value;
                throw new KeyNotFoundException($"Key '{key}' not found");
            }
            set
            {
                if (_trackWrites)
                {
                    AddOrUpdate(key, value, incrementCount: true);
                }
                else
                {
                    AddOrUpdateInternal(key, value, incrementCount: false);
                }
            }
        }

        public void AddOrUpdate(TKey key, TValue value, bool incrementCount = true)
        {
            AddOrUpdateInternal(key, value, incrementCount);
        }

        private void AddOrUpdateInternal(TKey key, TValue value, bool incrementCount)
        {
            var hashCode = _comparer.GetHashCode(key) & 0x7FFFFFFF;
            var bucketIndex = hashCode % _buckets.Length;
            UniversalDictionaryNode<TKey, TValue>? current = _buckets[bucketIndex];
            while (current != null)
            {
                if (current.HashCode == hashCode && _comparer.Equals(current.Key, key))
                {
                    current.Value = value;
                    if (incrementCount)
                    {
                        IncrementAccessCount(current);
                    }
                    _version++;
                    return;
                }
                current = current.Next;
            }
            if (_count >= _buckets.Length * _loadFactor)
            {
                Resize();
                bucketIndex = hashCode % _buckets.Length;
            }
            var newNode = new UniversalDictionaryNode<TKey, TValue>(key, value, hashCode);
            newNode.Next = _buckets[bucketIndex];
            _buckets[bucketIndex] = newNode;
            var initialFreq = incrementCount ? 1L : 0L;
            if (incrementCount) _totalAccessCount++;
            UniversalNodeHelper.SetAccessCount(newNode, initialFreq);
            lock (_frequencyLock)
            {
                AddToFrequency(newNode, initialFreq);
            }
            _count++;
            _version++;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            var hashCode = _comparer.GetHashCode(key) & 0x7FFFFFFF;
            var bucketIndex = hashCode % _buckets.Length;
            UniversalDictionaryNode<TKey, TValue>? current = _buckets[bucketIndex];
            while (current != null)
            {
                if (current.HashCode == hashCode && _comparer.Equals(current.Key, key))
                {
                    value = current.Value;
                    IncrementAccessCount(current);
                    return true;
                }
                current = current.Next;
            }
            value = default!;
            return false;
        }

        public bool TryPeek(TKey key, out TValue value, out long accessCount)
        {
            var hashCode = _comparer.GetHashCode(key) & 0x7FFFFFFF;
            var bucketIndex = hashCode % _buckets.Length;
            UniversalDictionaryNode<TKey, TValue>? current = _buckets[bucketIndex];
            while (current != null)
            {
                if (current.HashCode == hashCode && _comparer.Equals(current.Key, key))
                {
                    value = current.Value;
                    accessCount = UniversalNodeHelper.GetAccessCount(current);
                    return true;
                }
                current = current.Next;
            }
            value = default!;
            accessCount = 0;
            return false;
        }

        public long GetAccessCount(TKey key)
        {
            TryPeek(key, out _, out var count);
            return count;
        }

        public bool ContainsKey(TKey key)
        {
            return TryPeek(key, out _, out _);
        }

        public bool IncrementCount(TKey key)
        {
            var hashCode = _comparer.GetHashCode(key) & 0x7FFFFFFF;
            var bucketIndex = hashCode % _buckets.Length;
            UniversalDictionaryNode<TKey, TValue>? current = _buckets[bucketIndex];
            while (current != null)
            {
                if (current.HashCode == hashCode && _comparer.Equals(current.Key, key))
                {
                    IncrementAccessCount(current);
                    return true;
                }
                current = current.Next;
            }
            return false;
        }

        public IEnumerable<KeyValuePair<TKey, (TValue value, long count)>> GetMostFrequent(int count = 1)
        {
            if (count <= 0 || _maxFrequency == null)
                yield break;
            var returned = 0;
            var freqNode = _maxFrequency;
            while (freqNode != null && returned < count)
            {
                UniversalDictionaryNode<TKey, TValue>? node = freqNode.Head;
                while (node != null && returned < count)
                {
                    yield return new KeyValuePair<TKey, (TValue, long)>(
                        node.Key,
                        (node.Value, UniversalNodeHelper.GetAccessCount(node)));
                    returned++;
                    node = UniversalNodeHelper.GetNextInFrequency(node);
                }
                freqNode = freqNode.Prev;
            }
        }

        public IEnumerable<KeyValuePair<TKey, (TValue value, long count)>> GetLeastFrequent(int count = 1)
        {
            if (count <= 0 || _minFrequency == null)
                yield break;
            var returned = 0;
            var freqNode = _minFrequency;
            while (freqNode != null && returned < count)
            {
                UniversalDictionaryNode<TKey, TValue>? node = freqNode.Head;
                while (node != null && returned < count)
                {
                    yield return new KeyValuePair<TKey, (TValue, long)>(
                        node.Key,
                        (node.Value, UniversalNodeHelper.GetAccessCount(node)));
                    returned++;
                    node = UniversalNodeHelper.GetNextInFrequency(node);
                }
                freqNode = freqNode.Next;
            }
        }

        public IEnumerable<KeyValuePair<TKey, TValue>> GetItemsWithCount(long count)
        {
            if (_frequencyMap.TryGetValue(count, out var freqNode))
            {
                UniversalDictionaryNode<TKey, TValue>? node = freqNode.Head;
                while (node != null)
                {
                    yield return new KeyValuePair<TKey, TValue>(node.Key, node.Value);
                    node = UniversalNodeHelper.GetNextInFrequency(node);
                }
            }
        }

        public bool Remove(TKey key)
        {
            var hashCode = _comparer.GetHashCode(key) & 0x7FFFFFFF;
            var bucketIndex = hashCode % _buckets.Length;
            UniversalDictionaryNode<TKey, TValue>? current = _buckets[bucketIndex];
            UniversalDictionaryNode<TKey, TValue>? previous = null;
            while (current != null)
            {
                if (current.HashCode == hashCode && _comparer.Equals(current.Key, key))
                {
                    if (previous == null)
                        _buckets[bucketIndex] = current.Next;
                    else
                        previous.Next = current.Next;
                    lock (_frequencyLock)
                    {
                        RemoveFromFrequency(current);
                        _totalAccessCount -= UniversalNodeHelper.GetAccessCount(current);
                    }
                    _count--;
                    _version++;
                    return true;
                }
                previous = current;
                current = current.Next;
            }
            return false;
        }

        public KeyValuePair<TKey, TValue> RemoveLeastFrequent()
        {
            if (_minFrequency == null || _minFrequency.Head == null)
                throw new InvalidOperationException("Dictionary is empty");
            UniversalDictionaryNode<TKey, TValue>? node = _minFrequency.Head;
            var result = new KeyValuePair<TKey, TValue>(node.Key, node.Value);
            Remove(node.Key);
            return result;
        }

        public void Clear()
        {
            Array.Clear(_buckets, 0, _buckets.Length);
            _frequencyMap.Clear();
            _minFrequency = _maxFrequency = null;
            _count = 0;
            _totalAccessCount = 0;
            _version++;
        }

        public (double averageAccessCount, long minCount, long maxCount, double standardDeviation) GetStatistics()
        {
            if (_count == 0)
                return (0, 0, 0, 0);
            var avg = (double)_totalAccessCount / _count;
            var minCount = _minFrequency?.Frequency ?? 0;
            var maxCount = _maxFrequency?.Frequency ?? 0;
            double sumSquaredDiff = 0;
            foreach (KeyValuePair<TKey, (TValue value, long count)> kvp in this)
            {
                var diff = kvp.Value.count - avg;
                sumSquaredDiff += diff * diff;
            }
            var stdDev = Math.Sqrt(sumSquaredDiff / _count);
            return (avg, minCount, maxCount, stdDev);
        }
        #region Private Methods
        private void IncrementAccessCount(UniversalDictionaryNode<TKey, TValue> node)
        {
            lock (_frequencyLock)
            {
                var oldFreq = UniversalNodeHelper.GetAccessCount(node);
                var newFreq = oldFreq + 1;
                RemoveFromFrequency(node);
                UniversalNodeHelper.SetAccessCount(node, newFreq);
                AddToFrequency(node, newFreq);
                _totalAccessCount++;
                _version++;
            }
        }

        private void AddToFrequency(UniversalDictionaryNode<TKey, TValue> node, long frequency)
        {
            if (!_frequencyMap.TryGetValue(frequency, out var freqNode))
            {
                freqNode = new FrequencyNode(frequency);
                _frequencyMap[frequency] = freqNode;
                if (_minFrequency == null || frequency < _minFrequency.Frequency)
                {
                    freqNode.Next = _minFrequency;
                    if (_minFrequency != null)
                        _minFrequency.Prev = freqNode;
                    _minFrequency = freqNode;
                    if (_maxFrequency == null)
                        _maxFrequency = freqNode;
                }
                else if (frequency > _maxFrequency!.Frequency)
                {
                    freqNode.Prev = _maxFrequency;
                    _maxFrequency.Next = freqNode;
                    _maxFrequency = freqNode;
                }
                else
                {
                    var current = _minFrequency;
                    while (current!.Next != null && current.Next.Frequency < frequency)
                    {
                        current = current.Next;
                    }
                    freqNode.Next = current.Next;
                    freqNode.Prev = current;
                    if (current.Next != null)
                        current.Next.Prev = freqNode;
                    current.Next = freqNode;
                }
            }
            UniversalNodeHelper.SetNextInFrequency(node, freqNode.Head);
            UniversalNodeHelper.SetPrevInFrequency(node, null);
            if (freqNode.Head != null)
                UniversalNodeHelper.SetPrevInFrequency(freqNode.Head, node);
            freqNode.Head = node;
            if (freqNode.Tail == null)
                freqNode.Tail = node;
            _nodeToFrequency[node] = freqNode;
            freqNode.NodeCount++;
        }

        private void RemoveFromFrequency(UniversalDictionaryNode<TKey, TValue> node)
        {
            if (!_nodeToFrequency.TryGetValue(node, out var freqNode)) return;
            UniversalDictionaryNode<TKey, TValue>? prevInFreq = UniversalNodeHelper.GetPrevInFrequency(node);
            UniversalDictionaryNode<TKey, TValue>? nextInFreq = UniversalNodeHelper.GetNextInFrequency(node);
            if (prevInFreq != null)
                UniversalNodeHelper.SetNextInFrequency(prevInFreq, nextInFreq);
            else
                freqNode.Head = nextInFreq;
            if (nextInFreq != null)
                UniversalNodeHelper.SetPrevInFrequency(nextInFreq, prevInFreq);
            else
                freqNode.Tail = prevInFreq;
            freqNode.NodeCount--;
            if (freqNode.NodeCount == 0)
            {
                _frequencyMap.Remove(freqNode.Frequency);
                if (freqNode.Prev != null)
                    freqNode.Prev.Next = freqNode.Next;
                else
                    _minFrequency = freqNode.Next;
                if (freqNode.Next != null)
                    freqNode.Next.Prev = freqNode.Prev;
                else
                    _maxFrequency = freqNode.Prev;
            }
            _nodeToFrequency.Remove(node);
            UniversalNodeHelper.SetNextInFrequency(node, null);
            UniversalNodeHelper.SetPrevInFrequency(node, null);
        }

        private void Resize()
        {
            var newSize = HybridUtils.GetPrime(_buckets.Length * 2);
            var newBuckets = new UniversalDictionaryNode<TKey, TValue>?[newSize];
            for (int i = 0; i < _buckets.Length; i++)
            {
                UniversalDictionaryNode<TKey, TValue>? current = _buckets[i];
                while (current != null)
                {
                    UniversalDictionaryNode<TKey, TValue>? next = current.Next;
                    var bucketIndex = current.HashCode % newSize;
                    current.Next = newBuckets[bucketIndex];
                    newBuckets[bucketIndex] = current;
                    current = next;
                }
            }
            _buckets = newBuckets;
        }
        #endregion
        #region IEnumerable Implementation
        public Enumerator GetEnumerator() => new Enumerator(this);
        IEnumerator<KeyValuePair<TKey, (TValue value, long count)>> IEnumerable<KeyValuePair<TKey, (TValue value, long count)>>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion
        public void Dispose()
        {
            Clear();
        }

        public struct Enumerator : IEnumerator<KeyValuePair<TKey, (TValue value, long count)>>
        {
            private readonly CounterDictionary<TKey, TValue> _dictionary;
            private readonly int _version;
            private int _bucketIndex;
            private UniversalDictionaryNode<TKey, TValue>? _current;
            private KeyValuePair<TKey, (TValue value, long count)> _currentPair;
            internal Enumerator(CounterDictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary;
                _version = dictionary._version;
                _bucketIndex = 0;
                _current = null;
                _currentPair = default;
            }

            public bool MoveNext()
            {
                if (_version != _dictionary._version)
                    throw new InvalidOperationException("Collection was modified during enumeration");
                if (_current != null)
                {
                    _currentPair = new KeyValuePair<TKey, (TValue, long)>(
                        _current.Key,
                        (_current.Value, UniversalNodeHelper.GetAccessCount(_current)));
                    _current = _current.Next;
                    return true;
                }
                while (_bucketIndex < _dictionary._buckets.Length)
                {
                    _current = _dictionary._buckets[_bucketIndex];
                    _bucketIndex++;
                    if (_current != null)
                    {
                        _currentPair = new KeyValuePair<TKey, (TValue, long)>(
                            _current.Key,
                            (_current.Value, UniversalNodeHelper.GetAccessCount(_current)));
                        _current = _current.Next;
                        return true;
                    }
                }
                return false;
            }

            public KeyValuePair<TKey, (TValue value, long count)> Current => _currentPair;
            object IEnumerator.Current => Current;
            public void Reset()
            {
                if (_version != _dictionary._version)
                    throw new InvalidOperationException("Collection was modified during enumeration");
                _bucketIndex = 0;
                _current = null;
                _currentPair = default;
            }

            public void Dispose() { }
        }
    }
}
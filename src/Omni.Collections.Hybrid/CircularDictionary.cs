using System;
using System.Collections;
using System.Collections.Generic;
using Omni.Collections.Core.Security;

namespace Omni.Collections.Hybrid
{
    /// <summary>
    /// A fixed-capacity dictionary that automatically evicts oldest entries when full, implementing LRU-like behavior.
    /// Guarantees O(1) Add/Remove/Contains operations with automatic memory bounds through circular eviction strategy.
    /// Ideal for asset caches, recently accessed item storage, texture/sound caches in games, or any scenario
    /// requiring bounded O(1) performance with automatic cleanup.
    /// </summary>
    public class CircularDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>, IDisposable
        where TKey : notnull
    {
        #region Entry
        public sealed class Entry
        {
            public TKey Key = default!;

            public TValue Value = default!;
            public int HashCode;
            public int NextIndex = -1;
            public bool IsOccupied;
            public long Timestamp;
        }
        #endregion Entry
        private readonly Entry[] _buffer;
        private readonly int[] _buckets;
        private readonly int _capacity;
        private readonly IEqualityComparer<TKey> _comparer;
        private readonly SecureHashOptions _hashOptions;
        private int _collisionCount;
        private int _head;
        private int _tail;
        private int _count;
        private long _timestamp;
        private int _version;
        public int Capacity => _capacity;
        public int Count => _count;
        public bool IsFull => _count == _capacity;
        public bool IsEmpty => _count == 0;
        public CircularDictionary(int capacity, IEqualityComparer<TKey>? comparer = null, SecureHashOptions? hashOptions = null)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            _capacity = capacity;
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
            _buffer = new Entry[capacity];
            var bucketsSize = HybridUtils.GetPrime(capacity);
            _buckets = new int[bucketsSize];
            for (int i = 0; i < capacity; i++)
            {
                _buffer[i] = new Entry();
            }
            for (int i = 0; i < _buckets.Length; i++)
            {
                _buckets[i] = -1;
            }
            _head = 0;
            _tail = 0;
        }

        public void Add(TKey key, TValue value)
        {
            var hashCode = _comparer.GetHashCode(key) & 0x7FFFFFFF;
            var bucketIndex = hashCode % _buckets.Length;
            var currentIndex = _buckets[bucketIndex];
            var previousIndex = -1;
            var chainLength = 0;
            
            while (currentIndex != -1)
            {
                var entry = _buffer[currentIndex];
                if (entry.HashCode == hashCode && _comparer.Equals(entry.Key, key))
                {
                    entry.Value = value;
                    entry.Timestamp = ++_timestamp;
                    _version++;
                    return;
                }
                previousIndex = currentIndex;
                currentIndex = entry.NextIndex;
                chainLength++;
                
                // Check for excessive collisions
                if (_hashOptions.EnableCollisionMonitoring && chainLength > _hashOptions.MaxCollisionChainLength)
                {
                    _collisionCount++;
                    _hashOptions.OnExcessiveCollisions?.Invoke(
                        $"CircularDictionary: Excessive collision chain length ({chainLength}) detected for key: {key}");
                }
            }
            Entry newEntry;
            int newIndex;
            if (_count < _capacity)
            {
                newIndex = _tail;
                newEntry = _buffer[newIndex];
                _tail = (_tail + 1) % _capacity;
                _count++;
            }
            else
            {
                newIndex = _head;
                newEntry = _buffer[newIndex];
                RemoveFromBucket(newEntry);
                _head = (_head + 1) % _capacity;
                _tail = (_tail + 1) % _capacity;
            }
            newEntry.Key = key;
            newEntry.Value = value;
            newEntry.HashCode = hashCode;
            newEntry.IsOccupied = true;
            newEntry.Timestamp = ++_timestamp;
            newEntry.NextIndex = -1;
            if (previousIndex == -1)
            {
                newEntry.NextIndex = _buckets[bucketIndex];
                _buckets[bucketIndex] = newIndex;
            }
            else
            {
                newEntry.NextIndex = _buffer[previousIndex].NextIndex;
                _buffer[previousIndex].NextIndex = newIndex;
            }
            _version++;
        }

        public TValue this[TKey key]
        {
            get
            {
                if (TryGetValue(key, out var value))
                    return value;
                throw new KeyNotFoundException($"Key '{key}' not found");
            }
            set => Add(key, value);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            var hashCode = _comparer.GetHashCode(key) & 0x7FFFFFFF;
            var bucketIndex = hashCode % _buckets.Length;
            var currentIndex = _buckets[bucketIndex];
            while (currentIndex != -1)
            {
                var entry = _buffer[currentIndex];
                if (entry.HashCode == hashCode && _comparer.Equals(entry.Key, key))
                {
                    value = entry.Value;
                    return true;
                }
                currentIndex = entry.NextIndex;
            }
            value = default!;
            return false;
        }

        public bool ContainsKey(TKey key)
        {
            return TryGetValue(key, out _);
        }

        public KeyValuePair<TKey, TValue> GetOldest()
        {
            if (_count == 0)
                throw new InvalidOperationException("Dictionary is empty");
            var entry = _buffer[_head];
            return new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
        }

        public KeyValuePair<TKey, TValue> GetNewest()
        {
            if (_count == 0)
                throw new InvalidOperationException("Dictionary is empty");
            var newestIndex = (_tail - 1 + _capacity) % _capacity;
            var entry = _buffer[newestIndex];
            return new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
        }

        public bool Remove(TKey key)
        {
            var hashCode = _comparer.GetHashCode(key) & 0x7FFFFFFF;
            var bucketIndex = hashCode % _buckets.Length;
            var currentIndex = _buckets[bucketIndex];
            var previousIndex = -1;
            while (currentIndex != -1)
            {
                var entry = _buffer[currentIndex];
                if (entry.HashCode == hashCode && _comparer.Equals(entry.Key, key))
                {
                    if (previousIndex == -1)
                        _buckets[bucketIndex] = entry.NextIndex;
                    else
                        _buffer[previousIndex].NextIndex = entry.NextIndex;
                    entry.IsOccupied = false;
                    entry.Key = default!;
                    entry.Value = default!;
                    _version++;
                    return true;
                }
                previousIndex = currentIndex;
                currentIndex = entry.NextIndex;
            }
            return false;
        }

        public void Clear()
        {
            for (int i = 0; i < _buckets.Length; i++)
            {
                _buckets[i] = -1;
            }
            for (int i = 0; i < _capacity; i++)
            {
                var entry = _buffer[i];
                entry.Key = default!;
                entry.Value = default!;
                entry.IsOccupied = false;
                entry.NextIndex = -1;
            }
            _head = 0;
            _tail = 0;
            _count = 0;
            _timestamp = 0;
            _version++;
        }

        public IEnumerable<KeyValuePair<TKey, TValue>> GetRecentWindow(int windowSize)
        {
            if (windowSize <= 0 || windowSize > _count)
                windowSize = _count;
            var startIndex = (_tail - windowSize + _capacity) % _capacity;
            for (int i = 0; i < windowSize; i++)
            {
                var index = (startIndex + i) % _capacity;
                var entry = _buffer[index];
                if (entry.IsOccupied)
                {
                    yield return new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
                }
            }
        }

        public (int oldestIndex, int newestIndex, double averageAge) GetStatistics()
        {
            if (_count == 0)
                return (-1, -1, 0);
            var newestIndex = (_tail - 1 + _capacity) % _capacity;
            var totalAge = 0L;
            var validCount = 0;
            for (int i = 0; i < _capacity; i++)
            {
                if (_buffer[i].IsOccupied)
                {
                    totalAge += _timestamp - _buffer[i].Timestamp;
                    validCount++;
                }
            }
            var avgAge = validCount > 0 ? (double)totalAge / validCount : 0;
            return (_head, newestIndex, avgAge);
        }
        #region Private Methods
        private void RemoveFromBucket(Entry entryToRemove)
        {
            var bucketIndex = entryToRemove.HashCode % _buckets.Length;
            var currentIndex = _buckets[bucketIndex];
            var previousIndex = -1;
            while (currentIndex != -1)
            {
                var entry = _buffer[currentIndex];
                if (entry == entryToRemove)
                {
                    if (previousIndex == -1)
                        _buckets[bucketIndex] = entry.NextIndex;
                    else
                        _buffer[previousIndex].NextIndex = entry.NextIndex;
                    break;
                }
                previousIndex = currentIndex;
                currentIndex = entry.NextIndex;
            }
        }
        #endregion
        #region IEnumerable Implementation
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return new Enumerator(this);
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            private readonly CircularDictionary<TKey, TValue> _dictionary;
            private readonly int _version;
            private int _index;
            private int _visitedCount;
            private KeyValuePair<TKey, TValue> _current;
            internal Enumerator(CircularDictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary;
                _version = dictionary._version;
                _index = dictionary._head;
                _visitedCount = 0;
                _current = default;
            }

            public KeyValuePair<TKey, TValue> Current => _current;
            object IEnumerator.Current => Current;
            public bool MoveNext()
            {
                if (_version != _dictionary._version)
                    throw new InvalidOperationException("Collection was modified during enumeration");
                while (_visitedCount < _dictionary._count)
                {
                    var entry = _dictionary._buffer[_index];
                    _index = (_index + 1) % _dictionary._capacity;
                    _visitedCount++;
                    if (entry.IsOccupied)
                    {
                        _current = new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
                        return true;
                    }
                }
                return false;
            }

            public void Reset()
            {
                if (_version != _dictionary._version)
                    throw new InvalidOperationException("Collection was modified during enumeration");
                _index = _dictionary._head;
                _visitedCount = 0;
                _current = default;
            }

            public void Dispose()
            {
            }
        }
        #endregion
        public void Dispose()
        {
            Clear();
        }
    }
}
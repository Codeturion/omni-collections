using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Omni.Collections.Hybrid;
using Omni.Collections.Core.Security;

namespace Omni.Collections.Probabilistic
{
    /// <summary>
    /// A dictionary enhanced with Bloom filter pre-screening that dramatically accelerates negative lookup scenarios.
    /// Delivers O(1) Contains/Add/Remove with probabilistic early rejection of non-existent keys through Bloom filtering.
    /// Exceptional for distributed caches, spell checkers, and database query optimization where most lookups
    /// return negative results and avoiding unnecessary work is critical.
    /// </summary>
    public class BloomDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDisposable
        where TKey : notnull
    {
        #region Core Data Structures
        private struct Entry
        {
            public TKey Key;

            public TValue Value;
            public int Hash;
            public EntryState State;
        }

        private enum EntryState : byte
        {
            Empty = 0,
            Occupied = 1,
            Deleted = 2
        }

        private Entry[] _entries;
        private readonly BloomFilter<TKey> _bloomFilter;
        private int _count;
        private int _version;
        private readonly IEqualityComparer<TKey> _comparer;
        private readonly SecureHashOptions _hashOptions;
        private readonly HashSet<int> _occupiedIndices = [];
        private int _deletedCount;
        private bool _disposed;
        private const double MaxLoadFactor = 0.75;
        private const double MinLoadFactor = 0.10;
        private const int DefaultCapacity = 16;
        private const int MaxProbeDistance = 128;
        #endregion
        #region Constructors
        public BloomDictionary() : this(DefaultCapacity)
        {
        }

        public BloomDictionary(int capacity) : this(capacity, 0.01)
        {
        }

        public BloomDictionary(int capacity, double falsePositiveRate)
            : this(capacity, falsePositiveRate, null, null)
        {
        }

        public BloomDictionary(int capacity, double falsePositiveRate, IEqualityComparer<TKey>? comparer, SecureHashOptions? hashOptions = null)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            if (falsePositiveRate <= 0 || falsePositiveRate >= 1)
                throw new ArgumentOutOfRangeException(nameof(falsePositiveRate), "Must be between 0 and 1");
            capacity = Math.Max(DefaultCapacity, HybridUtils.GetNextPowerOfTwo(capacity));
            _entries = new Entry[capacity];
            _bloomFilter = new BloomFilter<TKey>(capacity * 2, falsePositiveRate);
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
            _count = 0;
            _version = 0;
        }

        public BloomDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection)
            : this(collection, 0.01)
        {
        }

        public BloomDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, double falsePositiveRate)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            ICollection<KeyValuePair<TKey, TValue>> items = collection as ICollection<KeyValuePair<TKey, TValue>> ?? collection.ToList();
            var capacity = Math.Max(DefaultCapacity, HybridUtils.GetNextPowerOfTwo(items.Count));
            _entries = new Entry[capacity];
            _bloomFilter = new BloomFilter<TKey>(capacity * 2, falsePositiveRate);
            _hashOptions = SecureHashOptions.Default;
            
            // Use secure comparer if randomized hashing is enabled
            if (_hashOptions.EnableRandomizedHashing)
            {
                _comparer = SecureHashHelper.CreateSecureComparer<TKey>();
            }
            else
            {
                _comparer = EqualityComparer<TKey>.Default;
            }
            _count = 0;
            _version = 0;
            foreach (KeyValuePair<TKey, TValue> kvp in items)
            {
                Add(kvp.Key, kvp.Value);
            }
        }
        #endregion
        #region Dictionary Operations
        public TValue this[TKey key]
        {
            get
            {
                if (TryGetValue(key, out var value))
                    return value;
                throw new KeyNotFoundException($"Key '{key}' not found in BloomDictionary");
            }
            set
            {
                if (key == null)
                    throw new ArgumentNullException(nameof(key));
                Insert(key, value, overwrite: true);
            }
        }

        public void Add(TKey key, TValue value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (!Insert(key, value, overwrite: false))
                throw new ArgumentException($"Key '{key}' already exists in BloomDictionary");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(TKey key, out TValue value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (!_bloomFilter.Contains(key))
            {
                value = default!;
                return false;
            }
            int hash = _comparer.GetHashCode(key);
            int index = hash & (_entries.Length - 1);
            int probeCount = 0;
            while (probeCount < MaxProbeDistance)
            {
                ref var entry = ref _entries[index];
                if (entry.State == EntryState.Empty)
                {
                    value = default!;
                    return false;
                }
                if (entry.State == EntryState.Occupied &&
                    entry.Hash == hash &&
                    _comparer.Equals(entry.Key, key))
                {
                    value = entry.Value;
                    return true;
                }
                index = (index + 1) & (_entries.Length - 1);
                probeCount++;
            }
            value = default!;
            return false;
        }

        public bool Remove(TKey key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (!_bloomFilter.Contains(key))
                return false;
            int hash = _comparer.GetHashCode(key);
            int index = hash & (_entries.Length - 1);
            int probeCount = 0;
            while (probeCount < MaxProbeDistance)
            {
                ref var entry = ref _entries[index];
                if (entry.State == EntryState.Empty)
                    return false;
                if (entry.State == EntryState.Occupied &&
                    entry.Hash == hash &&
                    _comparer.Equals(entry.Key, key))
                {
                    entry.State = EntryState.Deleted;
                    entry.Key = default!;
                    entry.Value = default!;
                    _occupiedIndices.Remove(index);
                    _deletedCount++;
                    _count--;
                    _version++;
                    
                    if ((_count & 15) == 0)
                    {
                        bool shouldShrink = _count > 0 && _count < _entries.Length * MinLoadFactor;
                        bool tooManyDeleted = _deletedCount > _entries.Length * 0.25;
                        if (shouldShrink || tooManyDeleted)
                        {
                            int newCapacity = shouldShrink ? _entries.Length / 2 : _entries.Length;
                            Resize(newCapacity);
                        }
                    }
                    return true;
                }
                index = (index + 1) & (_entries.Length - 1);
                probeCount++;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsKey(TKey key)
        {
            return TryGetValue(key, out _);
        }

        public void Clear()
        {
            Array.Clear(_entries, 0, _entries.Length);
            _bloomFilter.Clear();
            _occupiedIndices.Clear();
            _deletedCount = 0;
            _count = 0;
            _version++;
        }
        #endregion
        #region Internal Implementation
        private bool Insert(TKey key, TValue value, bool overwrite)
        {
            if (_count >= _entries.Length * MaxLoadFactor)
            {
                Resize(_entries.Length * 2);
            }
            int hash = _comparer.GetHashCode(key);
            int index = hash & (_entries.Length - 1);
            int probeCount = 0;
            int firstDeletedIndex = -1;
            while (probeCount < MaxProbeDistance)
            {
                ref var entry = ref _entries[index];
                if (entry.State == EntryState.Empty)
                {
                    int targetIndex = firstDeletedIndex >= 0 ? firstDeletedIndex : index;
                    ref var targetEntry = ref _entries[targetIndex];
                    targetEntry.Key = key;
                    targetEntry.Value = value;
                    targetEntry.Hash = hash;
                    targetEntry.State = EntryState.Occupied;
                    _occupiedIndices.Add(targetIndex);
                    _bloomFilter.Add(key);
                    _count++;
                    _version++;
                    return true;
                }
                if (entry.State == EntryState.Deleted && firstDeletedIndex < 0)
                {
                    firstDeletedIndex = index;
                }
                else if (entry.State == EntryState.Occupied &&
                         entry.Hash == hash &&
                         _comparer.Equals(entry.Key, key))
                {
                    if (overwrite)
                    {
                        entry.Value = value;
                        _version++;
                        return true;
                    }
                    return false;
                }
                index = (index + 1) & (_entries.Length - 1);
                probeCount++;
            }
            Resize(_entries.Length * 2);
            return Insert(key, value, overwrite);
        }

        private void Resize(int newCapacity)
        {
            newCapacity = Math.Max(DefaultCapacity, HybridUtils.GetNextPowerOfTwo(newCapacity));
            Entry[] oldEntries = _entries;
            _entries = new Entry[newCapacity];
            _count = 0;
            _occupiedIndices.Clear();
            _deletedCount = 0;
            _bloomFilter.Clear();
            foreach (var entry in oldEntries)
            {
                if (entry.State == EntryState.Occupied)
                {
                    Insert(entry.Key, entry.Value, overwrite: true);
                }
            }
        }
        #endregion
        #region Collection Properties
        public int Count => _count;
        public bool IsReadOnly => false;
        public ICollection<TKey> Keys
        {
            get
            {
                var keys = new List<TKey>(_count);
                foreach (var index in _occupiedIndices)
                {
                    ref var entry = ref _entries[index];
                    if (entry.State == EntryState.Occupied)
                        keys.Add(entry.Key);
                }
                return keys;
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                var values = new List<TValue>(_count);
                foreach (var index in _occupiedIndices)
                {
                    ref var entry = ref _entries[index];
                    if (entry.State == EntryState.Occupied)
                        values.Add(entry.Value);
                }
                return values;
            }
        }
        #endregion
        #region ICollection Implementation
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return TryGetValue(item.Key, out var value) &&
                   EqualityComparer<TValue>.Default.Equals(value, item.Value);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0 || arrayIndex > array.Length)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            if (array.Length - arrayIndex < _count)
                throw new ArgumentException("Destination array is not large enough");
            int index = arrayIndex;
            foreach (var entryIndex in _occupiedIndices)
            {
                ref var entry = ref _entries[entryIndex];
                if (entry.State == EntryState.Occupied)
                {
                    array[index++] = new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
                }
            }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return TryGetValue(item.Key, out var value) &&
                   EqualityComparer<TValue>.Default.Equals(value, item.Value) &&
                   Remove(item.Key);
        }
        #endregion
        #region IEnumerable Implementation
        public Enumerator GetEnumerator() => new Enumerator(this);
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion
        #region IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            if (disposing)
            {
                _bloomFilter?.Dispose();
                _entries = null!;
            }
            _disposed = true;
        }
        #endregion
        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            private readonly BloomDictionary<TKey, TValue> _dictionary;
            private readonly int _version;
            private readonly IEnumerator<int> _indexEnumerator;
            private KeyValuePair<TKey, TValue> _current;
            internal Enumerator(BloomDictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary;
                _version = dictionary._version;
                _indexEnumerator = dictionary._occupiedIndices.GetEnumerator();
                _current = default;
            }

            public bool MoveNext()
            {
                if (_version != _dictionary._version)
                    throw new InvalidOperationException("Collection was modified during enumeration");
                while (_indexEnumerator.MoveNext())
                {
                    var index = _indexEnumerator.Current;
                    var entry = _dictionary._entries[index];
                    if (entry.State == EntryState.Occupied)
                    {
                        _current = new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
                        return true;
                    }
                }
                return false;
            }

            public KeyValuePair<TKey, TValue> Current => _current;
            object IEnumerator.Current => Current;
            public void Reset()
            {
                if (_version != _dictionary._version)
                    throw new InvalidOperationException("Collection was modified during enumeration");
                _indexEnumerator.Reset();
                _current = default;
            }

            public void Dispose()
            {
                _indexEnumerator.Dispose();
            }
        }
    }
}
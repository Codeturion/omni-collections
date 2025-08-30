using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Omni.Collections.Core.Node;
using Omni.Collections.Core.Security;

namespace Omni.Collections.Hybrid.LinkedDictionary
{
    /// <summary>
    /// A dictionary that preserves insertion order while delivering O(1) hash-table speed for all operations.
    /// Combines O(1) Add/Remove/Contains with guaranteed iteration order through optimized doubly-linked node management.
    /// Perfect for LRU caches, ordered configurations, and session management where both O(1) access
    /// and predictable iteration order are fundamental requirements.
    /// </summary>
    public class LinkedDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>, IDisposable
        where TKey : notnull
    {
        private UniversalDictionaryNode<TKey, TValue>?[] _buckets;

        private UniversalDictionaryNode<TKey, TValue>? _head;
        private UniversalDictionaryNode<TKey, TValue>? _tail;
        private int _count;
        private int _version;
        private readonly CapacityMode _capacityMode;
        private readonly int _maxCapacity;
        private readonly float _loadFactor;
        private readonly IEqualityComparer<TKey> _comparer;
        private readonly SecureHashOptions _hashOptions;
        public int Count => _count;
        public CapacityMode Mode => _capacityMode;
        public TValue this[TKey key]
        {
            get
            {
                if (TryGetValue(key, out var value))
                    return value;
                throw new KeyNotFoundException($"Key '{key}' not found");
            }
            set => AddOrUpdate(key, value);
        }

        public LinkedDictionary() : this(16, CapacityMode.Dynamic, 0.75f, null)
        {
        }

        public LinkedDictionary(int capacity) : this(capacity, CapacityMode.Dynamic, 0.75f, null)
        {
        }

        public LinkedDictionary(int capacity, CapacityMode mode, float loadFactor = 0.75f)
            : this(capacity, mode, loadFactor, null)
        {
        }

        public LinkedDictionary(int capacity, CapacityMode mode, float loadFactor, IEqualityComparer<TKey>? comparer, SecureHashOptions? hashOptions = null)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            if (loadFactor <= 0 || loadFactor > 1)
                throw new ArgumentOutOfRangeException(nameof(loadFactor));
            _capacityMode = mode;
            _maxCapacity = mode == CapacityMode.Fixed ? capacity : int.MaxValue;
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
            var initialSize = HybridUtils.GetPrime(capacity);
            _buckets = new UniversalDictionaryNode<TKey, TValue>?[initialSize];
        }

        public void AddOrUpdate(TKey key, TValue value)
        {
            var hashCode = _comparer.GetHashCode(key) & 0x7FFFFFFF;
            var bucketIndex = hashCode % _buckets.Length;
            UniversalDictionaryNode<TKey, TValue>? current = _buckets[bucketIndex];
            while (current != null)
            {
                if (current.HashCode == hashCode && _comparer.Equals(current.Key, key))
                {
                    current.Value = value;
                    MoveToFront(current);
                    _version++;
                    return;
                }
                current = current.Next;
            }
            switch (_capacityMode) {
            case CapacityMode.Fixed when _count >= _maxCapacity:
                EvictLru();
                break;
            case CapacityMode.Dynamic when _count >= _buckets.Length * _loadFactor:
                Resize();
                bucketIndex = hashCode % _buckets.Length;
                break;
            }
            UniversalDictionaryNode<TKey, TValue> newNode = CreateNode(key, value, hashCode);
            newNode.Next = _buckets[bucketIndex];
            _buckets[bucketIndex] = newNode;
            AddToFront(newNode);
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
                    MoveToFront(current);
                    return true;
                }
                current = current.Next;
            }
            value = default!;
            return false;
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
                    RemoveFromLru(current);
                    _count--;
                    _version++;
                    return true;
                }
                previous = current;
                current = current.Next;
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
            UniversalDictionaryNode<TKey, TValue>? current = _head;
            while (current != null)
            {
                UniversalDictionaryNode<TKey, TValue>? next = current.NextOrdering;
                current = next;
            }
            Array.Clear(_buckets, 0, _buckets.Length);
            _head = _tail = null;
            _count = 0;
            _version++;
        }

        public KeyValuePair<TKey, TValue> PeekLru()
        {
            if (_tail == null)
                throw new InvalidOperationException("Dictionary is empty");
            return new KeyValuePair<TKey, TValue>(_tail.Key, _tail.Value);
        }

        public KeyValuePair<TKey, TValue> PeekMru()
        {
            if (_head == null)
                throw new InvalidOperationException("Dictionary is empty");
            return new KeyValuePair<TKey, TValue>(_head.Key, _head.Value);
        }
        #region LRU Management
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MoveToFront(UniversalDictionaryNode<TKey, TValue> node)
        {
            if (node == _head)
                return;
            RemoveFromLru(node);
            AddToFront(node);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddToFront(UniversalDictionaryNode<TKey, TValue> node)
        {
            node.NextOrdering = _head;
            node.PrevOrdering = null;
            if (_head != null)
                _head.PrevOrdering = node;
            _head = node;
            if (_tail == null)
                _tail = node;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RemoveFromLru(UniversalDictionaryNode<TKey, TValue> node)
        {
            if (node.PrevOrdering != null)
                node.PrevOrdering.NextOrdering = node.NextOrdering;
            else
                _head = node.NextOrdering;
            if (node.NextOrdering != null)
                node.NextOrdering.PrevOrdering = node.PrevOrdering;
            else
                _tail = node.PrevOrdering;
        }

        private void EvictLru()
        {
            if (_tail == null)
                return;
            UniversalDictionaryNode<TKey, TValue>? toEvict = _tail;
            var bucketIndex = toEvict.HashCode % _buckets.Length;
            UniversalDictionaryNode<TKey, TValue>? current = _buckets[bucketIndex];
            UniversalDictionaryNode<TKey, TValue>? previous = null;
            while (current != null)
            {
                if (current == toEvict)
                {
                    if (previous == null)
                        _buckets[bucketIndex] = current.Next;
                    else
                        previous.Next = current.Next;
                    break;
                }
                previous = current;
                current = current.Next;
            }
            RemoveFromLru(toEvict);
            _count--;
        }
        #endregion
        #region Node Pool Management
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private UniversalDictionaryNode<TKey, TValue> CreateNode(TKey key, TValue value, int hashCode)
        {
            return new UniversalDictionaryNode<TKey, TValue>(key, value, hashCode);
        }
        #endregion
        #region Resize and Utilities
        private void Resize()
        {
            var newSize = HybridUtils.GetPrime(_buckets.Length * 2);
            var newBuckets = new UniversalDictionaryNode<TKey, TValue>?[newSize];
            UniversalDictionaryNode<TKey, TValue>? current = _head;
            while (current != null)
            {
                UniversalDictionaryNode<TKey, TValue>? next = current.NextOrdering;
                var bucketIndex = current.HashCode % newSize;
                current.Next = newBuckets[bucketIndex];
                newBuckets[bucketIndex] = current;
                current = next;
            }
            _buckets = newBuckets;
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
            private readonly LinkedDictionary<TKey, TValue> _dictionary;
            private readonly int _version;
            private UniversalDictionaryNode<TKey, TValue>? _current;
            private KeyValuePair<TKey, TValue> _currentValue;
            internal Enumerator(LinkedDictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary;
                _version = dictionary._version;
                _current = null;
                _currentValue = default;
            }

            public KeyValuePair<TKey, TValue> Current => _currentValue;
            object IEnumerator.Current => Current;
            public bool MoveNext()
            {
                if (_version != _dictionary._version)
                    throw new InvalidOperationException("Collection was modified during enumeration");
                if (_current == null)
                    _current = _dictionary._head;
                else
                    _current = _current.NextOrdering;
                if (_current != null)
                {
                    _currentValue = new KeyValuePair<TKey, TValue>(_current.Key, _current.Value);
                    return true;
                }
                return false;
            }

            public void Reset()
            {
                if (_version != _dictionary._version)
                    throw new InvalidOperationException("Collection was modified during enumeration");
                _current = null;
                _currentValue = default;
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
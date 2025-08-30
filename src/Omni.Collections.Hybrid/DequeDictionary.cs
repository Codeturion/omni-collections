using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Omni.Collections.Core.Node;
using Omni.Collections.Core.Security;

namespace Omni.Collections.Hybrid
{
    /// <summary>
    /// A dictionary combined with double-ended queue capabilities for lightning-fast head/tail operations.
    /// Delivers O(1) AddFirst/AddLast/RemoveFirst/RemoveLast operations alongside O(1) key-based lookups through dual indexing.
    /// Ideal for task scheduling systems, undo/redo implementations, and sliding window algorithms where bidirectional
    /// access patterns determine application responsiveness.
    /// </summary>
    public class DequeDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>, IDisposable
        where TKey : notnull
    {
        private UniversalDictionaryNode<TKey, TValue>?[] _buckets;

        private UniversalDictionaryNode<TKey, TValue>? _head;
        private UniversalDictionaryNode<TKey, TValue>? _tail;
        private int _count;
        private int _version;
        private readonly IEqualityComparer<TKey> _comparer;
        private readonly SecureHashOptions _hashOptions;
        private readonly float _loadFactor;
        public int Count => _count;
        public bool IsEmpty => _count == 0;
        public DequeDictionary() : this(16, 0.75f, null)
        {
        }

        public DequeDictionary(int capacity) : this(capacity, 0.75f, null)
        {
        }

        public DequeDictionary(int capacity, float loadFactor, IEqualityComparer<TKey>? comparer = null, SecureHashOptions? hashOptions = null)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            if (loadFactor <= 0 || loadFactor >= 1)
                throw new ArgumentOutOfRangeException(nameof(loadFactor));
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

        public void PushFront(TKey key, TValue value)
        {
            var hashCode = _comparer.GetHashCode(key) & 0x7FFFFFFF;
            var bucketIndex = hashCode % _buckets.Length;
            UniversalDictionaryNode<TKey, TValue>? current = _buckets[bucketIndex];
            while (current != null)
            {
                if (current.HashCode == hashCode && _comparer.Equals(current.Key, key))
                {
                    current.Value = value;
                    RemoveFromDeque(current);
                    AddToFront(current);
                    _version++;
                    return;
                }
                current = current.Next;
            }
            AddNewNode(key, value, hashCode, addToFront: true);
        }

        public void PushBack(TKey key, TValue value)
        {
            var hashCode = _comparer.GetHashCode(key) & 0x7FFFFFFF;
            var bucketIndex = hashCode % _buckets.Length;
            UniversalDictionaryNode<TKey, TValue>? current = _buckets[bucketIndex];
            while (current != null)
            {
                if (current.HashCode == hashCode && _comparer.Equals(current.Key, key))
                {
                    current.Value = value;
                    RemoveFromDeque(current);
                    AddToBack(current);
                    _version++;
                    return;
                }
                current = current.Next;
            }
            AddNewNode(key, value, hashCode, addToFront: false);
        }

        public KeyValuePair<TKey, TValue> PopFront()
        {
            if (_head == null)
                throw new InvalidOperationException("Deque is empty");
            UniversalDictionaryNode<TKey, TValue>? node = _head;
            var result = new KeyValuePair<TKey, TValue>(node.Key, node.Value);
            _head = node.NextOrdering;
            if (_head != null)
                _head.PrevOrdering = null;
            else
                _tail = null;
            RemoveFromBucket(node);
            _count--;
            _version++;
            return result;
        }

        public KeyValuePair<TKey, TValue> PopBack()
        {
            if (_tail == null)
                throw new InvalidOperationException("Deque is empty");
            UniversalDictionaryNode<TKey, TValue>? node = _tail;
            var result = new KeyValuePair<TKey, TValue>(node.Key, node.Value);
            _tail = node.PrevOrdering;
            if (_tail != null)
                _tail.NextOrdering = null;
            else
                _head = null;
            RemoveFromBucket(node);
            _count--;
            _version++;
            return result;
        }

        public bool TryPopFront(out KeyValuePair<TKey, TValue> result)
        {
            if (_head == null)
            {
                result = default;
                return false;
            }
            result = PopFront();
            return true;
        }

        public bool TryPopBack(out KeyValuePair<TKey, TValue> result)
        {
            if (_tail == null)
            {
                result = default;
                return false;
            }
            result = PopBack();
            return true;
        }

        public KeyValuePair<TKey, TValue> PeekFront()
        {
            if (_head == null)
                throw new InvalidOperationException("Deque is empty");
            return new KeyValuePair<TKey, TValue>(_head.Key, _head.Value);
        }

        public KeyValuePair<TKey, TValue> PeekBack()
        {
            if (_tail == null)
                throw new InvalidOperationException("Deque is empty");
            return new KeyValuePair<TKey, TValue>(_tail.Key, _tail.Value);
        }

        public TValue this[TKey key]
        {
            get
            {
                if (TryGetValue(key, out var value))
                    return value;
                throw new KeyNotFoundException($"Key '{key}' not found");
            }
            set => PushBack(key, value);
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
                    return true;
                }
                current = current.Next;
            }
            value = default!;
            return false;
        }

        public bool ContainsKey(TKey key)
        {
            return TryGetValue(key, out _);
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
                    RemoveFromDeque(current);
                    _count--;
                    _version++;
                    return true;
                }
                previous = current;
                current = current.Next;
            }
            return false;
        }

        public bool MoveToFront(TKey key)
        {
            var hashCode = _comparer.GetHashCode(key) & 0x7FFFFFFF;
            var bucketIndex = hashCode % _buckets.Length;
            UniversalDictionaryNode<TKey, TValue>? current = _buckets[bucketIndex];
            while (current != null)
            {
                if (current.HashCode == hashCode && _comparer.Equals(current.Key, key))
                {
                    RemoveFromDeque(current);
                    AddToFront(current);
                    _version++;
                    return true;
                }
                current = current.Next;
            }
            return false;
        }

        public bool MoveToBack(TKey key)
        {
            var hashCode = _comparer.GetHashCode(key) & 0x7FFFFFFF;
            var bucketIndex = hashCode % _buckets.Length;
            UniversalDictionaryNode<TKey, TValue>? current = _buckets[bucketIndex];
            while (current != null)
            {
                if (current.HashCode == hashCode && _comparer.Equals(current.Key, key))
                {
                    RemoveFromDeque(current);
                    AddToBack(current);
                    _version++;
                    return true;
                }
                current = current.Next;
            }
            return false;
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

        public KeyValuePair<TKey, TValue>[] ToArray()
        {
            var array = new KeyValuePair<TKey, TValue>[_count];
            var index = 0;
            UniversalDictionaryNode<TKey, TValue>? current = _head;
            while (current != null)
            {
                array[index++] = new KeyValuePair<TKey, TValue>(current.Key, current.Value);
                current = current.NextOrdering;
            }
            return array;
        }

        public IEnumerable<KeyValuePair<TKey, TValue>> Reverse()
        {
            UniversalDictionaryNode<TKey, TValue>? current = _tail;
            while (current != null)
            {
                yield return new KeyValuePair<TKey, TValue>(current.Key, current.Value);
                current = current.PrevOrdering;
            }
        }
        #region Private Methods
        private void AddNewNode(TKey key, TValue value, int hashCode, bool addToFront)
        {
            if (_count >= _buckets.Length * _loadFactor)
            {
                Resize();
            }
            var bucketIndex = hashCode % _buckets.Length;
            UniversalDictionaryNode<TKey, TValue>? newNode = RentNode(key, value, hashCode);
            newNode.Next = _buckets[bucketIndex];
            _buckets[bucketIndex] = newNode;
            if (addToFront)
                AddToFront(newNode);
            else
                AddToBack(newNode);
            _count++;
            _version++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddToFront(UniversalDictionaryNode<TKey, TValue> node)
        {
            node.NextOrdering = _head;
            node.PrevOrdering = null;
            if (_head != null)
                _head.PrevOrdering = node;
            else
                _tail = node;
            _head = node;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddToBack(UniversalDictionaryNode<TKey, TValue> node)
        {
            node.NextOrdering = null;
            node.PrevOrdering = _tail;
            if (_tail != null)
                _tail.NextOrdering = node;
            else
                _head = node;
            _tail = node;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RemoveFromDeque(UniversalDictionaryNode<TKey, TValue> node)
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

        private void RemoveFromBucket(UniversalDictionaryNode<TKey, TValue> node)
        {
            var bucketIndex = node.HashCode % _buckets.Length;
            UniversalDictionaryNode<TKey, TValue>? current = _buckets[bucketIndex];
            UniversalDictionaryNode<TKey, TValue>? previous = null;
            while (current != null)
            {
                if (current == node)
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
        }

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
        #region Node Management
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private UniversalDictionaryNode<TKey, TValue> RentNode(TKey key, TValue value, int hashCode)
        {
            return new UniversalDictionaryNode<TKey, TValue>(key, value, hashCode);
        }
        #endregion
        #region IEnumerable Implementation
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return new Enumerator(this);
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            private readonly DequeDictionary<TKey, TValue> _dictionary;
            private readonly int _version;
            private UniversalDictionaryNode<TKey, TValue>? _current;
            private KeyValuePair<TKey, TValue> _currentValue;
            internal Enumerator(DequeDictionary<TKey, TValue> dictionary)
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
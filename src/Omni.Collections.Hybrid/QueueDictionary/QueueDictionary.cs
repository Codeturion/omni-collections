using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Omni.Collections.Core.Node;
using Omni.Collections.Core.Security;

namespace Omni.Collections.Hybrid.QueueDictionary
{
    /// <summary>
    /// A hybrid structure that seamlessly merges O(1) dictionary lookups with queue semantics for unmatched processing flexibility.
    /// Provides O(1) Enqueue/Dequeue/Contains operations while maintaining perfect FIFO ordering through optimized dual-indexing.
    /// Indispensable for message brokers, task schedulers, and event processing systems where both O(1) random access
    /// and ordered processing determine system responsiveness.
    /// </summary>
    public class QueueDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>, IDisposable
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
        private UniversalDictionaryNode<TKey, TValue>[]? _queueOrderCache;
        private int _queueOrderCacheVersion = -1;
        public int Count => _count;
        public bool IsEmpty => _count == 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InvalidateCache()
        {
            _version++;
            _queueOrderCacheVersion = -1;
        }

        public QueueDictionary() : this(16, 0.75f, null)
        {
        }

        public QueueDictionary(int capacity) : this(capacity, 0.75f, null)
        {
        }

        public QueueDictionary(int capacity, float loadFactor, IEqualityComparer<TKey>? comparer = null, SecureHashOptions? hashOptions = null)
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

        public void Enqueue(TKey key, TValue value)
        {
            var hashCode = _comparer.GetHashCode(key) & 0x7FFFFFFF;
            var bucketIndex = hashCode % _buckets.Length;
            UniversalDictionaryNode<TKey, TValue>? current = _buckets[bucketIndex];
            while (current != null)
            {
                if (current.HashCode == hashCode && _comparer.Equals(current.Key, key))
                {
                    current.Value = value;
                    RemoveFromQueue(current);
                    AddToBack(current);
                    InvalidateCache();
                    return;
                }
                current = current.Next;
            }
            if (_count >= _buckets.Length * _loadFactor)
            {
                Resize();
                bucketIndex = hashCode % _buckets.Length;
            }
            UniversalDictionaryNode<TKey, TValue>? newNode = CreateNode(key, value, hashCode);
            newNode.Next = _buckets[bucketIndex];
            _buckets[bucketIndex] = newNode;
            AddToBack(newNode);
            _count++;
            InvalidateCache();
        }

        public KeyValuePair<TKey, TValue> Dequeue()
        {
            if (_head == null)
                throw new InvalidOperationException("Queue is empty");
            UniversalDictionaryNode<TKey, TValue>? node = _head;
            var result = new KeyValuePair<TKey, TValue>(node.Key, node.Value);
            _head = node.NextOrdering;
            if (_head != null)
                _head.PrevOrdering = null;
            else
                _tail = null;
            RemoveFromBucket(node);
            _count--;
            InvalidateCache();
            return result;
        }

        public bool TryDequeue(out KeyValuePair<TKey, TValue> result)
        {
            if (_head == null)
            {
                result = default;
                return false;
            }
            result = Dequeue();
            return true;
        }

        public KeyValuePair<TKey, TValue> PeekFront()
        {
            if (_head == null)
                throw new InvalidOperationException("Queue is empty");
            return new KeyValuePair<TKey, TValue>(_head.Key, _head.Value);
        }

        public KeyValuePair<TKey, TValue> PeekBack()
        {
            if (_tail == null)
                throw new InvalidOperationException("Queue is empty");
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
            set => Enqueue(key, value);
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
                    RemoveFromQueue(current);
                    _count--;
                    InvalidateCache();
                    return true;
                }
                previous = current;
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
            _queueOrderCache = null;
            InvalidateCache();
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
        #region Private Methods
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
        private void RemoveFromQueue(UniversalDictionaryNode<TKey, TValue> node)
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
        #region Node Pool Management
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private UniversalDictionaryNode<TKey, TValue> CreateNode(TKey key, TValue value, int hashCode)
        {
            return new UniversalDictionaryNode<TKey, TValue>(key, value, hashCode);
        }
        #endregion
        #region IEnumerable Implementation
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return GetQueueOrderEnumerator();
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator(EnumerationMode mode)
        {
            return GetQueueOrderEnumerator(mode);
        }

        private IEnumerator<KeyValuePair<TKey, TValue>> GetQueueOrderEnumerator(EnumerationMode mode = EnumerationMode.Fast)
        {
            switch (mode)
            {
                case EnumerationMode.Fast:
                    return GetFastDirectEnumerator();
                case EnumerationMode.InsertionOrder:
                    return GetDirectTraversalEnumerator();
                case EnumerationMode.Cached:
                    return GetCachedEnumerator();
                default:
                    return GetFastDirectEnumerator();
            }
        }

        private IEnumerator<KeyValuePair<TKey, TValue>> GetFastDirectEnumerator()
        {
            var version = _version;
            UniversalDictionaryNode<TKey, TValue>? current = _head;
            while (current != null)
            {
                if (version != _version)
                    throw new InvalidOperationException("Collection was modified during enumeration");
                yield return new KeyValuePair<TKey, TValue>(current.Key, current.Value);
                current = current.Link1;
            }
        }

        private IEnumerator<KeyValuePair<TKey, TValue>> GetDirectTraversalEnumerator()
        {
            return GetFastDirectEnumerator();
        }

        private IEnumerator<KeyValuePair<TKey, TValue>> GetCachedEnumerator()
        {
            if (_queueOrderCacheVersion == _version && _queueOrderCache != null)
            {
                for (int i = 0; i < _count; i++)
                {
                    UniversalDictionaryNode<TKey, TValue>? node = _queueOrderCache[i];
                    yield return new KeyValuePair<TKey, TValue>(node.Key, node.Value);
                }
                yield break;
            }
            using (IEnumerator<KeyValuePair<TKey, TValue>>? enumerator = GetFastDirectEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    yield return enumerator.Current;
                }
            }
        }

        public FastEnumerator GetFastEnumerator()
        {
            return new FastEnumerator(this);
        }

        public void WarmEnumerationCache()
        {
            if (_queueOrderCacheVersion != _version && _count > 0)
            {
                _queueOrderCache = new UniversalDictionaryNode<TKey, TValue>[_count];
                UniversalDictionaryNode<TKey, TValue>? current = _head;
                var index = 0;
                while (current != null && index < _count)
                {
                    _queueOrderCache[index++] = current;
                    current = current.NextOrdering;
                }
                _queueOrderCacheVersion = _version;
            }
        }

        public struct FastEnumerator
        {
            private readonly QueueDictionary<TKey, TValue> _dictionary;
            private readonly int _version;
            private UniversalDictionaryNode<TKey, TValue>? _current;
            private bool _started;
            private readonly bool _useCache;
            private int _cacheIndex;
            internal FastEnumerator(QueueDictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary;
                _version = dictionary._version;
                _current = null;
                _started = false;
                _useCache = dictionary._queueOrderCacheVersion == dictionary._version && dictionary._queueOrderCache != null;
                _cacheIndex = -1;
            }

            public KeyValuePair<TKey, TValue> Current
            {
                get
                {
                    if (_current == null)
                        throw new InvalidOperationException("Enumeration has not started or has finished");
                    return new KeyValuePair<TKey, TValue>(_current.Key, _current.Value);
                }
            }

            public bool MoveNext()
            {
                if (_version != _dictionary._version)
                    throw new InvalidOperationException("Collection was modified during enumeration");
                if (_useCache)
                {
                    _cacheIndex++;
                    if (_cacheIndex < _dictionary._count && _dictionary._queueOrderCache != null)
                    {
                        _current = _dictionary._queueOrderCache[_cacheIndex];
                        return true;
                    }
                    return false;
                }
                else
                {
                    if (!_started)
                    {
                        _current = _dictionary._head;
                        _started = true;
                    }
                    else if (_current != null)
                    {
                        _current = _current.Link1;
                    }
                    return _current != null;
                }
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            private readonly QueueDictionary<TKey, TValue> _dictionary;
            private readonly int _version;
            private UniversalDictionaryNode<TKey, TValue>? _current;
            private KeyValuePair<TKey, TValue> _currentValue;
            internal Enumerator(QueueDictionary<TKey, TValue> dictionary)
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
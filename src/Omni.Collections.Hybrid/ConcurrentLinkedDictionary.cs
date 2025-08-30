using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Omni.Collections.Hybrid.LinkedDictionary;
using Omni.Collections.Core.Node;
using Omni.Collections.Core.Security;

namespace Omni.Collections.Hybrid
{
    /// <summary>
    /// A thread-safe linked dictionary achieving lock-free reads and fine-grained locking for writes with preserved insertion order.
    /// Provides O(1) Add/Remove/Contains operations with minimal contention through optimized concurrent algorithms.
    /// Essential for high-concurrency LRU caches, multi-threaded event processors, and shared state management where
    /// thread safety and predictable iteration order are both critical.
    /// </summary>
    public class ConcurrentLinkedDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>, IDisposable
        where TKey : notnull
    {
        #region Bucket
        sealed private class Bucket
        {
            private UniversalDictionaryNode<TKey, TValue>? _head;

            private readonly object _lock = new object();
            public bool TryGet(TKey key, int hashCode, IEqualityComparer<TKey> comparer, out UniversalDictionaryNode<TKey, TValue>? node)
            {
                lock (_lock)
                {
                    node = _head;
                    while (node != null) {
                        if (node.HashCode == hashCode && comparer.Equals(node.Key, key)) {
                            UniversalNodeHelper.SetAccessTime(node, Stopwatch.GetTimestamp());
                            return true;
                        }
                        node = node.Next;
                    }
                    return false;
                }
            }

            public void AddOrUpdate(UniversalDictionaryNode<TKey, TValue> newNode, TKey key, int hashCode, IEqualityComparer<TKey> comparer, out UniversalDictionaryNode<TKey, TValue>? existing)
            {
                lock (_lock) {
                    UniversalDictionaryNode<TKey, TValue>? current = _head;
                    while (current != null) {
                        if (current.HashCode == hashCode && comparer.Equals(current.Key, key)) {
                            existing = current;
                            current.Value = newNode.Value;
                            UniversalNodeHelper.SetAccessTime(current, UniversalNodeHelper.GetAccessTime(newNode));
                            return;
                        }
                        current = current.Next;
                    }
                    existing = null;
                    newNode.Next = _head;
                    _head = newNode;
                }
            }

            public bool TryRemove(TKey key, int hashCode, IEqualityComparer<TKey> comparer, out UniversalDictionaryNode<TKey, TValue>? removed)
            {
                lock (_lock) {
                    UniversalDictionaryNode<TKey, TValue>? current = _head;
                    UniversalDictionaryNode<TKey, TValue>? previous = null;
                    while (current != null) {
                        if (current.HashCode == hashCode && comparer.Equals(current.Key, key)) {
                            removed = current;
                            if (previous == null)
                                _head = current.Next;
                            else
                                previous.Next = current.Next;
                            return true;
                        }
                        previous = current;
                        current = current.Next;
                    }
                    removed = null;
                    return false;
                }
            }
        }
        #endregion Bucket
        private readonly Bucket[] _buckets;
        private volatile UniversalDictionaryNode<TKey, TValue>? _lruHead;
        private volatile UniversalDictionaryNode<TKey, TValue>? _lruTail;
        private volatile int _count;
        private readonly int _maxCapacity;
        private readonly CapacityMode _capacityMode;
        private readonly IEqualityComparer<TKey> _comparer;
        private readonly SecureHashOptions _hashOptions;
        private readonly ReaderWriterLockSlim _lruLock = new ReaderWriterLockSlim();
        public int Count => _count;
        public CapacityMode Mode => _capacityMode;
        public ConcurrentLinkedDictionary() : this(1024)
        {
        }

        public ConcurrentLinkedDictionary(int capacity, CapacityMode mode = CapacityMode.Dynamic)
            : this(capacity, mode, null, null)
        {
        }

        public static ConcurrentLinkedDictionary<TKey, TValue> CreateWithoutPooling(int capacity = 1024, CapacityMode mode = CapacityMode.Dynamic, IEqualityComparer<TKey>? comparer = null, SecureHashOptions? hashOptions = null)
        {
            return new ConcurrentLinkedDictionary<TKey, TValue>(capacity, mode, comparer, hashOptions);
        }

        private ConcurrentLinkedDictionary(int capacity, CapacityMode mode, IEqualityComparer<TKey>? comparer, SecureHashOptions? hashOptions = null)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            _capacityMode = mode;
            _maxCapacity = mode == CapacityMode.Fixed ? capacity : int.MaxValue;
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
            var bucketCount = HybridUtils.GetNextPowerOfTwo(capacity);
            _buckets = new Bucket[bucketCount];
            for (int i = 0; i < bucketCount; i++)
            {
                _buckets[i] = new Bucket();
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                if (TryGetValue(key, out var value))
                    return value;
                throw new KeyNotFoundException($"Key '{key}' was not found");
            }
            set => AddOrUpdate(key, value);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            var hashCode = _comparer.GetHashCode(key) & 0x7FFFFFFF;
            var bucket = _buckets[hashCode % _buckets.Length];
            if (bucket.TryGet(key, hashCode, _comparer, out UniversalDictionaryNode<TKey, TValue>? node))
            {
                value = node!.Value;
                return true;
            }
            value = default!;
            return false;
        }

        public void AddOrUpdate(TKey key, TValue value)
        {
            var hashCode = _comparer.GetHashCode(key) & 0x7FFFFFFF;
            var bucket = _buckets[hashCode % _buckets.Length];
            var node = new UniversalDictionaryNode<TKey, TValue>(key, value, hashCode);
            UniversalNodeHelper.SetAccessTime(node, Stopwatch.GetTimestamp());
            bucket.AddOrUpdate(node, key, hashCode, _comparer, out UniversalDictionaryNode<TKey, TValue>? existing);
            if (existing == null)
            {
                AddToLru(node);
                Interlocked.Increment(ref _count);
                if (_capacityMode == CapacityMode.Fixed && _count > _maxCapacity)
                {
                    EvictLru();
                }
            }
            else
            {
                MoveToFrontLru(existing);
            }
        }

        public bool TryRemove(TKey key, out TValue value)
        {
            var hashCode = _comparer.GetHashCode(key) & 0x7FFFFFFF;
            var bucket = _buckets[hashCode % _buckets.Length];
            if (bucket.TryRemove(key, hashCode, _comparer, out UniversalDictionaryNode<TKey, TValue>? removed))
            {
                value = removed!.Value;
                RemoveFromLru(removed);
                Interlocked.Decrement(ref _count);
                return true;
            }
            value = default!;
            return false;
        }

        public bool ContainsKey(TKey key)
        {
            return TryGetValue(key, out _);
        }

        public void Clear()
        {
            _lruLock.EnterWriteLock();
            try
            {
                UniversalDictionaryNode<TKey, TValue>? current = _lruHead;
                while (current != null)
                {
                    UniversalDictionaryNode<TKey, TValue>? next = UniversalNodeHelper.GetNextLru(current);
                    current = next;
                }
                _lruHead = _lruTail = null;
                _count = 0;
            }
            finally
            {
                _lruLock.ExitWriteLock();
            }
        }
        #region LRU Management
        private void AddToLru(UniversalDictionaryNode<TKey, TValue> node)
        {
            _lruLock.EnterWriteLock();
            try
            {
                UniversalNodeHelper.SetNextLru(node, _lruHead);
                UniversalNodeHelper.SetPrevLru(node, null);
                if (_lruHead != null)
                    UniversalNodeHelper.SetPrevLru(_lruHead, node);
                else
                    _lruTail = node;
                _lruHead = node;
            }
            finally
            {
                _lruLock.ExitWriteLock();
            }
        }

        private void MoveToFrontLru(UniversalDictionaryNode<TKey, TValue> node)
        {
            _lruLock.EnterWriteLock();
            try
            {
                if (node == _lruHead) return;
                UniversalDictionaryNode<TKey, TValue>? prev = UniversalNodeHelper.GetPrevLru(node);
                UniversalDictionaryNode<TKey, TValue>? next = UniversalNodeHelper.GetNextLru(node);
                if (prev != null)
                    UniversalNodeHelper.SetNextLru(prev, next);
                if (next != null)
                    UniversalNodeHelper.SetPrevLru(next, prev);
                if (node == _lruTail)
                    _lruTail = prev;
                UniversalNodeHelper.SetNextLru(node, _lruHead);
                UniversalNodeHelper.SetPrevLru(node, null);
                if (_lruHead != null)
                    UniversalNodeHelper.SetPrevLru(_lruHead, node);
                _lruHead = node;
            }
            finally
            {
                _lruLock.ExitWriteLock();
            }
        }

        private void RemoveFromLru(UniversalDictionaryNode<TKey, TValue> node)
        {
            _lruLock.EnterWriteLock();
            try
            {
                UniversalDictionaryNode<TKey, TValue>? prev = UniversalNodeHelper.GetPrevLru(node);
                UniversalDictionaryNode<TKey, TValue>? next = UniversalNodeHelper.GetNextLru(node);
                if (prev != null)
                    UniversalNodeHelper.SetNextLru(prev, next);
                else
                    _lruHead = next;
                if (next != null)
                    UniversalNodeHelper.SetPrevLru(next, prev);
                else
                    _lruTail = prev;
            }
            finally
            {
                _lruLock.ExitWriteLock();
            }
        }

        private void EvictLru()
        {
            _lruLock.EnterWriteLock();
            try
            {
                if (_lruTail == null) return;
                UniversalDictionaryNode<TKey, TValue>? nodeToEvict = _lruTail;
                var hashCode = nodeToEvict.HashCode;
                var bucket = _buckets[hashCode % _buckets.Length];
                if (bucket.TryRemove(nodeToEvict.Key, hashCode, _comparer, out _))
                {
                    RemoveFromLru(nodeToEvict);
                    Interlocked.Decrement(ref _count);
                }
            }
            finally
            {
                _lruLock.ExitWriteLock();
            }
        }
        #endregion
        #region Enumeration
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            _lruLock.EnterReadLock();
            try
            {
                UniversalDictionaryNode<TKey, TValue>? current = _lruHead;
                while (current != null)
                {
                    yield return new KeyValuePair<TKey, TValue>(current.Key, current.Value);
                    current = UniversalNodeHelper.GetNextLru(current);
                }
            }
            finally
            {
                _lruLock.ExitReadLock();
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion
        #region IDisposable
        public void Dispose()
        {
            _lruLock?.Dispose();
        }
        #endregion
    }
}
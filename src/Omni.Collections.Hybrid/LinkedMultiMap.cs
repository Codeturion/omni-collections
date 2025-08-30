using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Omni.Collections.Hybrid.LinkedDictionary;
using Omni.Collections.Core.Security;

namespace Omni.Collections.Hybrid
{
    /// <summary>
    /// A multi-value dictionary that elegantly handles one-to-many relationships with preserved insertion order.
    /// Achieves O(1) Add operations and O(m) value removal where m is values per key, through optimized linked node management.
    /// Perfect for dependency graphs, event subscription systems, and tag-based indexing where multiple ordered
    /// values per key are fundamental to the data model.
    /// </summary>
    public class LinkedMultiMap<TKey, TValue> : IEnumerable<KeyValuePair<TKey, IReadOnlyList<TValue>>>, IDisposable
        where TKey : notnull
    {
        sealed private class ValueNode
        {
            public readonly TValue Value;

            public ValueNode? Next;
            public ValueNode(TValue value)
            {
                Value = value;
            }
        }

        sealed private class KeyNode
        {
            public readonly TKey Key;
            public readonly int HashCode;
            public KeyNode? NextInBucket;
            public KeyNode? PreviousLru;
            public KeyNode? NextLru;
            public ValueNode? FirstValue;
            public ValueNode? LastValue;
            public int ValueCount;
            public KeyNode(TKey key, int hashCode)
            {
                Key = key;
                HashCode = hashCode;
            }
        }

        sealed private class NodeValueView : IReadOnlyList<TValue>
        {
            private KeyNode? _node;
            public void SetNode(KeyNode node)
            {
                _node = node;
            }

            public int Count => _node?.ValueCount ?? 0;
            public TValue this[int index]
            {
                get
                {
                    if (_node == null || index < 0 || index >= _node.ValueCount)
                        throw new ArgumentOutOfRangeException(nameof(index));
                    var current = _node.FirstValue;
                    for (int i = 0; i < index; i++)
                    {
                        current = current!.Next;
                    }
                    return current!.Value;
                }
            }

            public IEnumerator<TValue> GetEnumerator()
            {
                var current = _node?.FirstValue;
                while (current != null)
                {
                    yield return current.Value;
                    current = current.Next;
                }
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private readonly KeyNode?[] _buckets;
        private KeyNode? _lruHead;
        private KeyNode? _lruTail;
        private int _keyCount;
        private int _totalValueCount;
        private readonly int _maxCapacity;
        private readonly CapacityMode _capacityMode;
        private readonly bool _allowDuplicateValues;
        private readonly IEqualityComparer<TKey> _keyComparer;
        private readonly IEqualityComparer<TValue> _valueComparer;
        private readonly SecureHashOptions _hashOptions;
        private readonly bool _enableLruOptimization;
        private readonly NodeValueView _cachedView;
        public int KeyCount => _keyCount;
        public int TotalValueCount => _totalValueCount;
        public IEnumerable<TKey> Keys
        {
            get
            {
                var current = _lruHead;
                while (current != null)
                {
                    yield return current.Key;
                    current = current.NextLru;
                }
            }
        }

        public LinkedMultiMap() : this(16, CapacityMode.Dynamic, true, null, null, false)
        {
        }

        public LinkedMultiMap(int capacity, CapacityMode mode = CapacityMode.Dynamic, bool allowDuplicateValues = true)
            : this(capacity, mode, allowDuplicateValues, null, null, false)
        {
        }

        public LinkedMultiMap(
            int capacity,
            CapacityMode mode,
            bool allowDuplicateValues,
            IEqualityComparer<TKey>? keyComparer,
            IEqualityComparer<TValue>? valueComparer,
            bool enableLruOptimization = false,
            SecureHashOptions? hashOptions = null)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            _capacityMode = mode;
            _maxCapacity = mode == CapacityMode.Fixed ? capacity : int.MaxValue;
            _allowDuplicateValues = allowDuplicateValues;
            _hashOptions = hashOptions ?? SecureHashOptions.Default;
            
            // Use secure comparer if randomized hashing is enabled and no custom comparer provided
            if (_hashOptions.EnableRandomizedHashing && keyComparer == null)
            {
                _keyComparer = SecureHashHelper.CreateSecureComparer<TKey>();
            }
            else
            {
                _keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;
            }
            _valueComparer = valueComparer ?? EqualityComparer<TValue>.Default;
            _enableLruOptimization = enableLruOptimization;
            _cachedView = new NodeValueView();
            var bucketCount = HybridUtils.GetPrime(capacity);
            _buckets = new KeyNode?[bucketCount];
        }

        public void Add(TKey key, TValue value)
        {
            var hashCode = _keyComparer.GetHashCode(key) & 0x7FFFFFFF;
            var bucketIndex = hashCode % _buckets.Length;
            var node = FindKeyNode(key, hashCode, bucketIndex);
            if (node != null)
            {
                if (!_allowDuplicateValues && ContainsValue(node, value))
                    return;
                AddValueToNode(node, value);
                MoveToFront(node);
            }
            else
            {
                if (_capacityMode == CapacityMode.Fixed && _keyCount >= _maxCapacity)
                {
                    EvictLru();
                }
                node = new KeyNode(key, hashCode);
                AddValueToNode(node, value);
                node.NextInBucket = _buckets[bucketIndex];
                _buckets[bucketIndex] = node;
                AddToFront(node);
                _keyCount++;
            }
        }

        public IReadOnlyList<TValue> this[TKey key]
        {
            get
            {
                if (TryGetValues(key, out IReadOnlyList<TValue>? values))
                    return values;
                return Array.Empty<TValue>();
            }
        }

        public bool TryGetValues(TKey key, out IReadOnlyList<TValue> values)
        {
            var hashCode = _keyComparer.GetHashCode(key) & 0x7FFFFFFF;
            var bucketIndex = hashCode % _buckets.Length;
            var node = FindKeyNode(key, hashCode, bucketIndex);
            if (node != null)
            {
                MoveToFront(node);
                values = GetNodeValues(node);
                return true;
            }
            values = Array.Empty<TValue>();
            return false;
        }

        public bool RemoveKey(TKey key)
        {
            var hashCode = _keyComparer.GetHashCode(key) & 0x7FFFFFFF;
            var bucketIndex = hashCode % _buckets.Length;
            KeyNode? current = _buckets[bucketIndex];
            KeyNode? previous = null;
            while (current != null)
            {
                if (current.HashCode == hashCode && _keyComparer.Equals(current.Key, key))
                {
                    if (previous == null)
                        _buckets[bucketIndex] = current.NextInBucket;
                    else
                        previous.NextInBucket = current.NextInBucket;
                    RemoveFromLru(current);
                    _keyCount--;
                    _totalValueCount -= current.ValueCount;
                    return true;
                }
                previous = current;
                current = current.NextInBucket;
            }
            return false;
        }

        public bool Remove(TKey key, TValue value)
        {
            var hashCode = _keyComparer.GetHashCode(key) & 0x7FFFFFFF;
            var bucketIndex = hashCode % _buckets.Length;
            var node = FindKeyNode(key, hashCode, bucketIndex);
            if (node == null)
            {
                return false;
            }
            ValueNode? current = node.FirstValue;
            ValueNode? previous = null;
            while (current != null)
            {
                if (_valueComparer.Equals(current.Value, value))
                {
                    if (previous == null)
                        node.FirstValue = current.Next;
                    else
                        previous.Next = current.Next;
                    if (current == node.LastValue)
                        node.LastValue = previous;
                    node.ValueCount--;
                    _totalValueCount--;
                    if (node.ValueCount == 0)
                    {
                        RemoveKey(key);
                    }
                    return true;
                }
                previous = current;
                current = current.Next;
            }
            return false;
        }

        public bool ContainsKey(TKey key)
        {
            var hashCode = _keyComparer.GetHashCode(key) & 0x7FFFFFFF;
            var bucketIndex = hashCode % _buckets.Length;
            return FindKeyNode(key, hashCode, bucketIndex) != null;
        }

        public bool Contains(TKey key, TValue value)
        {
            var hashCode = _keyComparer.GetHashCode(key) & 0x7FFFFFFF;
            var bucketIndex = hashCode % _buckets.Length;
            var node = FindKeyNode(key, hashCode, bucketIndex);
            return node != null && ContainsValue(node, value);
        }

        public void Clear()
        {
            Array.Clear(_buckets, 0, _buckets.Length);
            _lruHead = _lruTail = null;
            _keyCount = 0;
            _totalValueCount = 0;
        }

        public int GetValueCount(TKey key)
        {
            var hashCode = _keyComparer.GetHashCode(key) & 0x7FFFFFFF;
            var bucketIndex = hashCode % _buckets.Length;
            var node = FindKeyNode(key, hashCode, bucketIndex);
            return node?.ValueCount ?? 0;
        }
        #region Private Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private KeyNode? FindKeyNode(TKey key, int hashCode, int bucketIndex)
        {
            var current = _buckets[bucketIndex];
            while (current != null)
            {
                if (current.HashCode == hashCode && _keyComparer.Equals(current.Key, key))
                    return current;
                current = current.NextInBucket;
            }
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ContainsValue(KeyNode node, TValue value)
        {
            var current = node.FirstValue;
            while (current != null)
            {
                if (_valueComparer.Equals(current.Value, value))
                    return true;
                current = current.Next;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddValueToNode(KeyNode node, TValue value)
        {
            var valueNode = new ValueNode(value);
            if (node.LastValue == null)
            {
                node.FirstValue = node.LastValue = valueNode;
            }
            else
            {
                node.LastValue.Next = valueNode;
                node.LastValue = valueNode;
            }
            node.ValueCount++;
            _totalValueCount++;
        }

        private IReadOnlyList<TValue> GetNodeValues(KeyNode node)
        {
            if (node.ValueCount == 0)
                return Array.Empty<TValue>();
            var result = new TValue[node.ValueCount];
            var current = node.FirstValue;
            int index = 0;
            while (current != null && index < node.ValueCount)
            {
                result[index++] = current.Value;
                current = current.Next;
            }
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IReadOnlyList<TValue> GetCachedNodeValues(KeyNode node)
        {
            _cachedView.SetNode(node);
            return _cachedView;
        }
        #endregion
        #region LRU Management
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MoveToFront(KeyNode node)
        {
            if (node == _lruHead)
                return;
            if (_enableLruOptimization)
            {
                if (node.PreviousLru?.PreviousLru?.PreviousLru != null)
                {
                    if (node.PreviousLru == _lruHead && _lruHead != null)
                    {
                        SwapWithHead(node);
                    }
                    else
                    {
                        RemoveFromLru(node);
                        AddToFront(node);
                    }
                }
            }
            else
            {
                if (node.PreviousLru == _lruHead && _lruHead != null)
                {
                    SwapWithHead(node);
                }
                else
                {
                    RemoveFromLru(node);
                    AddToFront(node);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SwapWithHead(KeyNode node)
        {
            var oldHead = _lruHead!;
            if (node.NextLru != null)
                node.NextLru.PreviousLru = oldHead;
            else
                _lruTail = oldHead;
            oldHead.PreviousLru = node.PreviousLru;
            oldHead.NextLru = node.NextLru;
            node.PreviousLru = null;
            node.NextLru = oldHead;
            _lruHead = node;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddToFront(KeyNode node)
        {
            node.NextLru = _lruHead;
            node.PreviousLru = null;
            if (_lruHead != null)
                _lruHead.PreviousLru = node;
            _lruHead = node;
            if (_lruTail == null)
                _lruTail = node;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RemoveFromLru(KeyNode node)
        {
            if (node.PreviousLru != null)
                node.PreviousLru.NextLru = node.NextLru;
            else
                _lruHead = node.NextLru;
            if (node.NextLru != null)
                node.NextLru.PreviousLru = node.PreviousLru;
            else
                _lruTail = node.PreviousLru;
        }

        private void EvictLru()
        {
            if (_lruTail != null)
            {
                RemoveKey(_lruTail.Key);
            }
        }
        #endregion
        #region IEnumerable Implementation
        public IEnumerator<KeyValuePair<TKey, IReadOnlyList<TValue>>> GetEnumerator()
        {
            var current = _lruHead;
            while (current != null)
            {
                _cachedView.SetNode(current);
                yield return new KeyValuePair<TKey, IReadOnlyList<TValue>>(current.Key, _cachedView);
                current = current.NextLru;
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion
        public void Dispose()
        {
            Clear();
        }
    }
}
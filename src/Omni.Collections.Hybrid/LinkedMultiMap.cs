using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Omni.Collections.Hybrid.LinkedDictionary;
using Omni.Collections.Core.Security;

namespace Omni.Collections.Hybrid
{
    /// <summary>
    /// A multi-value dictionary where each key maps to an ordered list of values, with insertion order
    /// preserved per key. Add is O(1) average; <see cref="this[TKey]"/> and <see cref="TryGetValues"/>
    /// return a <see cref="NodeValueView"/> in O(1) (the view aliases the per-key value list — mutations
    /// to the multimap after the view is obtained invalidate it; call the standard LINQ ToArray() for
    /// a snapshot). Enumeration of the view is O(values per key); indexed access via <c>view[i]</c> is
    /// O(i) since the per-key value list is singly linked — prefer <c>foreach</c> for sequential reads.
    /// Read paths mutate LRU order: the accessed key moves to the front of the recency list. Suited to
    /// dependency graphs, event subscription systems, and tag-based indexing where multiple ordered
    /// values per key are fundamental to the data model.
    /// </summary>
    public class LinkedMultiMap<TKey, TValue> : IEnumerable<KeyValuePair<TKey, LinkedMultiMap<TKey, TValue>.NodeValueView>>, IDisposable
        where TKey : notnull
    {
        sealed internal class ValueNode
        {
            public readonly TValue Value;

            public ValueNode? Next;
            public ValueNode(TValue value)
            {
                Value = value;
            }
        }

        sealed internal class KeyNode
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

        /// <summary>
        /// A live view over a key's value list. Construction is O(1); enumeration
        /// is O(values per key); indexed access (<c>view[i]</c>) is O(i) — prefer
        /// <c>foreach</c> for sequential reads. The view aliases the multimap's
        /// internal storage: mutations to the multimap after the view is obtained
        /// invalidate the view's contents (the next read sees post-mutation state).
        /// Call the view's standard LINQ <c>ToArray()</c> for a snapshot.
        /// </summary>
        public sealed class NodeValueView : IReadOnlyList<TValue>
        {
            private readonly KeyNode? _node;
            internal NodeValueView(KeyNode? node)
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
        private static readonly NodeValueView _emptyView = new NodeValueView(null);
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

        /// <summary>
        /// Returns a live view over the values for <paramref name="key"/>. O(1) per call —
        /// the returned <see cref="NodeValueView"/> holds a reference to the key's value
        /// list and walks it lazily. Indexed access is O(i); enumeration is O(values).
        /// The view aliases multimap state — subsequent mutations invalidate it. *Mutates
        /// LRU order*: the accessed key moves to the front of the recency list.
        /// </summary>
        public NodeValueView this[TKey key]
        {
            get
            {
                if (TryGetValues(key, out NodeValueView values))
                    return values;
                return _emptyView;
            }
        }

        /// <summary>
        /// Tries to retrieve the live view of values for <paramref name="key"/>. Same
        /// O(1) view semantics as the indexer; same LRU mutation. Returns <c>false</c>
        /// (with an empty view) if the key is absent.
        /// </summary>
        public bool TryGetValues(TKey key, out NodeValueView values)
        {
            var hashCode = _keyComparer.GetHashCode(key) & 0x7FFFFFFF;
            var bucketIndex = hashCode % _buckets.Length;
            var node = FindKeyNode(key, hashCode, bucketIndex);
            if (node != null)
            {
                MoveToFront(node);
                values = WrapNodeValues(node);
                return true;
            }
            values = _emptyView;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private NodeValueView WrapNodeValues(KeyNode node)
        {
            // O(1): allocate the lightweight view object that holds a reference
            // to the node and walks the value list lazily on demand. Previously
            // GetNodeValues allocated a TValue[node.ValueCount] and copied each
            // value into it — O(values per key) heap. The view aliases the
            // multimap's internal storage; mutations after the view is returned
            // invalidate it. Snapshot via standard LINQ ToArray() if needed.
            return new NodeValueView(node);
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
        public IEnumerator<KeyValuePair<TKey, NodeValueView>> GetEnumerator()
        {
            var current = _lruHead;
            while (current != null)
            {
                yield return new KeyValuePair<TKey, NodeValueView>(current.Key, new NodeValueView(current));
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
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Omni.Collections.Hybrid.PredictiveDictionary
{
    /// <summary>
    /// A dictionary that learns n-gram access patterns synchronously on every access and exposes the
    /// learned model via <see cref="GetPredictions"/> + <see cref="PrefetchLikely"/>. Use this when you
    /// want a keyed cache that surfaces "given the last N accessed keys, here are the likely next ones"
    /// — and you (the caller) decide whether to act on those predictions by calling
    /// <see cref="PrefetchLikely"/> with a value factory.
    /// </summary>
    /// <remarks>
    /// The dictionary does NOT prefetch transparently. Learning is synchronous on each access (no
    /// background timers, no async work). Cost is ~1.2× <see cref="System.Collections.Generic.Dictionary{TKey, TValue}"/>
    /// on Add and ~6.8× on Lookup at large N — the prediction model is the value, not raw lookup speed.
    /// Use a plain <see cref="System.Collections.Generic.Dictionary{TKey, TValue}"/> if you don't actually
    /// query <see cref="GetPredictions"/>.
    /// </remarks>
    public class PredictiveDictionary<TKey, TValue> : IDisposable where TKey : notnull
    {
        public readonly struct PredictionResult
        {
            public readonly TKey Key;
            public readonly double Confidence;
            public readonly string Reason;
            public PredictionResult(TKey key, double confidence, string reason)
            {
                Key = key;
                Confidence = confidence;
                Reason = reason;
            }
        }

        private readonly Dictionary<TKey, TValue> _dictionary;
        private readonly Dictionary<TKey, TValue> _predictiveCache;
        private readonly IEqualityComparer<TKey> _comparer;
        // pattern hash → (next key → seen count). 64-bit hash key avoids string-allocation per access.
        private readonly Dictionary<ulong, Dictionary<TKey, long>> _patternCounts;
        // Bounded — caps memory + keeps GetPredictions O(maxFrequencyKeys), not O(unique keys ever seen).
        private readonly Dictionary<TKey, long> _keyFrequency;
        private readonly TKey[] _recentAccesses;
        private readonly TKey[] _contextScratch;
        private int _recentHead;
        private int _recentCount;
        private long _totalAccesses;
        private readonly int _patternLength;
        private readonly int _maxPatterns;
        private readonly int _maxCacheSize;
        private readonly int _maxFrequencyKeys;
        private readonly double _confidenceThreshold;
        private bool _disposed;

        public int Count => _dictionary.Count;
        public int PredictiveCacheCount => _predictiveCache.Count;
        public int PatternCount => _patternCounts.Count;

        public PredictiveDictionary() : this(3, 1000, 100, 0.7) { }

        public PredictiveDictionary(int patternLength, int maxPatterns, int maxCacheSize, double confidenceThreshold)
        {
            if (patternLength < 2 || patternLength > 10)
                throw new ArgumentOutOfRangeException(nameof(patternLength), "Pattern length must be 2-10");
            if (maxPatterns < 10)
                throw new ArgumentOutOfRangeException(nameof(maxPatterns), "Must allow at least 10 patterns");
            if (confidenceThreshold < 0.0 || confidenceThreshold > 1.0)
                throw new ArgumentOutOfRangeException(nameof(confidenceThreshold), "Confidence threshold must be 0.0-1.0");

            _patternLength = patternLength;
            _maxPatterns = maxPatterns;
            _maxCacheSize = maxCacheSize;
            // Bound the frequency table relative to the pattern table so GetPredictions stays bounded.
            _maxFrequencyKeys = Math.Max(maxPatterns, 256);
            _confidenceThreshold = confidenceThreshold;
            _comparer = EqualityComparer<TKey>.Default;
            _dictionary = new Dictionary<TKey, TValue>(_comparer);
            _predictiveCache = new Dictionary<TKey, TValue>(_comparer);
            _patternCounts = new Dictionary<ulong, Dictionary<TKey, long>>();
            _keyFrequency = new Dictionary<TKey, long>(_comparer);
            _recentAccesses = new TKey[patternLength + 1];
            _contextScratch = new TKey[patternLength];
        }

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

        public void AddOrUpdate(TKey key, TValue value)
        {
            _dictionary[key] = value;
            if (_predictiveCache.ContainsKey(key))
                _predictiveCache[key] = value;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            RecordAccess(key);
            if (_dictionary.TryGetValue(key, out value!))
                return true;
            if (_predictiveCache.TryGetValue(key, out value!))
            {
                _dictionary[key] = value;
                _predictiveCache.Remove(key);
                return true;
            }
            value = default!;
            return false;
        }

        public IEnumerable<PredictionResult> GetPredictions(TKey[] contextKeys)
        {
            if (contextKeys.Length == 0)
                return [];

            var predictions = new List<PredictionResult>();
            var patternKey = CreatePatternKey(contextKeys, _comparer);
            if (_patternCounts.TryGetValue(patternKey, out var nextKeyCounts))
            {
                long total = 0;
                foreach (var c in nextKeyCounts.Values) total += c;
                if (total > 0)
                {
                    foreach (var kvp in nextKeyCounts)
                    {
                        var confidence = (double)kvp.Value / total;
                        if (confidence >= _confidenceThreshold)
                        {
                            predictions.Add(new PredictionResult(
                                kvp.Key,
                                confidence,
                                $"Pattern-based ({kvp.Value} occurrences)"));
                        }
                    }
                }
            }

            // Frequency fallback: bounded to _maxFrequencyKeys, so iteration stays O(maxFrequencyKeys).
            if (_totalAccesses > 0)
            {
                var seen = new HashSet<TKey>(_comparer);
                foreach (var p in predictions) seen.Add(p.Key);
                foreach (var kvp in _keyFrequency)
                {
                    if (seen.Contains(kvp.Key)) continue;
                    var confidence = Math.Min((double)kvp.Value / _totalAccesses, 0.8);
                    predictions.Add(new PredictionResult(kvp.Key, confidence, "Frequency-based"));
                }
            }

            predictions.Sort((a, b) => b.Confidence.CompareTo(a.Confidence));
            if (predictions.Count > 5) predictions.RemoveRange(5, predictions.Count - 5);
            return predictions;
        }

        public int PrefetchLikely(TKey[] contextKeys, Func<TKey, TValue> valueFactory)
        {
            if (_disposed) return 0;

            int preloaded = 0;
            foreach (var prediction in GetPredictions(contextKeys))
            {
                if (_predictiveCache.Count >= _maxCacheSize) break;
                if (prediction.Confidence < _confidenceThreshold) break;
                if (_dictionary.ContainsKey(prediction.Key) || _predictiveCache.ContainsKey(prediction.Key))
                    continue;
                try
                {
                    _predictiveCache[prediction.Key] = valueFactory(prediction.Key);
                    preloaded++;
                }
                catch
                {
                }
            }
            return preloaded;
        }

        public bool Remove(TKey key)
        {
            bool removed = _dictionary.Remove(key);
            _predictiveCache.Remove(key);
            _keyFrequency.Remove(key);
            return removed;
        }

        public void Clear()
        {
            _dictionary.Clear();
            _predictiveCache.Clear();
            _patternCounts.Clear();
            _keyFrequency.Clear();
            _recentHead = 0;
            _recentCount = 0;
            _totalAccesses = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RecordAccess(TKey key)
        {
            _totalAccesses++;
            _keyFrequency.TryGetValue(key, out var freq);
            _keyFrequency[key] = freq + 1;
            // Bound the frequency dict so it can't grow with the universe of unique keys ever seen.
            // When full and the new key is fresh, evict the lowest-frequency entry.
            if (freq == 0 && _keyFrequency.Count > _maxFrequencyKeys)
                EvictLowestFrequencyKey();

            // Append to ring; once it's full we have a (patternLength → next) tuple to learn.
            if (_recentCount < _recentAccesses.Length)
            {
                _recentAccesses[_recentCount++] = key;
                if (_recentCount == _recentAccesses.Length)
                    LearnFromRing();
            }
            else
            {
                _recentAccesses[_recentHead] = key;
                _recentHead = (_recentHead + 1) % _recentAccesses.Length;
                LearnFromRing();
            }
        }

        private void EvictLowestFrequencyKey()
        {
            // Single-pass scan for the min-frequency entry. O(_maxFrequencyKeys).
            TKey victim = default!;
            long min = long.MaxValue;
            bool found = false;
            foreach (var kvp in _keyFrequency)
            {
                if (kvp.Value < min)
                {
                    min = kvp.Value;
                    victim = kvp.Key;
                    found = true;
                }
            }
            if (found) _keyFrequency.Remove(victim);
        }

        private void LearnFromRing()
        {
            // Ring layout: starting at _recentHead, the next patternLength items are the context, the
            // last item is the predicted next key. Reuse _contextScratch — no per-access allocation.
            for (int i = 0; i < _patternLength; i++)
                _contextScratch[i] = _recentAccesses[(_recentHead + i) % _recentAccesses.Length];
            var nextKey = _recentAccesses[(_recentHead + _patternLength) % _recentAccesses.Length];

            var patternKey = CreatePatternKey(_contextScratch, _comparer);
            if (!_patternCounts.TryGetValue(patternKey, out var nextKeyCounts))
            {
                if (_patternCounts.Count >= _maxPatterns)
                {
                    // Evict oldest pattern entry — Dictionary insertion order keeps Keys stable.
                    ulong oldest = 0;
                    foreach (var k in _patternCounts.Keys) { oldest = k; break; }
                    _patternCounts.Remove(oldest);
                }
                nextKeyCounts = new Dictionary<TKey, long>(_comparer);
                _patternCounts[patternKey] = nextKeyCounts;
            }
            nextKeyCounts.TryGetValue(nextKey, out var c);
            nextKeyCounts[nextKey] = c + 1;
        }

        // FNV-1a 64 over the per-key hashcodes — zero-alloc, stable across runs in-process.
        // Collisions at 64 bits are negligible at the operating scale (<< 2^32 patterns).
        private static ulong CreatePatternKey(TKey[] sequence, IEqualityComparer<TKey> comparer)
        {
            const ulong FnvOffset = 14695981039346656037UL;
            const ulong FnvPrime = 1099511628211UL;
            ulong hash = FnvOffset;
            for (int i = 0; i < sequence.Length; i++)
            {
                var k = sequence[i];
                int kh = k is null ? 0 : comparer.GetHashCode(k);
                hash ^= unchecked((uint)kh);
                hash *= FnvPrime;
            }
            return hash;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Clear();
        }
    }
}

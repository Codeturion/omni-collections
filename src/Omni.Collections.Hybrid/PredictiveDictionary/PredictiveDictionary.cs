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
        // pattern key → (next key → seen count). Counts make confidence cheap to compute on demand.
        private readonly Dictionary<string, Dictionary<TKey, int>> _patternCounts;
        private readonly Dictionary<TKey, int> _keyFrequency;
        private readonly TKey[] _recentAccesses;
        private int _recentHead;
        private int _recentCount;
        private int _totalAccesses;
        private readonly int _patternLength;
        private readonly int _maxPatterns;
        private readonly int _maxCacheSize;
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
            _confidenceThreshold = confidenceThreshold;
            _comparer = EqualityComparer<TKey>.Default;
            _dictionary = new Dictionary<TKey, TValue>(_comparer);
            _predictiveCache = new Dictionary<TKey, TValue>(_comparer);
            _patternCounts = new Dictionary<string, Dictionary<TKey, int>>();
            _keyFrequency = new Dictionary<TKey, int>(_comparer);
            _recentAccesses = new TKey[patternLength + 1];
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
            var patternKey = CreatePatternKey(contextKeys);
            if (_patternCounts.TryGetValue(patternKey, out var nextKeyCounts))
            {
                int total = 0;
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

            // Fall back to global frequency for keys not already covered by pattern matches.
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

        private void LearnFromRing()
        {
            // Ring layout: starting at _recentHead, the next patternLength items are the context, the
            // last item is the predicted next key.
            var contextSpan = new TKey[_patternLength];
            for (int i = 0; i < _patternLength; i++)
                contextSpan[i] = _recentAccesses[(_recentHead + i) % _recentAccesses.Length];
            var nextKey = _recentAccesses[(_recentHead + _patternLength) % _recentAccesses.Length];

            var patternKey = CreatePatternKey(contextSpan);
            if (!_patternCounts.TryGetValue(patternKey, out var nextKeyCounts))
            {
                if (_patternCounts.Count >= _maxPatterns)
                {
                    // Evict oldest pattern entry — Dictionary insertion order keeps Keys stable.
                    var oldest = default(string)!;
                    foreach (var k in _patternCounts.Keys) { oldest = k; break; }
                    _patternCounts.Remove(oldest);
                }
                nextKeyCounts = new Dictionary<TKey, int>(_comparer);
                _patternCounts[patternKey] = nextKeyCounts;
            }
            nextKeyCounts.TryGetValue(nextKey, out var c);
            nextKeyCounts[nextKey] = c + 1;
        }

        private static string CreatePatternKey(TKey[] sequence)
        {
            return string.Join("|", Array.ConvertAll(sequence, k => k?.ToString() ?? "null"));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Clear();
        }
    }
}

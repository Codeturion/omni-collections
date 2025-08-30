using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Omni.Collections.Core.Security;

namespace Omni.Collections.Hybrid.PredictiveDictionary
{
    /// <summary>
    /// A dictionary with ML-powered predictive prefetching that learns and anticipates access patterns in real-time.
    /// Achieves O(1) lookups with intelligent prefetching reducing cache misses through pattern recognition algorithms.
    /// Revolutionary for database query optimization, recommendation systems, and read-heavy workloads where
    /// predictable access patterns can be exploited for dramatic performance gains.
    /// </summary>
    public class PredictiveDictionary<TKey, TValue> : IDisposable where TKey : notnull
    {
        #region Types
        private readonly struct AccessPattern
        {
            public readonly TKey[] Sequence;

            public readonly TKey NextKey;
            public readonly DateTime Timestamp;
            public readonly double Confidence;
            public AccessPattern(TKey[] sequence, TKey nextKey, double confidence = 1.0)
            {
                Sequence = sequence;
                NextKey = nextKey;
                Timestamp = DateTime.UtcNow;
                Confidence = confidence;
            }
        }

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
        #endregion Types
        private readonly Dictionary<TKey, TValue> _dictionary;
        private readonly IEqualityComparer<TKey> _comparer;
        private readonly SecureHashOptions _hashOptions;
        private readonly ConcurrentQueue<TKey> _accessHistory;
        private readonly Dictionary<string, List<AccessPattern>> _patterns;
        private readonly Dictionary<TKey, double> _keyFrequency;
        private readonly Dictionary<TKey, DateTime> _lastAccess;
        private readonly Dictionary<TKey, TValue> _predictiveCache;
        private readonly Dictionary<TKey, PredictionResult> _activePredictions;
        private readonly ConcurrentQueue<TKey> _prefetchQueue;
        private readonly int _patternLength;
        private readonly int _maxPatterns;
        private readonly int _maxCacheSize;
        private readonly double _confidenceThreshold;
        private readonly TimeSpan _patternTimeout;
        private int _totalPredictions;
        private int _successfulPredictions;
        private int _totalAccesses;
        private readonly object _statsLock = new object();
        private readonly Timer _learningTimer;
        private readonly Timer _cleanupTimer;
        private volatile bool _isDisposed;
        public int Count => _dictionary.Count;
        public int PredictiveCacheCount => _predictiveCache.Count;
        public int PatternCount => _patterns.Count;
        public PredictionStats Statistics
        {
            get
            {
                lock (_statsLock)
                {
                    var avgConfidence = _activePredictions.Count > 0
                        ? _activePredictions.Values.Average(p => p.Confidence)
                        : 0.0;
                    var memoryUsage = EstimateMemoryUsage();
                    return new PredictionStats(
                        _totalPredictions,
                        _successfulPredictions,
                        _patterns.Count,
                        avgConfidence,
                        memoryUsage);
                }
            }
        }

        public PredictiveDictionary() : this(3, 1000, 100, 0.7, TimeSpan.FromMinutes(10))
        {
        }

        public PredictiveDictionary(int patternLength, int maxPatterns, int maxCacheSize,
            double confidenceThreshold, TimeSpan patternTimeout, SecureHashOptions? hashOptions = null)
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
            _patternTimeout = patternTimeout;
            _hashOptions = hashOptions ?? SecureHashOptions.Default;
            
            // Use secure comparer if randomized hashing is enabled
            if (_hashOptions.EnableRandomizedHashing)
            {
                _comparer = SecureHashHelper.CreateSecureComparer<TKey>();
            }
            else
            {
                _comparer = EqualityComparer<TKey>.Default;
            }
            _dictionary = new Dictionary<TKey, TValue>(_comparer);
            _accessHistory = new ConcurrentQueue<TKey>();
            _patterns = new Dictionary<string, List<AccessPattern>>();
            _keyFrequency = new Dictionary<TKey, double>();
            _lastAccess = new Dictionary<TKey, DateTime>();
            _predictiveCache = new Dictionary<TKey, TValue>(_comparer);
            _activePredictions = new Dictionary<TKey, PredictionResult>(_comparer);
            _prefetchQueue = new ConcurrentQueue<TKey>();
            _learningTimer = new Timer(ProcessLearning, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5));
            _cleanupTimer = new Timer(CleanupPatterns, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5));
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
            {
                _predictiveCache[key] = value;
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            RecordAccess(key);
            if (_dictionary.TryGetValue(key, out value!))
            {
                RecordPredictionSuccess(key);
                UpdateKeyFrequency(key);
                return true;
            }
            if (_predictiveCache.TryGetValue(key, out value!))
            {
                _dictionary[key] = value;
                _predictiveCache.Remove(key);
                RecordPredictionSuccess(key);
                UpdateKeyFrequency(key);
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
            if (_patterns.TryGetValue(patternKey, out List<AccessPattern>? matchingPatterns))
            {
                var patternPredictions = matchingPatterns
                    .Where(p => p.Confidence >= _confidenceThreshold)
                    .GroupBy(p => p.NextKey)
                    .Select(g => new PredictionResult(
                        g.Key,
                        g.Average(p => p.Confidence),
                        $"Pattern-based ({g.Count()} occurrences)"))
                    .OrderByDescending(p => p.Confidence);
                predictions.AddRange(patternPredictions);
            }
            var frequencyPredictions = _keyFrequency
                .Where(kvp => !predictions.Any(p => _comparer.Equals(p.Key, kvp.Key)))
                .OrderByDescending(kvp => kvp.Value)
                .Take(3)
                .Select(kvp => new PredictionResult(
                    kvp.Key,
                    Math.Min(kvp.Value / _totalAccesses, 0.8),
                    "Frequency-based"));
            predictions.AddRange(frequencyPredictions);
            return predictions.OrderByDescending(p => p.Confidence).Take(5).ToList();
        }

        public int PrefetchLikely(TKey[] contextKeys, Func<TKey, TValue> valueFactory)
        {
            if (_isDisposed)
                return 0;
            var predictions = GetPredictions(contextKeys);
            int preloaded = 0;
            foreach (var prediction in predictions)
            {
                if (_predictiveCache.Count >= _maxCacheSize)
                    break;
                if (prediction.Confidence < _confidenceThreshold)
                    break;
                if (!_dictionary.ContainsKey(prediction.Key) &&
                    !_predictiveCache.ContainsKey(prediction.Key))
                {
                    try
                    {
                        var value = valueFactory(prediction.Key);
                        _predictiveCache[prediction.Key] = value;
                        _activePredictions[prediction.Key] = prediction;
                        preloaded++;
                        lock (_statsLock)
                        {
                            _totalPredictions++;
                        }
                    }
                    catch
                    {
                    }
                }
            }
            return preloaded;
        }

        public double GetConfidence(TKey key)
        {
            if (_activePredictions.TryGetValue(key, out var prediction))
                return prediction.Confidence;
            if (_keyFrequency.TryGetValue(key, out var frequency))
                return Math.Min(frequency / _totalAccesses, 0.8);
            return 0.0;
        }

        public void UpdateModel()
        {
            ProcessLearning(null);
        }

        public bool Remove(TKey key)
        {
            bool removed = _dictionary.Remove(key);
            _predictiveCache.Remove(key);
            _activePredictions.Remove(key);
            _keyFrequency.Remove(key);
            _lastAccess.Remove(key);
            return removed;
        }

        public void Clear()
        {
            _dictionary.Clear();
            _predictiveCache.Clear();
            _activePredictions.Clear();
            _patterns.Clear();
            _keyFrequency.Clear();
            _lastAccess.Clear();
            while (_accessHistory.TryDequeue(out _)) { }
            while (_prefetchQueue.TryDequeue(out _)) { }
            lock (_statsLock)
            {
                _totalPredictions = 0;
                _successfulPredictions = 0;
                _totalAccesses = 0;
            }
        }
        #region Private ML Implementation
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RecordAccess(TKey key)
        {
            _accessHistory.Enqueue(key);
            _lastAccess[key] = DateTime.UtcNow;
            lock (_statsLock)
            {
                _totalAccesses++;
            }
            while (_accessHistory.Count > _patternLength * 100)
            {
                _accessHistory.TryDequeue(out _);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RecordPredictionSuccess(TKey key)
        {
            if (_activePredictions.ContainsKey(key))
            {
                lock (_statsLock)
                {
                    _successfulPredictions++;
                }
                _activePredictions.Remove(key);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateKeyFrequency(TKey key)
        {
            _keyFrequency.TryGetValue(key, out var current);
            _keyFrequency[key] = current + 1.0;
        }

        private void ProcessLearning(object? state)
        {
            if (_isDisposed || _accessHistory.Count < _patternLength + 1)
                return;
            try
            {
                var recentAccesses = new List<TKey>();
                while (_accessHistory.TryDequeue(out var key) && recentAccesses.Count < _patternLength * 10)
                {
                    recentAccesses.Add(key);
                }
                for (int i = 0; i <= recentAccesses.Count - _patternLength - 1; i++)
                {
                    var pattern = recentAccesses.Skip(i).Take(_patternLength).ToArray();
                    var nextKey = recentAccesses[i + _patternLength];
                    LearnPattern(pattern, nextKey);
                }
            }
            catch (Exception)
            {
            }
        }

        private void LearnPattern(TKey[] sequence, TKey nextKey)
        {
            var patternKey = CreatePatternKey(sequence);
            if (!_patterns.TryGetValue(patternKey, out List<AccessPattern>? patterns))
            {
                patterns = [];
                _patterns[patternKey] = patterns;
            }
            var existingCount = patterns.Count(p => _comparer.Equals(p.NextKey, nextKey));
            var confidence = Math.Min(0.1 + (existingCount * 0.15), 0.95);
            patterns.Add(new AccessPattern(sequence, nextKey, confidence));
            if (patterns.Count > 20)
            {
                patterns.RemoveAt(0);
            }
            if (_patterns.Count > _maxPatterns)
            {
                var oldestPattern = _patterns.Keys.First();
                _patterns.Remove(oldestPattern);
            }
        }

        private string CreatePatternKey(TKey[] sequence)
        {
            return string.Join("|", sequence.Select(k => k?.ToString() ?? "null"));
        }

        private void CleanupPatterns(object? state)
        {
            if (_isDisposed)
                return;
            try
            {
                var cutoffTime = DateTime.UtcNow - _patternTimeout;
                var keysToRemove = new List<string>();
                foreach (KeyValuePair<string, List<AccessPattern>> kvp in _patterns)
                {
                    kvp.Value.RemoveAll(p => p.Timestamp < cutoffTime);
                    if (kvp.Value.Count == 0)
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }
                foreach (var key in keysToRemove)
                {
                    _patterns.Remove(key);
                }
                var activeKeys = new HashSet<TKey>(_dictionary.Keys);
                activeKeys.UnionWith(_predictiveCache.Keys);
                var frequencyKeysToRemove = _keyFrequency.Keys
                    .Where(k => !activeKeys.Contains(k) &&
                               (!_lastAccess.TryGetValue(k, out var lastAccess) ||
                                lastAccess < cutoffTime))
                    .ToList();
                foreach (var key in frequencyKeysToRemove)
                {
                    _keyFrequency.Remove(key);
                    _lastAccess.Remove(key);
                }
            }
            catch (Exception)
            {
            }
        }

        private long EstimateMemoryUsage()
        {
            long memory = 0;
            memory += _dictionary.Count * 64;
            memory += _predictiveCache.Count * 64;
            memory += _patterns.Count * 128;
            memory += _keyFrequency.Count * 32;
            memory += _accessHistory.Count * 16;
            return memory;
        }
        #endregion
        public void Dispose()
        {
            if (_isDisposed)
                return;
            _isDisposed = true;
            _learningTimer?.Dispose();
            _cleanupTimer?.Dispose();
            Clear();
        }
    }
}
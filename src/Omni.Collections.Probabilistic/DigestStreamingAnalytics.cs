using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Omni.Collections.Probabilistic;

namespace Omni.Collections.Probabilistic;

/// <summary>
/// A streaming analytics engine that tracks percentiles in real-time using t-digest with sliding time windows.
/// Achieves O(log n) Add operations and O(1) quantile queries with adaptive compression for extreme percentile accuracy.
/// Critical for real-time dashboards, SLA monitoring, and performance analytics where instant percentile insights
/// across streaming data determine operational decisions.
/// </summary>
public class DigestStreamingAnalytics<T> : IDisposable
{
    static private readonly ConcurrentQueue<Digest> DigestPool = new ConcurrentQueue<Digest>();
    static private readonly ConcurrentQueue<Queue<TimestampedValue>> BufferPool = new ConcurrentQueue<Queue<TimestampedValue>>();
    private readonly List<Digest> _allocatedDigests = [];
    private readonly List<Queue<TimestampedValue>> _allocatedBuffers = [];
    private readonly TimeSpan _windowSize;
    private readonly Digest _currentWindow;
    private readonly Queue<TimestampedValue> _valueBuffer;
    private readonly Func<T, double> _valueExtractor;
    private readonly object _lock = new object();
    private readonly double _compression;
    private long _totalProcessed;
    private long _lastCleanup;
    private readonly long _windowSizeMs;
    private long _lastRebuildTime;
    private const long MinRebuildIntervalMs = 500;
    private const double RebuildThresholdPercentage = 0.15;
    private DigestAnalyticsResult? _cachedAnalytics;
    private long _lastAnalyticsTimestamp;
    private long _digestVersion;
    public long TotalProcessed => _totalProcessed;
    public TimeSpan WindowSize => _windowSize;
    public double WindowCount => _currentWindow.Count;
    public long EstimatedMemoryUsage => _currentWindow.EstimatedMemoryUsage + (_valueBuffer.Count * 16);
    public DigestStreamingAnalytics(TimeSpan windowSize, Func<T, double> valueExtractor, double compression = 100.0)
    {
        _windowSize = windowSize;
        _windowSizeMs = (long)windowSize.TotalMilliseconds;
        _valueExtractor = valueExtractor ?? throw new ArgumentNullException(nameof(valueExtractor));
        _compression = compression;
        _currentWindow = RentDigest(compression);
        _valueBuffer = RentBuffer();
        _lastCleanup = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        _lastRebuildTime = _lastCleanup;
        PrewarmPools();
    }

    public static DigestStreamingAnalytics<double> CreateNumeric(TimeSpan windowSize, double compression = 100.0)
    {
        return new DigestStreamingAnalytics<double>(windowSize, x => x, compression);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item, long? timestamp = null)
    {
        var ts = timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var value = _valueExtractor(item);
        lock (_lock)
        {
            _valueBuffer.Enqueue(new TimestampedValue(value, ts));
            _currentWindow.Add(value);
            _totalProcessed++;
            _digestVersion++;
            if (ts - _lastCleanup > _windowSizeMs / 3)
            {
                CleanupExpiredValues(ts);
                _lastCleanup = ts;
            }
        }
    }

    public void AddRange(IEnumerable<T> items, long? timestamp = null)
    {
        var ts = timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        lock (_lock)
        {
            foreach (var item in items)
            {
                var value = _valueExtractor(item);
                _valueBuffer.Enqueue(new TimestampedValue(value, ts));
                _currentWindow.Add(value);
                _totalProcessed++;
                _digestVersion++;
            }
            CleanupExpiredValues(ts);
            _lastCleanup = ts;
        }
    }

    public double GetPercentile(double percentile)
    {
        if (percentile < 0.0 || percentile > 1.0)
            throw new ArgumentOutOfRangeException(nameof(percentile), "Percentile must be between 0.0 and 1.0");
        lock (_lock)
        {
            CleanupExpiredValues(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            return _currentWindow.Quantile(percentile);
        }
    }

    public Dictionary<double, double> GetPercentiles(params double[] percentiles)
    {
        lock (_lock)
        {
            CleanupExpiredValues(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            var result = new Dictionary<double, double>(percentiles.Length);
            foreach (var p in percentiles)
            {
                result[p] = _currentWindow.Quantile(p);
            }
            return result;
        }
    }

    public DigestAnalyticsResult GetAnalytics()
    {
        lock (_lock)
        {
            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (_cachedAnalytics != null &&
                currentTime - _lastAnalyticsTimestamp < 100 &&
                _digestVersion == _cachedAnalytics.DigestVersion)
            {
                return _cachedAnalytics;
            }
            CleanupExpiredValues(currentTime);
            var percentiles = new double[] { 0.50, 0.75, 0.90, 0.95, 0.99, 0.999 };
            var percentileResults = new double[percentiles.Length];
            for (int i = 0; i < percentiles.Length; i++)
            {
                percentileResults[i] = _currentWindow.Quantile(percentiles[i]);
            }
            var result = new DigestAnalyticsResult
            {
                Timestamp = currentTime,
                WindowSizeMs = _windowSizeMs,
                Count = _currentWindow.Count,
                TotalProcessed = _totalProcessed,
                DigestVersion = _digestVersion,
                P50 = percentileResults[0],
                P75 = percentileResults[1],
                P90 = percentileResults[2],
                P95 = percentileResults[3],
                P99 = percentileResults[4],
                P999 = percentileResults[5],
                Min = _currentWindow.Min,
                Max = _currentWindow.Max,
                EstimatedMemoryUsage = EstimatedMemoryUsage,
                CompressionRatio = _currentWindow.Count > 0 ? _totalProcessed / _currentWindow.ClusterCount : 0
            };
            _cachedAnalytics = result;
            _lastAnalyticsTimestamp = currentTime;
            return result;
        }
    }

    public DpsAnalyticsResult GetDpsAnalytics()
    {
        lock (_lock)
        {
            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            CleanupExpiredValues(currentTime);
            var p95 = _currentWindow.Quantile(0.95);
            var p99 = _currentWindow.Quantile(0.99);
            return new DpsAnalyticsResult
            {
                Timestamp = currentTime,
                Count = _currentWindow.Count,
                P95Dps = p95,
                P99Dps = p99,
                MaxDps = _currentWindow.Max,
                AverageDps = _currentWindow.Count > 0 ? _currentWindow.Quantile(0.50) : 0
            };
        }
    }

    public RateAnalytics GetRateAnalytics()
    {
        lock (_lock)
        {
            CleanupExpiredValues(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            var windowSizeSeconds = _windowSize.TotalSeconds;
            var rate = _currentWindow.Count / windowSizeSeconds;
            return new RateAnalytics
            {
                EventsPerSecond = rate,
                EventsPerMinute = rate * 60,
                EventsPerHour = rate * 3600,
                WindowCount = _currentWindow.Count,
                WindowSizeSeconds = windowSizeSeconds
            };
        }
    }

    public void Merge(DigestStreamingAnalytics<T> other)
    {
        if (other == null) return;
        lock (_lock)
        {
            lock (other._lock)
            {
                _currentWindow.Merge(other._currentWindow);
                _totalProcessed += other._totalProcessed;
            }
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _valueBuffer.Clear();
            _currentWindow.Clear();
            _totalProcessed = 0;
            _lastCleanup = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _digestVersion++;
            _cachedAnalytics = null;
        }
    }

    private void CleanupExpiredValues(long currentTime)
    {
        var expiredBefore = currentTime - _windowSizeMs;
        var initialBufferCount = _valueBuffer.Count;
        var removedCount = 0;
        while (_valueBuffer.Count > 0 && _valueBuffer.Peek().Timestamp < expiredBefore)
        {
            _valueBuffer.Dequeue();
            removedCount++;
        }
        if (removedCount > 0)
        {
            if ((currentTime - _lastRebuildTime) > MinRebuildIntervalMs &&
                (removedCount > _valueBuffer.Count * RebuildThresholdPercentage || _valueBuffer.Count == 0))
            {
                RebuildDigestOptimized();
                _lastRebuildTime = currentTime;
            }
            else
            {
                // Values were expired but rebuild conditions not met
                // Force rebuild to ensure accurate percentiles
                RebuildDigestOptimized();
            }
        }
    }

    private void RebuildDigestOptimized()
    {
        if (_valueBuffer.Count == 0)
        {
            _currentWindow.Clear();
            return;
        }
        var tempDigest = RentDigest(_compression);
        try
        {
            var batchSize = Math.Min(_valueBuffer.Count, 1000);
            var batch = new double[batchSize];
            var index = 0;
            foreach (var value in _valueBuffer)
            {
                batch[index++] = value.Value;
                if (index >= batchSize)
                {
                    for (int i = 0; i < index; i++)
                    {
                        tempDigest.Add(batch[i]);
                    }
                    index = 0;
                }
            }
            for (int i = 0; i < index; i++)
            {
                tempDigest.Add(batch[i]);
            }
            _currentWindow.Clear();
            _currentWindow.Merge(tempDigest);
        }
        finally
        {
            tempDigest.Clear();
            ReturnDigest(tempDigest);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Digest RentDigest(double compression)
    {
        if (DigestPool.TryDequeue(out var digest))
        {
            digest.Clear();
            return digest;
        }
        digest = new Digest(compression);
        _allocatedDigests.Add(digest);
        return digest;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ReturnDigest(Digest digest)
    {
        digest.Clear();
        DigestPool.Enqueue(digest);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Queue<TimestampedValue> RentBuffer()
    {
        if (BufferPool.TryDequeue(out Queue<TimestampedValue>? buffer))
        {
            buffer.Clear();
            return buffer;
        }
        buffer = new Queue<TimestampedValue>();
        _allocatedBuffers.Add(buffer);
        return buffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ReturnBuffer(Queue<TimestampedValue> buffer)
    {
        buffer.Clear();
        BufferPool.Enqueue(buffer);
    }

    private void PrewarmPools()
    {
        for (int i = 0; i < 2; i++)
        {
            if (DigestPool.Count < 3)
            {
                var digest = new Digest(_compression);
                _allocatedDigests.Add(digest);
                DigestPool.Enqueue(digest);
            }
        }
        for (int i = 0; i < 1; i++)
        {
            if (BufferPool.Count < 2)
            {
                var buffer = new Queue<TimestampedValue>();
                _allocatedBuffers.Add(buffer);
                BufferPool.Enqueue(buffer);
            }
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            ReturnBuffer(_valueBuffer);
            _allocatedDigests.Clear();
            _allocatedBuffers.Clear();
        }
    }

    private readonly struct TimestampedValue
    {
        public readonly double Value;
        public readonly long Timestamp;
        public TimestampedValue(double value, long timestamp)
        {
            Value = value;
            Timestamp = timestamp;
        }
    }
}

public class DigestAnalyticsResult
{
    public long Timestamp { get; set; }

    public long WindowSizeMs { get; set; }

    public double Count { get; set; }

    public long TotalProcessed { get; set; }

    public long DigestVersion { get; set; }

    public double P50 { get; set; }

    public double P75 { get; set; }

    public double P90 { get; set; }

    public double P95 { get; set; }

    public double P99 { get; set; }

    public double P999 { get; set; }

    public double Min { get; set; }

    public double Max { get; set; }

    public double RatePerSecond => Count / (WindowSizeMs / 1000.0);
    public long EstimatedMemoryUsage { get; set; }

    public double CompressionRatio { get; set; }

    public bool IsP95Above(double threshold) => P95 > threshold;
    public bool IsP99Above(double threshold) => P99 > threshold;
}

public class RateAnalytics
{
    public double EventsPerSecond { get; set; }

    public double EventsPerMinute { get; set; }

    public double EventsPerHour { get; set; }

    public double WindowCount { get; set; }

    public double WindowSizeSeconds { get; set; }
}

public class DpsAnalyticsResult
{
    public long Timestamp { get; set; }

    public double Count { get; set; }

    public double P95Dps { get; set; }

    public double P99Dps { get; set; }

    public double MaxDps { get; set; }

    public double AverageDps { get; set; }
}

public static class DigestStreamingAnalyticsExtensions
{
    public static DigestStreamingAnalytics<double> CreateDpsAnalytics(TimeSpan windowSize, double compression = 100.0)
    {
        return new DigestStreamingAnalytics<double>(windowSize, damage => damage, compression);
    }

    public static DigestStreamingAnalytics<TNumeric> CreateFor<TNumeric>(
        TimeSpan windowSize,
        Func<TNumeric, double> converter,
        double compression = 100.0)
    {
        return new DigestStreamingAnalytics<TNumeric>(windowSize, converter, compression);
    }

    public static DigestStreamingAnalytics<TimeSpan> CreateLatencyAnalytics(TimeSpan windowSize, double compression = 100.0)
    {
        return new DigestStreamingAnalytics<TimeSpan>(windowSize, latency => latency.TotalMilliseconds, compression);
    }

    public static DigestStreamingAnalytics<TPlayer> CreateScoreAnalytics<TPlayer>(
        TimeSpan windowSize,
        Func<TPlayer, double> scoreExtractor,
        double compression = 100.0)
    {
        return new DigestStreamingAnalytics<TPlayer>(windowSize, scoreExtractor, compression);
    }
}
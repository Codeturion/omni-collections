using System;
using System.Diagnostics;

namespace Omni.Collections.Core.Time;

/// <summary>
/// Abstraction for the current time. Production code that consults <see cref="DateTime.UtcNow"/>
/// or <see cref="Stopwatch.GetTimestamp()"/> should accept an <see cref="IClock"/> in its
/// constructor so tests can deterministically advance time without resorting to <c>Thread.Sleep</c>.
/// </summary>
public interface IClock
{
    /// <summary>
    /// The current UTC instant, equivalent to <see cref="DateTimeOffset.UtcNow"/> for the system clock.
    /// </summary>
    DateTimeOffset UtcNow { get; }

    /// <summary>
    /// A monotonic timestamp in <see cref="Stopwatch"/> ticks, equivalent to
    /// <see cref="Stopwatch.GetTimestamp()"/> for the system clock. Use for elapsed-time
    /// measurements and LRU access-time stamps where wall-clock drift is undesirable.
    /// </summary>
    long GetTimestamp();
}

/// <summary>
/// The default <see cref="IClock"/> implementation backed by the system clock.
/// </summary>
public sealed class SystemClock : IClock
{
    /// <summary>Singleton instance — safe to share across the entire process.</summary>
    public static readonly SystemClock Instance = new();

    private SystemClock() { }

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public long GetTimestamp() => Stopwatch.GetTimestamp();
}

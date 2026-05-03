using System;
using System.Diagnostics;
using Omni.Collections.Core.Time;

namespace Omni.Collections.Tests._TestHelpers;

/// <summary>
/// Deterministic <see cref="IClock"/> for tests that exercise time-dependent behaviour
/// (windowed analytics, snapshot recording, frame duration). Replaces <c>Thread.Sleep</c>
/// in unit tests so they run instantly and without flake. <see cref="UtcNow"/> starts at
/// the Unix epoch and the monotonic timestamp at zero — call <see cref="Advance(TimeSpan)"/>
/// to push both forward.
/// </summary>
internal sealed class FakeClock : IClock
{
    private long _timestamp;

    public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UnixEpoch;

    public long GetTimestamp() => _timestamp;

    public void Advance(TimeSpan delta)
    {
        UtcNow = UtcNow.Add(delta);
        _timestamp += (long)(delta.TotalSeconds * Stopwatch.Frequency);
    }
}

using System.Collections.Concurrent;
using WZ.RateLimiting.Abstractions;

namespace WZ.RateLimiting.Storage;

/// <summary>
/// Thread-safe, in-process implementation of <see cref="IRateLimitStore"/>.
/// Suitable for single-instance deployments. State is lost on restart and
/// is not shared across multiple app instances — see the Redis-backed
/// store planned for V3 for distributed scenarios.
/// </summary>
public sealed class InMemoryRateLimitStore : IRateLimitStore
{
    private readonly ConcurrentDictionary<string, CounterState> _counters = new();

    public ValueTask<RateLimitCounterEntry> GetOrCreateAsync(
        string key,
        TimeSpan window,
        CancellationToken cancellationToken)
    {
        var state = _counters.GetOrAdd(key, _ => new CounterState(DateTimeOffset.UtcNow));
        return ValueTask.FromResult(Snapshot(state));
    }

    public ValueTask<RateLimitCounterEntry> IncrementAsync(
        string key,
        TimeSpan window,
        CancellationToken cancellationToken)
    {
        var state = _counters.GetOrAdd(key, _ => new CounterState(DateTimeOffset.UtcNow));

        // The window-expiry check + reset is a "check then act" sequence,
        // which is NOT safe to do with Interlocked alone — two threads could
        // both observe an expired window and both reset it independently,
        // corrupting the count. This narrow section needs a real lock.
        lock (state.ResetLock)
        {
            var now = DateTimeOffset.UtcNow;
            if (now - state.WindowStart >= window)
            {
                state.WindowStart = now;
                state.Count = 0;
            }
        }

        // Incrementing the count itself, once we know we're in the current
        // window, is a single atomic operation — no lock needed here.
        Interlocked.Increment(ref state.Count);

        return ValueTask.FromResult(Snapshot(state));
    }

    private static RateLimitCounterEntry Snapshot(CounterState state) =>
        new(Volatile.Read(ref state.Count), state.WindowStart);

    private sealed class CounterState(DateTimeOffset windowStart)
    {
        public int Count;
        public DateTimeOffset WindowStart = windowStart;
        public readonly object ResetLock = new();
    }
}
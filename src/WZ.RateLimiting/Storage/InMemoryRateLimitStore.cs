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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <param name="window"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public ValueTask<RateLimitCounterEntry> GetOrCreateAsync(
        string key,
        TimeSpan window,
        CancellationToken cancellationToken)
    {
        var state = _counters.GetOrAdd(key, _ => new CounterState(DateTimeOffset.UtcNow));
        return ValueTask.FromResult(Snapshot(state));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <param name="window"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
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
                state.PCount = state.Count;
                state.WindowStart = now;
                state.Count = 0;
            }
        }
        // Incrementing the count itself, once we know we're in the current
        // window, is a single atomic operation — no lock needed here.
        Interlocked.Increment(ref state.Count);

        return ValueTask.FromResult(Snapshot(state));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public ValueTask<bool> GetAsync(string key)
    {
        var state = _counters.TryGetValue(key,out var counterState);
        return ValueTask.FromResult(state);
    }

    /// <inheritdoc />
    public ValueTask<RateLimitCounterEntry> UpdateAsync(string key, RateLimitCounterEntry entry, CancellationToken cancellationToken)
    {
        _counters[key].WindowStart = entry.WindowStart;
        _counters[key].Count = entry.Count;
        _counters[key].WindowStart = entry.WindowStart;   // duplicate line, harmless but pointless
        _counters[key].PCount = entry.PCount;
        return ValueTask.FromResult(Snapshot(_counters[key]));
    }
    private static RateLimitCounterEntry Snapshot(CounterState state) =>
        new(Volatile.Read(ref state.PCount),Volatile.Read(ref state.Count), state.WindowStart);

    private sealed class CounterState(DateTimeOffset windowStart)
    {
        public int PCount=0;
        public int Count;
        public DateTimeOffset WindowStart = windowStart;
        public readonly object ResetLock = new();
    }
}

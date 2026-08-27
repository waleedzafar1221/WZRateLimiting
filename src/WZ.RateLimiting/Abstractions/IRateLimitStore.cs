namespace WZ.RateLimiting.Abstractions;

/// <summary>
/// Persists and retrieves raw rate-limit counter state for a given key.
/// Has no knowledge of policies, algorithms, or HTTP — purely storage.
/// </summary>
public interface IRateLimitStore
{
    ValueTask<RateLimitCounterEntry> GetOrCreateAsync(
        string key,
        TimeSpan window,
        CancellationToken cancellationToken);

    ValueTask<RateLimitCounterEntry> IncrementAsync(
        string key,
        TimeSpan window,
        CancellationToken cancellationToken);
}
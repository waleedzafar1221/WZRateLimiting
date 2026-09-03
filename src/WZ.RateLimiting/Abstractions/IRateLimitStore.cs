namespace WZ.RateLimiting.Abstractions;

/// <summary>
/// Persists and retrieves raw rate-limit counter state for a given key.
/// Has no knowledge of policies, algorithms, or HTTP — purely storage.
/// </summary>
public interface IRateLimitStore
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <param name="window"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    ValueTask<RateLimitCounterEntry> GetOrCreateAsync(
        string key,
        TimeSpan window,
        CancellationToken cancellationToken);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <param name="window"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    ValueTask<RateLimitCounterEntry> IncrementAsync(
        string key,
        TimeSpan window,
        CancellationToken cancellationToken);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    ValueTask<bool> GetAsync(string key);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <param name="entry"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    ValueTask<RateLimitCounterEntry> UpdateAsync(string key,RateLimitCounterEntry entry, CancellationToken cancellationToken);
}
namespace WZ.RateLimiting.Abstractions;

/// <summary>
/// Evaluates whether a request identified by <see cref="RateLimitContext"/>
/// should be allowed, based on the algorithm's counting strategy
/// (e.g. fixed window, sliding window, token bucket).
/// </summary>
public interface IRateLimitAlgorithm
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    ValueTask<RateLimitDecision> EvaluateAsync(
        RateLimitContext context,
        CancellationToken cancellationToken);
}
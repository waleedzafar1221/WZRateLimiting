namespace WZ.RateLimiting.Abstractions;

/// <summary>
/// Evaluates whether a request identified by <see cref="RateLimitContext"/>
/// should be allowed, based on the algorithm's counting strategy
/// (e.g. fixed window, sliding window, token bucket).
/// </summary>
public interface IRateLimitAlgorithm
{
    ValueTask<RateLimitDecision> EvaluateAsync(
        RateLimitContext context,
        CancellationToken cancellationToken);
}
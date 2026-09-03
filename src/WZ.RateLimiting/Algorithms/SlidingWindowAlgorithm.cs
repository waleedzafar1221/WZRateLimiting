using WZ.RateLimiting.Abstractions;

namespace WZ.RateLimiting.Algorithms;

/// <summary>
/// Sliding-window rate limiting: estimates the effective request count in
/// the current window by weighting the previous window's count based on
/// how much time-overlap remains with it, avoiding the boundary-burst
/// problem of fixed window.
/// </summary>
/// <param name="store"></param>
public class SlidingWindowAlgorithm(IRateLimitStore store) : IRateLimitAlgorithm
{
    /// <inheritdoc />
    public async ValueTask<RateLimitDecision> EvaluateAsync(
        RateLimitContext context,
        CancellationToken cancellationToken)
    {
        var key = BuildKey(context.Policy.Name, context.IdentifierKey);
        var now = DateTimeOffset.UtcNow;

        var entry = await store.IncrementAsync(key, context.Policy.Window, cancellationToken);

        var elapsed = now - entry.WindowStart;

        // Overlap: how much of the previous window's time range still
        // "counts" toward the current sliding moment. Clamp to [0,1] —
        // elapsed can be slightly negative or > window due to clock/async
        // timing, and we never want overlap outside that range.
        var overlapFraction = Math.Clamp(
            1.0 - (elapsed.TotalSeconds / context.Policy.Window.TotalSeconds),
            0.0,
            1.0);

        // entry.Count already includes the current request (IncrementAsync
        // increments before returning), so the "current window" contribution
        // is entry.Count as-is — no separate "+1" needed.
        var estimatedCount = entry.PCount * overlapFraction + entry.Count;

        // Allowed when the smoothed estimate is still within the limit.
        var isAllowed = estimatedCount <= context.Policy.PermitLimit;

        var remaining = Math.Max(0, (int)(context.Policy.PermitLimit - estimatedCount));

        // Sliding window has no single hard reset instant like fixed window.
        // We give callers a reasonable estimate: the end of the current
        // fixed sub-window, which is when entry.Count itself will roll over
        // and overlapFraction will have decayed further.
        var resetsAt = entry.WindowStart + context.Policy.Window;

        return new RateLimitDecision(
            IsAllowed: isAllowed,
            Limit: context.Policy.PermitLimit,
            Remaining: remaining,
            ResetsAt: resetsAt,
            RetryAfter: isAllowed ? null : resetsAt - now);
    }

    /// <summary>
    /// Combines policy name and identifier key so different policies using
    /// the same identifier type never share a counter.
    /// </summary>
    private static string BuildKey(string policyName, string identifierKey) =>
        $"{policyName}:{identifierKey}";
}
using WZ.RateLimiting.Abstractions;

namespace WZ.RateLimiting.Algorithms;

/// <summary>
/// Fixed-window rate limiting: allows up to N requests per policy window,
/// then rejects until the window rolls over. Simple and cheap, but can
/// allow up to 2x the limit across a window boundary (a known, accepted
/// V1 tradeoff — sliding window / token bucket address this in V2).
/// </summary>
public sealed class FixedWindowAlgorithm(IRateLimitStore store) : IRateLimitAlgorithm
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async ValueTask<RateLimitDecision> EvaluateAsync(
        RateLimitContext context,
        CancellationToken cancellationToken)
    {
        var key = BuildKey(context.Policy.Name, context.IdentifierKey);

        var entry = await store.IncrementAsync(key, context.Policy.Window, cancellationToken);

        var isAllowed = entry.Count <= context.Policy.PermitLimit;
        var remaining = Math.Max(0, context.Policy.PermitLimit - entry.Count);
        var resetsAt = entry.WindowStart + context.Policy.Window;

        return new RateLimitDecision(
            IsAllowed: isAllowed,
            Limit: context.Policy.PermitLimit,
            Remaining: remaining,
            ResetsAt: resetsAt,
            RetryAfter: isAllowed ? null : resetsAt - DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Combines policy name and identifier key so different policies using
    /// the same identifier type never share a counter.
    /// </summary>
    private static string BuildKey(string policyName, string identifierKey) =>
        $"{policyName}:{identifierKey}";
}
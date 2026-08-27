namespace WZ.RateLimiting.Abstractions;

/// <summary>
/// The outcome of a rate-limit evaluation. Consumed by the middleware
/// to decide whether to continue the pipeline or return 429.
/// </summary>
public readonly record struct RateLimitDecision(
    bool IsAllowed,
    int Limit,
    int Remaining,
    DateTimeOffset ResetsAt,
    TimeSpan? RetryAfter);
namespace WZ.RateLimiting.Abstractions;

/// <summary>
/// Raw counter state as reported by an <see cref="IRateLimitStore"/>.
/// Contains facts only — no allow/deny decision.
/// </summary>
public readonly record struct RateLimitCounterEntry(
    int PCount,
    int Count,
    DateTimeOffset WindowStart);
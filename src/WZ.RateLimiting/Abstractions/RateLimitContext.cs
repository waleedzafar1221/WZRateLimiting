using WZ.RateLimiting.Policies;

namespace WZ.RateLimiting.Abstractions;

/// <summary>
/// Everything an <see cref="IRateLimitAlgorithm"/> needs to evaluate a
/// request, decoupled from HttpContext so algorithms are unit-testable
/// without ASP.NET Core.
/// </summary>
public sealed record RateLimitContext(
    string IdentifierKey,
    RateLimitPolicy Policy);
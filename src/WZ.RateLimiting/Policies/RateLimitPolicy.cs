using WZ.RateLimiting.Abstractions;

namespace WZ.RateLimiting.Policies;

/// <summary>
/// Configuration for a named rate limit rule: which identifier and
/// algorithm to use, and the limit/window to enforce.
/// Identifier and algorithm are resolved from DI per-request via their
/// Type, not baked in as fixed instances, so they can safely carry
/// scoped dependencies in future versions.
/// </summary>
public sealed class RateLimitPolicy
{
    /// <summary>
    /// 
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// 
    /// </summary>
    public Type IdentifierType { get; }
    /// <summary>
    /// 
    /// </summary>
    public Type AlgorithmType { get; }
    /// <summary>
    /// 
    /// </summary>
    public int PermitLimit { get; }
    /// <summary>
    /// 
    /// </summary>
    public TimeSpan Window { get; }

    private RateLimitPolicy(
        string name,
        Type identifierType,
        Type algorithmType,
        int permitLimit,
        TimeSpan window)
    {
        Name = name;
        IdentifierType = identifierType;
        AlgorithmType = algorithmType;
        PermitLimit = permitLimit;
        Window = window;
    }

    /// <summary>
    /// Creates a validated <see cref="RateLimitPolicy"/>.
    /// </summary>
    public static RateLimitPolicy Create(
        string name,
        Type identifierType,
        Type algorithmType,
        int permitLimit,
        TimeSpan window)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Policy name must not be empty.", nameof(name));

        if (!typeof(IClientIdentifier).IsAssignableFrom(identifierType))
            throw new ArgumentException(
                $"{identifierType.Name} must implement {nameof(IClientIdentifier)}.",
                nameof(identifierType));

        if (!typeof(IRateLimitAlgorithm).IsAssignableFrom(algorithmType))
            throw new ArgumentException(
                $"{algorithmType.Name} must implement {nameof(IRateLimitAlgorithm)}.",
                nameof(algorithmType));

        if (permitLimit <= 0)
            throw new ArgumentOutOfRangeException(nameof(permitLimit), "Must be greater than zero.");

        if (window <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(window), "Must be greater than zero.");

        return new RateLimitPolicy(name, identifierType, algorithmType, permitLimit, window);
    }
}
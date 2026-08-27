using WZ.RateLimiting.Policies;

namespace WZ.RateLimiting.Options;

/// <summary>
/// Holds all rate-limit policies registered via <c>AddWzRateLimiting</c>.
/// </summary>
public sealed class RateLimitingOptions
{
    private readonly Dictionary<string, RateLimitPolicy> _policies = new();

    /// <summary>Registered policies, keyed by policy name.</summary>
    public IReadOnlyDictionary<string, RateLimitPolicy> Policies => _policies;

    /// <summary>
    /// Registers a named policy using the fluent <see cref="RateLimitPolicyBuilder"/>.
    /// </summary>
    public RateLimitingOptions AddPolicy(string name, Action<RateLimitPolicyBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new RateLimitPolicyBuilder(name);
        configure(builder);
        _policies[name] = builder.Build();
        return this;
    }
}
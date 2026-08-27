using WZ.RateLimiting.Algorithms;
using WZ.RateLimiting.Identifiers;

namespace WZ.RateLimiting.Policies;

/// <summary>
/// Fluent builder for configuring a single <see cref="RateLimitPolicy"/>
/// inside <c>AddPolicy(name, policy => ...)</c>.
/// </summary>
public sealed class RateLimitPolicyBuilder
{
    private readonly string _name;
    private Type _identifierType = typeof(IpAddressIdentifier);
    private readonly Type _algorithmType = typeof(FixedWindowAlgorithm);
    private int _limit;
    private TimeSpan _window;

    internal RateLimitPolicyBuilder(string name)
    {
        _name = name;
    }

    /// <summary>Identifies clients by IP address. This is the only identifier available in V1.</summary>
    public RateLimitPolicyBuilder PerIp()
    {
        _identifierType = typeof(IpAddressIdentifier);
        return this;
    }

    /// <summary>Sets the maximum number of requests allowed within the window.</summary>
    public RateLimitPolicyBuilder Limit(int permitLimit)
    {
        _limit = permitLimit;
        return this;
    }

    /// <summary>Sets the window to one minute.</summary>
    public RateLimitPolicyBuilder PerMinute() => Window(TimeSpan.FromMinutes(1));

    /// <summary>Sets the window to one second.</summary>
    public RateLimitPolicyBuilder PerSecond() => Window(TimeSpan.FromSeconds(1));

    /// <summary>Sets an explicit window duration.</summary>
    public RateLimitPolicyBuilder Window(TimeSpan window)
    {
        _window = window;
        return this;
    }

    internal RateLimitPolicy Build() =>
        RateLimitPolicy.Create(_name, _identifierType, _algorithmType, _limit, _window);
}
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
    private Type _algorithmType = typeof(FixedWindowAlgorithm);
    private int _limit;
    private int _refillCapacity;
    private string _temp_storage="";
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
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public RateLimitPolicyBuilder PerUser()
    {
        _identifierType = typeof(UserIdentifier);
        return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="claimType"></param>
    /// <returns></returns>
    public RateLimitPolicyBuilder ClaimType(string claimType)
    {
        _temp_storage=claimType;
        return this;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public RateLimitPolicyBuilder PerApiKey()
    {
        _identifierType = typeof(ApiKeyIdentifier);
        return this;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="headerName"></param>
    /// <returns></returns>
    public RateLimitPolicyBuilder ApiKeyHeader(string headerName)
    {
        _temp_storage=headerName;
        return this;
    }
    /// <summary>
    /// Fixed window algorithm
    /// </summary>
    /// <returns></returns>
    public RateLimitPolicyBuilder UseFixedWindow()
    {
        _algorithmType = typeof(FixedWindowAlgorithm);
        return this;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public RateLimitPolicyBuilder UseSlideWindow()
    {
        _algorithmType = typeof(SlidingWindowAlgorithm);
        return this;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public RateLimitPolicyBuilder UseTokenBucket()
    {
        _algorithmType = typeof(TokenBucketAlgorithm);
        return this;
    }
    /// <summary>Sets the maximum number of requests allowed within the window.</summary>
    public RateLimitPolicyBuilder Limit(int permitLimit)
    {
        _limit = permitLimit;
        return this;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="burstCapacity"></param>
    /// <returns></returns>
    public RateLimitPolicyBuilder Capacity(int burstCapacity)
    {
        _limit = burstCapacity;
        return this;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="refillCapacity"></param>
    /// <returns></returns>
    public RateLimitPolicyBuilder Refill(int refillCapacity)
    {
        _refillCapacity = refillCapacity;
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
        RateLimitPolicy.Create(_name, _identifierType, _algorithmType, _limit, _window,_refillCapacity=1);
}
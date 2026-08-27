using WZ.RateLimiting.Abstractions;

namespace WZ.RateLimiting.Attributes;

/// <summary>
/// Applies a named rate-limit policy to a controller or action.
/// </summary>
/// <example>
/// <code>
/// [EnableRateLimiting("login")]
/// [HttpPost("login")]
/// public IActionResult Login(LoginRequest request) => Ok();
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class EnableRateLimitingAttribute : Attribute, IRateLimitingMetadata
{
    /// <summary>
    /// 
    /// </summary>
    public string PolicyName { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="policyName"></param>
    public EnableRateLimitingAttribute(string policyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);
        PolicyName = policyName;
    }
}
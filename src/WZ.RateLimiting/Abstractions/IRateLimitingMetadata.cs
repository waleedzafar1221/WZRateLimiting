using WZ.RateLimiting.Attributes;

namespace WZ.RateLimiting.Abstractions;

/// <summary>
/// Marks an endpoint as subject to a named rate-limit policy. Implemented
/// by both <see cref="EnableRateLimitingAttribute"/> (controllers) and the
/// endpoint metadata added by <c>RequireRateLimiting</c> (minimal APIs).
/// </summary>
public interface IRateLimitingMetadata
{
    /// <summary>
    /// 
    /// </summary>
    string PolicyName { get; }
}
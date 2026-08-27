using Microsoft.AspNetCore.Builder;
using WZ.RateLimiting.Abstractions;

namespace WZ.RateLimiting.Extensions;

/// <summary>
/// Applies a named rate-limit policy to a minimal API endpoint.
/// </summary>
/// <example>
/// <code>
/// app.MapGet("/api/products", () => Results.Ok())
///    .RequireRateLimiting("public-api");
/// </code>
/// </example>
public static class EndpointConventionBuilderExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="policyName"></param>
    /// <typeparam name="TBuilder"></typeparam>
    /// <returns></returns>
    public static TBuilder RequireWzRateLimiting<TBuilder>(this TBuilder builder, string policyName)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);
        builder.WithMetadata(new RateLimitingEndpointMetadata(policyName));
        return builder;
    }

    private sealed class RateLimitingEndpointMetadata(string policyName) : IRateLimitingMetadata
    {
        public string PolicyName { get; } = policyName;
    }
}
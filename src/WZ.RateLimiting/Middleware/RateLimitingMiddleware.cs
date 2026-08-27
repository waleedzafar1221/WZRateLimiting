using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using WZ.RateLimiting.Abstractions;
using WZ.RateLimiting.Options;

namespace WZ.RateLimiting.Middleware;

/// <summary>
/// Evaluates incoming requests against a rate-limit policy and either
/// continues the pipeline or short-circuits with 429 Too Many Requests.
/// </summary>
/// <remarks>
/// V1 limitation: this milestone applies a single policy to the whole
/// pipeline (the first one registered). Per-endpoint policy resolution via
/// [EnableRateLimiting]/.RequireRateLimiting(...) is added in Milestone 5.
/// </remarks>
public sealed class RateLimitingMiddleware(RequestDelegate next, RateLimitingOptions options)
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task InvokeAsync(HttpContext context)
    {
        var metadata = context.GetEndpoint()?.Metadata.GetMetadata<IRateLimitingMetadata>();

        if (metadata is null)
        {
            // No policy attached to this endpoint — not rate limited.
            await next(context);
            return;
        }

        if (!options.Policies.TryGetValue(metadata.PolicyName, out var policy))
        {
            throw new InvalidOperationException(
                $"Endpoint requires rate-limit policy \"{metadata.PolicyName}\", " +
                $"but no policy with that name is registered. Did you forget to call " +
                $"options.AddPolicy(\"{metadata.PolicyName}\", ...)?");
        }

        var identifier = (IClientIdentifier)context.RequestServices.GetRequiredService(policy.IdentifierType);
        var algorithm = (IRateLimitAlgorithm)context.RequestServices.GetRequiredService(policy.AlgorithmType);

        var identifierKey = await identifier.GetIdentifierAsync(context, context.RequestAborted);
        var rateLimitContext = new RateLimitContext(identifierKey, policy);

        var decision = await algorithm.EvaluateAsync(rateLimitContext, context.RequestAborted);

        context.Response.Headers["X-RateLimit-Limit"] = decision.Limit.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = decision.Remaining.ToString();
        context.Response.Headers["X-RateLimit-Reset"] = decision.ResetsAt.ToUnixTimeSeconds().ToString();

        if (!decision.IsAllowed)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;

            if (decision.RetryAfter is { } retryAfter)
            {
                var seconds = Math.Max(0, Math.Ceiling(retryAfter.TotalSeconds));
                context.Response.Headers["Retry-After"] = seconds.ToString(CultureInfo.InvariantCulture);
            }

            return;
        }

        await next(context);
    }
}
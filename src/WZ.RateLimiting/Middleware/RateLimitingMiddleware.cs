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
public sealed class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RateLimitingOptions _options;

    public RateLimitingMiddleware(RequestDelegate next, RateLimitingOptions options)
    {
        _next = next;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var policy = _options.Policies.Values.FirstOrDefault();
        if (policy is null)
        {
            // No policy configured — nothing to enforce.
            await _next(context);
            return;
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
                context.Response.Headers["Retry-After"] = seconds.ToString();
            }

            return;
        }

        await _next(context);
    }
}
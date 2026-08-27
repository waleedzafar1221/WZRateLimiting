using Microsoft.AspNetCore.Builder;
using WZ.RateLimiting.Middleware;

namespace WZ.RateLimiting.Extensions;

/// <summary>
/// Adds WZ.RateLimiting middleware to the request pipeline.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseWzRateLimiting(this IApplicationBuilder app) =>
        app.UseMiddleware<RateLimitingMiddleware>();
}
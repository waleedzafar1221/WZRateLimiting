using Microsoft.Extensions.DependencyInjection;
using WZ.RateLimiting.Abstractions;
using WZ.RateLimiting.Algorithms;
using WZ.RateLimiting.Identifiers;
using WZ.RateLimiting.Options;
using WZ.RateLimiting.Storage;

namespace WZ.RateLimiting.Extensions;

/// <summary>
/// Registers WZ.RateLimiting services into the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds WZ.RateLimiting with the given policy configuration.
    /// </summary>
    public static IServiceCollection AddWzRateLimiting(
        this IServiceCollection services,
        Action<RateLimitingOptions>? configure = null)
    {
        var options = new RateLimitingOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);

        // Store must be a singleton: counters need to persist across requests
        // for the lifetime of the app.
        services.AddSingleton<IRateLimitStore, InMemoryRateLimitStore>();

        // Registered as concrete types because RateLimitPolicy resolves them
        // by Type at request time (see Milestone 2). Singleton is safe for
        // both built-in types since neither holds per-request state.
        services.AddSingleton<IpAddressIdentifier>();
        services.AddSingleton<FixedWindowAlgorithm>();

        return services;
    }
}
using Microsoft.AspNetCore.Http;
namespace WZ.RateLimiting.Abstractions;

/// <summary>
/// Determines the identity of the client making the request, for the
/// purpose of applying a rate limit (e.g. by IP address, user, or API key).
/// </summary>
public interface IClientIdentifier
{
    /// <summary>
    /// A short, stable name for this identifier strategy (e.g. "ip").
    /// Used as part of the storage key so different identifier types
    /// never collide with each other.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Extracts the identifying key for the current request.
    /// </summary>
    ValueTask<string> GetIdentifierAsync(HttpContext context, CancellationToken cancellationToken);
}
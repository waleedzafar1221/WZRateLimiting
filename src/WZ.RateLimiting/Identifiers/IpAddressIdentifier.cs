using Microsoft.AspNetCore.Http;
using WZ.RateLimiting.Abstractions;

namespace WZ.RateLimiting.Identifiers;

/// <summary>
/// Identifies clients by remote IP address. Relies on
/// <see cref="HttpContext.Connection"/>.RemoteIpAddress, which correctly
/// reflects the real client IP only if the app has configured
/// ForwardedHeadersMiddleware when running behind a proxy/load balancer.
/// This type deliberately does not read X-Forwarded-For directly, to avoid
/// trusting a spoofable header without proper proxy validation.
/// </summary>
public sealed class IpAddressIdentifier : IClientIdentifier
{
    public string Name => "ip";

    public ValueTask<string> GetIdentifierAsync(HttpContext context, CancellationToken cancellationToken)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return ValueTask.FromResult(ip);
    }
}
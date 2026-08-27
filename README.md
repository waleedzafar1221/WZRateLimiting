# WZ.RateLimiting

ASP.NET Core rate-limiting infrastructure library — clean abstractions, correct concurrency, no magic.

[![CI](https://github.com/waleedzafar1221/WZRateLimiting/actions/workflows/ci.yml/badge.svg)](https://github.com/waleedzafar1221/WZRateLimiting/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/WZ.RateLimiting.svg)](https://www.nuget.org/packages/WZ.RateLimiting)

## The Problem

APIs need to control how many requests a client can make within a given
window — e.g. 5 requests/minute/IP on a login endpoint — to protect against
brute force, scraping, and resource exhaustion.

WZ.RateLimiting sits in the ASP.NET Core pipeline and makes that decision:
allow the request, or reject it with `429 Too Many Requests`.

## What this library is NOT

- Authentication / authorization
- A WAF or DDoS mitigation layer
- A queueing or backpressure system

## Install

```bash
dotnet add package WZ.RateLimiting
```

## Quick start

```csharp
builder.Services.AddWzRateLimiting(options =>
{
    options.AddPolicy("login", policy =>
    {
        policy.PerIp().Limit(5).PerMinute();
    });
});

var app = builder.Build();

app.UseRouting();       // required before UseWzRateLimiting()
app.UseWzRateLimiting();
app.MapControllers();   // or app.MapGet(...), etc.
```

### Controllers

```csharp
[EnableRateLimiting("login")]
[HttpPost("login")]
public IActionResult Login(LoginRequest request) => Ok();
```

### Minimal APIs

```csharp
app.MapGet("/api/products", () => Results.Ok())
   .RequireWzRateLimiting("public-api");
```

## Architecture

HTTP Request → Middleware → Endpoint Metadata → Policy → Identifier
→
Algorithm → Store → Decision

Identifier, Algorithm, and Store are all separate abstractions
(`IClientIdentifier`, `IRateLimitAlgorithm`, `IRateLimitStore`). Swapping
one (e.g. Redis storage in V3) never requires changing the others.

## V1 scope

- Fixed window algorithm
- IP-based identifier
- In-memory storage
- Controller + minimal API support
- 429 response with `Retry-After`, `X-RateLimit-*` headers

## Roadmap

- **V2** — Sliding window, token bucket, authenticated-user/API-key identifiers
- **V3** — Redis-backed distributed storage
- **V4** — Concurrency limiting, OpenTelemetry, quotas

See [samples/](./samples) for working examples.

## Security notes

- IP identification relies on `HttpContext.Connection.RemoteIpAddress`.
  If you're behind a proxy/load balancer, configure ASP.NET Core's
  `ForwardedHeadersMiddleware` yourself — this library does not read
  `X-Forwarded-For` directly, to avoid trusting a spoofable header.

## Contributing

See [CONTRIBUTING.md](./CONTRIBUTING.md).

## License

MIT — see [LICENSE](./LICENSE).
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

## Algorithms

WZ.RateLimiting ships three interchangeable algorithms. Pick per policy —
switching algorithms never requires touching the middleware, identifiers,
or storage.

### Fixed Window (default)

Counts requests in fixed time buckets (e.g. every calendar minute). Simple
and cheap, but can allow up to ~2x the limit across a window boundary.

```csharp
policy.PerIp().UseFixedWindow().Limit(10).PerMinute();
```

### Sliding Window

Smooths out the fixed-window boundary problem by weighting the previous
window's count based on how much time-overlap remains with it. A burst
timed right at a window boundary is still limited correctly.

```csharp
policy.PerIp().UseSlideWindow().Limit(10).PerMinute();
```

### Token Bucket

Allows controlled bursts. Each policy has a bucket with a maximum capacity
that refills continuously over time. A client that's been idle can burst
up to full capacity in one go; a client that's been active is limited to
the steady refill rate.

```csharp
policy.PerIp().UseTokenBucket().Capacity(10).Refill(10).PerMinute();
```

`Capacity` sets the maximum tokens the bucket can hold. `Refill` sets how
many tokens accumulate over the configured window (e.g. `Refill(10)` with
`PerMinute()` means 10 tokens refill every 60 seconds).

**Choosing an algorithm:** all three cost well under a microsecond per
decision (see [BENCHMARKS.md](./BENCHMARKS.md)) — pick based on the
behavior you want, not performance. Use Fixed Window for simplicity,
Sliding Window when boundary bursts must never be allowed, and Token
Bucket when occasional bursts from idle clients are acceptable or
desirable.

## Identifiers

Identifiers answer "who is being rate limited?" All are interchangeable
per policy.

### IP address (default)

```csharp
policy.PerIp().Limit(5).PerMinute();
```

Uses `HttpContext.Connection.RemoteIpAddress`. See **Security notes**
below regarding proxies.

### Authenticated user

```csharp
policy.PerUser().Limit(100).PerMinute();
```

Reads the `ClaimTypes.NameIdentifier` claim from `HttpContext.User`.
Requires the app to run its own authentication middleware
(`app.UseAuthentication()`) **before** `app.UseWzRateLimiting()` in the
pipeline — WZ.RateLimiting does not parse tokens or authenticate requests
itself; it only reads claims that authentication middleware already
populated.

**Important:** if a request reaches a `.PerUser()`-protected endpoint
without being authenticated, `UserIdentifier` throws
`BadRequest` rather than silently grouping all
unauthenticated callers into one shared bucket — a shared bucket would let
one anonymous client exhaust the limit for every other anonymous caller.
Only apply `.PerUser()` to endpoints that require authentication.

### API key

```csharp
policy.PerApiKey().Limit(1000).PerMinute();
```

Reads the `X-API-Key` request header. WZ.RateLimiting does not validate
that the key is real or authorized — it only uses the header's value to
distinguish callers for rate-limiting purposes. Key validation/auth is the
consuming application's responsibility.

Same fail-fast behavior as `.PerUser()`: a request with no `X-API-Key`
header throws `BadRequest` rather than sharing a bucket
across all keyless callers.

## Architecture

```
HTTP Request → Middleware → Endpoint Metadata → Policy → Identifier
                                                    ↓
                                                Algorithm → Store → Decision
```

Identifier, Algorithm, and Store are all separate abstractions
(`IClientIdentifier`, `IRateLimitAlgorithm`, `IRateLimitStore`). Swapping
one (e.g. Redis storage in a future version) never requires changing the
others.

## Current scope (V2)

- Fixed window, sliding window, and token bucket algorithms
- IP, authenticated-user, and API-key identifiers
- In-memory storage
- Controller + minimal API support
- 429 response with `Retry-After`, `X-RateLimit-*` headers

## Roadmap

- **V3** — Redis-backed distributed storage, dynamic/runtime-configurable
  policies, custom identifier extensibility docs
- **V4** — Concurrency limiting, OpenTelemetry, quotas

See [samples/](./samples) for working examples and
[BENCHMARKS.md](./BENCHMARKS.md) for measured performance.

## Security notes

- IP identification relies on `HttpContext.Connection.RemoteIpAddress`.
  If you're behind a proxy/load balancer, configure ASP.NET Core's
  `ForwardedHeadersMiddleware` yourself — this library does not read
  `X-Forwarded-For` directly, to avoid trusting a spoofable header.
- `.PerUser()` and `.PerApiKey()` throw on requests that don't carry the
  expected claim/header, rather than silently sharing a bucket across all
  such requests. See **Identifiers** above.
- Neither identifier logs the claim value or API key it reads. If you add
  logging around rate-limit decisions in your own application, avoid
  logging raw user IDs or API keys directly.

## Contributing

See [CONTRIBUTING.md](./CONTRIBUTING.md).

## License

MIT — see [LICENSE](./LICENSE).

# WZ.RateLimiting

ASP.NET Core rate-limiting infrastructure library.

> ⚠️ Work in progress — not yet published to NuGet.

## The Problem

APIs need to control how many requests a client can make within a given
window (e.g. 5 requests/minute/IP on a login endpoint). Without this,
a single client can exhaust shared resources or be used for brute-force
or scraping attacks.

WZ.RateLimiting sits in the ASP.NET Core request pipeline and makes that
decision: allow the request, or reject it with `429 Too Many Requests`.

## What this library is NOT

- Authentication / authorization
- A WAF or DDoS mitigation layer
- A queueing or backpressure system

## Status

Currently under active initial development. See [CHANGELOG.md](./CHANGELOG.md)
for progress and the roadmap below for what's planned.

## Roadmap

- **V1** — Fixed window algorithm, IP identifier, in-memory storage, middleware, controller + minimal API support
- **V2** — Sliding window, token bucket, authenticated-user/API-key identifiers, dynamic policies
- **V3** — Redis-backed distributed storage
- **V4** — Concurrency limiting, OpenTelemetry, quotas

## License

MIT (see [LICENSE](./LICENSE))

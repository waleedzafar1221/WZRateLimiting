# Benchmarks

Performance numbers for WZ.RateLimiting, measured with [BenchmarkDotNet](https://benchmarkdotnet.org/).

> These are real, measured results — never estimated or invented. Re-run
> `dotnet run -c Release --project benchmarks/WZ.RateLimiting.Benchmarks`
> yourself to reproduce on your own hardware; results will vary by machine.

## Environment

```
BenchmarkDotNet v0.15.8, Windows 10 (10.0.19045.6456/22H2/2022Update)
Intel Core i7-8565U CPU 1.80GHz (Max: 2.00GHz) (Whiskey Lake), 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.112
  [Host]     : .NET 8.0.31 (8.0.31, 8.0.3126.42015), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 8.0.31 (8.0.31, 8.0.3126.42015), X64 RyuJIT x86-64-v3
```

Fixed Window measured: 2026-08-27. Sliding Window and Token Bucket measured: 2026-09-10
(minor .NET patch version difference between runs — 8.0.30 vs 8.0.31 — noted for
transparency; not expected to materially affect the comparison).

## Results

All benchmarks measure a single call to `EvaluateAsync` against an
`InMemoryRateLimitStore`, repeatedly hitting the same identifier key, with
`PermitLimit` (and `RefillCapacity` for Token Bucket) set high enough that
every call takes the "allowed" path — i.e. this measures the cost of making
a decision, not the cost of rejecting a request.

| Algorithm       | Mean     | Error   | StdDev  | Gen0   | Allocated |
|-----------------|---------:|--------:|--------:|-------:|----------:|
| Fixed Window    | 160.1 ns | 1.40 ns | 1.24 ns | 0.0134 |      56 B |
| Sliding Window  | 196.6 ns | 3.85 ns | 5.00 ns | 0.0134 |      56 B |
| Token Bucket    | 299.9 ns | 6.03 ns | 5.92 ns | 0.0343 |     144 B |

## Interpretation

All three algorithms complete in well under a microsecond, which is
negligible next to typical ASP.NET Core request processing time (routing,
model binding, controller execution — usually measured in microseconds to
milliseconds). The differences between algorithms are real but small in
absolute terms.

**Sliding Window vs. Fixed Window** — about 23% slower (196.6 ns vs 160.1 ns),
with identical allocation (56 B). This matches expectations: Sliding Window
does one additional store read (the previous window's count) plus some
floating-point arithmetic to compute the weighted estimate, but none of that
work allocates on the heap — it's an extra store round-trip and some stack
arithmetic, not new objects.

**Token Bucket vs. Fixed Window** — about 87% slower (299.9 ns vs 160.1 ns)
and allocates 2.5x as much (144 B vs 56 B). This is the most expensive of
the three algorithms measured so far. The extra allocation is worth
investigating further — floating-point token math and the current
store-update pattern are the likely sources, and reducing this is a
reasonable target for a future optimization pass, though at ~300 ns per
call it is still not a practical concern for typical per-endpoint rate
limiting on a web API.

**Takeaway for choosing an algorithm:** performance is not the deciding
factor between these three — all are fast enough for production use at
sub-microsecond cost. Choose based on the *behavior* you need (see the
README for what each algorithm optimizes for), not raw speed.

## What is NOT yet benchmarked

- **No-limiter baseline** — a comparison run with rate limiting entirely
  absent, to isolate the middleware's own overhead from any algorithm's
  overhead.
- **Concurrent load** — all benchmarks above are single-threaded. Behavior
  under concurrent access (the scenario the concurrency unit tests already
  prove is *correct*) has not yet been measured for *throughput*.
- **Different identifiers** — only the fixed IP-string path is measured.
  `UserIdentifier` and `ApiKeyIdentifier` add a claims/header lookup that
  hasn't been isolated and measured separately.
- **Full middleware pipeline** — these benchmarks call the algorithm
  directly, not an end-to-end HTTP request through
  `RateLimitingMiddleware` (DI resolution, endpoint metadata lookup, header
  writing all add some overhead not captured here).
- **Memory under sustained load / high cardinality** — how
  `InMemoryRateLimitStore`'s underlying dictionary behaves with many
  thousands of distinct identifier keys over time (relevant to the
  "memory growth" and "high-cardinality identifiers" security
  consideration from the project's design goals).
- **Token Bucket allocation source** — the 144 B figure above is measured,
  but not yet root-caused to a specific line of code. Worth a follow-up
  pass with a memory profiler before further optimizing.

These will be added incrementally rather than all at once, following the
same "measure, don't guess" principle as every benchmark in this file.
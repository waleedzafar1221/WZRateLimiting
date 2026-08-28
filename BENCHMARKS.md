# Benchmarks

Performance numbers for WZ.RateLimiting, measured with [BenchmarkDotNet](https://benchmarkdotnet.org/).

> These are real, measured results — never estimated or invented. Re-run
> `dotnet run -c Release --project benchmarks/WZ.RateLimiting.Benchmarks`
> yourself to reproduce on your own hardware; results will vary by machine.

## Environment

```
BenchmarkDotNet v0.15.8, Windows 10 (10.0.19045.6456/22H2/2022Update)
Intel Core i7-8565U CPU 1.80GHz (Max: 2.00GHz) (Whiskey Lake), 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.111
  [Host]     : .NET 8.0.30 (8.0.30, 8.0.3026.36720), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 8.0.30 (8.0.30, 8.0.3026.36720), X64 RyuJIT x86-64-v3
```

Measured: 2026-08-27

## Results

| Method                  | Mean     | Error   | StdDev  | Gen0   | Allocated |
|------------------------ |---------:|--------:|--------:|-------:|----------:|
| EvaluateAsync_SingleKey | 160.1 ns | 1.40 ns | 1.24 ns | 0.0134 |      56 B |

**`EvaluateAsync_SingleKey`** — a single call to `FixedWindowAlgorithm.EvaluateAsync`
against an `InMemoryRateLimitStore`, repeatedly hitting the same identifier key
(no window expiry triggered, i.e. the "hot path" of an already-existing counter).

- **~160 nanoseconds** per rate-limit decision
- **56 bytes** allocated per call
- Confidence interval: [158.702 ns; 161.495 ns] (99.9% CI)

## Interpretation

At ~160 ns per evaluation, WZ.RateLimiting's core decision logic adds
negligible overhead relative to typical ASP.NET Core request processing
times (usually measured in microseconds to milliseconds once middleware,
routing, model binding, and application logic are included).

The 56 B allocation per call is small but non-zero — primarily from the
`RateLimitDecision` struct boxing and internal Task/ValueTask machinery.
Reducing this further is a candidate optimization for a future version,
but is not a concern for V1's target use cases (per-endpoint rate limiting
on typical web APIs).

## What is NOT yet benchmarked

This is a single micro-benchmark of the hot path, not a full picture.
Missing coverage, to be added as the library grows:

- **No-limiter baseline** — a comparison run with rate limiting entirely
  absent, to isolate the middleware's own overhead from the algorithm's
  overhead.
- **Concurrent load** — this benchmark is single-threaded. Behavior under
  concurrent access (the scenario the concurrency unit tests already prove
  is *correct*) has not yet been measured for *throughput*.
- **Different identifiers** — only the fixed IP-string path is measured.
- **Different algorithms** — only Fixed Window exists in V1; Sliding
  Window and Token Bucket will need their own benchmarks in V2.
- **Full middleware pipeline** — this benchmarks the algorithm directly,
  not an end-to-end HTTP request through `RateLimitingMiddleware`
  (DI resolution, endpoint metadata lookup, header writing all add some
  overhead not captured here).
- **Memory under sustained load / high cardinality** — how the
  `ConcurrentDictionary` in `InMemoryRateLimitStore` behaves with many
  thousands of distinct identifier keys over time (relevant to the
  "memory growth" and "high-cardinality identifiers" security
  consideration from the project's design goals).

These will be added incrementally rather than all at once, following the
same "measure, don't guess" principle as this first benchmark.
using BenchmarkDotNet.Running;

BenchmarkRunner.Run<WZ.RateLimiting.Benchmarks.FixedWindowBenchmarks>();
BenchmarkRunner.Run<WZ.RateLimiting.Benchmarks.SlidingWindowBenchmarks>();
BenchmarkRunner.Run<WZ.RateLimiting.Benchmarks.TokenBucketBenchmarks>();
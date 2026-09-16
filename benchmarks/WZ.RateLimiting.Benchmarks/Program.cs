using BenchmarkDotNet.Running;

BenchmarkRunner.Run<WZ.RateLimiting.Benchmarks.FixedWindowBenchmarks>();
BenchmarkRunner.Run<WZ.RateLimiting.Benchmarks.SlidingWindowBenchmarks>();
BenchmarkRunner.Run<WZ.RateLimiting.Benchmarks.TokenBucketBenchmarks>();
BenchmarkRunner.Run<WZ.RateLimiting.Benchmarks.RedisFixedWindowBenchmarks>();
BenchmarkRunner.Run<WZ.RateLimiting.Benchmarks.RedisSlidingWindowBenchmarks>();
BenchmarkRunner.Run<WZ.RateLimiting.Benchmarks.RedisTokenBucketBenchmarks>();
using BenchmarkDotNet.Attributes;
using StackExchange.Redis;
using WZ.RateLimiting.Abstractions;
using WZ.RateLimiting.Algorithms;
using WZ.RateLimiting.Identifiers;
using WZ.RateLimiting.Policies;
using WZ.RateLimiting.Redis;

namespace WZ.RateLimiting.Benchmarks;

/// <summary>
/// Benchmarks the Fixed Window algorithm's hot path against a real Redis
/// instance, for comparison against the in-memory store's cost. Requires
/// Redis running at localhost:6379.
/// </summary>
[MemoryDiagnoser]
public class RedisFixedWindowBenchmarks
{
    private FixedWindowAlgorithm _algorithm = null!;
    private RateLimitContext _context = null!;
    private IConnectionMultiplexer _redis = null!;

    /// <summary>
    /// 
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _redis = ConnectionMultiplexer.Connect("localhost:6379");
        var store = new RedisRateLimitStore(_redis);
        _algorithm = new FixedWindowAlgorithm(store);

        var policy = RateLimitPolicy.Create(
            "bench", typeof(IpAddressIdentifier), typeof(FixedWindowAlgorithm),
            permitLimit: int.MaxValue, window: TimeSpan.FromMinutes(1));

        _context = new RateLimitContext("203.0.113.1", policy);
    }

    /// <summary>
    /// 
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        _redis.Dispose();
    }

    /// <summary>
    /// 
    /// </summary>
    [Benchmark]
    public async Task EvaluateAsync_SingleKey()
    {
        await _algorithm.EvaluateAsync(_context, CancellationToken.None);
    }
}
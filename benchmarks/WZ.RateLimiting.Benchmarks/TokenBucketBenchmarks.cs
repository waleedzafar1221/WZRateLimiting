using BenchmarkDotNet.Attributes;
using WZ.RateLimiting.Abstractions;
using WZ.RateLimiting.Algorithms;
using WZ.RateLimiting.Identifiers;
using WZ.RateLimiting.Policies;
using WZ.RateLimiting.Storage;

namespace WZ.RateLimiting.Benchmarks;

/// <summary>
/// Benchmarks the Token Bucket algorithm's hot path — a single
/// EvaluateAsync call against an already-existing bucket key.
/// </summary>
[MemoryDiagnoser]
public class TokenBucketBenchmarks
{
    private TokenBucketAlgorithm _algorithm = null!;
    private RateLimitContext _context = null!;

    /// <summary>
    /// 
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        var store = new InMemoryRateLimitStore();
        _algorithm = new TokenBucketAlgorithm(store);

        var policy = RateLimitPolicy.Create(
            "bench", typeof(IpAddressIdentifier), typeof(TokenBucketAlgorithm),
            permitLimit: int.MaxValue, window: TimeSpan.FromMinutes(1),
            refillCapacity: int.MaxValue);

        _context = new RateLimitContext("203.0.113.1", policy);
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
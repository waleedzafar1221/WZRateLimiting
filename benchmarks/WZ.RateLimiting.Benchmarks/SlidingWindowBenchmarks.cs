using BenchmarkDotNet.Attributes;
using WZ.RateLimiting.Abstractions;
using WZ.RateLimiting.Algorithms;
using WZ.RateLimiting.Identifiers;
using WZ.RateLimiting.Policies;
using WZ.RateLimiting.Storage;

namespace WZ.RateLimiting.Benchmarks;

/// <summary>
/// Benchmarks the Sliding Window algorithm's hot path — a single
/// EvaluateAsync call against an already-existing counter key.
/// </summary>
[MemoryDiagnoser]
public class SlidingWindowBenchmarks
{
    private SlidingWindowAlgorithm _algorithm = null!;
    private RateLimitContext _context = null!;

    /// <summary>
    /// 
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        var store = new InMemoryRateLimitStore();
        _algorithm = new SlidingWindowAlgorithm(store);

        var policy = RateLimitPolicy.Create(
            "bench", typeof(IpAddressIdentifier), typeof(SlidingWindowAlgorithm),
            permitLimit: int.MaxValue, window: TimeSpan.FromMinutes(1));

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
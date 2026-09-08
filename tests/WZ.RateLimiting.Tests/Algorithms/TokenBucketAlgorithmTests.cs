using WZ.RateLimiting.Abstractions;
using WZ.RateLimiting.Algorithms;
using WZ.RateLimiting.Identifiers;
using WZ.RateLimiting.Policies;
using WZ.RateLimiting.Storage;

namespace WZ.RateLimiting.Tests.Algorithms;

/// <summary>
/// 
/// </summary>
public class TokenBucketAlgorithmTests
{
    private static RateLimitContext BuildContext(
        string identifierKey, int capacity, int refillCapacity, TimeSpan window)
    {
        var policy = RateLimitPolicy.Create(
            name: "test-policy",
            identifierType: typeof(IpAddressIdentifier),
            algorithmType: typeof(TokenBucketAlgorithm),
            permitLimit: capacity,
            window: window,
            refillCapacity: refillCapacity);

        return new RateLimitContext(identifierKey, policy);
    }

    /// <summary>
    /// A brand-new client should start with a full bucket and be able to
    /// make their very first request immediately — not be rejected because
    /// no time has "elapsed" yet to refill from an empty starting point.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_FirstRequestEver_StartsWithFullBucket_IsAllowed()
    {
        var algorithm = new TokenBucketAlgorithm(new InMemoryRateLimitStore());
        var context = BuildContext("1.2.3.4", capacity: 5, refillCapacity: 1, window: TimeSpan.FromSeconds(1));

        var decision = await algorithm.EvaluateAsync(context, CancellationToken.None);

        Assert.True(decision.IsAllowed);
    }

    /// <summary>
    /// A full bucket should allow a burst up to its full capacity in one go.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_BurstUpToCapacity_AllAllowed()
    {
        var algorithm = new TokenBucketAlgorithm(new InMemoryRateLimitStore());
        var context = BuildContext("1.2.3.4", capacity: 5, refillCapacity: 1, window: TimeSpan.FromSeconds(1));

        for (var i = 0; i < 5; i++)
        {
            var decision = await algorithm.EvaluateAsync(context, CancellationToken.None);
            Assert.True(decision.IsAllowed, $"Request {i + 1} of 5 should have been allowed.");
        }
    }

    /// <summary>
    /// Once capacity is exhausted, the very next request should be rejected
    /// (with effectively no time having passed to refill anything).
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_ExhaustedBucket_NextRequestRejected()
    {
        var algorithm = new TokenBucketAlgorithm(new InMemoryRateLimitStore());
        var context = BuildContext("1.2.3.4", capacity: 3, refillCapacity: 1, window: TimeSpan.FromSeconds(1));

        for (var i = 0; i < 3; i++)
        {
            await algorithm.EvaluateAsync(context, CancellationToken.None);
        }

        var fourth = await algorithm.EvaluateAsync(context, CancellationToken.None);

        Assert.False(fourth.IsAllowed);
    }

    /// <summary>
    /// After exhausting the bucket, waiting long enough for a meaningful
    /// refill should allow a request again — this is the core behavior that
    /// distinguishes token bucket from a hard window reset.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_AfterRefillDelay_RequestAllowedAgain()
    {
        // capacity=2, refill=2 per 300ms window -> full refill in ~300ms
        var window = TimeSpan.FromMilliseconds(300);
        var algorithm = new TokenBucketAlgorithm(new InMemoryRateLimitStore());
        var context = BuildContext("1.2.3.4", capacity: 2, refillCapacity: 2, window: window);

        await algorithm.EvaluateAsync(context, CancellationToken.None);
        await algorithm.EvaluateAsync(context, CancellationToken.None);

        var exhausted = await algorithm.EvaluateAsync(context, CancellationToken.None);
        Assert.False(exhausted.IsAllowed);

        // Wait for a full window's worth of refill.
        await Task.Delay(window + TimeSpan.FromMilliseconds(50));

        var afterRefill = await algorithm.EvaluateAsync(context, CancellationToken.None);
        Assert.True(afterRefill.IsAllowed);
    }

    /// <summary>
    /// Waiting only a short fraction of the refill window should NOT be
    /// enough to earn back a full token — proves refill is proportional to
    /// elapsed time, not an all-or-nothing reset.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_PartialRefillDelay_StillRejected()
    {
        // capacity=2, refill=1 per 2-second window -> refilling even a
        // single token takes the full 2 seconds. A short delay should not
        // be enough to earn back a token.
        var window = TimeSpan.FromSeconds(2);
        var algorithm = new TokenBucketAlgorithm(new InMemoryRateLimitStore());
        var context = BuildContext("1.2.3.4", capacity: 2, refillCapacity: 1, window: window);

        await algorithm.EvaluateAsync(context, CancellationToken.None);
        await algorithm.EvaluateAsync(context, CancellationToken.None);

        var exhausted = await algorithm.EvaluateAsync(context, CancellationToken.None);
        Assert.False(exhausted.IsAllowed);

        // Wait only a small fraction of the refill window.
        await Task.Delay(TimeSpan.FromMilliseconds(100));

        var stillRejected = await algorithm.EvaluateAsync(context, CancellationToken.None);
        Assert.False(stillRejected.IsAllowed);
    }

    /// <summary>
    /// A bucket sitting idle for much longer than its refill window should
    /// still cap out at its configured capacity, not accumulate unbounded
    /// tokens.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_LongIdlePeriod_CapsAtCapacity_DoesNotOverAccumulate()
    {
        var window = TimeSpan.FromMilliseconds(100);
        var algorithm = new TokenBucketAlgorithm(new InMemoryRateLimitStore());
        var context = BuildContext("1.2.3.4", capacity: 3, refillCapacity: 3, window: window);

        // Exhaust the bucket.
        for (var i = 0; i < 3; i++)
        {
            await algorithm.EvaluateAsync(context, CancellationToken.None);
        }

        // Wait much longer than several refill windows.
        await Task.Delay(window * 10);

        // Even after a long idle period, only `capacity` (3) requests
        // should be allowed in a row — not more.
        var allowedCount = 0;
        for (var i = 0; i < 5; i++)
        {
            var decision = await algorithm.EvaluateAsync(context, CancellationToken.None);
            if (decision.IsAllowed) allowedCount++;
        }

        Assert.Equal(3, allowedCount);
    }

    /// <summary>
    /// Different identifiers must have independent buckets.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_DifferentIdentifiers_AreIndependent()
    {
        var store = new InMemoryRateLimitStore();
        var algorithm = new TokenBucketAlgorithm(store);
        var contextA = BuildContext("ipA", capacity: 1, refillCapacity: 1, window: TimeSpan.FromSeconds(1));
        var contextB = BuildContext("ipB", capacity: 1, refillCapacity: 1, window: TimeSpan.FromSeconds(1));

        var decisionA1 = await algorithm.EvaluateAsync(contextA, CancellationToken.None);
        var decisionA2 = await algorithm.EvaluateAsync(contextA, CancellationToken.None);
        var decisionB1 = await algorithm.EvaluateAsync(contextB, CancellationToken.None);

        Assert.True(decisionA1.IsAllowed);
        Assert.False(decisionA2.IsAllowed);
        Assert.True(decisionB1.IsAllowed, "A different identifier's bucket should be unaffected by ipA's exhaustion.");
    }

    /// <summary>
    /// A rejected decision should carry a sensible RetryAfter value.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_RejectedRequest_HasRetryAfterSet()
    {
        var algorithm = new TokenBucketAlgorithm(new InMemoryRateLimitStore());
        var context = BuildContext("1.2.3.4", capacity: 1, refillCapacity: 1, window: TimeSpan.FromSeconds(1));

        await algorithm.EvaluateAsync(context, CancellationToken.None);
        var rejected = await algorithm.EvaluateAsync(context, CancellationToken.None);

        Assert.False(rejected.IsAllowed);
        Assert.NotNull(rejected.RetryAfter);
        Assert.True(rejected.RetryAfter > TimeSpan.Zero);
    }
}
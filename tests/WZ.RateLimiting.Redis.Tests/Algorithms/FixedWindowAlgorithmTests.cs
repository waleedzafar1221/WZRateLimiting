using StackExchange.Redis;
using WZ.RateLimiting.Abstractions;
using WZ.RateLimiting.Algorithms;
using WZ.RateLimiting.Identifiers;
using WZ.RateLimiting.Policies;

namespace WZ.RateLimiting.Redis.Tests.Algorithms;

/// <summary>
/// 
/// </summary>
public class FixedWindowAlgorithmTests:IAsyncLifetime
{
    private IConnectionMultiplexer _redis = null!;
    private RedisRateLimitStore _store = null!;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        _redis = await ConnectionMultiplexer.ConnectAsync("localhost:6379");
        _store = new RedisRateLimitStore(_redis);
    }

    /// <inheritdoc />
    public Task DisposeAsync()
    {
        _redis.Dispose();
        return Task.CompletedTask;
    }
    private static RateLimitContext BuildContext(string identifierKey, int limit, TimeSpan window)
    {
        var policy = RateLimitPolicy.Create(
            name: $"test-policy-{Guid.NewGuid()}",
            identifierType: typeof(IpAddressIdentifier),
            algorithmType: typeof(FixedWindowAlgorithm),
            permitLimit: limit,
            window: window);

        return new RateLimitContext(identifierKey, policy);
    }

    /// <summary>
    /// 
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_UnderLimit_IsAllowed()
    {
        var algorithm = new FixedWindowAlgorithm(_store);
        var context = BuildContext("1.2.3.4", limit: 5, window: TimeSpan.FromMinutes(1));

        var decision = await algorithm.EvaluateAsync(context, CancellationToken.None);

        Assert.True(decision.IsAllowed);
        Assert.Equal(4, decision.Remaining);
    }

    /// <summary>
    /// 
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_ExactlyAtLimit_LastRequestAllowed_NextRejected()
    {
        var algorithm = new FixedWindowAlgorithm(_store);
        var context = BuildContext("1.2.3.4", limit: 3, window: TimeSpan.FromMinutes(1));

        var first = await algorithm.EvaluateAsync(context, CancellationToken.None);
        var second = await algorithm.EvaluateAsync(context, CancellationToken.None);
        var third = await algorithm.EvaluateAsync(context, CancellationToken.None);
        var fourth = await algorithm.EvaluateAsync(context, CancellationToken.None);

        Assert.True(first.IsAllowed);
        Assert.True(second.IsAllowed);
        Assert.True(third.IsAllowed);
        Assert.False(fourth.IsAllowed);
        Assert.Equal(0, fourth.Remaining);
    }

    /// <summary>
    /// 
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_DifferentIdentifiers_AreIndependent()
    {
        var algorithm = new FixedWindowAlgorithm(_store);
        var contextA = BuildContext("ipA", limit: 1, window: TimeSpan.FromMinutes(1));
        var contextB = BuildContext("ipB", limit: 1, window: TimeSpan.FromMinutes(1));

        var decisionA = await algorithm.EvaluateAsync(contextA, CancellationToken.None);
        var decisionB = await algorithm.EvaluateAsync(contextB, CancellationToken.None);

        Assert.True(decisionA.IsAllowed);
        Assert.True(decisionB.IsAllowed);
    }

    /// <summary>
    /// 
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_RejectedRequest_HasRetryAfterSet()
    {
        var algorithm = new FixedWindowAlgorithm(_store);
        var context = BuildContext("1.2.3.4", limit: 1, window: TimeSpan.FromMinutes(1));

        await algorithm.EvaluateAsync(context, CancellationToken.None);
        var rejected = await algorithm.EvaluateAsync(context, CancellationToken.None);

        Assert.False(rejected.IsAllowed);
        Assert.NotNull(rejected.RetryAfter);
        Assert.True(rejected.RetryAfter > TimeSpan.Zero);
    }
    /// <summary>
    /// 
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_100ConcurrentRequests_Limit10_AllowsExactlyTen()
    {
        var algorithm = new FixedWindowAlgorithm(_store);
        var context = BuildContext($"concurrent-{Guid.NewGuid()}", limit: 10, window: TimeSpan.FromMinutes(1));

        var tasks = Enumerable.Range(0, 100)
            .Select(_ => algorithm.EvaluateAsync(context, CancellationToken.None).AsTask());

        var decisions = await Task.WhenAll(tasks);

        var allowed = decisions.Count(d => d.IsAllowed);
        var rejected = decisions.Count(d => !d.IsAllowed);

        Assert.Equal(10, allowed);
        Assert.Equal(90, rejected);
     
    }
}
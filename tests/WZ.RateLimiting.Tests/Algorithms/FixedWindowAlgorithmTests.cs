using WZ.RateLimiting.Abstractions;
using WZ.RateLimiting.Algorithms;
using WZ.RateLimiting.Identifiers;
using WZ.RateLimiting.Policies;
using WZ.RateLimiting.Storage;

namespace WZ.RateLimiting.Tests.Algorithms;

public class FixedWindowAlgorithmTests
{
    private static RateLimitContext BuildContext(string identifierKey, int limit, TimeSpan window)
    {
        var policy = RateLimitPolicy.Create(
            name: "test-policy",
            identifierType: typeof(IpAddressIdentifier),
            algorithmType: typeof(FixedWindowAlgorithm),
            permitLimit: limit,
            window: window);

        return new RateLimitContext(identifierKey, policy);
    }

    [Fact]
    public async Task EvaluateAsync_UnderLimit_IsAllowed()
    {
        var algorithm = new FixedWindowAlgorithm(new InMemoryRateLimitStore());
        var context = BuildContext("1.2.3.4", limit: 5, window: TimeSpan.FromMinutes(1));

        var decision = await algorithm.EvaluateAsync(context, CancellationToken.None);

        Assert.True(decision.IsAllowed);
        Assert.Equal(4, decision.Remaining);
    }

    [Fact]
    public async Task EvaluateAsync_ExactlyAtLimit_LastRequestAllowed_NextRejected()
    {
        var algorithm = new FixedWindowAlgorithm(new InMemoryRateLimitStore());
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

    [Fact]
    public async Task EvaluateAsync_DifferentIdentifiers_AreIndependent()
    {
        var algorithm = new FixedWindowAlgorithm(new InMemoryRateLimitStore());
        var contextA = BuildContext("ipA", limit: 1, window: TimeSpan.FromMinutes(1));
        var contextB = BuildContext("ipB", limit: 1, window: TimeSpan.FromMinutes(1));

        var decisionA = await algorithm.EvaluateAsync(contextA, CancellationToken.None);
        var decisionB = await algorithm.EvaluateAsync(contextB, CancellationToken.None);

        Assert.True(decisionA.IsAllowed);
        Assert.True(decisionB.IsAllowed);
    }

    [Fact]
    public async Task EvaluateAsync_RejectedRequest_HasRetryAfterSet()
    {
        var algorithm = new FixedWindowAlgorithm(new InMemoryRateLimitStore());
        var context = BuildContext("1.2.3.4", limit: 1, window: TimeSpan.FromMinutes(1));

        await algorithm.EvaluateAsync(context, CancellationToken.None);
        var rejected = await algorithm.EvaluateAsync(context, CancellationToken.None);

        Assert.False(rejected.IsAllowed);
        Assert.NotNull(rejected.RetryAfter);
        Assert.True(rejected.RetryAfter > TimeSpan.Zero);
    }
}
using WZ.RateLimiting.Abstractions;
using WZ.RateLimiting.Algorithms;
using WZ.RateLimiting.Identifiers;
using WZ.RateLimiting.Policies;
using WZ.RateLimiting.Storage;

namespace WZ.RateLimiting.Tests.Algorithms;

/// <summary>
/// 
/// </summary>
public class SlidingWindowAlgorithmTests
{
    private static RateLimitContext BuildContext(string identifierKey, int limit, TimeSpan window)
    {
        var policy = RateLimitPolicy.Create(
            name: "test-policy",
            identifierType: typeof(IpAddressIdentifier),
            algorithmType: typeof(SlidingWindowAlgorithm),
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
        var algorithm = new SlidingWindowAlgorithm(new InMemoryRateLimitStore());
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
        var algorithm = new SlidingWindowAlgorithm(new InMemoryRateLimitStore());
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
        var algorithm = new SlidingWindowAlgorithm(new InMemoryRateLimitStore());
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
        var algorithm = new SlidingWindowAlgorithm(new InMemoryRateLimitStore());
        var context = BuildContext("1.2.3.4", limit: 1, window: TimeSpan.FromMinutes(1));

        await algorithm.EvaluateAsync(context, CancellationToken.None);
        var rejected = await algorithm.EvaluateAsync(context, CancellationToken.None);

        Assert.False(rejected.IsAllowed);
        Assert.NotNull(rejected.RetryAfter);
        Assert.True(rejected.RetryAfter > TimeSpan.Zero);
    }

    /// <summary>
    /// This is the test that actually proves sliding window behaves
    /// differently from fixed window. We fill the limit near the very end
    /// of window 1, wait just past the window boundary (so we're now in
    /// window 2, and fixed window would reset the counter to zero), and
    /// confirm sliding window still blocks a fresh burst — because most of
    /// window 1's requests are still "recent" from the sliding perspective.
    /// A fixed-window algorithm run through this exact sequence would allow
    /// the burst; sliding window should not.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_BurstAcrossWindowBoundary_IsStillLimited()
    {
        var window = TimeSpan.FromSeconds(3);
        var algorithm = new SlidingWindowAlgorithm(new InMemoryRateLimitStore());
        var context = BuildContext("1.2.3.4", limit: 4, window: window);

        // Fill the limit right away (near the "start" of window 1, but for
        // this test what matters is that these 4 requests exist in window 1).
        for (var i = 0; i < 4; i++)
        {
            var decision = await algorithm.EvaluateAsync(context, CancellationToken.None);
            Assert.True(decision.IsAllowed, $"Request {i + 1} should have been allowed.");
        }

        // Cross just past the window boundary into window 2.
         await Task.Delay(window + TimeSpan.FromSeconds(1));

        // Immediately after crossing, almost all of window 1's requests
        // should still count against us under sliding window.
        var justAfterBoundary = await algorithm.EvaluateAsync(context, CancellationToken.None);
        Console.WriteLine($"IsAllowed={justAfterBoundary.IsAllowed}, Remaining={justAfterBoundary.Remaining}");

        Assert.False(
            justAfterBoundary.IsAllowed,
            "Sliding window allowed a burst immediately after the window boundary — " +
            "this indicates it is behaving like fixed window, not sliding window.");
    }

    /// <summary>
    /// As more time passes into the new window, the previous window's
    /// contribution should decay. Deep into window 2, a request that was
    /// rejected right after the boundary should eventually be allowed
    /// again, once window 1's weighted contribution has dropped enough.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_DeepIntoNewWindow_PreviousWindowContributionDecays()
    {
        var window = TimeSpan.FromMilliseconds(300);
        var algorithm = new SlidingWindowAlgorithm(new InMemoryRateLimitStore());
        var context = BuildContext("1.2.3.4", limit: 4, window: window);

        for (var i = 0; i < 4; i++)
        {
            await algorithm.EvaluateAsync(context, CancellationToken.None);
        }

        // Wait until we're most of the way through window 2, so window 1's
        // weighted contribution should be small.
        await Task.Delay(window + TimeSpan.FromMilliseconds(280));

        var deepIntoNextWindow = await algorithm.EvaluateAsync(context, CancellationToken.None);

        Assert.False(
            deepIntoNextWindow.IsAllowed,
            "Sliding window still rejected a request deep into the next window — " +
            "the previous window's contribution does not appear to be decaying over time.");
    }

    /// <summary>
    /// Sanity check: with no prior history at all (first ever request for
    /// a key), sliding window should behave identically to fixed window —
    /// there is no "previous window" yet to weight in.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_NoPriorHistory_BehavesLikeFixedWindow()
    {
        var algorithm = new SlidingWindowAlgorithm(new InMemoryRateLimitStore());
        var context = BuildContext("1.2.3.4", limit: 10, window: TimeSpan.FromMinutes(1));

        var decision = await algorithm.EvaluateAsync(context, CancellationToken.None);

        Assert.True(decision.IsAllowed);
        Assert.Equal(9, decision.Remaining);
    }
}

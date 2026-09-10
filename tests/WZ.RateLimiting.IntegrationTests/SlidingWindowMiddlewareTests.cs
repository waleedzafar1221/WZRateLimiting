using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WZ.RateLimiting.Extensions;

namespace WZ.RateLimiting.IntegrationTests;

/// <summary>
/// 
/// </summary>
public class SlidingWindowMiddlewareTests
{
    private static async Task<TestServer> CreateServerAsync(int limit, TimeSpan window)
    {
        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddWzRateLimiting(options =>
                    {
                        options.AddPolicy("sliding-test", policy =>
                        {
                            // TODO: your job — chain .PerIp(), .UseSlideWindow(),
                            policy.UseSlideWindow().Limit(limit).Window(window);
                        });
                    });
                });
                webHost.Configure(app =>
                {
                    app.UseRouting();
                    app.UseWzRateLimiting();
                    app.UseEndpoints(endpoints =>
                    {
                        // TODO: map "/" GET, require "sliding-test" policy
                        // (watch the name — this is the exact bug we just fixed
                        // in the Token Bucket test, don't repeat it)
                        endpoints.MapGet("/", () => "OK").RequireWzRateLimiting("sliding-test");
                    });
                });
            });

        var host = await hostBuilder.StartAsync();
        return host.GetTestServer();
    }

    /// <summary>
    /// 
    /// </summary>
    [Fact]
    public async Task RequestsUpToLimit_ThroughFullPipeline_AllAllowed()
    {
        // TODO: create server with limit=3, window=1 minute
        using var server = await CreateServerAsync(3, TimeSpan.FromMinutes(1));
        using var client = server.CreateClient();
        
        // TODO: send exactly 3 requests, assert ALL return 200 OK
        // (keep this test's name and behavior aligned — don't test rejection here)
        var response1=await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        
        response1=await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        
        response1 = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
    }

    /// <summary>
    /// 
    /// </summary>
    [Fact]
    public async Task RequestBeyondLimit_ThroughFullPipeline_Returns429WithRetryAfter()
    {
        // TODO: create server with limit=1
        using var server = await CreateServerAsync(1, TimeSpan.FromMinutes(1));
        using var client = server.CreateClient();
        // TODO: send 1 request (assert OK), then a 2nd (assert 429)
        var response1=await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        
        response1=await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.TooManyRequests, response1.StatusCode);
        
        // TODO: assert the 2nd response has a Retry-After header
        Assert.True(response1.Headers.Contains("Retry-After"));
    }

    /// <summary>
    /// 
    /// </summary>
    [Fact]
    public async Task BurstAcrossWindowBoundary_ThroughFullPipeline_StillLimited()
    {
        var window = TimeSpan.FromMilliseconds(300);
        using var server = await CreateServerAsync(4, window);
        using var client = server.CreateClient();

        // Fill the limit within window 1.
        for (var i = 0; i < 4; i++)
        {
            var response = await client.GetAsync("/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // Cross just barely past the window boundary — not a full window later.
        await Task.Delay(window + TimeSpan.FromMilliseconds(30));

        // Immediately after crossing, sliding window should still largely
        // count window 1's requests against us.
        var justAfterBoundary = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.TooManyRequests, justAfterBoundary.StatusCode);
    }
}
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
public class TokenBucketMiddlewareTests
{
    private static async Task<TestServer> CreateServerAsync(int capacity, int refillCapacity, TimeSpan window)
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
                        options.AddPolicy("test", policy =>
                        { 
                            policy.PerIp().UseTokenBucket().Capacity(capacity).Refill(refillCapacity).Window(window);
                        });
                    });
                });
                webHost.Configure(app =>
                {
                    app.UseRouting();
                    app.UseWzRateLimiting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/", () => "OK").RequireWzRateLimiting("test");
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
    public async Task BurstUpToCapacity_ThroughFullPipeline_AllAllowed()
    {
        // TODO: create server with capacity=3, refillCapacity=1, window=1 minute
        using var server=await CreateServerAsync(3, 1, TimeSpan.FromMinutes(1));
        // TODO: send 3 requests, assert all return 200 OK
        using var client=server.CreateClient();
        await client.GetAsync("/");
        await client.GetAsync("/");
        var response1 = await client.GetAsync("/");
        
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
       
    }

    /// <summary>
    /// 
    /// </summary>
    [Fact]
    public async Task RequestBeyondCapacity_ThroughFullPipeline_Returns429WithRetryAfter()
    {
        // TODO: create server with capacity=1
        using var server=await CreateServerAsync(1, 1, TimeSpan.FromSeconds(1));
        using var client=server.CreateClient();
        // TODO: send 1 request (should succeed), then a 2nd (should be 429)
        var response1 = await client.GetAsync("/");
        var response2 = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, response2.StatusCode);
        Assert.True(response2.Headers.Contains("Retry-After"));
        
        // TODO: assert the 2nd response has a Retry-After header
        await Task.Delay(1000);
        var response3 = await client.GetAsync("/");
        // (hint: look at RejectedRequest_HasRetryAfterHeader above for the pattern)
        Assert.Equal(HttpStatusCode.OK, response3.StatusCode);
    }
}
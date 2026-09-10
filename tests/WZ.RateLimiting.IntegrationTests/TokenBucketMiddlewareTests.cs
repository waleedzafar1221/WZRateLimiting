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
        var window = TimeSpan.FromSeconds(1);
        using var server = await CreateServerAsync(1, 1, window);
        using var client = server.CreateClient();

        var response1 = await client.GetAsync("/");
        var response2 = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, response2.StatusCode);
        Assert.True(response2.Headers.Contains("Retry-After"));

        // Wait comfortably longer than a full window so CI's scheduling
        // jitter can't flip this — a 1:1 delay-to-window ratio is a race.
        await Task.Delay(window + TimeSpan.FromSeconds(1));

        var response3 = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response3.StatusCode);
    }
}
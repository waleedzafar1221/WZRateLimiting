using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WZ.RateLimiting.Extensions;

namespace WZ.RateLimiting.IntegrationTests;

public class RateLimitingMiddlewareTests
{
    private static async Task<TestServer> CreateServerAsync(int limit, TimeSpan window)
    {
        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureServices(services =>
                {
                    services.AddWzRateLimiting(options =>
                    {
                        options.AddPolicy("test", policy =>
                        {
                            policy.PerIp().Limit(limit).Window(window);
                        });
                    });
                });
                webHost.Configure(app =>
                {
                    app.UseWzRateLimiting();
                    app.Run(async context => await context.Response.WriteAsync("OK"));
                });
            });

        var host = await hostBuilder.StartAsync();
        return host.GetTestServer();
    }

    [Fact]
    public async Task Request_UnderLimit_Returns200()
    {
        using var server = await CreateServerAsync(limit: 5, window: TimeSpan.FromMinutes(1));
        using var client = server.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Request_OverLimit_Returns429()
    {
        using var server = await CreateServerAsync(limit: 2, window: TimeSpan.FromMinutes(1));
        using var client = server.CreateClient();

        await client.GetAsync("/");
        await client.GetAsync("/");
        var third = await client.GetAsync("/");

        Assert.Equal((HttpStatusCode)429, third.StatusCode);
    }

    [Fact]
    public async Task RejectedRequest_HasRetryAfterHeader()
    {
        using var server = await CreateServerAsync(limit: 1, window: TimeSpan.FromMinutes(1));
        using var client = server.CreateClient();

        await client.GetAsync("/");
        var rejected = await client.GetAsync("/");

        Assert.True(rejected.Headers.Contains("Retry-After"));
    }

    [Fact]
    public async Task AllowedRequest_HasRateLimitHeaders()
    {
        using var server = await CreateServerAsync(limit: 5, window: TimeSpan.FromMinutes(1));
        using var client = server.CreateClient();

        var response = await client.GetAsync("/");

        Assert.True(response.Headers.Contains("X-RateLimit-Limit"));
        Assert.True(response.Headers.Contains("X-RateLimit-Remaining"));
        Assert.True(response.Headers.Contains("X-RateLimit-Reset"));
    }
}
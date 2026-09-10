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
                    services.AddRouting();
                    services.AddWzRateLimiting(options =>
                    {
                        options.AddPolicy("test", policy =>
                        {
                            policy.PerIp().Limit(limit).Window(window);
                        });
                        options.AddPolicy("test-user-identifier", policy =>
                        {
                            policy.PerUser().Limit(limit).Window(window);
                        });
                        options.AddPolicy("test-api-key", policy =>
                        {
                            policy.PerApiKey().Limit(limit).Window(window);
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
                        endpoints.MapGet("/user", () => "OK").RequireWzRateLimiting("test-user-identifier");
                        endpoints.MapGet("/apikey", () => "OK").RequireWzRateLimiting("test-api-key");
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
    public async Task Request_UnderLimit_Returns200()
    {
        using var server = await CreateServerAsync(limit: 5, window: TimeSpan.FromMinutes(1));
        using var client = server.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// 
    /// </summary>
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

    /// <summary>
    /// 
    /// </summary>
    [Fact]
    public async Task RejectedRequest_HasRetryAfterHeader()
    {
        using var server = await CreateServerAsync(limit: 1, window: TimeSpan.FromMinutes(1));
        using var client = server.CreateClient();

        await client.GetAsync("/");
        var rejected = await client.GetAsync("/");

        Assert.True(rejected.Headers.Contains("Retry-After"));
    }

    /// <summary>
    /// 
    /// </summary>
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
    
    /// <summary>
    /// 
    /// </summary>
    [Fact]
    public async Task RejectRequest_NotHas_User_Claim()
    {
        using var server = await CreateServerAsync(2, TimeSpan.FromMinutes(1));
        using var client = server.CreateClient();
        
        var response = await client.GetAsync("/user");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// 
    /// </summary>
    [Fact]
    public async Task RejectRequest_NotHas_Api_Key()
    {
        using var server = await CreateServerAsync(2, TimeSpan.FromMinutes(1));
        using var client = server.CreateClient();
        var response = await client.GetAsync("/apikey");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
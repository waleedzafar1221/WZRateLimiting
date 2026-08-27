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
public class EndpointPolicyResolutionTests
{
    private static async Task<TestServer> CreateServerAsync()
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
                        options.AddPolicy("strict", p => p.PerIp().Limit(1).Window(TimeSpan.FromMinutes(1)));
                        options.AddPolicy("relaxed", p => p.PerIp().Limit(100).Window(TimeSpan.FromMinutes(1)));
                    });
                });
                webHost.Configure(app =>
                {
                    app.UseRouting();
                    app.UseWzRateLimiting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/strict", () => "strict-ok").RequireWzRateLimiting("strict");
                        endpoints.MapGet("/relaxed", () => "relaxed-ok").RequireWzRateLimiting("relaxed");
                        endpoints.MapGet("/unlimited", () => "unlimited-ok");
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
    public async Task EndpointWithoutMetadata_IsNeverRateLimited()
    {
        using var server = await CreateServerAsync();
        using var client = server.CreateClient();

        for (var i = 0; i < 10; i++)
        {
            var response = await client.GetAsync("/unlimited");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [Fact]
    public async Task DifferentEndpoints_UseDifferentPolicies()
    {
        using var server = await CreateServerAsync();
        using var client = server.CreateClient();

        var strictFirst = await client.GetAsync("/strict");
        var strictSecond = await client.GetAsync("/strict");

        Assert.Equal(HttpStatusCode.OK, strictFirst.StatusCode);
        Assert.Equal((HttpStatusCode)429, strictSecond.StatusCode);

        // Relaxed policy is unaffected by strict's limit being hit.
        var relaxed = await client.GetAsync("/relaxed");
        Assert.Equal(HttpStatusCode.OK, relaxed.StatusCode);
    }

    /// <summary>
    /// 
    /// </summary>
    [Fact]
    public async Task UnregisteredPolicyName_Throws()
    {
        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddWzRateLimiting(); // no policies registered
                });
                webHost.Configure(app =>
                {
                    app.UseRouting();
                    app.UseWzRateLimiting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/missing", () => "ok").RequireWzRateLimiting("does-not-exist");
                    });
                });
            });

        using var host = await hostBuilder.StartAsync();
        using var client = host.GetTestServer().CreateClient();

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync("/missing"));
    }
}
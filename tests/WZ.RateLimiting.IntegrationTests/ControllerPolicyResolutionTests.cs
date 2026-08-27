using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WZ.RateLimiting.Attributes;
using WZ.RateLimiting.Extensions;

namespace WZ.RateLimiting.IntegrationTests;

[ApiController]
[Route("api")]
public class TestController : ControllerBase
{
    [EnableRateLimiting("controller-policy")]
    [HttpGet("login")]
    public IActionResult Login() => Ok("login-ok");
}

public class ControllerPolicyResolutionTests
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
                    services.AddControllers()
                        .AddApplicationPart(typeof(ControllerPolicyResolutionTests).Assembly);

                    services.AddWzRateLimiting(options =>
                    {
                        options.AddPolicy("controller-policy", p =>
                            p.PerIp().Limit(1).Window(TimeSpan.FromMinutes(1)));
                    });
                });
                webHost.Configure(app =>
                {
                    app.UseRouting();
                    app.UseWzRateLimiting();
                    app.UseEndpoints(endpoints => endpoints.MapControllers());
                });
            });

        var host = await hostBuilder.StartAsync();
        return host.GetTestServer();
    }

    [Fact]
    public async Task ControllerAction_WithAttribute_IsRateLimited()
    {
        using var server = await CreateServerAsync();
        using var client = server.CreateClient();

        var first = await client.GetAsync("/api/login");
        var second = await client.GetAsync("/api/login");

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal((HttpStatusCode)429, second.StatusCode);
    }
}
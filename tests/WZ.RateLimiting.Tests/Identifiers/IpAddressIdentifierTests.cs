using System.Net;
using Microsoft.AspNetCore.Http;
using WZ.RateLimiting.Identifiers;

namespace WZ.RateLimiting.Tests.Identifiers;

public class IpAddressIdentifierTests
{
    [Fact]
    public async Task GetIdentifierAsync_ReturnsRemoteIpAddress()
    {
        var identifier = new IpAddressIdentifier();
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.5");

        var result = await identifier.GetIdentifierAsync(context, CancellationToken.None);

        Assert.Equal("203.0.113.5", result);
    }

    [Fact]
    public async Task GetIdentifierAsync_NoRemoteIp_ReturnsUnknown()
    {
        var identifier = new IpAddressIdentifier();
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = null;

        var result = await identifier.GetIdentifierAsync(context, CancellationToken.None);

        Assert.Equal("unknown", result);
    }
}
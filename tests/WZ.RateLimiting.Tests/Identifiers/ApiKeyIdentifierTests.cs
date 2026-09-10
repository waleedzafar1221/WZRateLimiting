using Microsoft.AspNetCore.Http;
using WZ.RateLimiting.Identifiers;

namespace WZ.RateLimiting.Tests.Identifiers;

/// <summary>
/// 
/// </summary>
public class ApiKeyIdentifierTests
{
    /// <summary>
    /// 
    /// </summary>
    [Fact]
    public async Task GetIdentifierAsync_HeaderPresent_ReturnsValue()
    {
        // Arrange
        var identifier = new ApiKeyIdentifier();
        var context = new DefaultHttpContext();
        context.Request.Headers["X-API-Key"] = "abc123";

        // Act
        var result = await identifier.GetIdentifierAsync(context,CancellationToken.None);

        // Assert
        Assert.Equal("abc123", result);
    }

    /// <summary>
    /// 
    /// </summary>
    [Fact]
    public async Task GetIdentifierAsync_HeaderMissing_Throws()
    {
        // Arrange
        var identifier = new ApiKeyIdentifier();
        var context = new DefaultHttpContext();

        // Act & Assert
        var  result = await identifier.GetIdentifierAsync(context,CancellationToken.None);
        Assert.Equal("", result);
    }
}
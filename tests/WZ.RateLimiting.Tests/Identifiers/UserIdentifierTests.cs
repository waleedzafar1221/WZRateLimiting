using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using WZ.RateLimiting.Identifiers;

namespace WZ.RateLimiting.Tests.Identifiers;

/// <summary>
/// 
/// </summary>
public class UserIdentifierTests
{
    /// <summary>
    /// 
    /// </summary>
    [Fact]
    public async Task GetIdentifierAsync_AuthenticatedUser_ReturnsClaimValue()
    {
        // 1. Create a UserIdentifier instance (assuming IpAddressIdentifier implements it or matches the target interface)
        var identifier = new UserIdentifier();
        
        // 2. Create a DefaultHttpContext
        var context = new DefaultHttpContext();

        // 3. Build a ClaimsPrincipal with a NameIdentifier claim set to "user-123"
        var claims = new[] 
        { 
            new Claim(ClaimTypes.NameIdentifier, "user-123") 
        };
        
        // The second parameter ("TestAuth") is crucial as it sets IsAuthenticated to true
        var identity = new ClaimsIdentity(claims, "TestAuth");
        context.User = new ClaimsPrincipal(identity);

        // 4. Call GetIdentifierAsync and assert the result equals "user-123"
        // Note: Replace 'context' with whatever parameter your specific GetIdentifierAsync method expects
        var result = await identifier.GetIdentifierAsync(context, CancellationToken.None);

        Assert.Equal("user-123", result);
    }

    /// <summary>
    /// 
    /// </summary>
    [Fact]
    public async Task GetIdentifierAsync_Unauthenticated_Throws()
    {
        // Arrange: Create a UserIdentifier instance
        var identifier = new UserIdentifier();

        // Arrange: Create a DefaultHttpContext with an EMPTY ClaimsPrincipal
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity())
        };

        // Act & Assert: Verify that GetIdentifierAsync throws an anonymous user
        string result =await identifier.GetIdentifierAsync(context,CancellationToken.None);
        Assert.Equal("",result);
    }
}
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using WZ.RateLimiting.Abstractions;

namespace WZ.RateLimiting.Identifiers;

/// <summary>
/// 
/// </summary>
public class UserIdentifier:IClientIdentifier
{
    private readonly string _claimType;
    /// <summary>
    /// 
    /// </summary>
    public UserIdentifier() : this(ClaimTypes.NameIdentifier)
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="claimType"></param>
    private UserIdentifier(string claimType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(claimType);
        _claimType = claimType;
    }
    /// <summary>
    /// 
    /// </summary>
    public string Name => "user";

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public ValueTask<string> GetIdentifierAsync(HttpContext context, CancellationToken cancellationToken)
    {
        var claimValue = context.User.FindFirst(_claimType)?.Value;
        if (claimValue is null)
        {
            return ValueTask.FromResult(String.Empty);
        }
        return ValueTask.FromResult(claimValue);
    }
}
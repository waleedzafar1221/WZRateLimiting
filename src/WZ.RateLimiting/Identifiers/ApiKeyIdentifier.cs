using Microsoft.AspNetCore.Http;
using WZ.RateLimiting.Abstractions;

namespace WZ.RateLimiting.Identifiers;

/// <summary>
/// 
/// </summary>
public class ApiKeyIdentifier:IClientIdentifier
{
    private readonly string _headerName;

    /// <summary>
    /// 
    /// </summary>
    public ApiKeyIdentifier() : this("X-API-Key")
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="headerName"></param>
    public ApiKeyIdentifier(string headerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(headerName);
        _headerName = headerName;
    }
    /// <summary>
    /// 
    /// </summary>
    public string Name => "api-key";

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public ValueTask<string> GetIdentifierAsync(HttpContext context, CancellationToken cancellationToken)
    {
        if (!context.Request.Headers.TryGetValue(_headerName, out var values) ||
            string.IsNullOrEmpty(values.ToString()))
        {
            return ValueTask.FromResult(string.Empty);
        }

        return ValueTask.FromResult(values.ToString());
    }
}
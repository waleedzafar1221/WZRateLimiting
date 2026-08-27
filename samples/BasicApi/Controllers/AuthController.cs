using Microsoft.AspNetCore.Mvc;
using WZ.RateLimiting.Attributes;

namespace BasicApi.Controllers;

/// <summary>
/// 
/// </summary> 
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    [EnableRateLimiting("login")]
    [HttpPost("login")]
    public IActionResult Login() => Ok(new { message = "Login successful" });
}
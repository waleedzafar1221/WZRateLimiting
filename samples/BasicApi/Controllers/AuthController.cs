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
    [EnableRateLimiting("fixed-public-api")]
    [HttpPost("login-fixed")]
    public IActionResult LoginFixed() => Ok(new { message = "Login successful" });
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    [EnableRateLimiting("slide-public-api")]
    [HttpPost("login-Slide")]
    public IActionResult LoginSlide() => Ok(new { message = "Login successful" });
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    [EnableRateLimiting("bucket-public-api")]
    [HttpPost("login-Bucket")]
    public IActionResult LoginBucket() => Ok(new { message = "Login successful" });
   
}
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContosoBank.Web.Controllers;

/// <summary>
/// Security-related endpoints for CSRF token management and security headers
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SecurityController : ControllerBase
{
    private readonly IAntiforgery _antiforgery;

    public SecurityController(IAntiforgery antiforgery)
    {
        _antiforgery = antiforgery;
    }

    /// <summary>
    /// Get CSRF token for client-side use
    /// </summary>
    /// <returns>CSRF token</returns>
    [HttpGet("csrf-token")]
    public IActionResult GetCsrfToken()
    {
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
        
        return Ok(new { 
            token = tokens.RequestToken,
            headerName = "X-CSRF-TOKEN"
        });
    }

    /// <summary>
    /// Validate CSRF token (for testing purposes)
    /// </summary>
    /// <returns>Validation result</returns>
    [HttpPost("validate-csrf")]
    [ValidateAntiForgeryToken]
    public IActionResult ValidateCsrfToken()
    {
        return Ok(new { message = "CSRF token is valid" });
    }

    /// <summary>
    /// Get security headers information
    /// </summary>
    /// <returns>Security headers status</returns>
    [HttpGet("headers")]
    public IActionResult GetSecurityHeaders()
    {
        var headers = new
        {
            isHttps = Request.IsHttps,
            scheme = Request.Scheme,
            protocol = Request.Protocol,
            headers = new
            {
                xFrameOptions = Response.Headers.ContainsKey("X-Frame-Options"),
                xContentTypeOptions = Response.Headers.ContainsKey("X-Content-Type-Options"),
                xXssProtection = Response.Headers.ContainsKey("X-XSS-Protection"),
                referrerPolicy = Response.Headers.ContainsKey("Referrer-Policy"),
                contentSecurityPolicy = Response.Headers.ContainsKey("Content-Security-Policy")
            }
        };

        return Ok(headers);
    }
}

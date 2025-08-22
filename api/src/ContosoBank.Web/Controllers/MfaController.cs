using ContosoBank.Application.DTOs;
using ContosoBank.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ContosoBank.Web.Controllers;

/// <summary>
/// Controller for multi-factor authentication operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MfaController : ControllerBase
{
    private readonly IMfaService _mfaService;
    private readonly ILogger<MfaController> _logger;

    public MfaController(IMfaService mfaService, ILogger<MfaController> logger)
    {
        _mfaService = mfaService;
        _logger = logger;
    }

    /// <summary>
    /// Sends an MFA code to the user
    /// </summary>
    /// <param name="request">MFA request containing email and method</param>
    /// <returns>MFA response with session information</returns>
    [HttpPost("send-code")]
    public async Task<ActionResult<MfaResponseDto>> SendCode([FromBody] MfaRequestDto request)
    {
        try
        {
            var clientIpAddress = GetClientIpAddress();
            var userAgent = Request.Headers["User-Agent"].ToString();

            var result = await _mfaService.SendMfaCodeAsync(request, clientIpAddress, userAgent);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized MFA code request: {Message}", ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid MFA code request: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("MFA code request failed: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending MFA code");
            return StatusCode(500, new { message = "Erro interno do servidor. Tente novamente mais tarde." });
        }
    }

    /// <summary>
    /// Verifies an MFA code provided by the user
    /// </summary>
    /// <param name="request">MFA verification request</param>
    /// <returns>Verification response with authentication result</returns>
    [HttpPost("verify-code")]
    public async Task<ActionResult<MfaVerificationResponseDto>> VerifyCode([FromBody] MfaVerificationDto request)
    {
        try
        {
            var clientIpAddress = GetClientIpAddress();
            var userAgent = Request.Headers["User-Agent"].ToString();

            var result = await _mfaService.VerifyMfaCodeAsync(request, clientIpAddress, userAgent);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized MFA verification: {Message}", ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid MFA verification request: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("MFA verification failed: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying MFA code");
            return StatusCode(500, new { message = "Erro interno do servidor. Tente novamente mais tarde." });
        }
    }

    /// <summary>
    /// Resends an MFA code
    /// </summary>
    /// <param name="sessionId">MFA session identifier</param>
    /// <returns>MFA response with updated session information</returns>
    [HttpPost("resend-code/{sessionId}")]
    public async Task<ActionResult<MfaResponseDto>> ResendCode(string sessionId)
    {
        try
        {
            var clientIpAddress = GetClientIpAddress();
            var userAgent = Request.Headers["User-Agent"].ToString();

            var result = await _mfaService.ResendMfaCodeAsync(sessionId, clientIpAddress, userAgent);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized MFA resend request: {Message}", ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid MFA resend request: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("MFA resend failed: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resending MFA code");
            return StatusCode(500, new { message = "Erro interno do servidor. Tente novamente mais tarde." });
        }
    }

    /// <summary>
    /// Checks if an MFA session is valid
    /// </summary>
    /// <param name="sessionId">MFA session identifier</param>
    /// <returns>Session validity status</returns>
    [HttpGet("session/{sessionId}/status")]
    public async Task<ActionResult<object>> GetSessionStatus(string sessionId)
    {
        try
        {
            var isValid = await _mfaService.IsSessionValidAsync(sessionId);
            return Ok(new { isValid, sessionId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking MFA session status");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }

    /// <summary>
    /// Gets the client IP address from the request
    /// </summary>
    /// <returns>Client IP address</returns>
    private string GetClientIpAddress()
    {
        var ipAddress = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (string.IsNullOrEmpty(ipAddress))
        {
            ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
        }
        return ipAddress ?? "unknown";
    }
}

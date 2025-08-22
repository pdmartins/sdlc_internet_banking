using ContosoBank.Application.DTOs;
using ContosoBank.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ContosoBank.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PasswordResetController : ControllerBase
{
    private readonly IPasswordResetService _passwordResetService;
    private readonly ILogger<PasswordResetController> _logger;

    public PasswordResetController(
        IPasswordResetService passwordResetService,
        ILogger<PasswordResetController> logger)
    {
        _passwordResetService = passwordResetService;
        _logger = logger;
    }

    [HttpPost("request")]
    public async Task<ActionResult<PasswordResetResponseDto>> RequestPasswordReset([FromBody] PasswordResetRequestDto request)
    {
        try
        {
            var clientIpAddress = GetClientIpAddress();
            var userAgent = GetUserAgent();

            var result = await _passwordResetService.RequestPasswordResetAsync(request, clientIpAddress, userAgent);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new PasswordResetResponseDto { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error requesting password reset");
            return StatusCode(500, new PasswordResetResponseDto 
            { 
                Success = false, 
                Message = "Erro interno do servidor. Tente novamente mais tarde." 
            });
        }
    }

    [HttpGet("validate/{token}")]
    public async Task<ActionResult<PasswordResetResponseDto>> ValidateResetToken(string token)
    {
        try
        {
            var result = await _passwordResetService.ValidateResetTokenAsync(token);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new PasswordResetResponseDto { Success = false, Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new PasswordResetResponseDto { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error validating reset token");
            return StatusCode(500, new PasswordResetResponseDto 
            { 
                Success = false, 
                Message = "Erro interno do servidor. Tente novamente mais tarde." 
            });
        }
    }

    [HttpPost("reset")]
    public async Task<ActionResult<PasswordResetResponseDto>> ResetPassword([FromBody] PasswordResetDto request)
    {
        try
        {
            var clientIpAddress = GetClientIpAddress();
            var userAgent = GetUserAgent();

            var result = await _passwordResetService.ResetPasswordAsync(request, clientIpAddress, userAgent);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new PasswordResetResponseDto { Success = false, Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new PasswordResetResponseDto { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error resetting password");
            return StatusCode(500, new PasswordResetResponseDto 
            { 
                Success = false, 
                Message = "Erro interno do servidor. Tente novamente mais tarde." 
            });
        }
    }

    private string GetClientIpAddress()
    {
        // Check for forwarded IP first (when behind proxy/load balancer)
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // Fallback to connection remote IP
        return Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private string GetUserAgent()
    {
        return Request.Headers["User-Agent"].FirstOrDefault() ?? "unknown";
    }
}

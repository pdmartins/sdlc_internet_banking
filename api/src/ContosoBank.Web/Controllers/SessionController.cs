using ContosoBank.Application.DTOs;
using ContosoBank.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ContosoBank.Web.Controllers;

/// <summary>
/// Controller for session management and automatic logout functionality
/// Implements User Story 2.3.1: Automatic logout after inactivity and logout all devices
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SessionController : ControllerBase
{
    private readonly ISessionService _sessionService;
    private readonly ILogger<SessionController> _logger;

    public SessionController(
        ISessionService sessionService,
        ILogger<SessionController> logger)
    {
        _sessionService = sessionService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all active sessions for the current user
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <returns>List of active sessions</returns>
    [HttpGet("active/{userId}")]
    public async Task<ActionResult<IEnumerable<SessionDto>>> GetActiveSessions(Guid userId)
    {
        try
        {
            var sessions = await _sessionService.GetActiveSessionsAsync(userId);
            var currentSessionToken = GetSessionTokenFromHeader();

            var sessionDtos = sessions.Select(s => new SessionDto
            {
                Id = s.Id,
                DeviceInfo = ExtractDeviceInfo(s.UserAgent),
                Location = s.Location,
                IpAddress = s.IpAddress,
                CreatedAt = s.CreatedAt,
                LastActivityAt = s.LastActivityAt,
                IsTrustedDevice = s.IsTrustedDevice,
                IsCurrentSession = s.SessionToken == currentSessionToken
            });

            return Ok(sessionDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active sessions for user {UserId}", userId);
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }

    /// <summary>
    /// Validates the current session and checks for inactivity timeout
    /// </summary>
    /// <returns>Session validation status</returns>
    [HttpPost("validate")]
    public async Task<ActionResult<SessionValidationDto>> ValidateSession()
    {
        try
        {
            var sessionToken = GetSessionTokenFromHeader();
            
            if (string.IsNullOrEmpty(sessionToken))
            {
                return Ok(new SessionValidationDto 
                { 
                    IsValid = false, 
                    Message = "Token de sessão não encontrado" 
                });
            }

            var session = await _sessionService.ValidateSessionAsync(sessionToken);
            
            if (session == null)
            {
                return Ok(new SessionValidationDto 
                { 
                    IsValid = false, 
                    IsExpired = true,
                    Message = "Sessão expirada ou inválida" 
                });
            }

            // Update session activity
            await _sessionService.UpdateSessionActivityAsync(sessionToken);

            // Calculate minutes until timeout
            var lastActivity = session.LastActivityAt ?? session.CreatedAt;
            var elapsedMinutes = (DateTime.UtcNow - lastActivity).TotalMinutes;
            var minutesUntilTimeout = Math.Max(0, session.InactivityTimeoutMinutes - (int)elapsedMinutes);

            return Ok(new SessionValidationDto
            {
                IsValid = true,
                IsExpired = false,
                IsInactive = false,
                MinutesUntilTimeout = minutesUntilTimeout,
                Message = "Sessão válida"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating session");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }

    /// <summary>
    /// Updates session activity timestamp (heartbeat)
    /// </summary>
    /// <returns>Success status</returns>
    [HttpPost("heartbeat")]
    public async Task<ActionResult> UpdateActivity()
    {
        try
        {
            var sessionToken = GetSessionTokenFromHeader();
            
            if (string.IsNullOrEmpty(sessionToken))
            {
                return BadRequest(new { message = "Token de sessão não encontrado" });
            }

            var updated = await _sessionService.UpdateSessionActivityAsync(sessionToken);
            
            if (!updated)
            {
                return NotFound(new { message = "Sessão não encontrada" });
            }

            return Ok(new { message = "Atividade atualizada", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating session activity");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }

    /// <summary>
    /// Logs out the current session
    /// </summary>
    /// <returns>Logout confirmation</returns>
    [HttpPost("logout")]
    public async Task<ActionResult> Logout()
    {
        try
        {
            var sessionToken = GetSessionTokenFromHeader();
            
            if (string.IsNullOrEmpty(sessionToken))
            {
                return Ok(new { message = "Nenhuma sessão ativa encontrada" });
            }

            var revoked = await _sessionService.RevokeSessionAsync(sessionToken, "User logout");
            
            return Ok(new { 
                message = revoked ? "Logout realizado com sucesso" : "Sessão já estava inativa",
                success = true 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }

    /// <summary>
    /// Logs out from all devices except the current one
    /// Implements the "logout all devices" feature from User Story 2.3.1
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="request">Logout confirmation request</param>
    /// <returns>Number of sessions revoked</returns>
    [HttpPost("logout-all-devices/{userId}")]
    public async Task<ActionResult<LogoutAllDevicesResponseDto>> LogoutAllDevices(
        Guid userId, 
        [FromBody] LogoutAllDevicesRequestDto request)
    {
        try
        {
            if (!request.ConfirmLogoutAll)
            {
                return BadRequest(new LogoutAllDevicesResponseDto
                {
                    Success = false,
                    Message = "Confirmação necessária para fazer logout de todos os dispositivos"
                });
            }

            var currentSessionToken = GetSessionTokenFromHeader();
            var revokedCount = await _sessionService.RevokeAllOtherSessionsAsync(userId, currentSessionToken);

            _logger.LogInformation("User {UserId} logged out from {Count} other devices", userId, revokedCount);

            return Ok(new LogoutAllDevicesResponseDto
            {
                Success = true,
                SessionsRevoked = revokedCount,
                Message = revokedCount > 0 
                    ? $"Logout realizado em {revokedCount} dispositivo(s)"
                    : "Nenhuma sessão adicional encontrada"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout all devices for user {UserId}", userId);
            return StatusCode(500, new LogoutAllDevicesResponseDto
            {
                Success = false,
                Message = "Erro interno do servidor."
            });
        }
    }

    /// <summary>
    /// Revokes a specific session by ID
    /// </summary>
    /// <param name="sessionId">Session identifier to revoke</param>
    /// <returns>Revocation status</returns>
    [HttpDelete("{sessionId}")]
    public async Task<ActionResult> RevokeSession(Guid sessionId)
    {
        try
        {
            // Note: This would require getting the session by ID first, then revoking by token
            // For now, we'll return a placeholder response
            return Ok(new { message = "Funcionalidade será implementada", sessionId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking session {SessionId}", sessionId);
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }

    /// <summary>
    /// Gets session token from Authorization header or session header
    /// </summary>
    /// <returns>Session token if found</returns>
    private string? GetSessionTokenFromHeader()
    {
        // Try Authorization header first (Bearer token)
        var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            return authHeader.Substring("Bearer ".Length).Trim();
        }

        // Try custom session header
        var sessionHeader = HttpContext.Request.Headers["X-Session-Token"].FirstOrDefault();
        if (!string.IsNullOrEmpty(sessionHeader))
        {
            return sessionHeader;
        }

        return null;
    }

    /// <summary>
    /// Extracts device information from User-Agent string
    /// </summary>
    /// <param name="userAgent">User agent string</param>
    /// <returns>Simplified device info</returns>
    private string ExtractDeviceInfo(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return "Dispositivo desconhecido";

        // Simple device detection
        if (userAgent.Contains("Mobile") || userAgent.Contains("Android") || userAgent.Contains("iPhone"))
            return "Dispositivo móvel";
        
        if (userAgent.Contains("Tablet") || userAgent.Contains("iPad"))
            return "Tablet";
        
        if (userAgent.Contains("Windows"))
            return "Windows PC";
        
        if (userAgent.Contains("Mac"))
            return "Mac";
        
        if (userAgent.Contains("Linux"))
            return "Linux PC";

        return "Navegador web";
    }
}

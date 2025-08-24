using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ContosoBank.Application.DTOs;
using ContosoBank.Application.Interfaces;
using System.Security.Claims;

namespace ContosoBank.Web.Controllers;

/// <summary>
/// Controller for user profile management operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
// TODO: Implement proper authentication scheme before enabling [Authorize]
// [Authorize]
public class UserProfileController : ControllerBase
{
    private readonly IUserProfileService _userProfileService;
    private readonly ISessionService _sessionService;
    private readonly ILogger<UserProfileController> _logger;

    /// <summary>
    /// Initializes a new instance of the UserProfileController
    /// </summary>
    /// <param name="userProfileService">User profile service</param>
    /// <param name="sessionService">Session service for authentication</param>
    /// <param name="logger">Logger instance</param>
    public UserProfileController(
        IUserProfileService userProfileService,
        ISessionService sessionService,
        ILogger<UserProfileController> logger)
    {
        _userProfileService = userProfileService;
        _sessionService = sessionService;
        _logger = logger;
    }

    /// <summary>
    /// Get user profile information
    /// </summary>
    /// <returns>User profile data</returns>
    [HttpGet]
    public async Task<ActionResult<UserProfileDto>> GetProfile()
    {
        try
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == Guid.Empty)
            {
                return Unauthorized("Invalid user session");
            }

            var profile = await _userProfileService.GetUserProfileAsync(userId);
            
            if (profile == null)
            {
                return NotFound("User profile not found");
            }

            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user profile");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update user profile information
    /// </summary>
    /// <param name="request">Profile update request</param>
    /// <returns>Updated profile data</returns>
    [HttpPut]
    public async Task<ActionResult<UserProfileDto>> UpdateProfile([FromBody] UpdateProfileRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = await GetCurrentUserIdAsync();
            if (userId == Guid.Empty)
            {
                return Unauthorized("Invalid user session");
            }
            var updatedProfile = await _userProfileService.UpdateProfileAsync(userId, request);
            
            return Ok(updatedProfile);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized profile update attempt");
            return Unauthorized(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid profile update data");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update user password
    /// </summary>
    /// <param name="request">Password update request</param>
    /// <returns>Success status</returns>
    [HttpPut("password")]
    public async Task<ActionResult> UpdatePassword([FromBody] UpdatePasswordRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = await GetCurrentUserIdAsync();
            if (userId == Guid.Empty)
            {
                return Unauthorized("Invalid user session");
            }
            var success = await _userProfileService.UpdatePasswordAsync(userId, request);
            
            if (success)
            {
                return Ok(new { message = "Password updated successfully" });
            }

            return BadRequest("Failed to update password");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized password update attempt");
            return Unauthorized(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid password update data");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating password");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update security question and answer
    /// </summary>
    /// <param name="request">Security question update request</param>
    /// <returns>Success status</returns>
    [HttpPut("security-question")]
    public async Task<ActionResult> UpdateSecurityQuestion([FromBody] UpdateSecurityQuestionRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = await GetCurrentUserIdAsync();
            if (userId == Guid.Empty)
            {
                return Unauthorized("Invalid user session");
            }
            var success = await _userProfileService.UpdateSecurityQuestionAsync(userId, request);
            
            if (success)
            {
                return Ok(new { message = "Security question updated successfully" });
            }

            return BadRequest("Failed to update security question");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized security question update attempt");
            return Unauthorized(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid security question data");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating security question");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update MFA option
    /// </summary>
    /// <param name="request">MFA update request</param>
    /// <returns>Success status</returns>
    [HttpPut("mfa")]
    public async Task<ActionResult> UpdateMfaOption([FromBody] UpdateMfaRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = await GetCurrentUserIdAsync();
            if (userId == Guid.Empty)
            {
                return Unauthorized("Invalid user session");
            }
            var success = await _userProfileService.UpdateMfaOptionAsync(userId, request);
            
            if (success)
            {
                return Ok(new { message = "MFA option updated successfully" });
            }

            return BadRequest("Failed to update MFA option");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized MFA update attempt");
            return Unauthorized(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid MFA option data");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating MFA option");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get security settings
    /// </summary>
    /// <returns>Security settings</returns>
    [HttpGet("security-settings")]
    public async Task<ActionResult<SecuritySettingsDto>> GetSecuritySettings()
    {
        try
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == Guid.Empty)
            {
                return Unauthorized("Invalid user session");
            }
            var settings = await _userProfileService.GetSecuritySettingsAsync(userId);
            
            if (settings == null)
            {
                return NotFound("Security settings not found");
            }

            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving security settings");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get active devices/sessions
    /// </summary>
    /// <returns>List of active devices</returns>
    [HttpGet("devices")]
    public async Task<ActionResult<IEnumerable<DeviceInfoDto>>> GetActiveDevices()
    {
        try
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == Guid.Empty)
            {
                return Unauthorized("Invalid user session");
            }
            var devices = await _userProfileService.GetActiveDevicesAsync(userId);
            
            return Ok(devices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active devices");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Revoke a specific device/session
    /// </summary>
    /// <param name="deviceId">Device ID to revoke</param>
    /// <returns>Success status</returns>
    [HttpDelete("devices/{deviceId}")]
    public async Task<ActionResult> RevokeDevice(Guid deviceId)
    {
        try
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == Guid.Empty)
            {
                return Unauthorized("Invalid user session");
            }
            var success = await _userProfileService.RevokeDeviceAsync(userId, deviceId);
            
            if (success)
            {
                return Ok(new { message = "Device revoked successfully" });
            }

            return NotFound("Device not found or already revoked");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking device");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Revoke all other devices/sessions except current
    /// </summary>
    /// <returns>Number of revoked sessions</returns>
    [HttpPost("revoke-all-devices")]
    public async Task<ActionResult> RevokeAllOtherDevices()
    {
        try
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == Guid.Empty)
            {
                return Unauthorized("Invalid user session");
            }
            var currentSessionId = GetCurrentSessionId();
            
            var revokedCount = await _userProfileService.RevokeAllOtherDevicesAsync(userId, currentSessionId);
            
            return Ok(new { message = $"Revoked {revokedCount} devices successfully", revokedCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking all other devices");
            return StatusCode(500, "Internal server error");
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Gets the current user ID from the session token
    /// </summary>
    /// <returns>User ID or Guid.Empty if not found or invalid session</returns>
    private async Task<Guid> GetCurrentUserIdAsync()
    {
        try
        {
            var sessionToken = GetSessionTokenFromHeader();
            if (string.IsNullOrEmpty(sessionToken))
            {
                return Guid.Empty;
            }

            var session = await _sessionService.ValidateSessionAsync(sessionToken);
            return session?.UserId ?? Guid.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating session token");
            return Guid.Empty;
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
        _logger.LogInformation("Auth header: {AuthHeader}", authHeader ?? "null");
        
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            var token = authHeader.Substring("Bearer ".Length).Trim();
            _logger.LogInformation("Extracted token: {TokenPreview}...", token.Length > 20 ? token.Substring(0, 20) : token);
            return token;
        }

        // Try custom session header
        var sessionHeader = HttpContext.Request.Headers["X-Session-Token"].FirstOrDefault();
        if (!string.IsNullOrEmpty(sessionHeader))
        {
            _logger.LogInformation("Using X-Session-Token: {TokenPreview}...", sessionHeader.Length > 20 ? sessionHeader.Substring(0, 20) : sessionHeader);
            return sessionHeader;
        }

        _logger.LogWarning("No session token found in headers");
        return null;
    }

    private Guid GetCurrentSessionId()
    {
        // This would need to be extracted from the current session context
        // For now, return a new GUID - this should be improved in production
        var sessionIdClaim = User.FindFirst("SessionId")?.Value;
        if (string.IsNullOrEmpty(sessionIdClaim) || !Guid.TryParse(sessionIdClaim, out var sessionId))
        {
            // If no session ID in claims, we'll need to look it up or create a new one
            return Guid.NewGuid();
        }
        return sessionId;
    }

    #endregion
}

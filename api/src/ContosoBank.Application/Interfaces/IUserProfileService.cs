using ContosoBank.Application.DTOs;

namespace ContosoBank.Application.Interfaces;

public interface IUserProfileService
{
    /// <summary>
    /// Get user profile information by user ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>User profile data</returns>
    Task<UserProfileDto?> GetUserProfileAsync(Guid userId);

    /// <summary>
    /// Update user profile information
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="request">Profile update request</param>
    /// <returns>Updated profile data</returns>
    Task<UserProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileRequestDto request);

    /// <summary>
    /// Update user password
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="request">Password update request</param>
    /// <returns>Success status</returns>
    Task<bool> UpdatePasswordAsync(Guid userId, UpdatePasswordRequestDto request);

    /// <summary>
    /// Update security question and answer
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="request">Security question update request</param>
    /// <returns>Success status</returns>
    Task<bool> UpdateSecurityQuestionAsync(Guid userId, UpdateSecurityQuestionRequestDto request);

    /// <summary>
    /// Update MFA option
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="request">MFA update request</param>
    /// <returns>Success status</returns>
    Task<bool> UpdateMfaOptionAsync(Guid userId, UpdateMfaRequestDto request);

    /// <summary>
    /// Get security settings for user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Security settings</returns>
    Task<SecuritySettingsDto?> GetSecuritySettingsAsync(Guid userId);

    /// <summary>
    /// Get active devices/sessions for user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of active devices/sessions</returns>
    Task<IEnumerable<DeviceInfoDto>> GetActiveDevicesAsync(Guid userId);

    /// <summary>
    /// Revoke a specific device/session
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="deviceId">Device/Session ID to revoke</param>
    /// <returns>Success status</returns>
    Task<bool> RevokeDeviceAsync(Guid userId, Guid deviceId);

    /// <summary>
    /// Revoke all devices/sessions except current
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="currentSessionId">Current session ID to keep active</param>
    /// <returns>Number of revoked sessions</returns>
    Task<int> RevokeAllOtherDevicesAsync(Guid userId, Guid currentSessionId);
}

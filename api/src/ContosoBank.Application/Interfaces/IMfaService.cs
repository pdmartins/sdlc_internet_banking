using ContosoBank.Application.DTOs;

namespace ContosoBank.Application.Interfaces;

/// <summary>
/// Service interface for multi-factor authentication operations
/// </summary>
public interface IMfaService
{
    /// <summary>
    /// Sends an MFA code to the user via their preferred method
    /// </summary>
    /// <param name="request">MFA request containing user email and method</param>
    /// <param name="clientIpAddress">Client IP address for security logging</param>
    /// <param name="userAgent">Client user agent for security logging</param>
    /// <returns>MFA response with session information</returns>
    Task<MfaResponseDto> SendMfaCodeAsync(MfaRequestDto request, string clientIpAddress, string userAgent);

    /// <summary>
    /// Verifies an MFA code provided by the user
    /// </summary>
    /// <param name="request">MFA verification request containing email, code, and session ID</param>
    /// <param name="clientIpAddress">Client IP address for security logging</param>
    /// <param name="userAgent">Client user agent for security logging</param>
    /// <returns>Verification response with authentication result</returns>
    Task<MfaVerificationResponseDto> VerifyMfaCodeAsync(MfaVerificationDto request, string clientIpAddress, string userAgent);

    /// <summary>
    /// Resends an MFA code if allowed
    /// </summary>
    /// <param name="sessionId">MFA session identifier</param>
    /// <param name="clientIpAddress">Client IP address for security logging</param>
    /// <param name="userAgent">Client user agent for security logging</param>
    /// <returns>MFA response with updated session information</returns>
    Task<MfaResponseDto> ResendMfaCodeAsync(string sessionId, string clientIpAddress, string userAgent);

    /// <summary>
    /// Cleans up expired MFA sessions
    /// </summary>
    Task CleanupExpiredSessionsAsync();

    /// <summary>
    /// Checks if an MFA session is valid and not expired
    /// </summary>
    /// <param name="sessionId">MFA session identifier</param>
    /// <returns>True if session is valid</returns>
    Task<bool> IsSessionValidAsync(string sessionId);
}

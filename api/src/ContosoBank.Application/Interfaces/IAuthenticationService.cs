using ContosoBank.Application.DTOs;

namespace ContosoBank.Application.Interfaces;

/// <summary>
/// Service interface for authentication operations
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticates a user with email and password
    /// </summary>
    /// <param name="request">Login request data</param>
    /// <param name="clientIpAddress">Client IP address for security logging</param>
    /// <returns>Login response with user information</returns>
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request, string clientIpAddress);

    /// <summary>
    /// Validates user credentials without performing full login
    /// </summary>
    /// <param name="email">User's email</param>
    /// <param name="password">User's password</param>
    /// <returns>True if credentials are valid</returns>
    Task<bool> ValidateCredentialsAsync(string email, string password);

    /// <summary>
    /// Records a successful login event
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="clientIpAddress">Client IP address</param>
    /// <param name="userAgent">Client user agent</param>
    Task RecordLoginSuccessAsync(Guid userId, string clientIpAddress, string userAgent);

    /// <summary>
    /// Records a failed login attempt
    /// </summary>
    /// <param name="email">Email that was attempted</param>
    /// <param name="clientIpAddress">Client IP address</param>
    /// <param name="failureReason">Reason for failure</param>
    Task RecordLoginFailureAsync(string email, string clientIpAddress, string failureReason);

    /// <summary>
    /// Checks if account is locked due to failed attempts
    /// </summary>
    /// <param name="email">User's email</param>
    /// <returns>True if account is locked</returns>
    Task<bool> IsAccountLockedAsync(string email);
}

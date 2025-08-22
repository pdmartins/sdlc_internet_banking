using ContosoBank.Domain.Entities;

namespace ContosoBank.Application.Interfaces;

/// <summary>
/// Service interface for managing user sessions and implementing inactivity timeout
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// Creates a new user session with specified timeout
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="userAgent">Client user agent</param>
    /// <param name="deviceFingerprint">Device fingerprint for fraud detection</param>
    /// <param name="location">Approximate location</param>
    /// <param name="inactivityTimeoutMinutes">Session inactivity timeout in minutes</param>
    /// <returns>Session token</returns>
    Task<string> CreateSessionAsync(Guid userId, string ipAddress, string userAgent, 
        string? deviceFingerprint = null, string? location = null, int inactivityTimeoutMinutes = 30);
    
    /// <summary>
    /// Validates a session token and checks for inactivity timeout
    /// </summary>
    /// <param name="sessionToken">Session token</param>
    /// <returns>Session if valid and active</returns>
    Task<UserSession?> ValidateSessionAsync(string sessionToken);
    
    /// <summary>
    /// Updates the last activity timestamp for a session
    /// </summary>
    /// <param name="sessionToken">Session token</param>
    /// <returns>True if session was found and updated</returns>
    Task<bool> UpdateSessionActivityAsync(string sessionToken);
    
    /// <summary>
    /// Revokes a specific session
    /// </summary>
    /// <param name="sessionToken">Session token to revoke</param>
    /// <param name="reason">Reason for revocation</param>
    /// <returns>True if session was found and revoked</returns>
    Task<bool> RevokeSessionAsync(string sessionToken, string reason = "User logout");
    
    /// <summary>
    /// Revokes all sessions for a user except the current one
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="currentSessionToken">Current session to keep active</param>
    /// <returns>Number of sessions revoked</returns>
    Task<int> RevokeAllOtherSessionsAsync(Guid userId, string? currentSessionToken = null);
    
    /// <summary>
    /// Gets all active sessions for a user
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <returns>List of active sessions</returns>
    Task<IEnumerable<UserSession>> GetActiveSessionsAsync(Guid userId);
    
    /// <summary>
    /// Cleans up expired and inactive sessions
    /// </summary>
    /// <returns>Number of sessions cleaned up</returns>
    Task<int> CleanupExpiredSessionsAsync();
    
    /// <summary>
    /// Checks for suspicious session activity (multiple devices, unusual locations)
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <returns>True if suspicious activity is detected</returns>
    Task<bool> DetectSuspiciousActivityAsync(Guid userId);
    
    /// <summary>
    /// Generates a secure session token
    /// </summary>
    /// <returns>Secure session token</returns>
    string GenerateSessionToken();
}

using ContosoBank.Domain.Entities;

namespace ContosoBank.Domain.Interfaces;

/// <summary>
/// Repository interface for user session management
/// </summary>
public interface IUserSessionRepository : IRepository<UserSession>
{
    /// <summary>
    /// Gets all active sessions for a specific user
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <returns>List of active sessions</returns>
    Task<IEnumerable<UserSession>> GetActiveSessionsForUserAsync(Guid userId);
    
    /// <summary>
    /// Gets a session by token
    /// </summary>
    /// <param name="sessionToken">Session token</param>
    /// <returns>Session if found and active</returns>
    Task<UserSession?> GetByTokenAsync(string sessionToken);
    
    /// <summary>
    /// Gets expired sessions that need cleanup
    /// </summary>
    /// <returns>List of expired sessions</returns>
    Task<IEnumerable<UserSession>> GetExpiredSessionsAsync();
    
    /// <summary>
    /// Gets inactive sessions based on last activity
    /// </summary>
    /// <returns>List of inactive sessions</returns>
    Task<IEnumerable<UserSession>> GetInactiveSessionsAsync();
    
    /// <summary>
    /// Revokes all sessions for a user except the current one
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="currentSessionId">Current session to keep active</param>
    /// <returns>Number of sessions revoked</returns>
    Task<int> RevokeAllOtherSessionsAsync(Guid userId, Guid? currentSessionId = null);
    
    /// <summary>
    /// Updates the last activity timestamp for a session
    /// </summary>
    /// <param name="sessionToken">Session token</param>
    /// <returns>True if session was found and updated</returns>
    Task<bool> UpdateLastActivityAsync(string sessionToken);
    
    /// <summary>
    /// Gets sessions by device fingerprint for fraud detection
    /// </summary>
    /// <param name="deviceFingerprint">Device fingerprint</param>
    /// <returns>List of sessions from the same device</returns>
    Task<IEnumerable<UserSession>> GetSessionsByDeviceFingerprintAsync(string deviceFingerprint);
}

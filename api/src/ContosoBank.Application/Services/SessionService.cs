using System.Security.Cryptography;
using System.Text;
using ContosoBank.Application.Interfaces;
using ContosoBank.Domain.Entities;
using ContosoBank.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ContosoBank.Application.Services;

/// <summary>
/// Service for managing user sessions and implementing automatic logout functionality
/// </summary>
public class SessionService : ISessionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SessionService> _logger;
    private readonly IConfiguration _configuration;
    private readonly int _defaultSessionTimeoutHours;
    private readonly int _defaultInactivityTimeoutMinutes;

    public SessionService(
        IUnitOfWork unitOfWork,
        ILogger<SessionService> logger,
        IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _configuration = configuration;
        _defaultSessionTimeoutHours = _configuration.GetValue<int>("Session:DefaultTimeoutHours", 8);
        _defaultInactivityTimeoutMinutes = _configuration.GetValue<int>("Session:DefaultInactivityTimeoutMinutes", 30);
    }

    public async Task<string> CreateSessionAsync(Guid userId, string ipAddress, string userAgent,
        string? deviceFingerprint = null, string? location = null, int inactivityTimeoutMinutes = 30)
    {
        try
        {
            // Generate secure session token
            var sessionToken = GenerateSessionToken();
            
            // Create new session
            var session = new UserSession
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SessionToken = sessionToken,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                DeviceFingerprint = deviceFingerprint ?? string.Empty,
                Location = location ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(_defaultSessionTimeoutHours),
                LastActivityAt = DateTime.UtcNow,
                IsActive = true,
                IsRevoked = false,
                IsTrustedDevice = false,
                InactivityTimeoutMinutes = inactivityTimeoutMinutes
            };

            await _unitOfWork.UserSessions.AddAsync(session);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Created new session for user {UserId} from IP {IpAddress}", 
                userId, ipAddress);

            return sessionToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating session for user {UserId}", userId);
            throw;
        }
    }

    public async Task<UserSession?> ValidateSessionAsync(string sessionToken)
    {
        try
        {
            var session = await _unitOfWork.UserSessions.GetByTokenAsync(sessionToken);
            
            if (session == null)
            {
                _logger.LogWarning("Session validation failed - session not found for token");
                return null;
            }

            // Check if session is expired
            if (session.ExpiresAt <= DateTime.UtcNow)
            {
                _logger.LogInformation("Session {SessionId} expired at {ExpiresAt}", 
                    session.Id, session.ExpiresAt);
                await RevokeSessionAsync(sessionToken, "Session expired");
                return null;
            }

            // Check for inactivity timeout
            var lastActivity = session.LastActivityAt ?? session.CreatedAt;
            var inactiveMinutes = (DateTime.UtcNow - lastActivity).TotalMinutes;
            
            if (inactiveMinutes > session.InactivityTimeoutMinutes)
            {
                _logger.LogInformation("Session {SessionId} timed out after {Minutes} minutes of inactivity", 
                    session.Id, inactiveMinutes);
                await RevokeSessionAsync(sessionToken, "Inactivity timeout");
                return null;
            }

            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating session");
            return null;
        }
    }

    public async Task<bool> UpdateSessionActivityAsync(string sessionToken)
    {
        try
        {
            return await _unitOfWork.UserSessions.UpdateLastActivityAsync(sessionToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating session activity for token");
            return false;
        }
    }

    public async Task<bool> RevokeSessionAsync(string sessionToken, string reason = "User logout")
    {
        try
        {
            var session = await _unitOfWork.UserSessions.GetByTokenAsync(sessionToken);
            
            if (session == null)
                return false;

            session.IsRevoked = true;
            session.IsActive = false;
            session.RevokedAt = DateTime.UtcNow;
            session.RevokedReason = reason;

            _unitOfWork.UserSessions.Update(session);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Revoked session {SessionId} for user {UserId}: {Reason}", 
                session.Id, session.UserId, reason);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking session");
            return false;
        }
    }

    public async Task<int> RevokeAllOtherSessionsAsync(Guid userId, string? currentSessionToken = null)
    {
        try
        {
            Guid? currentSessionId = null;
            
            if (!string.IsNullOrEmpty(currentSessionToken))
            {
                var currentSession = await _unitOfWork.UserSessions.GetByTokenAsync(currentSessionToken);
                currentSessionId = currentSession?.Id;
            }

            var revokedCount = await _unitOfWork.UserSessions.RevokeAllOtherSessionsAsync(userId, currentSessionId);
            
            _logger.LogInformation("Revoked {Count} sessions for user {UserId}", revokedCount, userId);
            
            return revokedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking all other sessions for user {UserId}", userId);
            return 0;
        }
    }

    public async Task<IEnumerable<UserSession>> GetActiveSessionsAsync(Guid userId)
    {
        try
        {
            return await _unitOfWork.UserSessions.GetActiveSessionsForUserAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active sessions for user {UserId}", userId);
            return Enumerable.Empty<UserSession>();
        }
    }

    public async Task<int> CleanupExpiredSessionsAsync()
    {
        try
        {
            // Clean up expired sessions
            var expiredSessions = await _unitOfWork.UserSessions.GetExpiredSessionsAsync();
            var cleanupCount = 0;

            foreach (var session in expiredSessions)
            {
                session.IsActive = false;
                session.IsRevoked = true;
                session.RevokedAt ??= DateTime.UtcNow;
                session.RevokedReason ??= "Session expired";
                cleanupCount++;
            }

            // Clean up inactive sessions
            var inactiveSessions = await _unitOfWork.UserSessions.GetInactiveSessionsAsync();
            
            foreach (var session in inactiveSessions)
            {
                session.IsActive = false;
                session.IsRevoked = true;
                session.RevokedAt = DateTime.UtcNow;
                session.RevokedReason = "Inactivity timeout";
                cleanupCount++;
            }

            if (cleanupCount > 0)
            {
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Cleaned up {Count} expired/inactive sessions", cleanupCount);
            }

            return cleanupCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired sessions");
            return 0;
        }
    }

    public async Task<bool> DetectSuspiciousActivityAsync(Guid userId)
    {
        try
        {
            var activeSessions = await GetActiveSessionsAsync(userId);
            var sessionList = activeSessions.ToList();

            // Check for multiple concurrent sessions from different locations
            var uniqueLocations = sessionList
                .Where(s => !string.IsNullOrEmpty(s.Location))
                .Select(s => s.Location)
                .Distinct()
                .Count();

            // Check for multiple devices
            var uniqueDevices = sessionList
                .Where(s => !string.IsNullOrEmpty(s.DeviceFingerprint))
                .Select(s => s.DeviceFingerprint)
                .Distinct()
                .Count();

            // Flag as suspicious if user has active sessions from:
            // - More than 3 different locations
            // - More than 5 different devices
            // - Sessions created within short time span from different IPs
            var isSuspicious = uniqueLocations > 3 || uniqueDevices > 5;

            if (!isSuspicious)
            {
                // Check for rapid session creation from different IPs
                var recentSessions = sessionList
                    .Where(s => s.CreatedAt > DateTime.UtcNow.AddHours(-1))
                    .GroupBy(s => s.IpAddress)
                    .Count();

                isSuspicious = recentSessions > 3;
            }

            if (isSuspicious)
            {
                _logger.LogWarning("Suspicious activity detected for user {UserId}: " +
                    "{UniqueLocations} locations, {UniqueDevices} devices", 
                    userId, uniqueLocations, uniqueDevices);

                // Log security event
                var securityEvent = new SecurityEvent
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    EventType = "SUSPICIOUS_ACTIVITY",
                    Description = $"Multiple concurrent sessions detected: {uniqueLocations} locations, {uniqueDevices} devices",
                    IpAddress = "Multiple",
                    UserAgent = "Multiple",
                    CreatedAt = DateTime.UtcNow,
                    Severity = "High"
                };

                await _unitOfWork.SecurityEvents.AddAsync(securityEvent);
                await _unitOfWork.SaveChangesAsync();
            }

            return isSuspicious;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting suspicious activity for user {UserId}", userId);
            return false;
        }
    }

    public string GenerateSessionToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        
        // Combine with timestamp for uniqueness
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var combined = Encoding.UTF8.GetBytes(timestamp.ToString()).Concat(bytes).ToArray();
        
        return Convert.ToBase64String(combined).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }
}

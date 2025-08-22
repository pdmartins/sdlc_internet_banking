using ContosoBank.Domain.Entities;
using ContosoBank.Domain.Interfaces;
using ContosoBank.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ContosoBank.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for user session management
/// </summary>
public class UserSessionRepository : Repository<UserSession>, IUserSessionRepository
{
    public UserSessionRepository(ContosoBankDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<UserSession>> GetActiveSessionsForUserAsync(Guid userId)
    {
        return await _context.UserSessions
            .Where(s => s.UserId == userId && 
                       s.IsActive && 
                       !s.IsRevoked && 
                       s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.LastActivityAt ?? s.CreatedAt)
            .ToListAsync();
    }

    public async Task<UserSession?> GetByTokenAsync(string sessionToken)
    {
        return await _context.UserSessions
            .Where(s => s.SessionToken == sessionToken && 
                       s.IsActive && 
                       !s.IsRevoked && 
                       s.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<UserSession>> GetExpiredSessionsAsync()
    {
        return await _context.UserSessions
            .Where(s => s.ExpiresAt <= DateTime.UtcNow && s.IsActive)
            .ToListAsync();
    }

    public async Task<IEnumerable<UserSession>> GetInactiveSessionsAsync()
    {
        var cutoffTime = DateTime.UtcNow;
        
        return await _context.UserSessions
            .Where(s => s.IsActive && 
                       !s.IsRevoked &&
                       s.ExpiresAt > cutoffTime &&
                       ((s.LastActivityAt != null && 
                         EF.Functions.DateDiffMinute(s.LastActivityAt, cutoffTime) > s.InactivityTimeoutMinutes) ||
                        (s.LastActivityAt == null && 
                         EF.Functions.DateDiffMinute(s.CreatedAt, cutoffTime) > s.InactivityTimeoutMinutes)))
            .ToListAsync();
    }

    public async Task<int> RevokeAllOtherSessionsAsync(Guid userId, Guid? currentSessionId = null)
    {
        var sessionsQuery = _context.UserSessions
            .Where(s => s.UserId == userId && s.IsActive && !s.IsRevoked);
            
        if (currentSessionId.HasValue)
        {
            sessionsQuery = sessionsQuery.Where(s => s.Id != currentSessionId.Value);
        }

        var sessionsToRevoke = await sessionsQuery.ToListAsync();

        foreach (var session in sessionsToRevoke)
        {
            session.IsRevoked = true;
            session.IsActive = false;
            session.RevokedAt = DateTime.UtcNow;
            session.RevokedReason = "User requested logout from all devices";
        }

        await _context.SaveChangesAsync();
        return sessionsToRevoke.Count;
    }

    public async Task<bool> UpdateLastActivityAsync(string sessionToken)
    {
        var session = await _context.UserSessions
            .Where(s => s.SessionToken == sessionToken && s.IsActive && !s.IsRevoked)
            .FirstOrDefaultAsync();

        if (session == null)
            return false;

        session.LastActivityAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<UserSession>> GetSessionsByDeviceFingerprintAsync(string deviceFingerprint)
    {
        return await _context.UserSessions
            .Where(s => s.DeviceFingerprint == deviceFingerprint &&
                       s.IsActive &&
                       !s.IsRevoked &&
                       s.ExpiresAt > DateTime.UtcNow)
            .Include(s => s.User)
            .OrderByDescending(s => s.LastActivityAt ?? s.CreatedAt)
            .ToListAsync();
    }
}

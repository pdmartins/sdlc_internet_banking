using ContosoBank.Domain.Entities;
using ContosoBank.Domain.Interfaces;
using ContosoBank.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ContosoBank.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for security alert operations
/// </summary>
public class SecurityAlertRepository : Repository<SecurityAlert>, ISecurityAlertRepository
{
    public SecurityAlertRepository(ContosoBankDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<SecurityAlert>> GetByUserIdAsync(Guid userId, bool unreadOnly = false, int limit = 50)
    {
        var query = _context.SecurityAlerts
            .Include(sa => sa.User)
            .Include(sa => sa.AnomalyDetection)
                .ThenInclude(ad => ad.LoginAttempt)
            .Where(sa => sa.UserId == userId);

        if (unreadOnly)
        {
            query = query.Where(sa => !sa.IsRead);
        }

        return await query
            .OrderByDescending(sa => sa.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<SecurityAlert>> GetUndeliveredAlertsAsync()
    {
        return await _context.SecurityAlerts
            .Include(sa => sa.User)
            .Include(sa => sa.AnomalyDetection)
                .ThenInclude(ad => ad.LoginAttempt)
            .Where(sa => sa.Status == "Pending" || sa.Status == "Failed")
            .OrderByDescending(sa => sa.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<SecurityAlert>> GetExpiredAlertsAsync()
    {
        var now = DateTime.UtcNow;
        return await _context.SecurityAlerts
            .Include(sa => sa.User)
            .Where(sa => sa.ExpiresAt < now && sa.Status != "Expired")
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountByUserIdAsync(Guid userId)
    {
        return await _context.SecurityAlerts
            .CountAsync(sa => sa.UserId == userId && !sa.IsRead);
    }

    public async Task MarkAsReadAsync(Guid alertId, Guid userId)
    {
        var alert = await _context.SecurityAlerts
            .FirstOrDefaultAsync(sa => sa.Id == alertId && sa.UserId == userId);
        
        if (alert != null && !alert.IsRead)
        {
            alert.IsRead = true;
            alert.ReadAt = DateTime.UtcNow;
            _context.SecurityAlerts.Update(alert);
        }
    }

    public async Task MarkActionTakenAsync(Guid alertId, Guid userId)
    {
        var alert = await _context.SecurityAlerts
            .FirstOrDefaultAsync(sa => sa.Id == alertId && sa.UserId == userId);
        
        if (alert != null)
        {
            alert.ActionTakenAt = DateTime.UtcNow;
            if (!alert.IsRead)
            {
                alert.IsRead = true;
                alert.ReadAt = DateTime.UtcNow;
            }
            _context.SecurityAlerts.Update(alert);
        }
    }

    public async Task<IEnumerable<SecurityAlert>> GetBySeverityAsync(string severity, DateTime from, DateTime to)
    {
        return await _context.SecurityAlerts
            .Include(sa => sa.User)
            .Include(sa => sa.AnomalyDetection)
                .ThenInclude(ad => ad.LoginAttempt)
            .Where(sa => sa.Severity == severity && 
                        sa.CreatedAt >= from && 
                        sa.CreatedAt <= to)
            .OrderByDescending(sa => sa.CreatedAt)
            .ToListAsync();
    }
}

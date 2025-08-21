using ContosoBank.Domain.Entities;
using ContosoBank.Domain.Interfaces;
using ContosoBank.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ContosoBank.Infrastructure.Repositories;

public class SecurityEventRepository : Repository<SecurityEvent>, ISecurityEventRepository
{
    public SecurityEventRepository(ContosoBankDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<SecurityEvent>> GetByUserIdAsync(Guid userId)
    {
        return await _dbSet
            .Where(se => se.UserId == userId)
            .OrderByDescending(se => se.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<SecurityEvent>> GetByEventTypeAsync(string eventType)
    {
        return await _dbSet
            .Where(se => se.EventType == eventType)
            .OrderByDescending(se => se.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<SecurityEvent>> GetBySeverityAsync(string severity)
    {
        return await _dbSet
            .Where(se => se.Severity == severity)
            .OrderByDescending(se => se.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<SecurityEvent>> GetByUserAndEventTypeAsync(Guid userId, string eventType)
    {
        return await _dbSet
            .Where(se => se.UserId == userId && se.EventType == eventType)
            .OrderByDescending(se => se.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<SecurityEvent>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Where(se => se.CreatedAt >= startDate && se.CreatedAt <= endDate)
            .OrderByDescending(se => se.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<SecurityEvent>> GetByIpAddressAsync(string ipAddress)
    {
        return await _dbSet
            .Where(se => se.IpAddress == ipAddress)
            .OrderByDescending(se => se.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<SecurityEvent>> GetRecentEventsByUserAsync(Guid userId, int count = 10)
    {
        return await _dbSet
            .Where(se => se.UserId == userId)
            .OrderByDescending(se => se.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<IEnumerable<SecurityEvent>> GetHighSeverityEventsAsync()
    {
        return await _dbSet
            .Where(se => se.Severity == "HIGH" || se.Severity == "CRITICAL")
            .OrderByDescending(se => se.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> GetEventCountByUserAsync(Guid userId, string eventType, DateTime since)
    {
        return await _dbSet
            .CountAsync(se => se.UserId == userId && 
                             se.EventType == eventType && 
                             se.CreatedAt >= since);
    }

    public async Task<SecurityEvent?> GetLastEventByUserAsync(Guid userId, string eventType)
    {
        return await _dbSet
            .Where(se => se.UserId == userId && se.EventType == eventType)
            .OrderByDescending(se => se.CreatedAt)
            .FirstOrDefaultAsync();
    }
}

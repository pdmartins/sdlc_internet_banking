using ContosoBank.Domain.Entities;
using ContosoBank.Domain.Interfaces;
using ContosoBank.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ContosoBank.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for login attempt operations
/// </summary>
public class LoginAttemptRepository : Repository<LoginAttempt>, ILoginAttemptRepository
{
    public LoginAttemptRepository(ContosoBankDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<LoginAttempt>> GetByUserIdAsync(Guid userId, int limit = 50)
    {
        return await _context.LoginAttempts
            .Where(la => la.UserId == userId)
            .OrderByDescending(la => la.AttemptedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<LoginAttempt>> GetByUserIdAndDateRangeAsync(Guid userId, DateTime from, DateTime to)
    {
        return await _context.LoginAttempts
            .Where(la => la.UserId == userId && la.AttemptedAt >= from && la.AttemptedAt <= to)
            .OrderByDescending(la => la.AttemptedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<LoginAttempt>> GetByIpAddressAsync(string ipAddress, DateTime from, DateTime to)
    {
        return await _context.LoginAttempts
            .Where(la => la.IpAddress == ipAddress && la.AttemptedAt >= from && la.AttemptedAt <= to)
            .OrderByDescending(la => la.AttemptedAt)
            .ToListAsync();
    }

    public async Task<LoginAttempt?> GetLatestByUserIdAsync(Guid userId)
    {
        return await _context.LoginAttempts
            .Where(la => la.UserId == userId)
            .OrderByDescending(la => la.AttemptedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<LoginAttempt>> GetAnomalousAttemptsAsync(DateTime from, DateTime to)
    {
        return await _context.LoginAttempts
            .Where(la => la.IsAnomalous && la.AttemptedAt >= from && la.AttemptedAt <= to)
            .OrderByDescending(la => la.AttemptedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<LoginAttempt>> GetRecentFailedAttemptsAsync(string email, TimeSpan timeWindow)
    {
        var cutoffTime = DateTime.UtcNow.Subtract(timeWindow);
        return await _context.LoginAttempts
            .Where(la => la.Email.ToLower() == email.ToLower() && 
                        !la.IsSuccessful && 
                        la.AttemptedAt >= cutoffTime)
            .OrderByDescending(la => la.AttemptedAt)
            .ToListAsync();
    }

    public async Task<int> GetFailedAttemptCountAsync(string ipAddress, TimeSpan timeWindow)
    {
        var cutoffTime = DateTime.UtcNow.Subtract(timeWindow);
        return await _context.LoginAttempts
            .CountAsync(la => la.IpAddress == ipAddress && 
                             !la.IsSuccessful && 
                             la.AttemptedAt >= cutoffTime);
    }

    public async Task<IEnumerable<LoginAttempt>> GetRecentByLocationAsync(string country, string region, string city, TimeSpan timeWindow)
    {
        var cutoffTime = DateTime.UtcNow.Subtract(timeWindow);
        return await _context.LoginAttempts
            .Where(la => la.Country == country && 
                        la.Region == region && 
                        la.City == city && 
                        la.AttemptedAt >= cutoffTime)
            .OrderByDescending(la => la.AttemptedAt)
            .ToListAsync();
    }
}

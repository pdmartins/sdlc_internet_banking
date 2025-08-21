using ContosoBank.Domain.Entities;
using ContosoBank.Domain.Interfaces;
using ContosoBank.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ContosoBank.Infrastructure.Repositories;

public class RateLimitRepository : Repository<RateLimitEntry>, IRateLimitRepository
{
    public RateLimitRepository(ContosoBankDbContext context) : base(context)
    {
    }

    public async Task<RateLimitEntry?> GetByClientAndTypeAsync(string clientIdentifier, string attemptType)
    {
        return await _dbSet
            .Where(entry => entry.ClientIdentifier == clientIdentifier && 
                           entry.AttemptType == attemptType)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<RateLimitEntry>> GetBlockedEntriesAsync()
    {
        return await _dbSet
            .Where(entry => entry.IsBlocked && 
                           entry.BlockedUntil.HasValue && 
                           entry.BlockedUntil > DateTime.UtcNow)
            .OrderByDescending(entry => entry.BlockedUntil)
            .ToListAsync();
    }

    public async Task<IEnumerable<RateLimitEntry>> GetExpiredBlocksAsync()
    {
        return await _dbSet
            .Where(entry => entry.IsBlocked && 
                           entry.BlockedUntil.HasValue && 
                           entry.BlockedUntil <= DateTime.UtcNow)
            .ToListAsync();
    }

    public async Task<int> GetAttemptCountAsync(string clientIdentifier, string attemptType, DateTime since)
    {
        return await _dbSet
            .Where(entry => entry.ClientIdentifier == clientIdentifier && 
                           entry.AttemptType == attemptType && 
                           entry.LastAttempt >= since)
            .SumAsync(entry => entry.AttemptCount);
    }

    public async Task CleanupExpiredEntriesAsync(DateTime olderThan)
    {
        var expiredEntries = await _dbSet
            .Where(entry => !entry.IsBlocked && entry.UpdatedAt < olderThan)
            .ToListAsync();

        if (expiredEntries.Any())
        {
            _dbSet.RemoveRange(expiredEntries);
            await _context.SaveChangesAsync();
        }
    }
}

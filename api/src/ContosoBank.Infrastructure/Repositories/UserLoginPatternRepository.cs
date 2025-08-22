using ContosoBank.Domain.Entities;
using ContosoBank.Domain.Interfaces;
using ContosoBank.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ContosoBank.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for user login pattern operations
/// </summary>
public class UserLoginPatternRepository : Repository<UserLoginPattern>, IUserLoginPatternRepository
{
    public UserLoginPatternRepository(ContosoBankDbContext context) : base(context)
    {
    }

    public async Task<UserLoginPattern?> GetByUserIdAsync(Guid userId)
    {
        return await _context.UserLoginPatterns
            .FirstOrDefaultAsync(ulp => ulp.UserId == userId);
    }

    public async Task<IEnumerable<UserLoginPattern>> GetPatternsRequiringUpdateAsync(DateTime cutoffDate)
    {
        return await _context.UserLoginPatterns
            .Where(ulp => ulp.LastUpdatedAt < cutoffDate)
            .ToListAsync();
    }

    public async Task UpdatePatternAsync(UserLoginPattern pattern)
    {
        _context.UserLoginPatterns.Update(pattern);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsByUserIdAsync(Guid userId)
    {
        return await _context.UserLoginPatterns
            .AnyAsync(ulp => ulp.UserId == userId);
    }
}

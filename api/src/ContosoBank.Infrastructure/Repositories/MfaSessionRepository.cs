using ContosoBank.Domain.Entities;
using ContosoBank.Domain.Interfaces;
using ContosoBank.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ContosoBank.Infrastructure.Repositories;

public class MfaSessionRepository : Repository<MfaSession>, IMfaSessionRepository
{
    public MfaSessionRepository(ContosoBankDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<MfaSession>> GetActiveSessionsForUserAsync(Guid userId)
    {
        return await _context.MfaSessions
            .Where(s => s.UserId == userId && 
                       !s.IsUsed && 
                       !s.IsBlocked && 
                       s.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
    }

    public async Task<IEnumerable<MfaSession>> GetExpiredSessionsAsync()
    {
        return await _context.MfaSessions
            .Where(s => s.ExpiresAt <= DateTime.UtcNow || s.IsUsed)
            .ToListAsync();
    }

    public async Task<MfaSession?> GetActiveSessionByEmailAsync(string email)
    {
        return await _context.MfaSessions
            .Where(s => s.Email.ToLower() == email.ToLower() && 
                       !s.IsUsed && 
                       !s.IsBlocked && 
                       s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();
    }
}

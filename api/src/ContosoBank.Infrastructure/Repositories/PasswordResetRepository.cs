using ContosoBank.Domain.Entities;
using ContosoBank.Domain.Interfaces;
using ContosoBank.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ContosoBank.Infrastructure.Repositories;

public class PasswordResetRepository : Repository<PasswordReset>, IPasswordResetRepository
{
    public PasswordResetRepository(ContosoBankDbContext context) : base(context)
    {
    }

    public async Task<PasswordReset?> GetByTokenAsync(string token)
    {
        return await _context.PasswordResets
            .Include(pr => pr.User)
            .FirstOrDefaultAsync(pr => pr.Token == token && !pr.IsUsed && pr.ExpiresAt > DateTime.UtcNow);
    }

    public async Task<PasswordReset?> GetActiveResetForUserAsync(Guid userId)
    {
        return await _context.PasswordResets
            .Include(pr => pr.User)
            .FirstOrDefaultAsync(pr => pr.UserId == userId && !pr.IsUsed && pr.ExpiresAt > DateTime.UtcNow);
    }

    public async Task<IEnumerable<PasswordReset>> GetActiveResetsForEmailAsync(string email)
    {
        return await _context.PasswordResets
            .Include(pr => pr.User)
            .Where(pr => pr.Email.ToLower() == email.ToLower() && !pr.IsUsed && pr.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
    }

    public async Task<int> CountRecentAttemptsAsync(string email, TimeSpan timeWindow)
    {
        var cutoffTime = DateTime.UtcNow - timeWindow;
        return await _context.PasswordResets
            .CountAsync(pr => pr.Email.ToLower() == email.ToLower() && pr.CreatedAt >= cutoffTime);
    }
}

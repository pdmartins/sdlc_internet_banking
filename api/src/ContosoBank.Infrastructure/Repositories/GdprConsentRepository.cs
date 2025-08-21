using ContosoBank.Domain.Entities;
using ContosoBank.Domain.Interfaces;
using ContosoBank.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ContosoBank.Infrastructure.Repositories;

public class GdprConsentRepository : Repository<GdprConsent>, IGdprConsentRepository
{
    public GdprConsentRepository(ContosoBankDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<GdprConsent>> GetUserConsentsAsync(Guid userId)
    {
        return await _dbSet
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.ConsentDate)
            .ToListAsync();
    }

    public async Task<GdprConsent?> GetUserConsentByTypeAsync(Guid userId, string consentType)
    {
        return await _dbSet
            .Where(c => c.UserId == userId && c.ConsentType == consentType && c.IsActive)
            .OrderByDescending(c => c.ConsentDate)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> HasActiveConsentAsync(Guid userId, string consentType)
    {
        return await _dbSet
            .AnyAsync(c => c.UserId == userId && 
                          c.ConsentType == consentType && 
                          c.HasConsented && 
                          c.IsActive &&
                          c.WithdrawnDate == null);
    }

    public async Task<IEnumerable<GdprConsent>> GetConsentsToExpireAsync(DateTime date)
    {
        return await _dbSet
            .Where(c => c.IsActive && c.ConsentDate <= date)
            .ToListAsync();
    }
}

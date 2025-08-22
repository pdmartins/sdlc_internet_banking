using ContosoBank.Domain.Entities;

namespace ContosoBank.Domain.Interfaces;

public interface IMfaSessionRepository : IRepository<MfaSession>
{
    Task<IEnumerable<MfaSession>> GetActiveSessionsForUserAsync(Guid userId);
    Task<IEnumerable<MfaSession>> GetExpiredSessionsAsync();
    Task<MfaSession?> GetActiveSessionByEmailAsync(string email);
}

using ContosoBank.Domain.Entities;

namespace ContosoBank.Domain.Interfaces;

public interface IPasswordResetRepository : IRepository<PasswordReset>
{
    Task<PasswordReset?> GetByTokenAsync(string token);
    Task<PasswordReset?> GetActiveResetForUserAsync(Guid userId);
    Task<IEnumerable<PasswordReset>> GetActiveResetsForEmailAsync(string email);
    Task<int> CountRecentAttemptsAsync(string email, TimeSpan timeWindow);
}

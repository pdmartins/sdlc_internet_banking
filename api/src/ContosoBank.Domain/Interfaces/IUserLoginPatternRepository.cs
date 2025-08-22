using ContosoBank.Domain.Entities;

namespace ContosoBank.Domain.Interfaces;

/// <summary>
/// Repository interface for user login pattern operations
/// </summary>
public interface IUserLoginPatternRepository : IRepository<UserLoginPattern>
{
    Task<UserLoginPattern?> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<UserLoginPattern>> GetPatternsRequiringUpdateAsync(DateTime cutoffDate);
    Task UpdatePatternAsync(UserLoginPattern pattern);
    Task<bool> ExistsByUserIdAsync(Guid userId);
}

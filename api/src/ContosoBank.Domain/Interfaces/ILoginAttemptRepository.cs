using ContosoBank.Domain.Entities;

namespace ContosoBank.Domain.Interfaces;

/// <summary>
/// Repository interface for login attempt operations
/// </summary>
public interface ILoginAttemptRepository : IRepository<LoginAttempt>
{
    Task<IEnumerable<LoginAttempt>> GetByUserIdAsync(Guid userId, int limit = 50);
    Task<IEnumerable<LoginAttempt>> GetByUserIdAndDateRangeAsync(Guid userId, DateTime from, DateTime to);
    Task<IEnumerable<LoginAttempt>> GetByIpAddressAsync(string ipAddress, DateTime from, DateTime to);
    Task<LoginAttempt?> GetLatestByUserIdAsync(Guid userId);
    Task<IEnumerable<LoginAttempt>> GetAnomalousAttemptsAsync(DateTime from, DateTime to);
    Task<IEnumerable<LoginAttempt>> GetRecentFailedAttemptsAsync(string email, TimeSpan timeWindow);
    Task<int> GetFailedAttemptCountAsync(string ipAddress, TimeSpan timeWindow);
    Task<IEnumerable<LoginAttempt>> GetRecentByLocationAsync(string country, string region, string city, TimeSpan timeWindow);
}

using ContosoBank.Domain.Entities;

namespace ContosoBank.Domain.Interfaces;

public interface IRateLimitRepository : IRepository<RateLimitEntry>
{
    Task<RateLimitEntry?> GetByClientAndTypeAsync(string clientIdentifier, string attemptType);
    Task<IEnumerable<RateLimitEntry>> GetBlockedEntriesAsync();
    Task<IEnumerable<RateLimitEntry>> GetExpiredBlocksAsync();
    Task<int> GetAttemptCountAsync(string clientIdentifier, string attemptType, DateTime since);
    Task CleanupExpiredEntriesAsync(DateTime olderThan);
}

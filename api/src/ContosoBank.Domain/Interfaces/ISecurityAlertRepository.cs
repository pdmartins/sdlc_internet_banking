using ContosoBank.Domain.Entities;

namespace ContosoBank.Domain.Interfaces;

/// <summary>
/// Repository interface for security alert operations
/// </summary>
public interface ISecurityAlertRepository : IRepository<SecurityAlert>
{
    Task<IEnumerable<SecurityAlert>> GetByUserIdAsync(Guid userId, bool unreadOnly = false, int limit = 50);
    Task<IEnumerable<SecurityAlert>> GetUndeliveredAlertsAsync();
    Task<IEnumerable<SecurityAlert>> GetExpiredAlertsAsync();
    Task<int> GetUnreadCountByUserIdAsync(Guid userId);
    Task MarkAsReadAsync(Guid alertId, Guid userId);
    Task MarkActionTakenAsync(Guid alertId, Guid userId);
    Task<IEnumerable<SecurityAlert>> GetBySeverityAsync(string severity, DateTime from, DateTime to);
}

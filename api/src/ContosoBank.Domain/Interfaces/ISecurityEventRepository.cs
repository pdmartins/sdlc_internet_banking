using ContosoBank.Domain.Entities;

namespace ContosoBank.Domain.Interfaces;

public interface ISecurityEventRepository : IRepository<SecurityEvent>
{
    Task<IEnumerable<SecurityEvent>> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<SecurityEvent>> GetByEventTypeAsync(string eventType);
    Task<IEnumerable<SecurityEvent>> GetBySeverityAsync(string severity);
    Task<IEnumerable<SecurityEvent>> GetByUserAndEventTypeAsync(Guid userId, string eventType);
    Task<IEnumerable<SecurityEvent>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<SecurityEvent>> GetByIpAddressAsync(string ipAddress);
    Task<IEnumerable<SecurityEvent>> GetRecentEventsByUserAsync(Guid userId, int count = 10);
    Task<IEnumerable<SecurityEvent>> GetHighSeverityEventsAsync();
    Task<int> GetEventCountByUserAsync(Guid userId, string eventType, DateTime since);
    Task<SecurityEvent?> GetLastEventByUserAsync(Guid userId, string eventType);
}

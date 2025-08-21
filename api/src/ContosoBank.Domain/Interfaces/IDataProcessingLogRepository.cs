using ContosoBank.Domain.Entities;

namespace ContosoBank.Domain.Interfaces;

public interface IDataProcessingLogRepository : IRepository<DataProcessingLog>
{
    Task<IEnumerable<DataProcessingLog>> GetUserProcessingLogsAsync(Guid userId);
    Task<IEnumerable<DataProcessingLog>> GetProcessingLogsByActivityAsync(string activity, DateTime fromDate, DateTime toDate);
    Task<IEnumerable<DataProcessingLog>> GetProcessingLogsByPurposeAsync(string purpose, DateTime fromDate, DateTime toDate);
    Task<int> GetUserProcessingCountAsync(Guid userId, string activity, DateTime fromDate);
}

using ContosoBank.Domain.Entities;

namespace ContosoBank.Domain.Interfaces;

/// <summary>
/// Repository interface for anomaly detection operations
/// </summary>
public interface IAnomalyDetectionRepository : IRepository<AnomalyDetection>
{
    Task<IEnumerable<AnomalyDetection>> GetByUserIdAsync(Guid userId, int limit = 50);
    Task<IEnumerable<AnomalyDetection>> GetUnresolvedAnomaliesAsync();
    Task<IEnumerable<AnomalyDetection>> GetBySeverityAsync(int minSeverity, DateTime from, DateTime to);
    Task<IEnumerable<AnomalyDetection>> GetByAnomalyTypeAsync(string anomalyType, DateTime from, DateTime to);
    Task<AnomalyDetection?> GetByLoginAttemptIdAsync(Guid loginAttemptId);
    Task<int> GetAnomalyCountByUserAsync(Guid userId, TimeSpan timeWindow);
}

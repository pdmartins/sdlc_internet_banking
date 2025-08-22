using ContosoBank.Domain.Entities;
using ContosoBank.Domain.Interfaces;
using ContosoBank.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ContosoBank.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for anomaly detection operations
/// </summary>
public class AnomalyDetectionRepository : Repository<AnomalyDetection>, IAnomalyDetectionRepository
{
    public AnomalyDetectionRepository(ContosoBankDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<AnomalyDetection>> GetByUserIdAsync(Guid userId, int limit = 50)
    {
        return await _context.AnomalyDetections
            .Include(ad => ad.LoginAttempt)
            .Include(ad => ad.User)
            .Where(ad => ad.UserId == userId)
            .OrderByDescending(ad => ad.DetectedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<AnomalyDetection>> GetUnresolvedAnomaliesAsync()
    {
        return await _context.AnomalyDetections
            .Include(ad => ad.LoginAttempt)
            .Include(ad => ad.User)
            .Include(ad => ad.ResolvedByUser)
            .Where(ad => !ad.IsResolved)
            .OrderByDescending(ad => ad.DetectedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<AnomalyDetection>> GetBySeverityAsync(int minSeverity, DateTime from, DateTime to)
    {
        return await _context.AnomalyDetections
            .Include(ad => ad.LoginAttempt)
            .Include(ad => ad.User)
            .Where(ad => ad.Severity >= minSeverity && 
                        ad.DetectedAt >= from && 
                        ad.DetectedAt <= to)
            .OrderByDescending(ad => ad.DetectedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<AnomalyDetection>> GetByAnomalyTypeAsync(string anomalyType, DateTime from, DateTime to)
    {
        return await _context.AnomalyDetections
            .Include(ad => ad.LoginAttempt)
            .Include(ad => ad.User)
            .Where(ad => ad.AnomalyType == anomalyType && 
                        ad.DetectedAt >= from && 
                        ad.DetectedAt <= to)
            .OrderByDescending(ad => ad.DetectedAt)
            .ToListAsync();
    }

    public async Task<AnomalyDetection?> GetByLoginAttemptIdAsync(Guid loginAttemptId)
    {
        return await _context.AnomalyDetections
            .Include(ad => ad.LoginAttempt)
            .Include(ad => ad.User)
            .FirstOrDefaultAsync(ad => ad.LoginAttemptId == loginAttemptId);
    }

    public async Task<int> GetAnomalyCountByUserAsync(Guid userId, TimeSpan timeWindow)
    {
        var cutoffTime = DateTime.UtcNow.Subtract(timeWindow);
        return await _context.AnomalyDetections
            .CountAsync(ad => ad.UserId == userId && ad.DetectedAt >= cutoffTime);
    }
}

using ContosoBank.Domain.Entities;
using ContosoBank.Domain.Interfaces;
using ContosoBank.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ContosoBank.Infrastructure.Repositories;

public class DataProcessingLogRepository : Repository<DataProcessingLog>, IDataProcessingLogRepository
{
    public DataProcessingLogRepository(ContosoBankDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<DataProcessingLog>> GetUserProcessingLogsAsync(Guid userId)
    {
        return await _dbSet
            .Where(log => log.UserId == userId)
            .OrderByDescending(log => log.ProcessedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<DataProcessingLog>> GetProcessingLogsByActivityAsync(string activity, DateTime fromDate, DateTime toDate)
    {
        return await _dbSet
            .Where(log => log.Activity == activity && 
                         log.ProcessedAt >= fromDate && 
                         log.ProcessedAt <= toDate)
            .OrderByDescending(log => log.ProcessedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<DataProcessingLog>> GetProcessingLogsByPurposeAsync(string purpose, DateTime fromDate, DateTime toDate)
    {
        return await _dbSet
            .Where(log => log.Purpose == purpose && 
                         log.ProcessedAt >= fromDate && 
                         log.ProcessedAt <= toDate)
            .OrderByDescending(log => log.ProcessedAt)
            .ToListAsync();
    }

    public async Task<int> GetUserProcessingCountAsync(Guid userId, string activity, DateTime fromDate)
    {
        return await _dbSet
            .CountAsync(log => log.UserId == userId && 
                              log.Activity == activity && 
                              log.ProcessedAt >= fromDate);
    }
}

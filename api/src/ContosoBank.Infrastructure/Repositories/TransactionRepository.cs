using ContosoBank.Domain.Entities;
using ContosoBank.Domain.Interfaces;
using ContosoBank.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ContosoBank.Infrastructure.Repositories;

public class TransactionRepository : Repository<Transaction>, ITransactionRepository
{
    public TransactionRepository(ContosoBankDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Transaction>> GetByAccountIdAsync(Guid accountId)
    {
        return await _dbSet
            .Where(t => t.AccountId == accountId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Transaction>> GetByAccountIdAndDateRangeAsync(Guid accountId, DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Where(t => t.AccountId == accountId && 
                       t.CreatedAt >= startDate && 
                       t.CreatedAt <= endDate)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Transaction>> GetByTypeAsync(Guid accountId, string type)
    {
        return await _dbSet
            .Where(t => t.AccountId == accountId && t.Type == type)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Transaction>> GetByCategoryAsync(Guid accountId, string category)
    {
        return await _dbSet
            .Where(t => t.AccountId == accountId && t.Category == category)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Transaction>> GetPendingTransactionsAsync(Guid accountId)
    {
        return await _dbSet
            .Where(t => t.AccountId == accountId && t.Status == "PENDING")
            .OrderBy(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<decimal> GetTotalAmountByTypeAsync(Guid accountId, string type, DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Where(t => t.AccountId == accountId && 
                       t.Type == type && 
                       t.CreatedAt >= startDate && 
                       t.CreatedAt <= endDate &&
                       t.Status == "COMPLETED")
            .SumAsync(t => t.Amount);
    }

    public async Task<decimal> GetDailyTransactionAmountAsync(Guid accountId, DateTime date)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1).AddTicks(-1);

        return await _dbSet
            .Where(t => t.AccountId == accountId && 
                       t.CreatedAt >= startOfDay && 
                       t.CreatedAt <= endOfDay &&
                       (t.Type == "DEBIT" || t.Type == "TRANSFER") &&
                       t.Status == "COMPLETED")
            .SumAsync(t => t.Amount);
    }

    public async Task<decimal> GetMonthlyTransactionAmountAsync(Guid accountId, int year, int month)
    {
        var startOfMonth = new DateTime(year, month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddTicks(-1);

        return await _dbSet
            .Where(t => t.AccountId == accountId && 
                       t.CreatedAt >= startOfMonth && 
                       t.CreatedAt <= endOfMonth &&
                       (t.Type == "DEBIT" || t.Type == "TRANSFER") &&
                       t.Status == "COMPLETED")
            .SumAsync(t => t.Amount);
    }

    public async Task<int> GetTransactionCountAsync(Guid accountId, DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .CountAsync(t => t.AccountId == accountId && 
                            t.CreatedAt >= startDate && 
                            t.CreatedAt <= endDate);
    }

    public async Task<Transaction?> GetLastTransactionAsync(Guid accountId)
    {
        return await _dbSet
            .Where(t => t.AccountId == accountId)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync();
    }
}

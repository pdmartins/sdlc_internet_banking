using ContosoBank.Domain.Entities;

namespace ContosoBank.Domain.Interfaces;

public interface ITransactionRepository : IRepository<Transaction>
{
    Task<IEnumerable<Transaction>> GetByAccountIdAsync(Guid accountId);
    Task<IEnumerable<Transaction>> GetByAccountIdAndDateRangeAsync(Guid accountId, DateTime startDate, DateTime endDate);
    Task<IEnumerable<Transaction>> GetByTypeAsync(Guid accountId, string type);
    Task<IEnumerable<Transaction>> GetByCategoryAsync(Guid accountId, string category);
    Task<IEnumerable<Transaction>> GetPendingTransactionsAsync(Guid accountId);
    Task<decimal> GetTotalAmountByTypeAsync(Guid accountId, string type, DateTime startDate, DateTime endDate);
    Task<decimal> GetDailyTransactionAmountAsync(Guid accountId, DateTime date);
    Task<decimal> GetMonthlyTransactionAmountAsync(Guid accountId, int year, int month);
    Task<int> GetTransactionCountAsync(Guid accountId, DateTime startDate, DateTime endDate);
    Task<Transaction?> GetLastTransactionAsync(Guid accountId);
}

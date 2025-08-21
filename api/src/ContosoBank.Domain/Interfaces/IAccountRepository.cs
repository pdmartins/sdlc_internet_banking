using ContosoBank.Domain.Entities;

namespace ContosoBank.Domain.Interfaces;

public interface IAccountRepository : IRepository<Account>
{
    Task<Account?> GetByAccountNumberAsync(string accountNumber);
    Task<Account?> GetByUserIdAsync(Guid userId);
    Task<bool> IsAccountNumberUniqueAsync(string accountNumber);
    Task<IEnumerable<Account>> GetAccountsByBranchAsync(string branchCode);
    Task<IEnumerable<Account>> GetActiveAccountsAsync();
    Task<decimal> GetTotalBalanceAsync();
    Task<decimal> GetBranchTotalBalanceAsync(string branchCode);
    Task<IEnumerable<Account>> GetAccountsCreatedAfterAsync(DateTime date);
}

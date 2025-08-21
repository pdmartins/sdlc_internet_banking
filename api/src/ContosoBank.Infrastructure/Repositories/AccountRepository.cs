using ContosoBank.Domain.Entities;
using ContosoBank.Domain.Interfaces;
using ContosoBank.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ContosoBank.Infrastructure.Repositories;

public class AccountRepository : Repository<Account>, IAccountRepository
{
    public AccountRepository(ContosoBankDbContext context) : base(context)
    {
    }

    public async Task<Account?> GetByAccountNumberAsync(string accountNumber)
    {
        return await _dbSet
            .Include(a => a.User)
            .Include(a => a.Transactions)
            .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
    }

    public async Task<Account?> GetByUserIdAsync(Guid userId)
    {
        return await _dbSet
            .Include(a => a.User)
            .Include(a => a.Transactions.OrderByDescending(t => t.CreatedAt))
            .FirstOrDefaultAsync(a => a.UserId == userId);
    }

    public async Task<bool> IsAccountNumberUniqueAsync(string accountNumber)
    {
        return !await _dbSet.AnyAsync(a => a.AccountNumber == accountNumber);
    }

    public async Task<IEnumerable<Account>> GetAccountsByBranchAsync(string branchCode)
    {
        return await _dbSet
            .Include(a => a.User)
            .Where(a => a.BranchCode == branchCode)
            .OrderBy(a => a.AccountNumber)
            .ToListAsync();
    }

    public async Task<IEnumerable<Account>> GetActiveAccountsAsync()
    {
        return await _dbSet
            .Include(a => a.User)
            .Where(a => a.IsActive)
            .OrderBy(a => a.AccountNumber)
            .ToListAsync();
    }

    public async Task<decimal> GetTotalBalanceAsync()
    {
        return await _dbSet.SumAsync(a => a.Balance);
    }

    public async Task<decimal> GetBranchTotalBalanceAsync(string branchCode)
    {
        return await _dbSet
            .Where(a => a.BranchCode == branchCode)
            .SumAsync(a => a.Balance);
    }

    public async Task<IEnumerable<Account>> GetAccountsCreatedAfterAsync(DateTime date)
    {
        return await _dbSet
            .Include(a => a.User)
            .Where(a => a.CreatedAt >= date)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }
}

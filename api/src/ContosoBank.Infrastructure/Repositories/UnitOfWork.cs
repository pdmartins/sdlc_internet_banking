using ContosoBank.Domain.Interfaces;
using ContosoBank.Infrastructure.Data;

namespace ContosoBank.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly ContosoBankDbContext _context;
    private bool _disposed = false;

    private IUserRepository? _userRepository;
    private IAccountRepository? _accountRepository;
    private ITransactionRepository? _transactionRepository;
    private ISecurityEventRepository? _securityEventRepository;

    public UnitOfWork(ContosoBankDbContext context)
    {
        _context = context;
    }

    public IUserRepository Users
    {
        get
        {
            _userRepository ??= new UserRepository(_context);
            return _userRepository;
        }
    }

    public IAccountRepository Accounts
    {
        get
        {
            _accountRepository ??= new AccountRepository(_context);
            return _accountRepository;
        }
    }

    public ITransactionRepository Transactions
    {
        get
        {
            _transactionRepository ??= new TransactionRepository(_context);
            return _transactionRepository;
        }
    }

    public ISecurityEventRepository SecurityEvents
    {
        get
        {
            _securityEventRepository ??= new SecurityEventRepository(_context);
            return _securityEventRepository;
        }
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        var transaction = _context.Database.CurrentTransaction;
        if (transaction != null)
        {
            await transaction.CommitAsync();
        }
    }

    public async Task RollbackTransactionAsync()
    {
        var transaction = _context.Database.CurrentTransaction;
        if (transaction != null)
        {
            await transaction.RollbackAsync();
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _context.Dispose();
        }
        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

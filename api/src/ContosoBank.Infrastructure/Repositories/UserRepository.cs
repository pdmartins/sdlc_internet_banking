using ContosoBank.Domain.Entities;
using ContosoBank.Domain.Interfaces;
using ContosoBank.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ContosoBank.Infrastructure.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(ContosoBankDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet
            .Include(u => u.Account)
            .Include(u => u.SecurityEvents)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetByCPFAsync(string cpf)
    {
        return await _dbSet
            .Include(u => u.Account)
            .Include(u => u.SecurityEvents)
            .FirstOrDefaultAsync(u => u.CPF == cpf);
    }

    public async Task<User?> GetByPhoneAsync(string phone)
    {
        return await _dbSet
            .Include(u => u.Account)
            .Include(u => u.SecurityEvents)
            .FirstOrDefaultAsync(u => u.Phone == phone);
    }

    public async Task<User?> GetUserWithAccountAsync(Guid userId)
    {
        return await _dbSet
            .Include(u => u.Account)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<User?> GetUserWithSecurityEventsAsync(Guid userId)
    {
        return await _dbSet
            .Include(u => u.SecurityEvents.OrderByDescending(se => se.CreatedAt))
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<bool> IsEmailUniqueAsync(string email)
    {
        return !await _dbSet.AnyAsync(u => u.Email == email);
    }

    public async Task<bool> IsCPFUniqueAsync(string cpf)
    {
        return !await _dbSet.AnyAsync(u => u.CPF == cpf);
    }

    public async Task<bool> IsPhoneUniqueAsync(string phone)
    {
        return !await _dbSet.AnyAsync(u => u.Phone == phone);
    }

    public async Task<IEnumerable<User>> GetUsersCreatedAfterAsync(DateTime date)
    {
        return await _dbSet
            .Where(u => u.CreatedAt >= date)
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();
    }
}

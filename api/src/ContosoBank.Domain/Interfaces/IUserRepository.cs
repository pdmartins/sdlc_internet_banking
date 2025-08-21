using ContosoBank.Domain.Entities;

namespace ContosoBank.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByCPFAsync(string cpf);
    Task<User?> GetByPhoneAsync(string phone);
    Task<User?> GetUserWithAccountAsync(Guid userId);
    Task<User?> GetUserWithSecurityEventsAsync(Guid userId);
    Task<bool> IsEmailUniqueAsync(string email);
    Task<bool> IsCPFUniqueAsync(string cpf);
    Task<bool> IsPhoneUniqueAsync(string phone);
    Task<IEnumerable<User>> GetUsersCreatedAfterAsync(DateTime date);
}

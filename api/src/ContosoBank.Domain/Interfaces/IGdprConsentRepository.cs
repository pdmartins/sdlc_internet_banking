using ContosoBank.Domain.Entities;

namespace ContosoBank.Domain.Interfaces;

public interface IGdprConsentRepository : IRepository<GdprConsent>
{
    Task<IEnumerable<GdprConsent>> GetUserConsentsAsync(Guid userId);
    Task<GdprConsent?> GetUserConsentByTypeAsync(Guid userId, string consentType);
    Task<bool> HasActiveConsentAsync(Guid userId, string consentType);
    Task<IEnumerable<GdprConsent>> GetConsentsToExpireAsync(DateTime date);
}

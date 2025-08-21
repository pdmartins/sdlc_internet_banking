using ContosoBank.Domain.Entities;

namespace ContosoBank.Application.Interfaces;

public interface IGdprComplianceService
{
    /// <summary>
    /// Records user consent for GDPR compliance
    /// </summary>
    Task<bool> RecordConsentAsync(Guid userId, string consentType, string ipAddress, string userAgent);
    
    /// <summary>
    /// Validates if user has provided required GDPR consent
    /// </summary>
    Task<bool> HasValidConsentAsync(Guid userId);
    
    /// <summary>
    /// Encrypts personally identifiable information
    /// </summary>
    string EncryptPii(string data);
    
    /// <summary>
    /// Decrypts personally identifiable information
    /// </summary>
    string DecryptPii(string encryptedData);
    
    /// <summary>
    /// Logs data processing activity for GDPR audit trail
    /// </summary>
    Task LogDataProcessingAsync(Guid userId, string activity, string purpose, string legalBasis);
    
    /// <summary>
    /// Validates if user data can be processed based on GDPR rules
    /// </summary>
    Task<bool> CanProcessUserDataAsync(Guid userId, string processingPurpose);
    
    /// <summary>
    /// Anonymizes user data for compliance
    /// </summary>
    Task<bool> AnonymizeUserDataAsync(Guid userId);
}

using ContosoBank.Application.Interfaces;
using ContosoBank.Domain.Entities;
using ContosoBank.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace ContosoBank.Application.Services;

public class GdprComplianceService : IGdprComplianceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GdprComplianceService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _encryptionKey;

    public GdprComplianceService(
        IUnitOfWork unitOfWork, 
        ILogger<GdprComplianceService> logger,
        IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _configuration = configuration;
        _encryptionKey = _configuration["Security:EncryptionKey"] ?? "ContosoBank2025EncryptionKey!";
    }

    public async Task<bool> RecordConsentAsync(Guid userId, string consentType, string ipAddress, string userAgent)
    {
        try
        {
            _logger.LogInformation("Recording GDPR consent for user {UserId}, type: {ConsentType}", userId, consentType);

            var existingConsent = await _unitOfWork.GdprConsents.GetUserConsentByTypeAsync(userId, consentType);
            
            if (existingConsent != null)
            {
                // Update existing consent
                existingConsent.HasConsented = true;
                existingConsent.ConsentDate = DateTime.UtcNow;
                existingConsent.IpAddress = ipAddress;
                existingConsent.UserAgent = userAgent;
                existingConsent.WithdrawnDate = null;
                existingConsent.IsActive = true;
                
                _unitOfWork.GdprConsents.Update(existingConsent);
            }
            else
            {
                // Create new consent record
                var consent = new GdprConsent
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ConsentType = consentType,
                    HasConsented = true,
                    ConsentDetails = $"Consent granted for {consentType}",
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    ConsentDate = DateTime.UtcNow,
                    IsActive = true
                };

                await _unitOfWork.GdprConsents.AddAsync(consent);
            }

            await _unitOfWork.SaveChangesAsync();

            // Log the data processing activity
            await LogDataProcessingAsync(userId, "CONSENT_RECORDING", "GDPR_COMPLIANCE", "CONSENT");

            _logger.LogInformation("GDPR consent recorded successfully for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording GDPR consent for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> HasValidConsentAsync(Guid userId)
    {
        try
        {
            var requiredConsents = new[] { "DATA_PROCESSING", "TERMS_CONDITIONS" };
            
            foreach (var consentType in requiredConsents)
            {
                var hasConsent = await _unitOfWork.GdprConsents.HasActiveConsentAsync(userId, consentType);
                if (!hasConsent)
                {
                    _logger.LogWarning("User {UserId} missing required consent: {ConsentType}", userId, consentType);
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking GDPR consent for user {UserId}", userId);
            return false;
        }
    }

    public string EncryptPii(string data)
    {
        if (string.IsNullOrEmpty(data))
            return string.Empty;

        try
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(_encryptionKey.PadRight(32).Substring(0, 32));
            aes.IV = new byte[16]; // Zero IV for simplicity in demo

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var msEncrypt = new MemoryStream();
            using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
            using var swEncrypt = new StreamWriter(csEncrypt);
            
            swEncrypt.Write(data);
            swEncrypt.Close();
            
            return Convert.ToBase64String(msEncrypt.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encrypting PII data");
            throw new InvalidOperationException("Failed to encrypt sensitive data", ex);
        }
    }

    public string DecryptPii(string encryptedData)
    {
        if (string.IsNullOrEmpty(encryptedData))
            return string.Empty;

        try
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(_encryptionKey.PadRight(32).Substring(0, 32));
            aes.IV = new byte[16]; // Zero IV for simplicity in demo

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var msDecrypt = new MemoryStream(Convert.FromBase64String(encryptedData));
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);
            
            return srDecrypt.ReadToEnd();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting PII data");
            throw new InvalidOperationException("Failed to decrypt sensitive data", ex);
        }
    }

    public async Task LogDataProcessingAsync(Guid userId, string activity, string purpose, string legalBasis)
    {
        try
        {
            var log = new DataProcessingLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Activity = activity,
                Purpose = purpose,
                LegalBasis = legalBasis,
                Details = $"Data processing for {activity} with purpose {purpose}",
                ProcessedAt = DateTime.UtcNow
            };

            await _unitOfWork.DataProcessingLogs.AddAsync(log);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Data processing logged for user {UserId}: {Activity}", userId, activity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging data processing for user {UserId}", userId);
        }
    }

    public async Task<bool> CanProcessUserDataAsync(Guid userId, string processingPurpose)
    {
        try
        {
            // Check if user has valid consent for data processing
            var hasConsent = await HasValidConsentAsync(userId);
            if (!hasConsent)
            {
                _logger.LogWarning("Cannot process data for user {UserId} - missing valid consent", userId);
                return false;
            }

            // Check if processing purpose is legitimate
            var legitimatePurposes = new[] 
            { 
                "USER_ONBOARDING", 
                "FRAUD_PREVENTION", 
                "COMPLIANCE_CHECK", 
                "SERVICE_PROVISION",
                "SECURITY_MONITORING"
            };

            if (!legitimatePurposes.Contains(processingPurpose))
            {
                _logger.LogWarning("Invalid processing purpose: {Purpose}", processingPurpose);
                return false;
            }

            // Log this processing check
            await LogDataProcessingAsync(userId, "PROCESSING_CHECK", processingPurpose, "CONSENT");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking data processing permission for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> AnonymizeUserDataAsync(Guid userId)
    {
        try
        {
            _logger.LogInformation("Starting data anonymization for user {UserId}", userId);

            await _unitOfWork.BeginTransactionAsync();

            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for anonymization", userId);
                return false;
            }

            // Anonymize user data
            user.FullName = "ANONYMIZED_USER";
            user.Email = $"anonymized_{userId}@anonymized.local";
            user.Phone = "000000000";
            user.CPF = "00000000000";
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Users.Update(user);

            // Log the anonymization
            await LogDataProcessingAsync(userId, "DATA_ANONYMIZATION", "GDPR_COMPLIANCE", "LEGAL_OBLIGATION");

            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("User data anonymized successfully for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error anonymizing user data for user {UserId}", userId);
            return false;
        }
    }
}

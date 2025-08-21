using ContosoBank.Application.DTOs;

namespace ContosoBank.Application.Interfaces;

public interface IEnhancedRegistrationService : IRegistrationService
{
    /// <summary>
    /// Enhanced user registration with full identity verification and GDPR compliance
    /// </summary>
    Task<EnhancedRegistrationResponseDto> RegisterUserWithVerificationAsync(
        RegisterUserRequestDto request, 
        string clientIdentifier, 
        string ipAddress,
        string userAgent);

    /// <summary>
    /// Record GDPR consent during registration
    /// </summary>
    Task<bool> RecordRegistrationConsentAsync(
        Guid userId, 
        GdprConsentRequestDto consentRequest,
        string ipAddress,
        string userAgent);

    /// <summary>
    /// Validate registration with comprehensive checks
    /// </summary>
    Task<RegistrationValidationResult> ValidateRegistrationWithComplianceAsync(
        RegisterUserRequestDto request,
        string clientIdentifier,
        string ipAddress);
}

public class EnhancedRegistrationResponseDto : UserRegistrationResponseDto
{
    public string VerificationReference { get; set; } = string.Empty;
    public bool RequiresAdditionalVerification { get; set; }
    public List<string> ComplianceWarnings { get; set; } = new();
    public bool GdprConsentRecorded { get; set; }
    public string ComplianceReference { get; set; } = string.Empty;
}

public class GdprConsentRequestDto
{
    public bool DataProcessingConsent { get; set; }
    public bool TermsAndConditionsConsent { get; set; }
    public bool MarketingConsent { get; set; }
    public string ConsentDetails { get; set; } = string.Empty;
}

public class RegistrationValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public bool RequiresAdditionalVerification { get; set; }
    public bool IsRateLimited { get; set; }
    public TimeSpan? RetryAfter { get; set; }
}

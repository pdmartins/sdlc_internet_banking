using ContosoBank.Application.DTOs;
using ContosoBank.Application.Interfaces;
using ContosoBank.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ContosoBank.Application.Services;

public interface IIdentityVerificationService
{
    Task<IdentityVerificationResult> VerifyUserAsync(RegisterUserRequestDto request, string clientIdentifier, string ipAddress);
    Task<bool> ValidateAgeRequirementAsync(DateTime dateOfBirth);
    Task<bool> ValidateDataUniquenessAsync(RegisterUserRequestDto request);
    Task<bool> ValidateCpfAsync(string cpf);
    Task<bool> ValidateEmailFormatAsync(string email);
    Task<bool> ValidatePhoneFormatAsync(string phone);
    Task<ComplianceCheckResult> PerformComplianceChecksAsync(RegisterUserRequestDto request, string ipAddress);
}

public class IdentityVerificationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public bool RequiresAdditionalVerification { get; set; }
    public string? VerificationReference { get; set; }
}

public class ComplianceCheckResult
{
    public bool IsCompliant { get; set; }
    public List<string> Issues { get; set; } = new();
    public bool RequiresGdprConsent { get; set; }
    public bool IsFromRestrictedRegion { get; set; }
    public string? ComplianceReference { get; set; }
}

public class IdentityVerificationService : IIdentityVerificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGdprComplianceService _gdprService;
    private readonly IRateLimitingService _rateLimitingService;
    private readonly ILogger<IdentityVerificationService> _logger;
    private readonly IConfiguration _configuration;

    // Validation patterns
    private static readonly Regex EmailRegex = new(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", RegexOptions.Compiled);
    private static readonly Regex PhoneRegex = new(@"^(\+55)?(\(?\d{2}\)?)?(\d{4,5})-?(\d{4})$", RegexOptions.Compiled);
    private static readonly Regex CpfRegex = new(@"^\d{3}\.?\d{3}\.?\d{3}-?\d{2}$", RegexOptions.Compiled);

    public IdentityVerificationService(
        IUnitOfWork unitOfWork,
        IGdprComplianceService gdprService,
        IRateLimitingService rateLimitingService,
        ILogger<IdentityVerificationService> logger,
        IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _gdprService = gdprService;
        _rateLimitingService = rateLimitingService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<IdentityVerificationResult> VerifyUserAsync(RegisterUserRequestDto request, string clientIdentifier, string ipAddress)
    {
        var result = new IdentityVerificationResult();
        
        try
        {
            _logger.LogInformation("Starting identity verification for email: {Email}", request.Email);

            // Check rate limiting first
            if (!await _rateLimitingService.CanAttemptRegistrationAsync(clientIdentifier))
            {
                result.Errors.Add("Too many registration attempts. Please try again later.");
                return result;
            }

            // Perform basic validation checks
            await ValidateBasicRequirements(request, result);
            
            // Perform uniqueness checks
            await ValidateDataUniqueness(request, result);
            
            // Perform compliance checks
            var complianceResult = await PerformComplianceChecksAsync(request, ipAddress);
            if (!complianceResult.IsCompliant)
            {
                result.Errors.AddRange(complianceResult.Issues);
            }

            // Additional verification requirements
            if (await RequiresEnhancedVerification(request))
            {
                result.RequiresAdditionalVerification = true;
                result.VerificationReference = Guid.NewGuid().ToString();
                result.Warnings.Add("Additional identity verification may be required.");
            }

            result.IsValid = !result.Errors.Any();

            _logger.LogInformation("Identity verification completed for {Email}. Valid: {IsValid}, Errors: {ErrorCount}", 
                request.Email, result.IsValid, result.Errors.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during identity verification for {Email}", request.Email);
            result.Errors.Add("An error occurred during verification. Please try again.");
            return result;
        }
    }

    public async Task<bool> ValidateAgeRequirementAsync(DateTime dateOfBirth)
    {
        try
        {
            var age = DateTime.UtcNow.Year - dateOfBirth.Year;
            if (dateOfBirth.Date > DateTime.UtcNow.AddYears(-age))
                age--;

            var isValid = age >= 18;
            
            if (!isValid)
            {
                _logger.LogWarning("Age validation failed. Age: {Age}", age);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating age for date of birth: {DateOfBirth}", dateOfBirth);
            return false;
        }
    }

    public async Task<bool> ValidateDataUniquenessAsync(RegisterUserRequestDto request)
    {
        try
        {
            var tasks = new[]
            {
                _unitOfWork.Users.IsEmailUniqueAsync(request.Email.Trim().ToLowerInvariant()),
                _unitOfWork.Users.IsCPFUniqueAsync(request.CPF.Trim()),
                _unitOfWork.Users.IsPhoneUniqueAsync(request.Phone.Trim())
            };

            var results = await Task.WhenAll(tasks);
            return results.All(r => r);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating data uniqueness for {Email}", request.Email);
            return false;
        }
    }

    public Task<bool> ValidateCpfAsync(string cpf)
    {
        return Task.FromResult(IsValidCpf(cpf));
    }

    public Task<bool> ValidateEmailFormatAsync(string email)
    {
        return Task.FromResult(EmailRegex.IsMatch(email));
    }

    public Task<bool> ValidatePhoneFormatAsync(string phone)
    {
        return Task.FromResult(PhoneRegex.IsMatch(phone));
    }

    public async Task<ComplianceCheckResult> PerformComplianceChecksAsync(RegisterUserRequestDto request, string ipAddress)
    {
        var result = new ComplianceCheckResult
        {
            IsCompliant = true,
            RequiresGdprConsent = true, // Always require GDPR consent for EU users
            ComplianceReference = Guid.NewGuid().ToString()
        };

        try
        {
            _logger.LogInformation("Performing compliance checks for {Email} from IP {IP}", request.Email, ipAddress);

            // Check for restricted regions (simplified check)
            if (await IsFromRestrictedRegion(ipAddress))
            {
                result.IsFromRestrictedRegion = true;
                result.Issues.Add("Registration from this region requires additional verification.");
                result.IsCompliant = false;
            }

            // Check age compliance
            if (!await ValidateAgeRequirementAsync(request.DateOfBirth))
            {
                result.Issues.Add("Applicant must be at least 18 years old.");
                result.IsCompliant = false;
            }

            // Check for suspicious patterns
            if (await HasSuspiciousPatterns(request))
            {
                result.Issues.Add("Additional verification required due to data patterns.");
                result.IsCompliant = false;
            }

            // Check against sanctions lists (simplified)
            if (await IsOnSanctionsList(request))
            {
                result.Issues.Add("Unable to process registration due to compliance restrictions.");
                result.IsCompliant = false;
            }

            _logger.LogInformation("Compliance check completed for {Email}. Compliant: {IsCompliant}", 
                request.Email, result.IsCompliant);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during compliance checks for {Email}", request.Email);
            result.IsCompliant = false;
            result.Issues.Add("Compliance verification temporarily unavailable.");
            return result;
        }
    }

    private async Task ValidateBasicRequirements(RegisterUserRequestDto request, IdentityVerificationResult result)
    {
        // Name validation
        if (string.IsNullOrWhiteSpace(request.FullName) || request.FullName.Trim().Length < 2)
        {
            result.Errors.Add("Full name must be at least 2 characters long.");
        }

        // Email validation
        if (!await ValidateEmailFormatAsync(request.Email))
        {
            result.Errors.Add("Invalid email format.");
        }

        // Phone validation
        if (!await ValidatePhoneFormatAsync(request.Phone))
        {
            result.Errors.Add("Invalid phone number format. Use Brazilian format: (11) 99999-9999");
        }

        // CPF validation
        if (!await ValidateCpfAsync(request.CPF))
        {
            result.Errors.Add("Invalid CPF format or check digits.");
        }

        // Age validation
        if (!await ValidateAgeRequirementAsync(request.DateOfBirth))
        {
            result.Errors.Add("You must be at least 18 years old to register.");
        }

        // Date of birth sanity check
        if (request.DateOfBirth > DateTime.UtcNow.AddYears(-16) || request.DateOfBirth < DateTime.UtcNow.AddYears(-120))
        {
            result.Errors.Add("Please provide a valid date of birth.");
        }
    }

    private async Task ValidateDataUniqueness(RegisterUserRequestDto request, IdentityVerificationResult result)
    {
        if (!await _unitOfWork.Users.IsEmailUniqueAsync(request.Email.Trim().ToLowerInvariant()))
        {
            result.Errors.Add("Email address is already registered.");
        }

        if (!await _unitOfWork.Users.IsCPFUniqueAsync(request.CPF.Trim()))
        {
            result.Errors.Add("CPF is already registered.");
        }

        if (!await _unitOfWork.Users.IsPhoneUniqueAsync(request.Phone.Trim()))
        {
            result.Errors.Add("Phone number is already registered.");
        }
    }

    private async Task<bool> RequiresEnhancedVerification(RegisterUserRequestDto request)
    {
        // Enhanced verification logic (simplified for demo)
        await Task.Delay(1); // Simulate async check
        
        // Check for high-risk indicators
        var riskFactors = 0;
        
        // Check if email domain is suspicious
        var emailDomain = request.Email.Split('@').LastOrDefault()?.ToLowerInvariant();
        var suspiciousDomains = new[] { "temp-mail.org", "10minutemail.com", "guerrillamail.com" };
        if (suspiciousDomains.Contains(emailDomain))
            riskFactors++;

        // Check for sequential or repeated patterns in phone/CPF
        if (HasSequentialPattern(request.Phone) || HasSequentialPattern(request.CPF))
            riskFactors++;

        return riskFactors > 0;
    }

    private async Task<bool> IsFromRestrictedRegion(string ipAddress)
    {
        // Simplified region check - in real implementation, use GeoIP service
        await Task.Delay(1);
        
        // For demo, consider certain IP ranges as restricted
        return ipAddress.StartsWith("192.168.") || ipAddress.StartsWith("127."); // Local IPs for demo
    }

    private async Task<bool> HasSuspiciousPatterns(RegisterUserRequestDto request)
    {
        await Task.Delay(1);
        
        // Check for common suspicious patterns
        var suspiciousPatterns = new[]
        {
            request.FullName.ToLower().Contains("test"),
            request.FullName.ToLower().Contains("fake"),
            request.Email.Contains("test"),
            HasSequentialPattern(request.Phone),
            HasSequentialPattern(request.CPF)
        };

        return suspiciousPatterns.Any(p => p);
    }

    private async Task<bool> IsOnSanctionsList(RegisterUserRequestDto request)
    {
        // Simplified sanctions check - in real implementation, check against OFAC, EU sanctions, etc.
        await Task.Delay(1);
        
        var sanctionedNames = new[] { "test user", "banned user", "sanctioned person" };
        return sanctionedNames.Contains(request.FullName.ToLowerInvariant());
    }

    private static bool HasSequentialPattern(string input)
    {
        if (string.IsNullOrEmpty(input) || input.Length < 3)
            return false;

        var digits = input.Where(char.IsDigit).ToArray();
        if (digits.Length < 3)
            return false;

        // Check for sequential digits
        for (int i = 0; i < digits.Length - 2; i++)
        {
            if (char.GetNumericValue(digits[i]) + 1 == char.GetNumericValue(digits[i + 1]) &&
                char.GetNumericValue(digits[i + 1]) + 1 == char.GetNumericValue(digits[i + 2]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsValidCpf(string cpf)
    {
        // Remove formatting
        var digits = cpf.Replace(".", "").Replace("-", "");
        
        if (digits.Length != 11 || !digits.All(char.IsDigit))
            return false;

        // Check for all same digits
        if (digits.All(c => c == digits[0]))
            return false;

        // Calculate first verification digit
        var sum = 0;
        for (int i = 0; i < 9; i++)
        {
            sum += int.Parse(digits[i].ToString()) * (10 - i);
        }
        var digit1 = 11 - (sum % 11);
        if (digit1 > 9) digit1 = 0;

        // Calculate second verification digit
        sum = 0;
        for (int i = 0; i < 10; i++)
        {
            sum += int.Parse(digits[i].ToString()) * (11 - i);
        }
        var digit2 = 11 - (sum % 11);
        if (digit2 > 9) digit2 = 0;

        // Validate check digits
        return int.Parse(digits[9].ToString()) == digit1 && 
               int.Parse(digits[10].ToString()) == digit2;
    }
}

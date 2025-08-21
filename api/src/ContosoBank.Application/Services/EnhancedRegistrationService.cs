using ContosoBank.Application.DTOs;
using ContosoBank.Application.Interfaces;
using ContosoBank.Domain.Entities;
using ContosoBank.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace ContosoBank.Application.Services;

public class EnhancedRegistrationService : IEnhancedRegistrationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EnhancedRegistrationService> _logger;
    private readonly IIdentityVerificationService _identityVerificationService;
    private readonly IGdprComplianceService _gdprComplianceService;
    private readonly IRateLimitingService _rateLimitingService;

    public EnhancedRegistrationService(
        IUnitOfWork unitOfWork,
        ILogger<EnhancedRegistrationService> logger,
        IIdentityVerificationService identityVerificationService,
        IGdprComplianceService gdprComplianceService,
        IRateLimitingService rateLimitingService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _identityVerificationService = identityVerificationService;
        _gdprComplianceService = gdprComplianceService;
        _rateLimitingService = rateLimitingService;
    }

    public async Task<EnhancedRegistrationResponseDto> RegisterUserWithVerificationAsync(
        RegisterUserRequestDto request, 
        string clientIdentifier, 
        string ipAddress,
        string userAgent)
    {
        _logger.LogInformation("Starting enhanced user registration for email: {Email} from IP: {IP}", 
            request.Email, ipAddress);

        try
        {
            // Step 1: Rate limiting check
            if (!await _rateLimitingService.CanAttemptRegistrationAsync(clientIdentifier))
            {
                await _rateLimitingService.RecordRegistrationAttemptAsync(clientIdentifier, false);
                throw new InvalidOperationException("Registration rate limit exceeded. Please try again later.");
            }

            // Step 2: Comprehensive identity verification
            var verificationResult = await _identityVerificationService.VerifyUserAsync(request, clientIdentifier, ipAddress);
            if (!verificationResult.IsValid)
            {
                await _rateLimitingService.RecordRegistrationAttemptAsync(clientIdentifier, false);
                throw new ArgumentException($"Identity verification failed: {string.Join(", ", verificationResult.Errors)}");
            }

            // Step 3: GDPR compliance check
            if (!await _gdprComplianceService.CanProcessUserDataAsync(Guid.Empty, "USER_ONBOARDING"))
            {
                await _rateLimitingService.RecordRegistrationAttemptAsync(clientIdentifier, false);
                throw new InvalidOperationException("Data processing requirements not met for registration.");
            }

            await _unitOfWork.BeginTransactionAsync();

            // Step 4: Create user with encrypted PII
            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = _gdprComplianceService.EncryptPii(request.FullName.Trim()),
                Email = request.Email.Trim().ToLowerInvariant(),
                Phone = _gdprComplianceService.EncryptPii(request.Phone.Trim()),
                DateOfBirth = request.DateOfBirth,
                CPF = _gdprComplianceService.EncryptPii(request.CPF.Trim()),
                IsEmailVerified = false,
                IsPhoneVerified = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                PasswordHash = string.Empty,
                SecurityQuestion = string.Empty,
                SecurityAnswerHash = string.Empty,
                MfaOption = string.Empty,
                PasswordStrength = 0
            };

            var savedUser = await _unitOfWork.Users.AddAsync(user);

            // Step 5: Record mandatory GDPR consents
            await _gdprComplianceService.RecordConsentAsync(savedUser.Id, "DATA_PROCESSING", ipAddress, userAgent);
            await _gdprComplianceService.RecordConsentAsync(savedUser.Id, "TERMS_CONDITIONS", ipAddress, userAgent);

            // Step 6: Log data processing activities
            await _gdprComplianceService.LogDataProcessingAsync(savedUser.Id, "USER_REGISTRATION", "USER_ONBOARDING", "CONSENT");

            // Step 7: Log security event
            await LogSecurityEventAsync(savedUser.Id, "USER_REGISTRATION", "INFO", 
                $"Enhanced user registration completed from IP: {ipAddress}", true);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            // Step 8: Record successful registration attempt
            await _rateLimitingService.RecordRegistrationAttemptAsync(clientIdentifier, true);

            _logger.LogInformation("Enhanced user registration completed successfully for user ID: {UserId}", savedUser.Id);

            return new EnhancedRegistrationResponseDto
            {
                UserId = savedUser.Id,
                FullName = _gdprComplianceService.DecryptPii(savedUser.FullName),
                Email = savedUser.Email,
                Phone = _gdprComplianceService.DecryptPii(savedUser.Phone),
                CPF = _gdprComplianceService.DecryptPii(savedUser.CPF),
                DateOfBirth = savedUser.DateOfBirth,
                IsEmailVerified = savedUser.IsEmailVerified,
                IsPhoneVerified = savedUser.IsPhoneVerified,
                CreatedAt = savedUser.CreatedAt,
                VerificationReference = verificationResult.VerificationReference ?? Guid.NewGuid().ToString(),
                RequiresAdditionalVerification = verificationResult.RequiresAdditionalVerification,
                ComplianceWarnings = verificationResult.Warnings,
                GdprConsentRecorded = true,
                ComplianceReference = Guid.NewGuid().ToString()
            };
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            await _rateLimitingService.RecordRegistrationAttemptAsync(clientIdentifier, false);
            _logger.LogError(ex, "Error during enhanced user registration for email: {Email}", request.Email);
            throw;
        }
    }

    public async Task<bool> RecordRegistrationConsentAsync(
        Guid userId, 
        GdprConsentRequestDto consentRequest,
        string ipAddress,
        string userAgent)
    {
        try
        {
            _logger.LogInformation("Recording registration consents for user: {UserId}", userId);

            var tasks = new List<Task<bool>>();

            if (consentRequest.DataProcessingConsent)
            {
                tasks.Add(_gdprComplianceService.RecordConsentAsync(userId, "DATA_PROCESSING", ipAddress, userAgent));
            }

            if (consentRequest.TermsAndConditionsConsent)
            {
                tasks.Add(_gdprComplianceService.RecordConsentAsync(userId, "TERMS_CONDITIONS", ipAddress, userAgent));
            }

            if (consentRequest.MarketingConsent)
            {
                tasks.Add(_gdprComplianceService.RecordConsentAsync(userId, "MARKETING", ipAddress, userAgent));
            }

            var results = await Task.WhenAll(tasks);
            return results.All(r => r);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording registration consents for user: {UserId}", userId);
            return false;
        }
    }

    public async Task<RegistrationValidationResult> ValidateRegistrationWithComplianceAsync(
        RegisterUserRequestDto request,
        string clientIdentifier,
        string ipAddress)
    {
        var result = new RegistrationValidationResult();

        try
        {
            // Check rate limiting
            if (!await _rateLimitingService.CanAttemptRegistrationAsync(clientIdentifier))
            {
                result.IsRateLimited = true;
                result.RetryAfter = await _rateLimitingService.GetTimeUntilResetAsync(clientIdentifier, "REGISTRATION");
                result.Errors.Add("Too many registration attempts. Please try again later.");
                return result;
            }

            // Perform identity verification
            var verificationResult = await _identityVerificationService.VerifyUserAsync(request, clientIdentifier, ipAddress);
            
            result.IsValid = verificationResult.IsValid;
            result.Errors.AddRange(verificationResult.Errors);
            result.Warnings.AddRange(verificationResult.Warnings);
            result.RequiresAdditionalVerification = verificationResult.RequiresAdditionalVerification;

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration validation for email: {Email}", request.Email);
            result.IsValid = false;
            result.Errors.Add("Validation service temporarily unavailable.");
            return result;
        }
    }

    // Implement IRegistrationService interface methods
    public async Task<UserRegistrationResponseDto> RegisterUserAsync(RegisterUserRequestDto request)
    {
        // Use enhanced registration with default values
        var enhancedResult = await RegisterUserWithVerificationAsync(request, "unknown", "0.0.0.0", "unknown");
        
        return new UserRegistrationResponseDto
        {
            UserId = enhancedResult.UserId,
            FullName = enhancedResult.FullName,
            Email = enhancedResult.Email,
            Phone = enhancedResult.Phone,
            CPF = enhancedResult.CPF,
            DateOfBirth = enhancedResult.DateOfBirth,
            IsEmailVerified = enhancedResult.IsEmailVerified,
            IsPhoneVerified = enhancedResult.IsPhoneVerified,
            CreatedAt = enhancedResult.CreatedAt
        };
    }

    public async Task<RegistrationCompleteResponseDto> SetupSecurityAsync(Guid userId, SetupSecurityRequestDto request)
    {
        _logger.LogInformation("Completing enhanced registration for user ID: {UserId}", userId);

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            throw new ArgumentException("User not found", nameof(userId));
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync();

            // Update user with security information
            user.PasswordHash = HashPassword(request.Password);
            user.SecurityQuestion = request.SecurityQuestion;
            user.SecurityAnswerHash = HashPassword(request.SecurityAnswer.ToLowerInvariant());
            user.MfaOption = request.MfaOption;
            user.PasswordStrength = request.PasswordStrength;
            user.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Users.Update(user);

            // Create account for user
            var accountNumber = GenerateAccountNumber();
            var account = new Account
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                AccountNumber = accountNumber,
                BranchCode = "0001",
                AccountType = "CHECKING",
                Balance = 0.00m,
                DailyLimit = 5000.00m,
                MonthlyLimit = 50000.00m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var savedAccount = await _unitOfWork.Accounts.AddAsync(account);

            // Log GDPR-compliant data processing
            await _gdprComplianceService.LogDataProcessingAsync(user.Id, "SECURITY_SETUP", "ACCOUNT_SECURITY", "CONTRACT");
            await _gdprComplianceService.LogDataProcessingAsync(user.Id, "ACCOUNT_CREATION", "SERVICE_PROVISION", "CONTRACT");

            // Log security events
            await LogSecurityEventAsync(user.Id, "SECURITY_SETUP", "INFO", 
                $"Security configuration completed with MFA option: {request.MfaOption}", true);
            
            await LogSecurityEventAsync(user.Id, "ACCOUNT_CREATION", "INFO", 
                $"Account created with number: {accountNumber}", true);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Enhanced registration completed successfully for user ID: {UserId} with account: {AccountNumber}", 
                user.Id, accountNumber);

            return new RegistrationCompleteResponseDto
            {
                User = new UserRegistrationResponseDto
                {
                    UserId = user.Id,
                    FullName = _gdprComplianceService.DecryptPii(user.FullName),
                    Email = user.Email,
                    Phone = _gdprComplianceService.DecryptPii(user.Phone),
                    CPF = _gdprComplianceService.DecryptPii(user.CPF),
                    DateOfBirth = user.DateOfBirth,
                    IsEmailVerified = user.IsEmailVerified,
                    IsPhoneVerified = user.IsPhoneVerified,
                    CreatedAt = user.CreatedAt
                },
                Account = new AccountResponseDto
                {
                    AccountId = savedAccount.Id,
                    UserId = savedAccount.UserId,
                    AccountNumber = savedAccount.AccountNumber,
                    BranchCode = savedAccount.BranchCode,
                    AccountType = savedAccount.AccountType,
                    Balance = savedAccount.Balance,
                    DailyLimit = savedAccount.DailyLimit,
                    MonthlyLimit = savedAccount.MonthlyLimit,
                    IsActive = savedAccount.IsActive,
                    CreatedAt = savedAccount.CreatedAt
                },
                SecurityConfigured = "Completed",
                MfaOption = user.MfaOption,
                PasswordStrength = user.PasswordStrength,
                CompletedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error completing enhanced registration for user ID: {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> ValidateUserDataAsync(RegisterUserRequestDto request)
    {
        var result = await ValidateRegistrationWithComplianceAsync(request, "validation", "0.0.0.0");
        return result.IsValid;
    }

    public async Task<bool> IsEmailAvailableAsync(string email)
    {
        return await _unitOfWork.Users.IsEmailUniqueAsync(email.Trim().ToLowerInvariant());
    }

    public async Task<bool> IsCPFAvailableAsync(string cpf)
    {
        return await _unitOfWork.Users.IsCPFUniqueAsync(cpf.Trim());
    }

    public async Task<bool> IsPhoneAvailableAsync(string phone)
    {
        return await _unitOfWork.Users.IsPhoneUniqueAsync(phone.Trim());
    }

    // Private helper methods
    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var saltedPassword = password + "ContosoBank2025";
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
        return Convert.ToBase64String(hashedBytes);
    }

    private string GenerateAccountNumber()
    {
        var random = new Random();
        var accountNumber = "";
        for (int i = 0; i < 10; i++)
        {
            accountNumber += random.Next(0, 10).ToString();
        }
        return accountNumber;
    }

    private async Task LogSecurityEventAsync(Guid userId, string eventType, string severity, 
        string description, bool isSuccessful)
    {
        var securityEvent = new SecurityEvent
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EventType = eventType,
            Severity = severity,
            Description = description,
            IsSuccessful = isSuccessful,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.SecurityEvents.AddAsync(securityEvent);
    }
}

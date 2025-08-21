using ContosoBank.Application.DTOs;
using ContosoBank.Application.Interfaces;
using ContosoBank.Domain.Entities;
using ContosoBank.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace ContosoBank.Application.Services;

public class RegistrationService : IRegistrationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RegistrationService> _logger;

    public RegistrationService(
        IUnitOfWork unitOfWork, 
        ILogger<RegistrationService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<UserRegistrationResponseDto> RegisterUserAsync(RegisterUserRequestDto request)
    {
        _logger.LogInformation("Starting user registration for email: {Email}", request.Email);

        // Validate business rules
        await ValidateRegistrationDataAsync(request);

        // Create user entity
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Phone = request.Phone.Trim(),
            DateOfBirth = request.DateOfBirth,
            CPF = request.CPF.Trim(),
            IsEmailVerified = false,
            IsPhoneVerified = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            // Security fields will be set during security setup
            PasswordHash = string.Empty,
            SecurityQuestion = string.Empty,
            SecurityAnswerHash = string.Empty,
            MfaOption = string.Empty,
            PasswordStrength = 0
        };

        try
        {
            await _unitOfWork.BeginTransactionAsync();

            // Save user
            var savedUser = await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            // Log security event
            await LogSecurityEventAsync(savedUser.Id, "USER_REGISTRATION", "INFO", 
                "User registration initiated", true);

            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("User registration completed successfully for user ID: {UserId}", savedUser.Id);

            return new UserRegistrationResponseDto
            {
                UserId = savedUser.Id,
                FullName = savedUser.FullName,
                Email = savedUser.Email,
                Phone = savedUser.Phone,
                CPF = savedUser.CPF,
                DateOfBirth = savedUser.DateOfBirth,
                IsEmailVerified = savedUser.IsEmailVerified,
                IsPhoneVerified = savedUser.IsPhoneVerified,
                CreatedAt = savedUser.CreatedAt
            };
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error during user registration for email: {Email}", request.Email);
            throw;
        }
    }

    public async Task<RegistrationCompleteResponseDto> SetupSecurityAsync(Guid userId, SetupSecurityRequestDto request)
    {
        _logger.LogInformation("Completing registration for user ID: {UserId}", userId);

        // Get existing user
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
                BranchCode = "0001", // Default branch
                AccountType = "CHECKING",
                Balance = 0.00m,
                DailyLimit = 5000.00m,
                MonthlyLimit = 50000.00m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var savedAccount = await _unitOfWork.Accounts.AddAsync(account);

            // Log security events
            await LogSecurityEventAsync(user.Id, "SECURITY_SETUP", "INFO", 
                $"Security configuration completed with MFA option: {request.MfaOption}", true);
            
            await LogSecurityEventAsync(user.Id, "ACCOUNT_CREATION", "INFO", 
                $"Account created with number: {accountNumber}", true);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Registration completed successfully for user ID: {UserId} with account: {AccountNumber}", 
                user.Id, accountNumber);

            return new RegistrationCompleteResponseDto
            {
                User = new UserRegistrationResponseDto
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    Phone = user.Phone,
                    CPF = user.CPF,
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
            _logger.LogError(ex, "Error completing registration for user ID: {UserId}", request.UserId);
            throw;
        }
    }

    public async Task<bool> ValidateUserDataAsync(RegisterUserRequestDto request)
    {
        return await ValidateRegistrationDataAsync(request);
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

    private async Task<bool> ValidateRegistrationDataAsync(RegisterUserRequestDto request)
    {
        // Age validation (must be 18+)
        var age = DateTime.UtcNow.Year - request.DateOfBirth.Year;
        if (request.DateOfBirth.Date > DateTime.UtcNow.AddYears(-age))
            age--;

        if (age < 18)
        {
            throw new ArgumentException("User must be at least 18 years old", nameof(request.DateOfBirth));
        }

        // Uniqueness validation
        if (!await IsEmailAvailableAsync(request.Email))
        {
            throw new ArgumentException("Email address is already registered", nameof(request.Email));
        }

        if (!await IsCPFAvailableAsync(request.CPF))
        {
            throw new ArgumentException("CPF is already registered", nameof(request.CPF));
        }

        if (!await IsPhoneAvailableAsync(request.Phone))
        {
            throw new ArgumentException("Phone number is already registered", nameof(request.Phone));
        }

        // CPF validation
        if (!IsValidCpf(request.CPF))
        {
            throw new ArgumentException("Invalid CPF format or check digits", nameof(request.CPF));
        }

        return true;
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

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var saltedPassword = password + "ContosoBank2025"; // Simple salt for demo
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
        return Convert.ToBase64String(hashedBytes);
    }

    private string GenerateAccountNumber()
    {
        // Generate a random 10-digit account number
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

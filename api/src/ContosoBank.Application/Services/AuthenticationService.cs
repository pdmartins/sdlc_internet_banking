using System.Security.Cryptography;
using System.Text;
using ContosoBank.Application.DTOs;
using ContosoBank.Application.Interfaces;
using ContosoBank.Domain.Entities;
using ContosoBank.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ContosoBank.Application.Services;

/// <summary>
/// Service for handling user authentication operations
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRateLimitingService _rateLimitingService;
    private readonly ISessionService _sessionService;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly int _maxFailedAttempts;
    private readonly int _lockoutDurationMinutes;

    public AuthenticationService(
        IUnitOfWork unitOfWork,
        IRateLimitingService rateLimitingService,
        ISessionService sessionService,
        ILogger<AuthenticationService> logger,
        IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _rateLimitingService = rateLimitingService;
        _sessionService = sessionService;
        _logger = logger;
        _configuration = configuration;
        _maxFailedAttempts = _configuration.GetValue<int>("Authentication:MaxFailedAttempts", 5);
        _lockoutDurationMinutes = _configuration.GetValue<int>("Authentication:LockoutDurationMinutes", 30);
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, string clientIpAddress, string userAgent)
    {
        _logger.LogInformation("Login attempt for email: {Email} from IP: {IP}", request.Email, clientIpAddress);

        // Check rate limiting
        if (!await _rateLimitingService.CanAttemptLoginAsync(clientIpAddress))
        {
            await RecordLoginFailureAsync(request.Email, clientIpAddress, "Rate limit exceeded");
            throw new InvalidOperationException("Muitas tentativas de login. Tente novamente mais tarde.");
        }

        // Check if account is locked
        if (await IsAccountLockedAsync(request.Email))
        {
            await RecordLoginFailureAsync(request.Email, clientIpAddress, "Account locked");
            throw new InvalidOperationException("Conta bloqueada devido a muitas tentativas de login falharam. Tente novamente mais tarde.");
        }

        // Find user by email
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email.Trim().ToLowerInvariant());
        if (user == null)
        {
            await RecordLoginFailureAsync(request.Email, clientIpAddress, "User not found");
            await _rateLimitingService.RecordAttemptAsync(clientIpAddress, "LOGIN", false);
            throw new UnauthorizedAccessException("Email ou senha inválidos.");
        }

        // Check if user is active
        if (!user.IsActive)
        {
            await RecordLoginFailureAsync(request.Email, clientIpAddress, "Account inactive");
            await _rateLimitingService.RecordAttemptAsync(clientIpAddress, "LOGIN", false);
            throw new UnauthorizedAccessException("Conta inativa. Entre em contato com o suporte.");
        }

        // Validate password
        if (!ValidatePassword(request.Password, user.PasswordHash))
        {
            await RecordLoginFailureAsync(request.Email, clientIpAddress, "Invalid password");
            await _rateLimitingService.RecordAttemptAsync(clientIpAddress, "LOGIN", false);
            
            // Increment failed attempts
            user.FailedLoginAttempts += 1;
            user.LastFailedLoginAt = DateTime.UtcNow;
            
            // Lock account if too many attempts
            if (user.FailedLoginAttempts >= _maxFailedAttempts)
            {
                user.AccountLockedUntil = DateTime.UtcNow.AddMinutes(_lockoutDurationMinutes);
                _logger.LogWarning("Account locked for user {Email} due to {Attempts} failed attempts", 
                    request.Email, user.FailedLoginAttempts);
            }
            
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();
            
            throw new UnauthorizedAccessException("Email ou senha inválidos.");
        }

        // Reset failed attempts on successful login
        user.FailedLoginAttempts = 0;
        user.LastFailedLoginAt = null;
        user.AccountLockedUntil = null;
        user.LastLoginAt = DateTime.UtcNow;
        
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        // Record successful login
        await RecordLoginSuccessAsync(user.Id, clientIpAddress, userAgent);
        await _rateLimitingService.RecordAttemptAsync(clientIpAddress, "LOGIN", true);

        // Create session for the user
        var sessionToken = await _sessionService.CreateSessionAsync(
            user.Id, 
            clientIpAddress, 
            userAgent);

        // Get user's account
        var account = await _unitOfWork.Accounts.GetByUserIdAsync(user.Id);

        _logger.LogInformation("Successful login for user: {UserId} ({Email})", user.Id, user.Email);

        // Determine if MFA is required
        bool requiresMfa = !string.IsNullOrEmpty(user.MfaOption) && user.MfaOption != "none";

        return new LoginResponseDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Token = sessionToken, // Use session token instead of temporary token
            TokenExpiresAt = DateTime.UtcNow.AddHours(8),
            RequiresMfa = requiresMfa,
            MfaMethod = user.MfaOption,
            Account = account != null ? new AccountResponseDto
            {
                AccountNumber = account.AccountNumber,
                BranchCode = account.BranchCode,
                AccountType = account.AccountType,
                Balance = account.Balance,
                IsActive = account.IsActive
            } : null,
            Message = "Login realizado com sucesso"
        };
    }

    public async Task<bool> ValidateCredentialsAsync(string email, string password)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(email.Trim().ToLowerInvariant());
        
        if (user == null || !user.IsActive)
        {
            return false;
        }

        return ValidatePassword(password, user.PasswordHash);
    }

    public async Task RecordLoginSuccessAsync(Guid userId, string clientIpAddress, string userAgent)
    {
        var securityEvent = new SecurityEvent
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EventType = "LOGIN_SUCCESS",
            Description = "User successfully logged in",
            IpAddress = clientIpAddress,
            UserAgent = userAgent,
            CreatedAt = DateTime.UtcNow,
            Severity = "Information"
        };

        await _unitOfWork.SecurityEvents.AddAsync(securityEvent);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task RecordLoginFailureAsync(string email, string clientIpAddress, string failureReason)
    {
        var securityEvent = new SecurityEvent
        {
            Id = Guid.NewGuid(),
            UserId = null, // No user ID for failed attempts
            EventType = "LOGIN_FAILURE",
            Description = $"Failed login attempt for {email}: {failureReason}",
            IpAddress = clientIpAddress,
            UserAgent = "Unknown",
            CreatedAt = DateTime.UtcNow,
            Severity = "Warning"
        };

        await _unitOfWork.SecurityEvents.AddAsync(securityEvent);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogWarning("Failed login attempt for email {Email} from IP {IP}: {Reason}", 
            email, clientIpAddress, failureReason);
    }

    public async Task<bool> IsAccountLockedAsync(string email)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(email.Trim().ToLowerInvariant());
        
        if (user == null)
        {
            return false;
        }

        return user.AccountLockedUntil.HasValue && user.AccountLockedUntil.Value > DateTime.UtcNow;
    }

    /// <summary>
    /// Validates a password against its hash using SHA-256
    /// </summary>
    private bool ValidatePassword(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
        {
            return false;
        }

        // Use the same salt as in registration (ContosoBank2025)
        using var sha256 = SHA256.Create();
        var saltedPassword = password + "ContosoBank2025";
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
        var hashedPassword = Convert.ToBase64String(hashedBytes);
        
        return hashedPassword == storedHash;
    }
}

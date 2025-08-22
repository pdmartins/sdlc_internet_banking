using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using ContosoBank.Application.Services;
using ContosoBank.Application.DTOs;
using ContosoBank.Application.Interfaces;
using ContosoBank.Domain.Interfaces;
using ContosoBank.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace ContosoBank.Tests.Services;

public class AuthenticationServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IRateLimitingService> _mockRateLimitingService;
    private readonly Mock<ILogger<AuthenticationService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IAccountRepository> _mockAccountRepository;
    private readonly Mock<ISecurityEventRepository> _mockSecurityEventRepository;
    private readonly AuthenticationService _authenticationService;

    public AuthenticationServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockRateLimitingService = new Mock<IRateLimitingService>();
        _mockLogger = new Mock<ILogger<AuthenticationService>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockAccountRepository = new Mock<IAccountRepository>();
        _mockSecurityEventRepository = new Mock<ISecurityEventRepository>();

        // Setup configuration defaults
        _mockConfiguration.Setup(c => c.GetValue<int>("Authentication:MaxFailedAttempts", 5))
            .Returns(5);
        _mockConfiguration.Setup(c => c.GetValue<int>("Authentication:LockoutDurationMinutes", 30))
            .Returns(30);

        // Setup unit of work
        _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);
        _mockUnitOfWork.Setup(u => u.Accounts).Returns(_mockAccountRepository.Object);
        _mockUnitOfWork.Setup(u => u.SecurityEvents).Returns(_mockSecurityEventRepository.Object);

        _authenticationService = new AuthenticationService(
            _mockUnitOfWork.Object,
            _mockRateLimitingService.Object,
            _mockLogger.Object,
            _mockConfiguration.Object
        );
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsLoginResponse()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Email = "test@example.com",
            Password = "password123",
            RememberDevice = false
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FullName = "Test User",
            PasswordHash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes("password123"))),
            IsActive = true,
            FailedLoginAttempts = 0,
            MfaOption = "sms"
        };

        var account = new Account
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            AccountNumber = "123456789",
            BranchCode = "001",
            AccountType = "Checking",
            Balance = 1000.00m,
            IsActive = true
        };

        _mockRateLimitingService.Setup(r => r.CanAttemptLoginAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        _mockUserRepository.Setup(u => u.GetByEmailAsync(loginRequest.Email.ToLowerInvariant()))
            .ReturnsAsync(user);
        _mockAccountRepository.Setup(a => a.GetByUserIdAsync(user.Id))
            .ReturnsAsync(account);
        _mockRateLimitingService.Setup(r => r.RecordAttemptAsync(It.IsAny<string>(), "LOGIN", true))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _authenticationService.LoginAsync(loginRequest, "127.0.0.1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(user.Email, result.Email);
        Assert.Equal(user.FullName, result.FullName);
        Assert.True(result.RequiresMfa);
        Assert.Equal("sms", result.MfaMethod);
        Assert.NotNull(result.Account);
        Assert.Equal(account.AccountNumber, result.Account.AccountNumber);
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Email = "test@example.com",
            Password = "wrongpassword",
            RememberDevice = false
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes("correctpassword"))),
            IsActive = true,
            FailedLoginAttempts = 0
        };

        _mockRateLimitingService.Setup(r => r.CanAttemptLoginAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        _mockUserRepository.Setup(u => u.GetByEmailAsync(loginRequest.Email.ToLowerInvariant()))
            .ReturnsAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authenticationService.LoginAsync(loginRequest, "127.0.0.1"));
        
        Assert.Equal("Email ou senha inválidos.", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_UserNotFound_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Email = "nonexistent@example.com",
            Password = "password123",
            RememberDevice = false
        };

        _mockRateLimitingService.Setup(r => r.CanAttemptLoginAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        _mockUserRepository.Setup(u => u.GetByEmailAsync(loginRequest.Email.ToLowerInvariant()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authenticationService.LoginAsync(loginRequest, "127.0.0.1"));
        
        Assert.Equal("Email ou senha inválidos.", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Email = "test@example.com",
            Password = "password123",
            RememberDevice = false
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            IsActive = false
        };

        _mockRateLimitingService.Setup(r => r.CanAttemptLoginAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        _mockUserRepository.Setup(u => u.GetByEmailAsync(loginRequest.Email.ToLowerInvariant()))
            .ReturnsAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authenticationService.LoginAsync(loginRequest, "127.0.0.1"));
        
        Assert.Equal("Conta inativa. Entre em contato com o suporte.", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_RateLimitExceeded_ThrowsInvalidOperationException()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Email = "test@example.com",
            Password = "password123",
            RememberDevice = false
        };

        _mockRateLimitingService.Setup(r => r.CanAttemptLoginAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _authenticationService.LoginAsync(loginRequest, "127.0.0.1"));
        
        Assert.Equal("Muitas tentativas de login. Tente novamente mais tarde.", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_AccountLocked_ThrowsInvalidOperationException()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Email = "test@example.com",
            Password = "password123",
            RememberDevice = false
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            AccountLockedUntil = DateTime.UtcNow.AddMinutes(10)
        };

        _mockRateLimitingService.Setup(r => r.CanAttemptLoginAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        _mockUserRepository.Setup(u => u.GetByEmailAsync(loginRequest.Email.ToLowerInvariant()))
            .ReturnsAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _authenticationService.LoginAsync(loginRequest, "127.0.0.1"));
        
        Assert.Equal("Conta bloqueada devido a muitas tentativas de login falharam. Tente novamente mais tarde.", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_MultipleFailedAttempts_LocksAccount()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Email = "test@example.com",
            Password = "wrongpassword",
            RememberDevice = false
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes("correctpassword"))),
            IsActive = true,
            FailedLoginAttempts = 4 // One less than the limit
        };

        _mockRateLimitingService.Setup(r => r.CanAttemptLoginAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        _mockUserRepository.Setup(u => u.GetByEmailAsync(loginRequest.Email.ToLowerInvariant()))
            .ReturnsAsync(user);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authenticationService.LoginAsync(loginRequest, "127.0.0.1"));

        // Verify account was locked
        Assert.Equal(5, user.FailedLoginAttempts);
        Assert.NotNull(user.AccountLockedUntil);
        Assert.True(user.AccountLockedUntil > DateTime.UtcNow);
    }

    [Fact]
    public async Task LoginAsync_SuccessfulLogin_ResetsFailedAttempts()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Email = "test@example.com",
            Password = "password123",
            RememberDevice = false
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes("password123"))),
            IsActive = true,
            FailedLoginAttempts = 3,
            MfaOption = ""
        };

        _mockRateLimitingService.Setup(r => r.CanAttemptLoginAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        _mockUserRepository.Setup(u => u.GetByEmailAsync(loginRequest.Email.ToLowerInvariant()))
            .ReturnsAsync(user);

        // Act
        await _authenticationService.LoginAsync(loginRequest, "127.0.0.1");

        // Assert
        Assert.Equal(0, user.FailedLoginAttempts);
        Assert.Null(user.LastFailedLoginAt);
        Assert.Null(user.AccountLockedUntil);
        Assert.NotNull(user.LastLoginAt);
        Assert.True(user.LastLoginAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_ValidCredentials_ReturnsTrue()
    {
        // Arrange
        var email = "test@example.com";
        var password = "password123";

        var user = new User
        {
            Email = email,
            PasswordHash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(password))),
            IsActive = true
        };

        _mockUserRepository.Setup(u => u.GetByEmailAsync(email.ToLowerInvariant()))
            .ReturnsAsync(user);

        // Act
        var result = await _authenticationService.ValidateCredentialsAsync(email, password);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_InvalidCredentials_ReturnsFalse()
    {
        // Arrange
        var email = "test@example.com";
        var password = "wrongpassword";

        var user = new User
        {
            Email = email,
            PasswordHash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes("correctpassword"))),
            IsActive = true
        };

        _mockUserRepository.Setup(u => u.GetByEmailAsync(email.ToLowerInvariant()))
            .ReturnsAsync(user);

        // Act
        var result = await _authenticationService.ValidateCredentialsAsync(email, password);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsAccountLockedAsync_LockedAccount_ReturnsTrue()
    {
        // Arrange
        var email = "test@example.com";
        var user = new User
        {
            Email = email,
            AccountLockedUntil = DateTime.UtcNow.AddMinutes(10)
        };

        _mockUserRepository.Setup(u => u.GetByEmailAsync(email.ToLowerInvariant()))
            .ReturnsAsync(user);

        // Act
        var result = await _authenticationService.IsAccountLockedAsync(email);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsAccountLockedAsync_UnlockedAccount_ReturnsFalse()
    {
        // Arrange
        var email = "test@example.com";
        var user = new User
        {
            Email = email,
            AccountLockedUntil = null
        };

        _mockUserRepository.Setup(u => u.GetByEmailAsync(email.ToLowerInvariant()))
            .ReturnsAsync(user);

        // Act
        var result = await _authenticationService.IsAccountLockedAsync(email);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RecordLoginSuccessAsync_CreatesSecurityEvent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var clientIpAddress = "127.0.0.1";
        var userAgent = "Test User Agent";

        // Act
        await _authenticationService.RecordLoginSuccessAsync(userId, clientIpAddress, userAgent);

        // Assert
        _mockSecurityEventRepository.Verify(
            r => r.AddAsync(It.Is<SecurityEvent>(se => 
                se.UserId == userId &&
                se.EventType == "LOGIN_SUCCESS" &&
                se.IpAddress == clientIpAddress &&
                se.UserAgent == userAgent)),
            Times.Once);
    }

    [Fact]
    public async Task RecordLoginFailureAsync_CreatesSecurityEvent()
    {
        // Arrange
        var email = "test@example.com";
        var clientIpAddress = "127.0.0.1";
        var failureReason = "Invalid password";

        // Act
        await _authenticationService.RecordLoginFailureAsync(email, clientIpAddress, failureReason);

        // Assert
        _mockSecurityEventRepository.Verify(
            r => r.AddAsync(It.Is<SecurityEvent>(se => 
                se.UserId == null &&
                se.EventType == "LOGIN_FAILURE" &&
                se.IpAddress == clientIpAddress &&
                se.Description.Contains(email) &&
                se.Description.Contains(failureReason))),
            Times.Once);
    }
}

using System.Security.Cryptography;
using System.Text;
using ContosoBank.Application.DTOs;
using ContosoBank.Application.Interfaces;
using ContosoBank.Domain.Entities;
using ContosoBank.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ContosoBank.Application.Services;

public class PasswordResetService : IPasswordResetService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PasswordResetService> _logger;
    private readonly IRateLimitingService _rateLimitingService;
    private readonly IConfiguration _configuration;

    // Configuration values
    private readonly int _tokenValidityMinutes;
    private readonly int _maxAttemptsPerToken;
    private readonly int _maxRequestsPerHour;
    private readonly TimeSpan _requestWindow;

    public PasswordResetService(
        IUnitOfWork unitOfWork,
        ILogger<PasswordResetService> logger,
        IRateLimitingService rateLimitingService,
        IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _rateLimitingService = rateLimitingService;
        _configuration = configuration;

        _tokenValidityMinutes = _configuration.GetValue<int>("PasswordReset:TokenValidityMinutes", 30);
        _maxAttemptsPerToken = _configuration.GetValue<int>("PasswordReset:MaxAttemptsPerToken", 3);
        _maxRequestsPerHour = _configuration.GetValue<int>("PasswordReset:MaxRequestsPerHour", 5);
        _requestWindow = TimeSpan.FromHours(1);
    }

    public async Task<PasswordResetResponseDto> RequestPasswordResetAsync(PasswordResetRequestDto request, string clientIpAddress, string userAgent)
    {
        _logger.LogInformation("Password reset request for email: {Email}", request.Email);

        try
        {
            // Check rate limiting
            if (!await _rateLimitingService.CanAttemptAsync(clientIpAddress, "PASSWORD_RESET_REQUEST", _maxRequestsPerHour))
            {
                throw new InvalidOperationException("Muitas tentativas de redefinição de senha. Tente novamente mais tarde.");
            }

            // Additional rate limiting by email
            var recentAttempts = await _unitOfWork.PasswordResets.CountRecentAttemptsAsync(request.Email, _requestWindow);
            if (recentAttempts >= _maxRequestsPerHour)
            {
                await _rateLimitingService.RecordAttemptAsync(clientIpAddress, "PASSWORD_RESET_REQUEST", false);
                throw new InvalidOperationException("Muitas tentativas de redefinição para este email. Tente novamente mais tarde.");
            }

            // Find user
            var user = await _unitOfWork.Users.GetByEmailAsync(request.Email.Trim().ToLowerInvariant());
            if (user == null)
            {
                await _rateLimitingService.RecordAttemptAsync(clientIpAddress, "PASSWORD_RESET_REQUEST", false);
                
                // For security, don't reveal if email exists - return success anyway
                _logger.LogWarning("Password reset requested for non-existent email: {Email}", request.Email);
                return new PasswordResetResponseDto
                {
                    Success = true,
                    Message = "Se o email existir em nosso sistema, você receberá instruções para redefinir sua senha."
                };
            }

            // Invalidate any existing active reset tokens for this user
            await InvalidateActiveResetTokensAsync(user.Id);

            // Generate secure reset token
            var resetToken = GenerateResetToken();
            var tokenHash = HashToken(resetToken);

            // Create password reset record
            var passwordReset = new PasswordReset
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Email = user.Email,
                Token = resetToken,
                TokenHash = tokenHash,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_tokenValidityMinutes),
                IsUsed = false,
                AttemptCount = 0,
                MaxAttempts = _maxAttemptsPerToken,
                IsBlocked = false,
                IpAddress = clientIpAddress,
                UserAgent = userAgent
            };

            await _unitOfWork.PasswordResets.AddAsync(passwordReset);
            await _unitOfWork.SaveChangesAsync();

            // Send reset email (simulated with logging)
            await SendPasswordResetEmailAsync(user, resetToken);

            await _rateLimitingService.RecordAttemptAsync(clientIpAddress, "PASSWORD_RESET_REQUEST", true);

            _logger.LogInformation("Password reset email sent to user {UserId}", user.Id);

            return new PasswordResetResponseDto
            {
                Success = true,
                Message = "Se o email existir em nosso sistema, você receberá instruções para redefinir sua senha.",
                ExpiresAt = passwordReset.ExpiresAt
            };
        }
        catch (Exception ex)
        {
            await _rateLimitingService.RecordAttemptAsync(clientIpAddress, "PASSWORD_RESET_REQUEST", false);
            _logger.LogError(ex, "Error processing password reset request for email: {Email}", request.Email);
            throw;
        }
    }

    public async Task<PasswordResetResponseDto> ValidateResetTokenAsync(string token)
    {
        _logger.LogInformation("Validating password reset token");

        try
        {
            var passwordReset = await _unitOfWork.PasswordResets.GetByTokenAsync(token);
            if (passwordReset == null)
            {
                throw new UnauthorizedAccessException("Token de redefinição inválido ou expirado.");
            }

            if (passwordReset.IsUsed)
            {
                throw new InvalidOperationException("Este token já foi utilizado.");
            }

            if (passwordReset.ExpiresAt < DateTime.UtcNow)
            {
                throw new InvalidOperationException("Token de redefinição expirado.");
            }

            if (passwordReset.IsBlocked)
            {
                throw new InvalidOperationException("Token bloqueado devido a muitas tentativas inválidas.");
            }

            return new PasswordResetResponseDto
            {
                Success = true,
                Message = "Token válido",
                SecurityQuestion = passwordReset.User.SecurityQuestion,
                ExpiresAt = passwordReset.ExpiresAt,
                RemainingAttempts = passwordReset.MaxAttempts - passwordReset.AttemptCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating password reset token");
            throw;
        }
    }

    public async Task<PasswordResetResponseDto> ResetPasswordAsync(PasswordResetDto request, string clientIpAddress, string userAgent)
    {
        _logger.LogInformation("Processing password reset");

        try
        {
            // Check rate limiting
            if (!await _rateLimitingService.CanAttemptAsync(clientIpAddress, "PASSWORD_RESET", 10))
            {
                throw new InvalidOperationException("Muitas tentativas de redefinição. Tente novamente mais tarde.");
            }

            // Find and validate token
            var passwordReset = await _unitOfWork.PasswordResets.GetByTokenAsync(request.Token);
            if (passwordReset == null)
            {
                await _rateLimitingService.RecordAttemptAsync(clientIpAddress, "PASSWORD_RESET", false);
                throw new UnauthorizedAccessException("Token de redefinição inválido ou expirado.");
            }

            if (passwordReset.IsUsed)
            {
                await _rateLimitingService.RecordAttemptAsync(clientIpAddress, "PASSWORD_RESET", false);
                throw new InvalidOperationException("Este token já foi utilizado.");
            }

            if (passwordReset.ExpiresAt < DateTime.UtcNow)
            {
                await _rateLimitingService.RecordAttemptAsync(clientIpAddress, "PASSWORD_RESET", false);
                throw new InvalidOperationException("Token de redefinição expirado.");
            }

            if (passwordReset.IsBlocked)
            {
                await _rateLimitingService.RecordAttemptAsync(clientIpAddress, "PASSWORD_RESET", false);
                throw new InvalidOperationException("Token bloqueado devido a muitas tentativas inválidas.");
            }

            // Increment attempt count
            passwordReset.AttemptCount++;

            // Validate security answer
            var user = passwordReset.User;
            var hashedAnswer = HashSecurityAnswer(request.SecurityAnswer);
            
            if (user.SecurityAnswerHash != hashedAnswer)
            {
                // Check if max attempts reached
                if (passwordReset.AttemptCount >= passwordReset.MaxAttempts)
                {
                    passwordReset.IsBlocked = true;
                    _logger.LogWarning("Password reset token blocked for user {UserId} - too many invalid security answers", user.Id);
                }

                _unitOfWork.PasswordResets.Update(passwordReset);
                await _unitOfWork.SaveChangesAsync();

                await _rateLimitingService.RecordAttemptAsync(clientIpAddress, "PASSWORD_RESET", false);
                
                var remainingAttempts = passwordReset.MaxAttempts - passwordReset.AttemptCount;
                throw new UnauthorizedAccessException(
                    remainingAttempts > 0 
                        ? $"Resposta de segurança incorreta. Restam {remainingAttempts} tentativa(s)."
                        : "Token bloqueado devido a muitas tentativas inválidas.");
            }

            // Update password
            user.PasswordHash = HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            // Mark token as used
            passwordReset.IsUsed = true;
            passwordReset.UsedAt = DateTime.UtcNow;

            _unitOfWork.Users.Update(user);
            _unitOfWork.PasswordResets.Update(passwordReset);
            await _unitOfWork.SaveChangesAsync();

            await _rateLimitingService.RecordAttemptAsync(clientIpAddress, "PASSWORD_RESET", true);

            _logger.LogInformation("Password successfully reset for user {UserId}", user.Id);

            return new PasswordResetResponseDto
            {
                Success = true,
                Message = "Senha redefinida com sucesso. Você pode fazer login com sua nova senha."
            };
        }
        catch (Exception ex)
        {
            await _rateLimitingService.RecordAttemptAsync(clientIpAddress, "PASSWORD_RESET", false);
            _logger.LogError(ex, "Error resetting password");
            throw;
        }
    }

    private async Task InvalidateActiveResetTokensAsync(Guid userId)
    {
        var activeReset = await _unitOfWork.PasswordResets.GetActiveResetForUserAsync(userId);
        if (activeReset != null)
        {
            activeReset.IsUsed = true;
            activeReset.UsedAt = DateTime.UtcNow;
            _unitOfWork.PasswordResets.Update(activeReset);
        }
    }

    private static string GenerateResetToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("/", "_").Replace("+", "-").Replace("=", "");
    }

    private static string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token + "ContosoPasswordReset2025"));
        return Convert.ToBase64String(hashedBytes);
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var saltedPassword = password + "ContosoBank2025"; // Use same salt as registration
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
        return Convert.ToBase64String(hashedBytes);
    }

    private static string HashSecurityAnswer(string answer)
    {
        using var sha256 = SHA256.Create();
        var saltedAnswer = answer.ToLowerInvariant().Trim() + "ContosoBank2025"; // Use same salt as registration
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedAnswer));
        return Convert.ToBase64String(hashedBytes);
    }

    private async Task SendPasswordResetEmailAsync(User user, string resetToken)
    {
        // In a real implementation, this would send an email with the reset link
        // For now, we'll log the reset link for development/testing
        var resetLink = $"http://localhost:3000/reset-password?token={resetToken}";
        
        _logger.LogWarning("🔐 PASSWORD RESET EMAIL for {Email}: Link: {ResetLink} (Token expires in {Minutes} minutes)", 
            user.Email, resetLink, _tokenValidityMinutes);
        
        // TODO: Integrate with email service (SendGrid, AWS SES, etc.)
        await Task.CompletedTask;
    }
}

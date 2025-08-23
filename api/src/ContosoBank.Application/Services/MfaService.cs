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
/// Service for handling multi-factor authentication operations
/// </summary>
public class MfaService : IMfaService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRateLimitingService _rateLimitingService;
    private readonly ISessionService _sessionService;
    private readonly ILogger<MfaService> _logger;
    private readonly IConfiguration _configuration;
    private readonly int _codeValidityMinutes;
    private readonly int _maxAttempts;
    private readonly int _resendCooldownMinutes;

    public MfaService(
        IUnitOfWork unitOfWork,
        IRateLimitingService rateLimitingService,
        ISessionService sessionService,
        ILogger<MfaService> logger,
        IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _rateLimitingService = rateLimitingService;
        _sessionService = sessionService;
        _logger = logger;
        _configuration = configuration;
        _codeValidityMinutes = _configuration.GetValue<int>("MFA:CodeValidityMinutes", 10);
        _maxAttempts = _configuration.GetValue<int>("MFA:MaxAttempts", 3);
        _resendCooldownMinutes = _configuration.GetValue<int>("MFA:ResendCooldownMinutes", 2);
    }

    public async Task<MfaResponseDto> SendMfaCodeAsync(MfaRequestDto request, string clientIpAddress, string userAgent)
    {
        _logger.LogInformation("MFA code request for email: {Email} via {Method} from IP: {IP}", 
            request.Email, request.MfaMethod, clientIpAddress);

        try
        {
            // Check rate limiting
            if (!await _rateLimitingService.CanAttemptAsync(clientIpAddress, "MFA_REQUEST", 5))
            {
                throw new InvalidOperationException("Muitas tentativas de MFA. Tente novamente mais tarde.");
            }

            // Find user
            var user = await _unitOfWork.Users.GetByEmailAsync(request.Email.Trim().ToLowerInvariant());
            if (user == null)
            {
                await _rateLimitingService.RecordAttemptAsync(clientIpAddress, "MFA_REQUEST", false);
                throw new UnauthorizedAccessException("Usuário não encontrado.");
            }

            // Validate MFA method matches user preference
            if (user.MfaOption.ToLowerInvariant() != request.MfaMethod.ToLowerInvariant())
            {
                await _rateLimitingService.RecordAttemptAsync(clientIpAddress, "MFA_REQUEST", false);
                throw new ArgumentException("Método MFA não corresponde às preferências do usuário.");
            }

            // Clean up any existing active sessions for this user
            await CleanupActiveSessionsForUserAsync(user.Id);

            // Generate 6-digit OTP code
            var otpCode = GenerateOtpCode();
            var codeHash = HashCode(otpCode);

            // Create MFA session
            var mfaSession = new MfaSession
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Email = user.Email,
                CodeHash = codeHash,
                Method = request.MfaMethod.ToLowerInvariant(),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_codeValidityMinutes),
                IsUsed = false,
                AttemptCount = 0,
                MaxAttempts = _maxAttempts,
                IsBlocked = false,
                IpAddress = clientIpAddress,
                UserAgent = userAgent
            };

            // Log the generated MFA code for development/testing
            _logger.LogWarning("🔐 MFA CODE GENERATED for {Email}: {Code} (Method: {Method}) - Session: {SessionId}", 
                user.Email, otpCode, request.MfaMethod, mfaSession.Id);

            await _unitOfWork.MfaSessions.AddAsync(mfaSession);
            await _unitOfWork.SaveChangesAsync();

            // Send the code via the specified method
            await SendCodeAsync(user, otpCode, request.MfaMethod);

            await _rateLimitingService.RecordAttemptAsync(clientIpAddress, "MFA_REQUEST", true);

            _logger.LogInformation("MFA code sent successfully to user {UserId} via {Method}", 
                user.Id, request.MfaMethod);

            return new MfaResponseDto
            {
                Success = true,
                Message = GetSuccessMessage(request.MfaMethod),
                SessionId = mfaSession.Id.ToString(),
                ExpiresAt = mfaSession.ExpiresAt,
                RemainingAttempts = _maxAttempts,
                CanResend = false,
                NextResendAt = DateTime.UtcNow.AddMinutes(_resendCooldownMinutes)
            };
        }
        catch (Exception ex)
        {
            await _rateLimitingService.RecordAttemptAsync(clientIpAddress, "MFA_REQUEST", false);
            _logger.LogError(ex, "Error sending MFA code for email: {Email}", request.Email);
            throw;
        }
    }

    public async Task<MfaVerificationResponseDto> VerifyMfaCodeAsync(MfaVerificationDto request, string clientIpAddress, string userAgent)
    {
        _logger.LogInformation("MFA code verification for email: {Email} with session: {SessionId}", 
            request.Email, request.SessionId);

        try
        {
            // Check rate limiting
            if (!await _rateLimitingService.CanAttemptAsync(clientIpAddress, "MFA_VERIFY", 10))
            {
                throw new InvalidOperationException("Muitas tentativas de verificação MFA. Tente novamente mais tarde.");
            }

            // Find MFA session
            if (!Guid.TryParse(request.SessionId, out var sessionGuid))
            {
                await _rateLimitingService.RecordAttemptAsync(clientIpAddress, "MFA_VERIFY", false);
                throw new ArgumentException("ID de sessão inválido.");
            }

            var mfaSession = await _unitOfWork.MfaSessions.GetByIdAsync(sessionGuid);
            if (mfaSession == null)
            {
                await _rateLimitingService.RecordAttemptAsync(clientIpAddress, "MFA_VERIFY", false);
                throw new UnauthorizedAccessException("Sessão MFA não encontrada.");
            }

            // Validate session
            if (mfaSession.Email.ToLowerInvariant() != request.Email.Trim().ToLowerInvariant())
            {
                await _rateLimitingService.RecordAttemptAsync(clientIpAddress, "MFA_VERIFY", false);
                throw new UnauthorizedAccessException("Email não corresponde à sessão MFA.");
            }

            if (mfaSession.IsUsed)
            {
                await _rateLimitingService.RecordAttemptAsync(clientIpAddress, "MFA_VERIFY", false);
                throw new InvalidOperationException("Código MFA já foi usado.");
            }

            if (mfaSession.ExpiresAt < DateTime.UtcNow)
            {
                await _rateLimitingService.RecordAttemptAsync(clientIpAddress, "MFA_VERIFY", false);
                throw new InvalidOperationException("Código MFA expirou. Solicite um novo código.");
            }

            if (mfaSession.IsBlocked)
            {
                await _rateLimitingService.RecordAttemptAsync(clientIpAddress, "MFA_VERIFY", false);
                throw new InvalidOperationException("Sessão MFA bloqueada devido a muitas tentativas incorretas.");
            }

            // Verify the code
            var codeHash = HashCode(request.Code);
            var isValidCode = mfaSession.CodeHash == codeHash;

            mfaSession.AttemptCount++;

            if (!isValidCode)
            {
                if (mfaSession.AttemptCount >= mfaSession.MaxAttempts)
                {
                    mfaSession.IsBlocked = true;
                    _logger.LogWarning("MFA session {SessionId} blocked due to too many attempts", mfaSession.Id);
                }

                _unitOfWork.MfaSessions.Update(mfaSession);
                await _unitOfWork.SaveChangesAsync();

                await _rateLimitingService.RecordAttemptAsync(clientIpAddress, "MFA_VERIFY", false);

                return new MfaVerificationResponseDto
                {
                    Success = false,
                    Message = "Código MFA inválido.",
                    RemainingAttempts = Math.Max(0, mfaSession.MaxAttempts - mfaSession.AttemptCount),
                    IsLocked = mfaSession.IsBlocked,
                    LockedUntil = mfaSession.IsBlocked ? mfaSession.ExpiresAt : null
                };
            }

            // Code is valid - mark session as used
            mfaSession.IsUsed = true;
            mfaSession.UsedAt = DateTime.UtcNow;
            _unitOfWork.MfaSessions.Update(mfaSession);

            // Create a proper session token using SessionService
            var sessionToken = await _sessionService.CreateSessionAsync(
                mfaSession.UserId, 
                clientIpAddress, 
                userAgent);

            await _unitOfWork.SaveChangesAsync();
            await _rateLimitingService.RecordAttemptAsync(clientIpAddress, "MFA_VERIFY", true);

            _logger.LogInformation("MFA verification successful for user {UserId}", mfaSession.UserId);

            return new MfaVerificationResponseDto
            {
                Success = true,
                Message = "MFA verificado com sucesso.",
                AccessToken = sessionToken, // Use proper session token
                RemainingAttempts = Math.Max(0, mfaSession.MaxAttempts - mfaSession.AttemptCount),
                IsLocked = false
            };
        }
        catch (Exception ex)
        {
            await _rateLimitingService.RecordAttemptAsync(clientIpAddress, "MFA_VERIFY", false);
            _logger.LogError(ex, "Error verifying MFA code for email: {Email}", request.Email);
            throw;
        }
    }

    public async Task<MfaResponseDto> ResendMfaCodeAsync(string sessionId, string clientIpAddress, string userAgent)
    {
        _logger.LogInformation("MFA code resend request for session: {SessionId}", sessionId);

        try
        {
            // Check rate limiting
            if (!await _rateLimitingService.CanAttemptAsync(clientIpAddress, "MFA_RESEND", 3))
            {
                throw new InvalidOperationException("Muitas tentativas de reenvio. Tente novamente mais tarde.");
            }

            if (!Guid.TryParse(sessionId, out var sessionGuid))
            {
                await _rateLimitingService.RecordAttemptAsync(clientIpAddress, "MFA_RESEND", false);
                throw new ArgumentException("ID de sessão inválido.");
            }

            var mfaSession = await _unitOfWork.MfaSessions.GetByIdAsync(sessionGuid);
            if (mfaSession == null)
            {
                await _rateLimitingService.RecordAttemptAsync(clientIpAddress, "MFA_RESEND", false);
                throw new UnauthorizedAccessException("Sessão MFA não encontrada.");
            }

            if (mfaSession.IsUsed)
            {
                await _rateLimitingService.RecordAttemptAsync(clientIpAddress, "MFA_RESEND", false);
                throw new InvalidOperationException("Sessão MFA já foi usada.");
            }

            if (mfaSession.ExpiresAt < DateTime.UtcNow)
            {
                await _rateLimitingService.RecordAttemptAsync(clientIpAddress, "MFA_RESEND", false);
                throw new InvalidOperationException("Sessão MFA expirou.");
            }

            // Check resend cooldown
            var timeSinceCreated = DateTime.UtcNow - mfaSession.CreatedAt;
            if (timeSinceCreated.TotalMinutes < _resendCooldownMinutes)
            {
                await _rateLimitingService.RecordAttemptAsync(clientIpAddress, "MFA_RESEND", false);
                var nextResendTime = mfaSession.CreatedAt.AddMinutes(_resendCooldownMinutes);
                throw new InvalidOperationException($"Aguarde até {nextResendTime:HH:mm} para reenviar o código.");
            }

            // Get user
            var user = await _unitOfWork.Users.GetByIdAsync(mfaSession.UserId);
            if (user == null)
            {
                await _rateLimitingService.RecordAttemptAsync(clientIpAddress, "MFA_RESEND", false);
                throw new UnauthorizedAccessException("Usuário não encontrado.");
            }

            // Generate new OTP code
            var otpCode = GenerateOtpCode();
            var codeHash = HashCode(otpCode);

            // Log the resent MFA code for development/testing
            _logger.LogWarning("🔄 MFA CODE RESENT for {Email}: {Code} (Method: {Method}) - Session: {SessionId}", 
                user.Email, otpCode, mfaSession.Method, mfaSession.Id);

            // Update session with new code
            mfaSession.CodeHash = codeHash;
            mfaSession.CreatedAt = DateTime.UtcNow;
            mfaSession.ExpiresAt = DateTime.UtcNow.AddMinutes(_codeValidityMinutes);
            mfaSession.AttemptCount = 0;
            mfaSession.IsBlocked = false;

            _unitOfWork.MfaSessions.Update(mfaSession);
            await _unitOfWork.SaveChangesAsync();

            // Send the new code
            await SendCodeAsync(user, otpCode, mfaSession.Method);

            await _rateLimitingService.RecordAttemptAsync(clientIpAddress, "MFA_RESEND", true);

            _logger.LogInformation("MFA code resent successfully for user {UserId}", user.Id);

            return new MfaResponseDto
            {
                Success = true,
                Message = GetSuccessMessage(mfaSession.Method),
                SessionId = mfaSession.Id.ToString(),
                ExpiresAt = mfaSession.ExpiresAt,
                RemainingAttempts = _maxAttempts,
                CanResend = false,
                NextResendAt = DateTime.UtcNow.AddMinutes(_resendCooldownMinutes)
            };
        }
        catch (Exception ex)
        {
            await _rateLimitingService.RecordAttemptAsync(clientIpAddress, "MFA_RESEND", false);
            _logger.LogError(ex, "Error resending MFA code for session: {SessionId}", sessionId);
            throw;
        }
    }

    public async Task CleanupExpiredSessionsAsync()
    {
        try
        {
            var expiredSessions = await _unitOfWork.MfaSessions.GetExpiredSessionsAsync();
            foreach (var session in expiredSessions)
            {
                _unitOfWork.MfaSessions.Remove(session);
            }
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Cleaned up {Count} expired MFA sessions", expiredSessions.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired MFA sessions");
        }
    }

    public async Task<bool> IsSessionValidAsync(string sessionId)
    {
        try
        {
            if (!Guid.TryParse(sessionId, out var sessionGuid))
                return false;

            var session = await _unitOfWork.MfaSessions.GetByIdAsync(sessionGuid);
            return session != null && 
                   !session.IsUsed && 
                   !session.IsBlocked && 
                   session.ExpiresAt > DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking session validity for: {SessionId}", sessionId);
            return false;
        }
    }

    // Private helper methods
    private async Task CleanupActiveSessionsForUserAsync(Guid userId)
    {
        var activeSessions = await _unitOfWork.MfaSessions.GetActiveSessionsForUserAsync(userId);
        foreach (var session in activeSessions)
        {
            _unitOfWork.MfaSessions.Remove(session);
        }
    }

    private static string GenerateOtpCode()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        var number = Math.Abs(BitConverter.ToInt32(bytes, 0));
        return (number % 1000000).ToString("D6");
    }

    private static string HashCode(string code)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(code + "ContosoMFA2025"));
        return Convert.ToBase64String(hashedBytes)[..6]; // Take first 6 characters
    }

    private async Task SendCodeAsync(User user, string code, string method)
    {
        // In a real implementation, this would send SMS or email
        // For now, we'll just log the code for development/testing
        switch (method.ToLowerInvariant())
        {
            case "sms":
                _logger.LogWarning("📱 SMS MFA CODE for user {UserId} ({Email}): {Code} (would send to phone)", 
                    user.Id, user.Email, code);
                // TODO: Integrate with SMS service (Twilio, AWS SNS, etc.)
                break;
            case "email":
                _logger.LogWarning("📧 EMAIL MFA CODE for user {UserId} ({Email}): {Code}", 
                    user.Id, user.Email, code);
                // TODO: Integrate with email service (SendGrid, AWS SES, etc.)
                break;
            default:
                throw new ArgumentException($"Unsupported MFA method: {method}");
        }
        
        await Task.CompletedTask;
    }

    private static string GetSuccessMessage(string method)
    {
        return method.ToLowerInvariant() switch
        {
            "sms" => "Código MFA enviado via SMS. Verifique suas mensagens.",
            "email" => "Código MFA enviado via email. Verifique sua caixa de entrada.",
            _ => "Código MFA enviado com sucesso."
        };
    }

}

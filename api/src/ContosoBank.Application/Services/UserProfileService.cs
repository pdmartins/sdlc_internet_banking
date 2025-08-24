using Microsoft.Extensions.Logging;
using ContosoBank.Application.DTOs;
using ContosoBank.Application.Interfaces;
using ContosoBank.Domain.Entities;
using ContosoBank.Domain.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace ContosoBank.Application.Services;

public class UserProfileService : IUserProfileService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGdprComplianceService _gdprService;
    private readonly ILogger<UserProfileService> _logger;

    public UserProfileService(
        IUnitOfWork unitOfWork,
        IGdprComplianceService gdprService,
        ILogger<UserProfileService> logger)
    {
        _unitOfWork = unitOfWork;
        _gdprService = gdprService;
        _logger = logger;
    }

    public async Task<UserProfileDto?> GetUserProfileAsync(Guid userId)
    {
        _logger.LogInformation("Getting profile for user: {UserId}", userId);

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User not found: {UserId}", userId);
            return null;
        }

        // Log data access for GDPR compliance
        await _gdprService.LogDataProcessingAsync(
            userId, 
            "Profile Access", 
            "User accessed their profile information", 
            "ViewProfile");

        return new UserProfileDto
        {
            UserId = user.Id,
            FullName = SafeDecrypt(user.FullName),
            Email = user.Email,
            Phone = SafeDecrypt(user.Phone),
            DateOfBirth = user.DateOfBirth,
            CPF = user.CPF, // This should remain masked for display
            IsEmailVerified = user.IsEmailVerified,
            IsPhoneVerified = user.IsPhoneVerified,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            LastLoginAt = user.LastLoginAt,
            MfaOption = user.MfaOption,
            SecurityQuestion = user.SecurityQuestion,
            PasswordStrength = user.PasswordStrength
        };
    }

    public async Task<UserProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileRequestDto request)
    {
        _logger.LogInformation("Updating profile for user: {UserId}", userId);

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        // Verify current password
        if (!ValidatePassword(request.CurrentPassword, user.PasswordHash))
        {
            _logger.LogWarning("Password verification failed for user: {UserId}", userId);
            throw new UnauthorizedAccessException("Current password is incorrect");
        }

        // Validate age (must be 18+)
        var age = DateTime.Now.Year - request.DateOfBirth.Year;
        if (request.DateOfBirth.Date > DateTime.Now.AddYears(-age)) age--;
        if (age < 18)
        {
            throw new ArgumentException("User must be at least 18 years old");
        }

        // Check for email uniqueness if email is changing
        if (request.Email.ToLowerInvariant() != user.Email.ToLowerInvariant())
        {
            var existingUser = await _unitOfWork.Users.GetByEmailAsync(request.Email);
            if (existingUser != null)
            {
                throw new ArgumentException("Email address is already in use");
            }
        }

        // Check for phone uniqueness if phone is changing
        var currentPhone = SafeDecrypt(user.Phone);
        if (request.Phone != currentPhone)
        {
            var existingUserByPhone = await _unitOfWork.Users.GetByPhoneAsync(_gdprService.EncryptPii(request.Phone));
            if (existingUserByPhone != null)
            {
                throw new ArgumentException("Phone number is already in use");
            }
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync();

            // Log the update for GDPR compliance
            var changes = new List<string>();
            if (user.FullName != _gdprService.EncryptPii(request.FullName))
                changes.Add("FullName");
            if (user.Email != request.Email.ToLowerInvariant())
                changes.Add("Email");
            if (user.Phone != _gdprService.EncryptPii(request.Phone))
                changes.Add("Phone");
            if (user.DateOfBirth != request.DateOfBirth)
                changes.Add("DateOfBirth");

            await _gdprService.LogDataProcessingAsync(
                userId,
                "Profile Update",
                $"User updated profile fields: {string.Join(", ", changes)}",
                "UpdateProfile");

            // Update user information
            user.FullName = _gdprService.EncryptPii(request.FullName.Trim());
            user.Email = request.Email.Trim().ToLowerInvariant();
            user.Phone = _gdprService.EncryptPii(request.Phone.Trim());
            user.DateOfBirth = request.DateOfBirth;
            user.UpdatedAt = DateTime.UtcNow;

            // Reset email verification if email changed
            if (changes.Contains("Email"))
            {
                user.IsEmailVerified = false;
            }

            // Reset phone verification if phone changed
            if (changes.Contains("Phone"))
            {
                user.IsPhoneVerified = false;
            }

            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Profile updated successfully for user: {UserId}", userId);

            return new UserProfileDto
            {
                UserId = user.Id,
                FullName = request.FullName,
                Email = user.Email,
                Phone = request.Phone,
                DateOfBirth = user.DateOfBirth,
                CPF = user.CPF,
                IsEmailVerified = user.IsEmailVerified,
                IsPhoneVerified = user.IsPhoneVerified,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                LastLoginAt = user.LastLoginAt,
                MfaOption = user.MfaOption,
                SecurityQuestion = user.SecurityQuestion,
                PasswordStrength = user.PasswordStrength
            };
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Failed to update profile for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> UpdatePasswordAsync(Guid userId, UpdatePasswordRequestDto request)
    {
        _logger.LogInformation("Updating password for user: {UserId}", userId);

        if (request.NewPassword != request.ConfirmPassword)
        {
            throw new ArgumentException("New password and confirmation do not match");
        }

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        // Verify current password
        if (!ValidatePassword(request.CurrentPassword, user.PasswordHash))
        {
            _logger.LogWarning("Password verification failed for user: {UserId}", userId);
            throw new UnauthorizedAccessException("Current password is incorrect");
        }

        // Calculate password strength
        var passwordStrength = CalculatePasswordStrength(request.NewPassword);

        try
        {
            await _unitOfWork.BeginTransactionAsync();

            // Hash new password
            user.PasswordHash = HashPassword(request.NewPassword);
            user.PasswordStrength = passwordStrength;
            user.UpdatedAt = DateTime.UtcNow;

            // Reset failed login attempts on successful password change
            user.FailedLoginAttempts = 0;
            user.LastFailedLoginAt = null;
            user.AccountLockedUntil = null;

            _unitOfWork.Users.Update(user);

            // Log security event
            await _gdprService.LogDataProcessingAsync(
                userId,
                "Password Change",
                "User changed their password",
                "UpdatePassword");

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Password updated successfully for user: {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Failed to update password for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> UpdateSecurityQuestionAsync(Guid userId, UpdateSecurityQuestionRequestDto request)
    {
        _logger.LogInformation("Updating security question for user: {UserId}", userId);

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        // Verify current password
        if (!ValidatePassword(request.CurrentPassword, user.PasswordHash))
        {
            _logger.LogWarning("Password verification failed for user: {UserId}", userId);
            throw new UnauthorizedAccessException("Current password is incorrect");
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync();

            // Update security question and hash the answer
            user.SecurityQuestion = request.SecurityQuestion;
            user.SecurityAnswerHash = HashSecurityAnswer(request.SecurityAnswer);
            user.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Users.Update(user);

            // Log security event
            await _gdprService.LogDataProcessingAsync(
                userId,
                "Security Question Change",
                "User updated their security question",
                "UpdateSecurityQuestion");

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Security question updated successfully for user: {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Failed to update security question for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> UpdateMfaOptionAsync(Guid userId, UpdateMfaRequestDto request)
    {
        _logger.LogInformation("Updating MFA option for user: {UserId}", userId);

        if (request.MfaOption != "sms" && request.MfaOption != "authenticator")
        {
            throw new ArgumentException("Invalid MFA option. Must be 'sms' or 'authenticator'");
        }

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        // Verify current password
        if (!ValidatePassword(request.CurrentPassword, user.PasswordHash))
        {
            _logger.LogWarning("Password verification failed for user: {UserId}", userId);
            throw new UnauthorizedAccessException("Current password is incorrect");
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync();

            user.MfaOption = request.MfaOption;
            user.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Users.Update(user);

            // Log security event
            await _gdprService.LogDataProcessingAsync(
                userId,
                "MFA Option Change",
                $"User changed MFA option to: {request.MfaOption}",
                "UpdateMfaOption");

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("MFA option updated successfully for user: {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Failed to update MFA option for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<SecuritySettingsDto?> GetSecuritySettingsAsync(Guid userId)
    {
        _logger.LogInformation("Getting security settings for user: {UserId}", userId);

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            return null;
        }

        // Log data access for GDPR compliance
        await _gdprService.LogDataProcessingAsync(
            userId,
            "Security Settings Access",
            "User accessed their security settings",
            "ViewSecuritySettings");

        return new SecuritySettingsDto
        {
            MfaOption = user.MfaOption,
            SecurityQuestion = user.SecurityQuestion,
            PasswordStrength = user.PasswordStrength,
            LastPasswordChange = user.UpdatedAt, // Approximation - could add specific field
            FailedLoginAttempts = user.FailedLoginAttempts,
            LastFailedLoginAt = user.LastFailedLoginAt,
            AccountLockedUntil = user.AccountLockedUntil
        };
    }

    public async Task<IEnumerable<DeviceInfoDto>> GetActiveDevicesAsync(Guid userId)
    {
        _logger.LogInformation("Getting active devices for user: {UserId}", userId);

        var sessions = await _unitOfWork.UserSessions.GetActiveSessionsForUserAsync(userId);
        
        // Log data access for GDPR compliance
        await _gdprService.LogDataProcessingAsync(
            userId,
            "Device Access",
            "User accessed their active devices list",
            "ViewDevices");

        return sessions.Select(session => new DeviceInfoDto
        {
            Id = session.Id,
            DeviceInfo = session.UserAgent ?? "Unknown Device",
            Location = session.Location ?? "Unknown Location",
            IpAddress = session.IpAddress ?? "Unknown IP",
            CreatedAt = session.CreatedAt,
            LastActivityAt = session.LastActivityAt,
            IsTrustedDevice = session.IsTrustedDevice,
            IsCurrentSession = false // This would need to be determined by comparing with current session
        });
    }

    public async Task<bool> RevokeDeviceAsync(Guid userId, Guid deviceId)
    {
        _logger.LogInformation("Revoking device {DeviceId} for user: {UserId}", deviceId, userId);

        var session = await _unitOfWork.UserSessions.GetByIdAsync(deviceId);
        if (session == null || session.UserId != userId)
        {
            return false;
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync();

            session.IsActive = false;
            session.LastActivityAt = DateTime.UtcNow;
            _unitOfWork.UserSessions.Update(session);

            // Log security event
            await _gdprService.LogDataProcessingAsync(
                userId,
                "Device Revocation",
                $"User revoked device: {session.UserAgent}",
                "RevokeDevice");

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Device revoked successfully for user: {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Failed to revoke device for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<int> RevokeAllOtherDevicesAsync(Guid userId, Guid currentSessionId)
    {
        _logger.LogInformation("Revoking all other devices for user: {UserId}, keeping session: {SessionId}", userId, currentSessionId);

        var sessions = await _unitOfWork.UserSessions.GetActiveSessionsForUserAsync(userId);
        var sessionsToRevoke = sessions.Where(s => s.Id != currentSessionId).ToList();

        if (!sessionsToRevoke.Any())
        {
            return 0;
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync();

            foreach (var session in sessionsToRevoke)
            {
                session.IsActive = false;
                session.LastActivityAt = DateTime.UtcNow;
                _unitOfWork.UserSessions.Update(session);
            }

            // Log security event
            await _gdprService.LogDataProcessingAsync(
                userId,
                "All Devices Revocation",
                $"User revoked {sessionsToRevoke.Count} other devices",
                "RevokeAllOtherDevices");

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Revoked {Count} devices for user: {UserId}", sessionsToRevoke.Count, userId);
            return sessionsToRevoke.Count;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Failed to revoke all other devices for user: {UserId}", userId);
            throw;
        }
    }

    private static int CalculatePasswordStrength(string password)
    {
        int score = 0;

        if (password.Length >= 8) score++;
        if (password.Any(char.IsLower)) score++;
        if (password.Any(char.IsUpper)) score++;
        if (password.Any(char.IsDigit)) score++;
        if (password.Any(c => !char.IsLetterOrDigit(c))) score++;

        return score;
    }

    private static string HashSecurityAnswer(string answer)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(answer.ToLowerInvariant().Trim()));
        return Convert.ToBase64String(hashedBytes);
    }

    private static bool ValidatePassword(string password, string storedHash)
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

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var saltedPassword = password + "ContosoBank2025";
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
        return Convert.ToBase64String(hashedBytes);
    }

    /// <summary>
    /// Safely decrypts PII data with error handling and fallback
    /// </summary>
    /// <param name="encryptedData">The encrypted data to decrypt</param>
    /// <returns>Decrypted data or original data if decryption fails</returns>
    private string SafeDecrypt(string encryptedData)
    {
        try
        {
            if (string.IsNullOrEmpty(encryptedData))
            {
                return string.Empty;
            }

            return _gdprService.DecryptPii(encryptedData);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to decrypt PII data, using original value. Data: {DataPreview}", 
                encryptedData.Length > 20 ? encryptedData.Substring(0, 20) + "..." : encryptedData);
            return encryptedData; // Return original data as fallback
        }
    }
}

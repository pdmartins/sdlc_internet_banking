using System.ComponentModel.DataAnnotations;

namespace ContosoBank.Application.DTOs;

/// <summary>
/// Request to send MFA code to user
/// </summary>
public class MfaRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string MfaMethod { get; set; } = string.Empty; // "sms" or "email"
}

/// <summary>
/// Request to verify MFA code
/// </summary>
public class MfaVerificationDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;
    
    [Required]
    public string SessionId { get; set; } = string.Empty;
}

/// <summary>
/// Response after sending MFA code
/// </summary>
public class MfaResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public int RemainingAttempts { get; set; }
    public bool CanResend { get; set; }
    public DateTime NextResendAt { get; set; }
}

/// <summary>
/// Response after verifying MFA code
/// </summary>
public class MfaVerificationResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? AccessToken { get; set; }
    public int RemainingAttempts { get; set; }
    public bool IsLocked { get; set; }
    public DateTime? LockedUntil { get; set; }
}

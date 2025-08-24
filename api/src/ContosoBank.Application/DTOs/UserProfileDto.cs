using System.ComponentModel.DataAnnotations;

namespace ContosoBank.Application.DTOs;

public class UserProfileDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string CPF { get; set; } = string.Empty;
    public bool IsEmailVerified { get; set; }
    public bool IsPhoneVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string MfaOption { get; set; } = string.Empty;
    public string SecurityQuestion { get; set; } = string.Empty;
    public int PasswordStrength { get; set; }
}

public class UpdateProfileRequestDto
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Required]
    public DateTime DateOfBirth { get; set; }

    [Required]
    [StringLength(500)]
    public string CurrentPassword { get; set; } = string.Empty;
}

public class UpdatePasswordRequestDto
{
    [Required]
    [StringLength(500)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [StringLength(500, MinimumLength = 8)]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class UpdateSecurityQuestionRequestDto
{
    [Required]
    [StringLength(500)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string SecurityQuestion { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string SecurityAnswer { get; set; } = string.Empty;
}

public class UpdateMfaRequestDto
{
    [Required]
    [StringLength(500)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string MfaOption { get; set; } = string.Empty; // "sms" or "authenticator"
}

public class SecuritySettingsDto
{
    public string MfaOption { get; set; } = string.Empty;
    public string SecurityQuestion { get; set; } = string.Empty;
    public int PasswordStrength { get; set; }
    public DateTime? LastPasswordChange { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LastFailedLoginAt { get; set; }
    public DateTime? AccountLockedUntil { get; set; }
}

public class DeviceInfoDto
{
    public Guid Id { get; set; }
    public string DeviceInfo { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public bool IsTrustedDevice { get; set; }
    public bool IsCurrentSession { get; set; }
}

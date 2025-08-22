using System.ComponentModel.DataAnnotations;

namespace ContosoBank.Domain.Entities;

/// <summary>
/// Entity representing a login attempt with detailed metadata for fraud detection
/// </summary>
public class LoginAttempt
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid? UserId { get; set; } // Nullable for failed attempts where user doesn't exist

    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(45)]
    public string IpAddress { get; set; } = string.Empty;

    [MaxLength(500)]
    public string UserAgent { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Country { get; set; }

    [MaxLength(100)]
    public string? Region { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    [MaxLength(100)]
    public string? DeviceFingerprint { get; set; }

    [MaxLength(50)]
    public string? DeviceType { get; set; } // Mobile, Desktop, Tablet

    [MaxLength(100)]
    public string? OperatingSystem { get; set; }

    [MaxLength(100)]
    public string? Browser { get; set; }

    [Required]
    public DateTime AttemptedAt { get; set; }

    [Required]
    public bool IsSuccessful { get; set; }

    [MaxLength(255)]
    public string? FailureReason { get; set; }

    [Required]
    public bool IsAnomalous { get; set; }

    [MaxLength(500)]
    public string? AnomalyReasons { get; set; } // JSON string of detected anomaly types

    [Required]
    public int RiskScore { get; set; } // 0-100, higher = more risky

    [MaxLength(50)]
    public string? ResponseAction { get; set; } // Allow, StepUp, Block, Lock

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public User? User { get; set; }
}

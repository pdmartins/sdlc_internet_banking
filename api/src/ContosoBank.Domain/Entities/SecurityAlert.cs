using System.ComponentModel.DataAnnotations;

namespace ContosoBank.Domain.Entities;

/// <summary>
/// Entity for tracking security alerts sent to users
/// </summary>
public class SecurityAlert
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string AlertType { get; set; } = string.Empty; // LoginAnomaly, NewDevice, PasswordChange, etc.

    [Required]
    [MaxLength(50)]
    public string Severity { get; set; } = string.Empty; // Low, Medium, High, Critical

    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Details { get; set; } = string.Empty; // JSON with additional context

    [Required]
    [MaxLength(50)]
    public string DeliveryMethod { get; set; } = string.Empty; // Email, SMS, InApp, Push

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty; // Pending, Sent, Delivered, Failed, Read

    [Required]
    public bool IsRead { get; set; }

    [Required]
    public bool RequiresAction { get; set; }

    [MaxLength(500)]
    public string ActionUrl { get; set; } = string.Empty;

    [MaxLength(100)]
    public string ActionText { get; set; } = string.Empty;

    public DateTime? ReadAt { get; set; }
    public DateTime? ActionTakenAt { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    // Reference to related entities
    public Guid? LoginAttemptId { get; set; }
    public Guid? AnomalyDetectionId { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public LoginAttempt? LoginAttempt { get; set; }
    public AnomalyDetection? AnomalyDetection { get; set; }
}

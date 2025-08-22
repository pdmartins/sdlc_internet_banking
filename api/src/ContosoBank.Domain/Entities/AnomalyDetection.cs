using System.ComponentModel.DataAnnotations;

namespace ContosoBank.Domain.Entities;

/// <summary>
/// Entity for tracking detected anomalies and their resolution
/// </summary>
public class AnomalyDetection
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid LoginAttemptId { get; set; }

    [Required]
    [MaxLength(50)]
    public string AnomalyType { get; set; } = string.Empty; // Location, Time, Device, Velocity, etc.

    [Required]
    public int Severity { get; set; } // 1-5, 5 being highest

    [Required]
    public int RiskScore { get; set; } // 0-100

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Details { get; set; } = string.Empty; // JSON with specific anomaly data

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty; // Pending, Resolved, Ignored, Escalated

    [MaxLength(50)]
    public string ResponseAction { get; set; } = string.Empty; // StepUp, Block, Lock, Allow

    [Required]
    public bool IsResolved { get; set; }

    [MaxLength(1000)]
    public string ResolutionNotes { get; set; } = string.Empty;

    public Guid? ResolvedByUserId { get; set; }
    public DateTime? ResolvedAt { get; set; }

    [Required]
    public DateTime DetectedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public LoginAttempt LoginAttempt { get; set; } = null!;
    public User? ResolvedByUser { get; set; }
}

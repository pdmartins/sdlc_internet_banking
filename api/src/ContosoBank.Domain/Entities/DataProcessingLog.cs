using System.ComponentModel.DataAnnotations;

namespace ContosoBank.Domain.Entities;

public class DataProcessingLog
{
    public Guid Id { get; set; }
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Activity { get; set; } = string.Empty; // "REGISTRATION", "LOGIN", "TRANSACTION"
    
    [Required]
    [StringLength(200)]
    public string Purpose { get; set; } = string.Empty; // "USER_ONBOARDING", "FRAUD_PREVENTION"
    
    [Required]
    [StringLength(100)]
    public string LegalBasis { get; set; } = string.Empty; // "CONSENT", "CONTRACT", "LEGITIMATE_INTEREST"
    
    [StringLength(500)]
    public string? Details { get; set; }
    
    [StringLength(45)]
    public string? IpAddress { get; set; }
    
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public virtual User User { get; set; } = null!;
}

using System.ComponentModel.DataAnnotations;

namespace ContosoBank.Domain.Entities;

public class RateLimitEntry
{
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string ClientIdentifier { get; set; } = string.Empty; // IP address, user ID, or session ID
    
    [Required]
    [StringLength(50)]
    public string AttemptType { get; set; } = string.Empty; // "REGISTRATION", "LOGIN", "TRANSACTION"
    
    public int AttemptCount { get; set; }
    
    public int SuccessfulCount { get; set; }
    
    public int FailedCount { get; set; }
    
    public DateTime FirstAttempt { get; set; }
    
    public DateTime LastAttempt { get; set; }
    
    public DateTime? BlockedUntil { get; set; }
    
    public bool IsBlocked { get; set; }
    
    [StringLength(200)]
    public string? BlockReason { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

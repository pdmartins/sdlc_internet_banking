using System.ComponentModel.DataAnnotations;

namespace ContosoBank.Domain.Entities;

public class MfaSession
{
    public Guid Id { get; set; }
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [StringLength(6)]
    public string CodeHash { get; set; } = string.Empty; // Hashed OTP code
    
    [Required]
    [StringLength(20)]
    public string Method { get; set; } = string.Empty; // "sms" or "email"
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Required]
    public DateTime ExpiresAt { get; set; }
    
    public bool IsUsed { get; set; }
    
    public DateTime? UsedAt { get; set; }
    
    public int AttemptCount { get; set; }
    
    public int MaxAttempts { get; set; } = 3;
    
    public bool IsBlocked { get; set; }
    
    [StringLength(45)]
    public string? IpAddress { get; set; }
    
    [StringLength(500)]
    public string? UserAgent { get; set; }
    
    // Navigation property
    public virtual User? User { get; set; }
}

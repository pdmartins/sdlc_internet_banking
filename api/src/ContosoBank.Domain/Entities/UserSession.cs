using System.ComponentModel.DataAnnotations;

namespace ContosoBank.Domain.Entities;

/// <summary>
/// Represents an active user session for authentication and device management
/// </summary>
public class UserSession
{
    public Guid Id { get; set; }
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    [StringLength(512)]
    public string SessionToken { get; set; } = string.Empty;
    
    [Required]
    [StringLength(45)]
    public string IpAddress { get; set; } = string.Empty;
    
    [Required]
    [StringLength(500)]
    public string UserAgent { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string DeviceFingerprint { get; set; } = string.Empty;
    
    [StringLength(50)]
    public string Location { get; set; } = string.Empty;
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Required]
    public DateTime ExpiresAt { get; set; }
    
    public DateTime? LastActivityAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public bool IsRevoked { get; set; } = false;
    
    public DateTime? RevokedAt { get; set; }
    
    [StringLength(100)]
    public string? RevokedReason { get; set; }
    
    public bool IsTrustedDevice { get; set; } = false;
    
    // Inactivity timeout in minutes (configurable per session)
    public int InactivityTimeoutMinutes { get; set; } = 30;
    
    // Navigation property
    public virtual User? User { get; set; }
}

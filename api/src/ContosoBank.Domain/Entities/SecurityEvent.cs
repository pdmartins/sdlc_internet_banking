using System.ComponentModel.DataAnnotations;

namespace ContosoBank.Domain.Entities;

public class SecurityEvent
{
    public Guid Id { get; set; }
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    [StringLength(50)]
    public string EventType { get; set; } = string.Empty; // LOGIN, LOGOUT, PASSWORD_CHANGE, MFA_SETUP, etc.
    
    [Required]
    [StringLength(20)]
    public string Severity { get; set; } = string.Empty; // INFO, WARNING, CRITICAL
    
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
    
    [StringLength(45)]
    public string IpAddress { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string UserAgent { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string Location { get; set; } = string.Empty;
    
    public bool IsSuccessful { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
}

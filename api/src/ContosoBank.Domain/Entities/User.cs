using System.ComponentModel.DataAnnotations;

namespace ContosoBank.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(100)]
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
    [StringLength(14)] // Format: 000.000.000-00
    public string CPF { get; set; } = string.Empty;
    
    [Required]
    [StringLength(255)]
    public string PasswordHash { get; set; } = string.Empty;
    
    [Required]
    [StringLength(255)]
    public string SecurityQuestion { get; set; } = string.Empty;
    
    [Required]
    [StringLength(500)]
    public string SecurityAnswerHash { get; set; } = string.Empty;
    
    [Required]
    [StringLength(20)]
    public string MfaOption { get; set; } = string.Empty; // "sms" or "authenticator"
    
    public int PasswordStrength { get; set; }
    
    public bool IsEmailVerified { get; set; }
    
    public bool IsPhoneVerified { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public DateTime? LastLoginAt { get; set; }
    
    // Navigation properties
    public virtual Account? Account { get; set; }
    public virtual ICollection<SecurityEvent> SecurityEvents { get; set; } = new List<SecurityEvent>();
}

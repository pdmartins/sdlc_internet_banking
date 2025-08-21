using System.ComponentModel.DataAnnotations;

namespace ContosoBank.Domain.Entities;

public class GdprConsent
{
    public Guid Id { get; set; }
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    [StringLength(50)]
    public string ConsentType { get; set; } = string.Empty; // "DATA_PROCESSING", "MARKETING", "ANALYTICS"
    
    [Required]
    public bool HasConsented { get; set; }
    
    [StringLength(255)]
    public string? ConsentDetails { get; set; }
    
    [StringLength(45)]
    public string? IpAddress { get; set; }
    
    [StringLength(500)]
    public string? UserAgent { get; set; }
    
    public DateTime ConsentDate { get; set; }
    
    public DateTime? WithdrawnDate { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Navigation property
    public virtual User User { get; set; } = null!;
}

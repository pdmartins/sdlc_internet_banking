using System.ComponentModel.DataAnnotations;

namespace ContosoBank.Domain.Entities;

public class Transaction
{
    public Guid Id { get; set; }
    
    [Required]
    public Guid AccountId { get; set; }
    
    [Required]
    [StringLength(20)]
    public string Type { get; set; } = string.Empty; // CREDIT, DEBIT
    
    [Required]
    [StringLength(50)]
    public string Category { get; set; } = string.Empty; // DEPOSIT, WITHDRAWAL, TRANSFER, etc.
    
    [Required]
    public decimal Amount { get; set; }
    
    [Required]
    public decimal BalanceAfter { get; set; }
    
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string Reference { get; set; } = string.Empty;
    
    [StringLength(255)]
    public string RecipientAccount { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string RecipientName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "PENDING"; // PENDING, COMPLETED, FAILED, CANCELLED
    
    public decimal Fee { get; set; } = 0.00m;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ProcessedAt { get; set; }
    
    // Navigation properties
    public virtual Account Account { get; set; } = null!;
}

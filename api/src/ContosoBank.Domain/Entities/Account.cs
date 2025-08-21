using System.ComponentModel.DataAnnotations;

namespace ContosoBank.Domain.Entities;

public class Account
{
    public Guid Id { get; set; }
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    [StringLength(20)]
    public string AccountNumber { get; set; } = string.Empty;
    
    [Required]
    [StringLength(10)]
    public string BranchCode { get; set; } = string.Empty;
    
    [Required]
    [StringLength(20)]
    public string AccountType { get; set; } = "CHECKING"; // CHECKING, SAVINGS
    
    [Required]
    public decimal Balance { get; set; } = 0.00m;
    
    [Required]
    public decimal DailyLimit { get; set; } = 5000.00m;
    
    [Required]
    public decimal MonthlyLimit { get; set; } = 50000.00m;
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}

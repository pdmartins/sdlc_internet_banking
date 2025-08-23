using System.ComponentModel.DataAnnotations;

namespace ContosoBank.Application.DTOs;

/// <summary>
/// DTO for initiating a new transaction
/// </summary>
public class TransactionRequestDto
{
    /// <summary>
    /// Type of transaction (CREDIT or DEBIT)
    /// </summary>
    [Required(ErrorMessage = "Transaction type is required")]
    [StringLength(20, ErrorMessage = "Transaction type cannot exceed 20 characters")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Category of transaction (DEPOSIT, WITHDRAWAL, TRANSFER, etc.)
    /// </summary>
    [Required(ErrorMessage = "Transaction category is required")]
    [StringLength(50, ErrorMessage = "Transaction category cannot exceed 50 characters")]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Transaction amount (must be positive)
    /// </summary>
    [Required(ErrorMessage = "Amount is required")]
    [Range(0.01, 999999999.99, ErrorMessage = "Amount must be between 0.01 and 999,999,999.99")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Optional description of the transaction
    /// </summary>
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Optional reference number for the transaction
    /// </summary>
    [StringLength(100, ErrorMessage = "Reference cannot exceed 100 characters")]
    public string Reference { get; set; } = string.Empty;

    /// <summary>
    /// Recipient account number (required for transfers and certain debits)
    /// </summary>
    [StringLength(255, ErrorMessage = "Recipient account cannot exceed 255 characters")]
    public string RecipientAccount { get; set; } = string.Empty;

    /// <summary>
    /// Recipient name (required for transfers and certain debits)
    /// </summary>
    [StringLength(100, ErrorMessage = "Recipient name cannot exceed 100 characters")]
    public string RecipientName { get; set; } = string.Empty;
}

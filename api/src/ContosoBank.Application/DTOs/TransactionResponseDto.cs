namespace ContosoBank.Application.DTOs;

/// <summary>
/// DTO for transaction response data
/// </summary>
public class TransactionResponseDto
{
    /// <summary>
    /// Unique transaction identifier
    /// </summary>
    public Guid TransactionId { get; set; }

    /// <summary>
    /// Account ID associated with the transaction
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Type of transaction (CREDIT or DEBIT)
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Category of transaction (DEPOSIT, WITHDRAWAL, TRANSFER, etc.)
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Transaction amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Account balance after the transaction
    /// </summary>
    public decimal BalanceAfter { get; set; }

    /// <summary>
    /// Description of the transaction
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Reference number for the transaction
    /// </summary>
    public string Reference { get; set; } = string.Empty;

    /// <summary>
    /// Recipient account number
    /// </summary>
    public string RecipientAccount { get; set; } = string.Empty;

    /// <summary>
    /// Recipient name
    /// </summary>
    public string RecipientName { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the transaction
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Transaction fee charged
    /// </summary>
    public decimal Fee { get; set; }

    /// <summary>
    /// When the transaction was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the transaction was processed (if completed)
    /// </summary>
    public DateTime? ProcessedAt { get; set; }
}

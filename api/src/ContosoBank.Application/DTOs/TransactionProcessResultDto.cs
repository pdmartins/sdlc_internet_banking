namespace ContosoBank.Application.DTOs;

/// <summary>
/// DTO for transaction processing result with validation details
/// </summary>
public class TransactionProcessResultDto
{
    /// <summary>
    /// Indicates if the transaction was successful
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Transaction details if successful
    /// </summary>
    public TransactionResponseDto? Transaction { get; set; }

    /// <summary>
    /// Error message if transaction failed
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Error code for programmatic handling
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// Validation errors from the request
    /// </summary>
    public List<string> ValidationErrors { get; set; } = new();

    /// <summary>
    /// Current account balance before the transaction
    /// </summary>
    public decimal AccountBalance { get; set; }

    /// <summary>
    /// Daily transaction limit information
    /// </summary>
    public decimal DailyLimit { get; set; }

    /// <summary>
    /// Amount already used today
    /// </summary>
    public decimal DailyUsed { get; set; }

    /// <summary>
    /// Monthly transaction limit information
    /// </summary>
    public decimal MonthlyLimit { get; set; }

    /// <summary>
    /// Amount already used this month
    /// </summary>
    public decimal MonthlyUsed { get; set; }

    /// <summary>
    /// Transaction fees that would be applied
    /// </summary>
    public decimal EstimatedFee { get; set; }
}

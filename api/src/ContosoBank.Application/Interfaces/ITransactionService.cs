using ContosoBank.Application.DTOs;

namespace ContosoBank.Application.Interfaces;

/// <summary>
/// Service interface for transaction management operations
/// </summary>
public interface ITransactionService
{
    /// <summary>
    /// Validates a transaction request without processing it
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="request">Transaction request details</param>
    /// <returns>Validation result with limits and fees</returns>
    Task<TransactionProcessResultDto> ValidateTransactionAsync(Guid userId, TransactionRequestDto request);

    /// <summary>
    /// Processes a transaction request
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="request">Transaction request details</param>
    /// <returns>Transaction processing result</returns>
    Task<TransactionProcessResultDto> ProcessTransactionAsync(Guid userId, TransactionRequestDto request);

    /// <summary>
    /// Gets transaction history for a user's account
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="pageSize">Number of transactions per page</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <returns>List of transactions</returns>
    Task<IEnumerable<TransactionResponseDto>> GetTransactionHistoryAsync(Guid userId, int pageSize = 20, int pageNumber = 1);

    /// <summary>
    /// Gets transaction history with date range filter
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="startDate">Start date for filtering</param>
    /// <param name="endDate">End date for filtering</param>
    /// <returns>List of transactions in date range</returns>
    Task<IEnumerable<TransactionResponseDto>> GetTransactionHistoryByDateRangeAsync(Guid userId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets transaction history filtered by type
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="type">Transaction type (CREDIT or DEBIT)</param>
    /// <returns>List of transactions of specified type</returns>
    Task<IEnumerable<TransactionResponseDto>> GetTransactionHistoryByTypeAsync(Guid userId, string type);

    /// <summary>
    /// Gets a specific transaction by ID
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="transactionId">Transaction identifier</param>
    /// <returns>Transaction details if found and belongs to user</returns>
    Task<TransactionResponseDto?> GetTransactionByIdAsync(Guid userId, Guid transactionId);

    /// <summary>
    /// Gets current account balance
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <returns>Current account balance</returns>
    Task<decimal> GetAccountBalanceAsync(Guid userId);

    /// <summary>
    /// Gets transaction limits and usage information
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <returns>Limits and current usage</returns>
    Task<(decimal DailyLimit, decimal DailyUsed, decimal MonthlyLimit, decimal MonthlyUsed)> GetTransactionLimitsAsync(Guid userId);
}

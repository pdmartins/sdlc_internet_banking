using ContosoBank.Application.DTOs;

namespace ContosoBank.Application.Interfaces;

/// <summary>
/// Service interface for transaction export operations
/// </summary>
public interface ITransactionExportService
{
    /// <summary>
    /// Exports transaction history to CSV format
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="request">Export request parameters</param>
    /// <returns>CSV export result</returns>
    Task<TransactionExportResultDto> ExportToCsvAsync(Guid userId, TransactionExportRequestDto request);

    /// <summary>
    /// Exports transaction history to PDF format
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="request">Export request parameters</param>
    /// <returns>PDF export result</returns>
    Task<TransactionExportResultDto> ExportToPdfAsync(Guid userId, TransactionExportRequestDto request);

    /// <summary>
    /// Gets filtered and paginated transaction history
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="filter">Filter and pagination parameters</param>
    /// <returns>Paginated transaction results</returns>
    Task<PaginatedTransactionResultDto> GetFilteredTransactionHistoryAsync(Guid userId, TransactionFilterDto filter);

    /// <summary>
    /// Gets transaction summary statistics
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="startDate">Start date for summary</param>
    /// <param name="endDate">End date for summary</param>
    /// <returns>Transaction summary statistics</returns>
    Task<TransactionSummaryDto> GetTransactionSummaryAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// Validates export request parameters
    /// </summary>
    /// <param name="request">Export request to validate</param>
    /// <returns>Validation result with any errors</returns>
    Task<(bool IsValid, List<string> Errors)> ValidateExportRequestAsync(TransactionExportRequestDto request);
}

using ContosoBank.Application.DTOs;
using ContosoBank.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ContosoBank.Web.Controllers;

/// <summary>
/// Enhanced controller for managing transactions with export capabilities
/// </summary>
[ApiController]
[Route("api/transaction-history")]
// TODO: Implement proper authentication scheme before enabling [Authorize]
// [Authorize]
public class TransactionHistoryController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly ITransactionExportService _transactionExportService;
    private readonly ISessionService _sessionService;
    private readonly ILogger<TransactionHistoryController> _logger;

    /// <summary>
    /// Initializes a new instance of the TransactionHistoryController
    /// </summary>
    /// <param name="transactionService">Transaction service for transaction operations</param>
    /// <param name="transactionExportService">Transaction export service for export operations</param>
    /// <param name="sessionService">Session service for user authentication</param>
    /// <param name="logger">Logger for transaction controller operations</param>
    public TransactionHistoryController(
        ITransactionService transactionService,
        ITransactionExportService transactionExportService,
        ISessionService sessionService,
        ILogger<TransactionHistoryController> logger)
    {
        _transactionService = transactionService;
        _transactionExportService = transactionExportService;
        _sessionService = sessionService;
        _logger = logger;
    }

    /// <summary>
    /// Gets filtered and paginated transaction history
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20)</param>
    /// <param name="startDate">Start date filter (optional)</param>
    /// <param name="endDate">End date filter (optional)</param>
    /// <param name="transactionType">Transaction type filter (optional)</param>
    /// <param name="minAmount">Minimum amount filter (optional)</param>
    /// <param name="maxAmount">Maximum amount filter (optional)</param>
    /// <param name="status">Status filter (optional)</param>
    /// <param name="searchText">Search text filter (optional)</param>
    /// <param name="sortBy">Sort field (default: Date)</param>
    /// <param name="sortDirection">Sort direction (default: DESC)</param>
    /// <returns>Paginated transaction results with summary</returns>
    [HttpGet("history/filtered")]
    public async Task<ActionResult<PaginatedTransactionResultDto>> GetFilteredTransactionHistory(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? transactionType = null,
        [FromQuery] decimal? minAmount = null,
        [FromQuery] decimal? maxAmount = null,
        [FromQuery] string? status = null,
        [FromQuery] string? searchText = null,
        [FromQuery] string sortBy = "Date",
        [FromQuery] string sortDirection = "DESC")
    {
        try
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == Guid.Empty)
            {
                return Unauthorized("Invalid user session");
            }

            var filter = new TransactionFilterDto
            {
                PageNumber = pageNumber,
                PageSize = Math.Min(pageSize, 100), // Cap at 100 items per page
                StartDate = startDate,
                EndDate = endDate,
                TransactionType = transactionType,
                MinAmount = minAmount,
                MaxAmount = maxAmount,
                Status = status,
                SearchText = searchText,
                SortBy = sortBy,
                SortDirection = sortDirection
            };

            var result = await _transactionExportService.GetFilteredTransactionHistoryAsync(userId, filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting filtered transaction history");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Exports transaction history to CSV format
    /// </summary>
    /// <param name="request">Export request parameters</param>
    /// <returns>CSV file download</returns>
    [HttpPost("export/csv")]
    public async Task<ActionResult> ExportTransactionsToCsv([FromBody] TransactionExportRequestDto request)
    {
        try
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == Guid.Empty)
            {
                return Unauthorized("Invalid user session");
            }

            request.Format = "CSV";
            var result = await _transactionExportService.ExportToCsvAsync(userId, request);

            if (!result.IsSuccessful)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            var fileBytes = Convert.FromBase64String(result.FileContent!);
            return File(fileBytes, result.ContentType!, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting transactions to CSV");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Exports transaction history to PDF format
    /// </summary>
    /// <param name="request">Export request parameters</param>
    /// <returns>PDF file download</returns>
    [HttpPost("export/pdf")]
    public async Task<ActionResult> ExportTransactionsToPdf([FromBody] TransactionExportRequestDto request)
    {
        try
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == Guid.Empty)
            {
                return Unauthorized("Invalid user session");
            }

            request.Format = "PDF";
            var result = await _transactionExportService.ExportToPdfAsync(userId, request);

            if (!result.IsSuccessful)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            var fileBytes = Convert.FromBase64String(result.FileContent!);
            return File(fileBytes, result.ContentType!, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting transactions to PDF");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets transaction summary statistics
    /// </summary>
    /// <param name="startDate">Start date for summary (optional)</param>
    /// <param name="endDate">End date for summary (optional)</param>
    /// <returns>Transaction summary statistics</returns>
    [HttpGet("summary")]
    public async Task<ActionResult<TransactionSummaryDto>> GetTransactionSummary(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == Guid.Empty)
            {
                return Unauthorized("Invalid user session");
            }

            var summary = await _transactionExportService.GetTransactionSummaryAsync(userId, startDate, endDate);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transaction summary");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    #region Private Methods

    /// <summary>
    /// Gets the current user ID from the session or token
    /// </summary>
    /// <returns>User ID if found, empty Guid otherwise</returns>
    private async Task<Guid> GetCurrentUserIdAsync()
    {
        try
        {
            // Try to get user ID from session service first
            var sessionToken = GetSessionToken();
            if (!string.IsNullOrEmpty(sessionToken))
            {
                var session = await _sessionService.ValidateSessionAsync(sessionToken);
                if (session != null && session.IsActive)
                {
                    return session.UserId;
                }
            }

            // Fallback: try to get from JWT claims if available
            var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }

            // TODO: Remove this demo fallback once proper authentication is implemented
            // This returns a hardcoded demo user ID for development purposes
            _logger.LogWarning("Using demo user ID - implement proper authentication");
            return new Guid("12345678-1234-1234-1234-123456789012");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user ID");
            return Guid.Empty;
        }
    }

    /// <summary>
    /// Extracts session token from various sources
    /// </summary>
    /// <returns>Session token if found, null otherwise</returns>
    private string? GetSessionToken()
    {
        // Try Authorization header first
        var authHeader = HttpContext.Request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            var token = authHeader["Bearer ".Length..].Trim();
            _logger.LogInformation("Extracted token: {TokenPreview}...", token.Length > 20 ? token.Substring(0, 20) : token);
            return token;
        }

        // Try custom session header
        var sessionHeader = HttpContext.Request.Headers["X-Session-Token"].FirstOrDefault();
        if (!string.IsNullOrEmpty(sessionHeader))
        {
            _logger.LogInformation("Using X-Session-Token: {TokenPreview}...", sessionHeader.Length > 20 ? sessionHeader.Substring(0, 20) : sessionHeader);
            return sessionHeader;
        }

        _logger.LogWarning("No session token found in headers");
        return null;
    }

    #endregion
}

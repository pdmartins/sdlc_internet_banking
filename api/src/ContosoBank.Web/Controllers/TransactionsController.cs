using ContosoBank.Application.DTOs;
using ContosoBank.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ContosoBank.Web.Controllers;

/// <summary>
/// Controller for managing transactions
/// </summary>
[ApiController]
[Route("api/[controller]")]
// TODO: Implement proper authentication scheme before enabling [Authorize]
// [Authorize]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly ITransactionExportService _transactionExportService;
    private readonly ISessionService _sessionService;
    private readonly ILogger<TransactionsController> _logger;

    /// <summary>
    /// Initializes a new instance of the TransactionsController
    /// </summary>
    /// <param name="transactionService">Service for managing transactions</param>
    /// <param name="transactionExportService">Service for exporting transaction data</param>
    /// <param name="sessionService">Service for managing user sessions</param>
    /// <param name="logger">Logger instance</param>
    public TransactionsController(
        ITransactionService transactionService,
        ITransactionExportService transactionExportService,
        ISessionService sessionService,
        ILogger<TransactionsController> logger)
    {
        _transactionService = transactionService;
        _transactionExportService = transactionExportService;
        _sessionService = sessionService;
        _logger = logger;
    }

    /// <summary>
    /// Validates a transaction request without processing it
    /// </summary>
    /// <param name="request">Transaction request details</param>
    /// <returns>Validation result with limits and fees</returns>
    [HttpPost("validate")]
    public async Task<ActionResult<TransactionProcessResultDto>> ValidateTransaction([FromBody] TransactionRequestDto request)
    {
        try
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == Guid.Empty)
            {
                return Unauthorized("Invalid user session");
            }

            var result = await _transactionService.ValidateTransactionAsync(userId, request);
            
            if (result.IsSuccessful)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating transaction");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Processes a transaction request
    /// </summary>
    /// <param name="request">Transaction request details</param>
    /// <returns>Transaction processing result</returns>
    [HttpPost("process")]
    public async Task<ActionResult<TransactionProcessResultDto>> ProcessTransaction([FromBody] TransactionRequestDto request)
    {
        try
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == Guid.Empty)
            {
                return Unauthorized("Invalid user session");
            }

            var result = await _transactionService.ProcessTransactionAsync(userId, request);
            
            if (result.IsSuccessful)
            {
                _logger.LogInformation("Transaction processed successfully for user {UserId}", userId);
                return Ok(result);
            }
            
            _logger.LogWarning("Transaction failed for user {UserId}: {ErrorMessage}", userId, result.ErrorMessage);
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing transaction");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets transaction history for the current user
    /// </summary>
    /// <param name="pageSize">Number of transactions per page (default: 20)</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <returns>List of transactions</returns>
    [HttpGet("history")]
    public async Task<ActionResult<IEnumerable<TransactionResponseDto>>> GetTransactionHistory(
        [FromQuery] int pageSize = 20, 
        [FromQuery] int pageNumber = 1)
    {
        try
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == Guid.Empty)
            {
                return Unauthorized("Invalid user session");
            }

            // Validate pagination parameters
            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest("Page size must be between 1 and 100");
            }
            
            if (pageNumber < 1)
            {
                return BadRequest("Page number must be greater than 0");
            }

            var transactions = await _transactionService.GetTransactionHistoryAsync(userId, pageSize, pageNumber);
            return Ok(transactions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transaction history");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets transaction history filtered by date range
    /// </summary>
    /// <param name="startDate">Start date (YYYY-MM-DD)</param>
    /// <param name="endDate">End date (YYYY-MM-DD)</param>
    /// <returns>List of transactions in date range</returns>
    [HttpGet("history/daterange")]
    public async Task<ActionResult<IEnumerable<TransactionResponseDto>>> GetTransactionHistoryByDateRange(
        [FromQuery] DateTime startDate, 
        [FromQuery] DateTime endDate)
    {
        try
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == Guid.Empty)
            {
                return Unauthorized("Invalid user session");
            }

            // Validate date range
            if (startDate > endDate)
            {
                return BadRequest("Start date cannot be greater than end date");
            }
            
            if (endDate > DateTime.Now)
            {
                return BadRequest("End date cannot be in the future");
            }

            var transactions = await _transactionService.GetTransactionHistoryByDateRangeAsync(userId, startDate, endDate);
            return Ok(transactions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transaction history by date range");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets transaction history filtered by type
    /// </summary>
    /// <param name="type">Transaction type (CREDIT or DEBIT)</param>
    /// <returns>List of transactions of specified type</returns>
    [HttpGet("history/type/{type}")]
    public async Task<ActionResult<IEnumerable<TransactionResponseDto>>> GetTransactionHistoryByType(string type)
    {
        try
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == Guid.Empty)
            {
                return Unauthorized("Invalid user session");
            }

            // Validate transaction type
            if (!IsValidTransactionType(type))
            {
                return BadRequest("Invalid transaction type. Must be CREDIT or DEBIT");
            }

            var transactions = await _transactionService.GetTransactionHistoryByTypeAsync(userId, type);
            return Ok(transactions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transaction history by type");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets transaction history with comprehensive filtering support
    /// </summary>
    /// <param name="pageSize">Number of transactions per page (default: 20)</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="startDate">Start date filter (YYYY-MM-DD) (optional)</param>
    /// <param name="endDate">End date filter (YYYY-MM-DD) (optional)</param>
    /// <param name="type">Transaction type filter (CREDIT or DEBIT) (optional)</param>
    /// <param name="minAmount">Minimum amount filter (optional)</param>
    /// <param name="maxAmount">Maximum amount filter (optional)</param>
    /// <param name="status">Status filter (COMPLETED, PENDING, FAILED, CANCELLED) (optional)</param>
    /// <returns>List of filtered transactions</returns>
    [HttpGet("history/filtered")]
    public async Task<ActionResult<IEnumerable<TransactionResponseDto>>> GetTransactionHistoryWithFilters(
        [FromQuery] int pageSize = 20,
        [FromQuery] int pageNumber = 1,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? type = null,
        [FromQuery] decimal? minAmount = null,
        [FromQuery] decimal? maxAmount = null,
        [FromQuery] string? status = null)
    {
        try
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == Guid.Empty)
            {
                return Unauthorized("Invalid user session");
            }

            // Validate pagination parameters
            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest("Page size must be between 1 and 100");
            }
            
            if (pageNumber < 1)
            {
                return BadRequest("Page number must be greater than 0");
            }

            // Validate date range if provided
            if (startDate.HasValue && endDate.HasValue && startDate > endDate)
            {
                return BadRequest("Start date cannot be greater than end date");
            }

            var transactions = await _transactionService.GetTransactionHistoryWithFiltersAsync(
                userId, pageSize, pageNumber, startDate, endDate, type, minAmount, maxAmount, status);
                
            return Ok(transactions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting filtered transaction history");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets a specific transaction by ID
    /// </summary>
    /// <param name="transactionId">Transaction identifier</param>
    /// <returns>Transaction details</returns>
    [HttpGet("{transactionId}")]
    public async Task<ActionResult<TransactionResponseDto>> GetTransactionById(Guid transactionId)
    {
        try
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == Guid.Empty)
            {
                return Unauthorized("Invalid user session");
            }

            var transaction = await _transactionService.GetTransactionByIdAsync(userId, transactionId);
            
            if (transaction == null)
            {
                return NotFound("Transaction not found");
            }

            return Ok(transaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transaction by ID");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets current account balance
    /// </summary>
    /// <returns>Current account balance</returns>
    [HttpGet("balance")]
    public async Task<ActionResult<decimal>> GetAccountBalance()
    {
        try
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == Guid.Empty)
            {
                return Unauthorized("Invalid user session");
            }

            var balance = await _transactionService.GetAccountBalanceAsync(userId);
            return Ok(new { balance });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account balance");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets transaction limits and usage information
    /// </summary>
    /// <returns>Limits and current usage</returns>
    [HttpGet("limits")]
    public async Task<ActionResult<object>> GetTransactionLimits()
    {
        try
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == Guid.Empty)
            {
                return Unauthorized("Invalid user session");
            }

            var (dailyLimit, dailyUsed, monthlyLimit, monthlyUsed) = await _transactionService.GetTransactionLimitsAsync(userId);
            
            return Ok(new
            {
                dailyLimit,
                dailyUsed,
                dailyRemaining = Math.Max(0, dailyLimit - dailyUsed),
                monthlyLimit,
                monthlyUsed,
                monthlyRemaining = Math.Max(0, monthlyLimit - monthlyUsed)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transaction limits");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Gets the current user ID from the session token
    /// </summary>
    /// <returns>User ID or Guid.Empty if not found or invalid session</returns>
    private async Task<Guid> GetCurrentUserIdAsync()
    {
        try
        {
            var sessionToken = GetSessionTokenFromHeader();
            if (string.IsNullOrEmpty(sessionToken))
            {
                return Guid.Empty;
            }

            var session = await _sessionService.ValidateSessionAsync(sessionToken);
            return session?.UserId ?? Guid.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating session token");
            return Guid.Empty;
        }
    }

    /// <summary>
    /// Gets session token from Authorization header or session header
    /// </summary>
    /// <returns>Session token if found</returns>
    private string? GetSessionTokenFromHeader()
    {
        // Try Authorization header first (Bearer token)
        var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
        _logger.LogInformation("Auth header: {AuthHeader}", authHeader ?? "null");
        
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            var token = authHeader.Substring("Bearer ".Length).Trim();
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

    /// <summary>
    /// Validates if the transaction type is valid
    /// </summary>
    /// <param name="type">Transaction type to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    private bool IsValidTransactionType(string type)
    {
        var validTypes = new[] { "CREDIT", "DEBIT" };
        return validTypes.Contains(type.ToUpper());
    }

    /// <summary>
    /// Exports transaction history to CSV format
    /// </summary>
    /// <param name="startDate">Start date for transaction history (optional)</param>
    /// <param name="endDate">End date for transaction history (optional)</param>
    /// <param name="type">Transaction type filter (optional)</param>
    /// <returns>CSV file with transaction history</returns>
    [HttpGet("export/csv")]
    public async Task<IActionResult> ExportTransactionsCsv(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? type = null)
    {
        try
        {
            _logger.LogInformation("CSV export requested: startDate={StartDate}, endDate={EndDate}, type={Type}", startDate, endDate, type);
            
            var userId = await GetCurrentUserIdAsync();
            if (userId == Guid.Empty)
            {
                _logger.LogWarning("CSV export failed: Invalid user session");
                return Unauthorized("Invalid user session");
            }

            _logger.LogInformation("CSV export for user {UserId}", userId);

            var request = new TransactionExportRequestDto
            {
                StartDate = startDate,
                EndDate = endDate,
                TransactionType = type,
                Format = "CSV"
            };

            _logger.LogInformation("Export request created: {@Request}", request);

            var result = await _transactionExportService.ExportToCsvAsync(userId, request);
            
            if (!result.IsSuccessful)
            {
                _logger.LogWarning("CSV export failed for user {UserId}: {ErrorMessage}", userId, result.ErrorMessage);
                return BadRequest(result.ErrorMessage);
            }

            var fileBytes = Convert.FromBase64String(result.FileContent ?? "");
            var fileName = result.FileName ?? $"transactions_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            return File(fileBytes, result.ContentType ?? "text/csv", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting transactions to CSV for user {UserId}", await GetCurrentUserIdAsync());
            return StatusCode(500, "An error occurred while exporting transactions");
        }
    }

    /// <summary>
    /// Exports transaction history to PDF format
    /// </summary>
    /// <param name="startDate">Start date for transaction history (optional)</param>
    /// <param name="endDate">End date for transaction history (optional)</param>
    /// <param name="type">Transaction type filter (optional)</param>
    /// <returns>PDF file with transaction history</returns>
    [HttpGet("export/pdf")]
    public async Task<IActionResult> ExportTransactionsPdf(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? type = null)
    {
        try
        {
            _logger.LogInformation("PDF export requested: startDate={StartDate}, endDate={EndDate}, type={Type}", startDate, endDate, type);
            
            var userId = await GetCurrentUserIdAsync();
            if (userId == Guid.Empty)
            {
                _logger.LogWarning("PDF export failed: Invalid user session");
                return Unauthorized("Invalid user session");
            }

            _logger.LogInformation("PDF export for user {UserId}", userId);

            var request = new TransactionExportRequestDto
            {
                StartDate = startDate,
                EndDate = endDate,
                TransactionType = type,
                Format = "PDF"
            };

            _logger.LogInformation("Export request created: {@Request}", request);

            var result = await _transactionExportService.ExportToPdfAsync(userId, request);
            
            if (!result.IsSuccessful)
            {
                _logger.LogWarning("PDF export failed for user {UserId}: {ErrorMessage}", userId, result.ErrorMessage);
                return BadRequest(result.ErrorMessage);
            }

            var fileBytes = Convert.FromBase64String(result.FileContent ?? "");
            var fileName = result.FileName ?? $"transactions_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(fileBytes, result.ContentType ?? "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting transactions to PDF for user {UserId}", await GetCurrentUserIdAsync());
            return StatusCode(500, "An error occurred while exporting transactions");
        }
    }

    #endregion
}

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
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(
        ITransactionService transactionService,
        ILogger<TransactionsController> logger)
    {
        _transactionService = transactionService;
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
            var userId = GetCurrentUserId();
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
            var userId = GetCurrentUserId();
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
            var userId = GetCurrentUserId();
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
            var userId = GetCurrentUserId();
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
            var userId = GetCurrentUserId();
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
    /// Gets a specific transaction by ID
    /// </summary>
    /// <param name="transactionId">Transaction identifier</param>
    /// <returns>Transaction details</returns>
    [HttpGet("{transactionId}")]
    public async Task<ActionResult<TransactionResponseDto>> GetTransactionById(Guid transactionId)
    {
        try
        {
            var userId = GetCurrentUserId();
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
            var userId = GetCurrentUserId();
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
            var userId = GetCurrentUserId();
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
    /// Gets the current user ID from the JWT token claims
    /// </summary>
    /// <returns>User ID or Guid.Empty if not found</returns>
    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
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

    #endregion
}

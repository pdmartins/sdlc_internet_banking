using ContosoBank.Application.DTOs;
using ContosoBank.Application.Interfaces;
using ContosoBank.Domain.Entities;
using ContosoBank.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ContosoBank.Application.Services;

/// <summary>
/// Service implementation for transaction management operations
/// </summary>
public class TransactionService : ITransactionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(
        IUnitOfWork unitOfWork,
        ILogger<TransactionService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<TransactionProcessResultDto> ValidateTransactionAsync(Guid userId, TransactionRequestDto request)
    {
        try
        {
            var result = new TransactionProcessResultDto();

            // Get user's account
            var account = await GetUserAccountAsync(userId);
            if (account == null)
            {
                result.IsSuccessful = false;
                result.ErrorMessage = "Account not found";
                result.ErrorCode = "ACCOUNT_NOT_FOUND";
                return result;
            }

            // Validate transaction request
            var validationErrors = await ValidateTransactionRequestAsync(request, account);
            if (validationErrors.Any())
            {
                result.IsSuccessful = false;
                result.ValidationErrors = validationErrors;
                result.ErrorMessage = "Validation failed";
                result.ErrorCode = "VALIDATION_ERROR";
                return result;
            }

            // Get current limits and usage
            var (dailyLimit, dailyUsed, monthlyLimit, monthlyUsed) = await GetTransactionLimitsAsync(userId);
            
            // Calculate estimated fee
            var estimatedFee = CalculateTransactionFee(request.Type, request.Category, request.Amount);

            result.IsSuccessful = true;
            result.AccountBalance = account.Balance;
            result.DailyLimit = dailyLimit;
            result.DailyUsed = dailyUsed;
            result.MonthlyLimit = monthlyLimit;
            result.MonthlyUsed = monthlyUsed;
            result.EstimatedFee = estimatedFee;

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating transaction for user {UserId}", userId);
            return new TransactionProcessResultDto
            {
                IsSuccessful = false,
                ErrorMessage = "Internal server error during validation",
                ErrorCode = "INTERNAL_ERROR"
            };
        }
    }

    public async Task<TransactionProcessResultDto> ProcessTransactionAsync(Guid userId, TransactionRequestDto request)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            // First validate the transaction
            var validationResult = await ValidateTransactionAsync(userId, request);
            if (!validationResult.IsSuccessful)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return validationResult;
            }

            // Get user's account
            var account = await GetUserAccountAsync(userId);
            if (account == null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new TransactionProcessResultDto
                {
                    IsSuccessful = false,
                    ErrorMessage = "Account not found",
                    ErrorCode = "ACCOUNT_NOT_FOUND"
                };
            }

            // Calculate transaction fee
            var fee = CalculateTransactionFee(request.Type, request.Category, request.Amount);

            // Check if sufficient balance for debit transactions
            if (request.Type.ToUpper() == "DEBIT")
            {
                var totalAmount = request.Amount + fee;
                if (account.Balance < totalAmount)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return new TransactionProcessResultDto
                    {
                        IsSuccessful = false,
                        ErrorMessage = "Insufficient balance",
                        ErrorCode = "INSUFFICIENT_BALANCE",
                        AccountBalance = account.Balance
                    };
                }
            }

            // Create transaction record
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = account.Id,
                Type = request.Type.ToUpper(),
                Category = request.Category.ToUpper(),
                Amount = request.Amount,
                Description = request.Description,
                Reference = request.Reference,
                RecipientAccount = request.RecipientAccount,
                RecipientName = request.RecipientName,
                Status = "PENDING",
                Fee = fee,
                CreatedAt = DateTime.UtcNow
            };

            // Update account balance
            if (request.Type.ToUpper() == "CREDIT")
            {
                account.Balance += request.Amount;
            }
            else if (request.Type.ToUpper() == "DEBIT")
            {
                account.Balance -= (request.Amount + fee);
            }

            transaction.BalanceAfter = account.Balance;
            transaction.Status = "COMPLETED";
            transaction.ProcessedAt = DateTime.UtcNow;

            // Save transaction and update account
            await _unitOfWork.Transactions.AddAsync(transaction);
            _unitOfWork.Accounts.Update(account);

            // Log security event
            await LogTransactionSecurityEventAsync(userId, transaction);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Transaction {TransactionId} processed successfully for user {UserId}", 
                transaction.Id, userId);

            return new TransactionProcessResultDto
            {
                IsSuccessful = true,
                Transaction = MapToTransactionResponseDto(transaction)
            };
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error processing transaction for user {UserId}", userId);
            return new TransactionProcessResultDto
            {
                IsSuccessful = false,
                ErrorMessage = "Internal server error during transaction processing",
                ErrorCode = "INTERNAL_ERROR"
            };
        }
    }

    public async Task<IEnumerable<TransactionResponseDto>> GetTransactionHistoryAsync(Guid userId, int pageSize = 20, int pageNumber = 1)
    {
        var account = await GetUserAccountAsync(userId);
        if (account == null)
        {
            return Enumerable.Empty<TransactionResponseDto>();
        }

        var transactions = await _unitOfWork.Transactions.GetByAccountIdAsync(account.Id);
        
        var pagedTransactions = transactions
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);

        return pagedTransactions.Select(MapToTransactionResponseDto);
    }

    public async Task<IEnumerable<TransactionResponseDto>> GetTransactionHistoryByDateRangeAsync(Guid userId, DateTime startDate, DateTime endDate)
    {
        var account = await GetUserAccountAsync(userId);
        if (account == null)
        {
            return Enumerable.Empty<TransactionResponseDto>();
        }

        var transactions = await _unitOfWork.Transactions.GetByAccountIdAndDateRangeAsync(account.Id, startDate, endDate);
        return transactions.Select(MapToTransactionResponseDto);
    }

    public async Task<IEnumerable<TransactionResponseDto>> GetTransactionHistoryByTypeAsync(Guid userId, string type)
    {
        var account = await GetUserAccountAsync(userId);
        if (account == null)
        {
            return Enumerable.Empty<TransactionResponseDto>();
        }

        var transactions = await _unitOfWork.Transactions.GetByTypeAsync(account.Id, type.ToUpper());
        return transactions.Select(MapToTransactionResponseDto);
    }

    public async Task<TransactionResponseDto?> GetTransactionByIdAsync(Guid userId, Guid transactionId)
    {
        var account = await GetUserAccountAsync(userId);
        if (account == null)
        {
            return null;
        }

        var transaction = await _unitOfWork.Transactions.GetByIdAsync(transactionId);
        if (transaction == null || transaction.AccountId != account.Id)
        {
            return null;
        }

        return MapToTransactionResponseDto(transaction);
    }

    public async Task<decimal> GetAccountBalanceAsync(Guid userId)
    {
        var account = await GetUserAccountAsync(userId);
        return account?.Balance ?? 0;
    }

    public async Task<(decimal DailyLimit, decimal DailyUsed, decimal MonthlyLimit, decimal MonthlyUsed)> GetTransactionLimitsAsync(Guid userId)
    {
        var account = await GetUserAccountAsync(userId);
        if (account == null)
        {
            return (0, 0, 0, 0);
        }

        var today = DateTime.Today;
        var startOfMonth = new DateTime(today.Year, today.Month, 1);

        var dailyUsed = await _unitOfWork.Transactions.GetDailyTransactionAmountAsync(account.Id, today);
        var monthlyUsed = await _unitOfWork.Transactions.GetMonthlyTransactionAmountAsync(account.Id, today.Year, today.Month);

        return (account.DailyLimit, dailyUsed, account.MonthlyLimit, monthlyUsed);
    }

    #region Private Helper Methods

    private async Task<Account?> GetUserAccountAsync(Guid userId)
    {
        var account = await _unitOfWork.Accounts.GetByUserIdAsync(userId);
        return account?.IsActive == true ? account : null;
    }

    private async Task<List<string>> ValidateTransactionRequestAsync(TransactionRequestDto request, Account account)
    {
        var errors = new List<string>();

        // Validate transaction type
        if (!IsValidTransactionType(request.Type))
        {
            errors.Add("Invalid transaction type. Must be CREDIT or DEBIT.");
        }

        // Validate transaction category
        if (!IsValidTransactionCategory(request.Category))
        {
            errors.Add("Invalid transaction category.");
        }

        // Validate amount
        if (request.Amount <= 0)
        {
            errors.Add("Transaction amount must be greater than zero.");
        }

        // Check daily and monthly limits for debit transactions
        if (request.Type.ToUpper() == "DEBIT")
        {
            var (dailyLimit, dailyUsed, monthlyLimit, monthlyUsed) = await GetTransactionLimitsAsync(account.UserId);
            
            if (dailyUsed + request.Amount > dailyLimit)
            {
                errors.Add($"Transaction would exceed daily limit of {dailyLimit:C}. Current usage: {dailyUsed:C}");
            }
            
            if (monthlyUsed + request.Amount > monthlyLimit)
            {
                errors.Add($"Transaction would exceed monthly limit of {monthlyLimit:C}. Current usage: {monthlyUsed:C}");
            }
        }

        // Validate recipient details for transfers
        if (request.Category.ToUpper() == "TRANSFER")
        {
            if (string.IsNullOrWhiteSpace(request.RecipientAccount))
            {
                errors.Add("Recipient account is required for transfers.");
            }
            
            if (string.IsNullOrWhiteSpace(request.RecipientName))
            {
                errors.Add("Recipient name is required for transfers.");
            }
        }

        return errors;
    }

    private bool IsValidTransactionType(string type)
    {
        var validTypes = new[] { "CREDIT", "DEBIT" };
        return validTypes.Contains(type.ToUpper());
    }

    private bool IsValidTransactionCategory(string category)
    {
        var validCategories = new[] { "DEPOSIT", "WITHDRAWAL", "TRANSFER", "PAYMENT", "REFUND" };
        return validCategories.Contains(category.ToUpper());
    }

    private decimal CalculateTransactionFee(string type, string category, decimal amount)
    {
        // Basic fee calculation logic - this could be made more sophisticated
        return type.ToUpper() switch
        {
            "DEBIT" when category.ToUpper() == "TRANSFER" => amount * 0.001m, // 0.1% for transfers
            "DEBIT" when category.ToUpper() == "WITHDRAWAL" => 2.50m, // Fixed fee for withdrawals
            _ => 0.00m // No fee for credits and deposits
        };
    }

    private async Task LogTransactionSecurityEventAsync(Guid userId, Transaction transaction)
    {
        var securityEvent = new SecurityEvent
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EventType = "TRANSACTION_PROCESSED",
            Severity = "INFO",
            Description = $"Transaction {transaction.Type} of {transaction.Amount:C} processed successfully",
            IsSuccessful = true,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.SecurityEvents.AddAsync(securityEvent);
    }

    private TransactionResponseDto MapToTransactionResponseDto(Transaction transaction)
    {
        return new TransactionResponseDto
        {
            TransactionId = transaction.Id,
            AccountId = transaction.AccountId,
            Type = transaction.Type,
            Category = transaction.Category,
            Amount = transaction.Amount,
            BalanceAfter = transaction.BalanceAfter,
            Description = transaction.Description,
            Reference = transaction.Reference,
            RecipientAccount = transaction.RecipientAccount,
            RecipientName = transaction.RecipientName,
            Status = transaction.Status,
            Fee = transaction.Fee,
            CreatedAt = transaction.CreatedAt,
            ProcessedAt = transaction.ProcessedAt
        };
    }

    #endregion
}

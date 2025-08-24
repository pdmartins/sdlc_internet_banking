using ContosoBank.Application.DTOs;
using ContosoBank.Application.Interfaces;
using ContosoBank.Domain.Entities;
using ContosoBank.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using CsvHelper;
using CsvHelper.Configuration;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Globalization;
using System.Text;

namespace ContosoBank.Application.Services;

/// <summary>
/// Service implementation for transaction export operations
/// </summary>
public class TransactionExportService : ITransactionExportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TransactionExportService> _logger;
    private readonly ITransactionService _transactionService;

    public TransactionExportService(
        IUnitOfWork unitOfWork,
        ILogger<TransactionExportService> logger,
        ITransactionService transactionService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _transactionService = transactionService;
    }

    public async Task<TransactionExportResultDto> ExportToCsvAsync(Guid userId, TransactionExportRequestDto request)
    {
        try
        {
            _logger.LogInformation("Starting CSV export for user {UserId}", userId);

            // Validate the request
            var (isValid, errors) = await ValidateExportRequestAsync(request);
            if (!isValid)
            {
                return new TransactionExportResultDto
                {
                    IsSuccessful = false,
                    ErrorMessage = string.Join("; ", errors)
                };
            }

            // Get filtered transactions
            var transactions = await GetFilteredTransactionsForExportAsync(userId, request);
            
            if (!transactions.Any())
            {
                return new TransactionExportResultDto
                {
                    IsSuccessful = false,
                    ErrorMessage = "No transactions found for the specified criteria"
                };
            }

            // Generate CSV content
            var csvContent = GenerateCsvContent(transactions, request);
            var fileName = GenerateFileName(userId, "csv", request.StartDate, request.EndDate);

            // Log export activity for audit
            await LogExportActivity(userId, "CSV", transactions.Count(), fileName);

            return new TransactionExportResultDto
            {
                IsSuccessful = true,
                FileContent = Convert.ToBase64String(Encoding.UTF8.GetBytes(csvContent)),
                FileName = fileName,
                ContentType = "text/csv",
                TransactionCount = transactions.Count(),
                ExportedAt = DateTime.UtcNow,
                FileSizeBytes = Encoding.UTF8.GetByteCount(csvContent)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting transactions to CSV for user {UserId}", userId);
            return new TransactionExportResultDto
            {
                IsSuccessful = false,
                ErrorMessage = "Failed to export transactions to CSV"
            };
        }
    }

    public async Task<TransactionExportResultDto> ExportToPdfAsync(Guid userId, TransactionExportRequestDto request)
    {
        try
        {
            _logger.LogInformation("Starting PDF export for user {UserId}", userId);

            // Validate the request
            var (isValid, errors) = await ValidateExportRequestAsync(request);
            if (!isValid)
            {
                return new TransactionExportResultDto
                {
                    IsSuccessful = false,
                    ErrorMessage = string.Join("; ", errors)
                };
            }

            // Get filtered transactions
            var transactions = await GetFilteredTransactionsForExportAsync(userId, request);
            
            if (!transactions.Any())
            {
                return new TransactionExportResultDto
                {
                    IsSuccessful = false,
                    ErrorMessage = "No transactions found for the specified criteria"
                };
            }

            // Get user account info for header
            var account = await GetUserAccountAsync(userId);
            if (account == null)
            {
                return new TransactionExportResultDto
                {
                    IsSuccessful = false,
                    ErrorMessage = "User account not found"
                };
            }

            // Generate PDF content
            var pdfBytes = GeneratePdfContent(transactions, account, request);
            var fileName = GenerateFileName(userId, "pdf", request.StartDate, request.EndDate);

            // Log export activity for audit
            await LogExportActivity(userId, "PDF", transactions.Count(), fileName);

            return new TransactionExportResultDto
            {
                IsSuccessful = true,
                FileContent = Convert.ToBase64String(pdfBytes),
                FileName = fileName,
                ContentType = "application/pdf",
                TransactionCount = transactions.Count(),
                ExportedAt = DateTime.UtcNow,
                FileSizeBytes = pdfBytes.Length
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting transactions to PDF for user {UserId}", userId);
            return new TransactionExportResultDto
            {
                IsSuccessful = false,
                ErrorMessage = "Failed to export transactions to PDF"
            };
        }
    }

    public async Task<PaginatedTransactionResultDto> GetFilteredTransactionHistoryAsync(Guid userId, TransactionFilterDto filter)
    {
        try
        {
            var account = await GetUserAccountAsync(userId);
            if (account == null)
            {
                return new PaginatedTransactionResultDto
                {
                    Transactions = [],
                    CurrentPage = filter.PageNumber,
                    TotalPages = 0,
                    TotalTransactions = 0,
                    PageSize = filter.PageSize
                };
            }

            // Get filtered transactions with count
            var (transactions, totalCount) = await GetFilteredTransactionsWithCountAsync(account.Id, filter);

            // Calculate pagination
            var totalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize);

            // Convert to DTOs
            var transactionDtos = transactions.Select(MapToTransactionResponseDto);

            // Get summary if requested
            var summary = await GetTransactionSummaryAsync(userId, filter.StartDate, filter.EndDate);

            return new PaginatedTransactionResultDto
            {
                Transactions = transactionDtos,
                CurrentPage = filter.PageNumber,
                TotalPages = totalPages,
                TotalTransactions = totalCount,
                PageSize = filter.PageSize,
                Summary = summary
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting filtered transaction history for user {UserId}", userId);
            throw;
        }
    }

    public async Task<TransactionSummaryDto> GetTransactionSummaryAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var account = await GetUserAccountAsync(userId);
            if (account == null)
            {
                return new TransactionSummaryDto();
            }

            var transactions = await GetTransactionsInDateRangeAsync(account.Id, startDate, endDate);

            var credits = transactions.Where(t => t.Type.ToUpper() == "CREDIT").ToList();
            var debits = transactions.Where(t => t.Type.ToUpper() == "DEBIT").ToList();

            return new TransactionSummaryDto
            {
                TotalCredits = credits.Count,
                TotalCreditAmount = credits.Sum(t => t.Amount),
                TotalDebits = debits.Count,
                TotalDebitAmount = debits.Sum(t => t.Amount),
                EarliestTransaction = transactions.Any() ? transactions.Min(t => t.CreatedAt) : null,
                LatestTransaction = transactions.Any() ? transactions.Max(t => t.CreatedAt) : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transaction summary for user {UserId}", userId);
            throw;
        }
    }

    public Task<(bool IsValid, List<string> Errors)> ValidateExportRequestAsync(TransactionExportRequestDto request)
    {
        var errors = new List<string>();

        // Validate format
        if (!string.IsNullOrEmpty(request.Format) && 
            !new[] { "CSV", "PDF" }.Contains(request.Format.ToUpper()))
        {
            errors.Add("Invalid export format. Must be CSV or PDF.");
        }

        // Validate date range
        if (request.StartDate.HasValue && request.EndDate.HasValue && 
            request.StartDate > request.EndDate)
        {
            errors.Add("Start date cannot be greater than end date.");
        }

        // Validate date range is not too large (prevent abuse)
        if (request.StartDate.HasValue && request.EndDate.HasValue)
        {
            var daysDifference = (request.EndDate.Value - request.StartDate.Value).TotalDays;
            if (daysDifference > 365)
            {
                errors.Add("Date range cannot exceed 365 days.");
            }
        }

        // Validate transaction type
        if (!string.IsNullOrEmpty(request.TransactionType) && 
            !new[] { "CREDIT", "DEBIT" }.Contains(request.TransactionType.ToUpper()))
        {
            errors.Add("Invalid transaction type. Must be CREDIT or DEBIT.");
        }

        // Validate amount range
        if (request.MinAmount.HasValue && request.MaxAmount.HasValue && 
            request.MinAmount > request.MaxAmount)
        {
            errors.Add("Minimum amount cannot be greater than maximum amount.");
        }

        return Task.FromResult((errors.Count == 0, errors));
    }

    #region Private Helper Methods

    private async Task<IEnumerable<Transaction>> GetFilteredTransactionsForExportAsync(Guid userId, TransactionExportRequestDto request)
    {
        _logger.LogInformation("Getting transactions for export - UserId: {UserId}", userId);
        
        var account = await GetUserAccountAsync(userId);
        if (account == null) 
        {
            _logger.LogWarning("No account found for user {UserId}", userId);
            return [];
        }

        _logger.LogInformation("Found account {AccountId} for user {UserId}", account.Id, userId);

        // Start with all transactions for the account
        var query = await _unitOfWork.Transactions.GetByAccountIdAsync(account.Id);
        
        _logger.LogInformation("Found {TransactionCount} total transactions for account {AccountId}", query.Count(), account.Id);

        // Apply filters
        if (request.StartDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt <= request.EndDate.Value);
        }

        if (!string.IsNullOrEmpty(request.TransactionType))
        {
            query = query.Where(t => t.Type.ToUpper() == request.TransactionType.ToUpper());
        }

        if (request.MinAmount.HasValue)
        {
            query = query.Where(t => t.Amount >= request.MinAmount.Value);
        }

        if (request.MaxAmount.HasValue)
        {
            query = query.Where(t => t.Amount <= request.MaxAmount.Value);
        }

        if (!string.IsNullOrEmpty(request.Status))
        {
            query = query.Where(t => t.Status.ToUpper() == request.Status.ToUpper());
        }

        if (!string.IsNullOrEmpty(request.SearchText))
        {
            var searchLower = request.SearchText.ToLower();
            query = query.Where(t => 
                t.Description.ToLower().Contains(searchLower) ||
                (t.RecipientName != null && t.RecipientName.ToLower().Contains(searchLower))
            );
        }

        var result = query.OrderByDescending(t => t.CreatedAt);
        _logger.LogInformation("After filtering: {FilteredTransactionCount} transactions for export", result.Count());
        
        return result;
    }

    private async Task<(IEnumerable<Transaction> Transactions, int TotalCount)> GetFilteredTransactionsWithCountAsync(Guid accountId, TransactionFilterDto filter)
    {
        // Start with all transactions for the account
        var query = await _unitOfWork.Transactions.GetByAccountIdAsync(accountId);

        // Apply filters
        if (filter.StartDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt <= filter.EndDate.Value);
        }

        if (!string.IsNullOrEmpty(filter.TransactionType))
        {
            query = query.Where(t => t.Type.ToUpper() == filter.TransactionType.ToUpper());
        }

        if (filter.MinAmount.HasValue)
        {
            query = query.Where(t => t.Amount >= filter.MinAmount.Value);
        }

        if (filter.MaxAmount.HasValue)
        {
            query = query.Where(t => t.Amount <= filter.MaxAmount.Value);
        }

        if (!string.IsNullOrEmpty(filter.Status))
        {
            query = query.Where(t => t.Status.ToUpper() == filter.Status.ToUpper());
        }

        if (!string.IsNullOrEmpty(filter.SearchText))
        {
            var searchLower = filter.SearchText.ToLower();
            query = query.Where(t => 
                t.Description.ToLower().Contains(searchLower) ||
                (t.RecipientName != null && t.RecipientName.ToLower().Contains(searchLower))
            );
        }

        var totalCount = query.Count();

        // Apply sorting
        if (filter.SortBy.ToUpper() == "AMOUNT")
        {
            query = filter.SortDirection.ToUpper() == "ASC" 
                ? query.OrderBy(t => t.Amount) 
                : query.OrderByDescending(t => t.Amount);
        }
        else if (filter.SortBy.ToUpper() == "TYPE")
        {
            query = filter.SortDirection.ToUpper() == "ASC" 
                ? query.OrderBy(t => t.Type) 
                : query.OrderByDescending(t => t.Type);
        }
        else if (filter.SortBy.ToUpper() == "DESCRIPTION")
        {
            query = filter.SortDirection.ToUpper() == "ASC" 
                ? query.OrderBy(t => t.Description) 
                : query.OrderByDescending(t => t.Description);
        }
        else // Default to date sorting
        {
            query = filter.SortDirection.ToUpper() == "ASC" 
                ? query.OrderBy(t => t.CreatedAt) 
                : query.OrderByDescending(t => t.CreatedAt);
        }

        // Apply pagination
        var pagedQuery = query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize);

        return (pagedQuery, totalCount);
    }

    private async Task<IEnumerable<Transaction>> GetTransactionsInDateRangeAsync(Guid accountId, DateTime? startDate, DateTime? endDate)
    {
        var query = await _unitOfWork.Transactions.GetByAccountIdAsync(accountId);

        if (startDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt <= endDate.Value);
        }

        return query;
    }

    private string GenerateCsvContent(IEnumerable<Transaction> transactions, TransactionExportRequestDto request)
    {
        var stringWriter = new StringWriter();
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
        };

        using var csv = new CsvWriter(stringWriter, config);

        // Write header
        csv.WriteField("Transaction ID");
        csv.WriteField("Date");
        csv.WriteField("Type");
        csv.WriteField("Amount");
        csv.WriteField("Description");
        csv.WriteField("Recipient Name");
        csv.WriteField("Recipient Account");
        csv.WriteField("Status");
        csv.WriteField("Category");
        csv.WriteField("Reference");
        csv.WriteField("Fee");
        csv.WriteField("Balance After");
        csv.NextRecord();

        // Write data
        foreach (var transaction in transactions)
        {
            csv.WriteField(transaction.Id.ToString());
            csv.WriteField(FormatDate(transaction.CreatedAt, request.DateFormat));
            csv.WriteField(transaction.Type);
            csv.WriteField(FormatCurrency(transaction.Amount, request.CurrencyFormat));
            csv.WriteField(transaction.Description);
            csv.WriteField(request.IncludeSensitiveData ? transaction.RecipientName : MaskSensitiveData(transaction.RecipientName));
            csv.WriteField(request.IncludeSensitiveData ? transaction.RecipientAccount : MaskSensitiveData(transaction.RecipientAccount));
            csv.WriteField(transaction.Status);
            csv.WriteField(transaction.Category);
            csv.WriteField(transaction.Reference);
            csv.WriteField(FormatCurrency(transaction.Fee, request.CurrencyFormat));
            csv.WriteField(FormatCurrency(transaction.BalanceAfter, request.CurrencyFormat));
            csv.NextRecord();
        }

        return stringWriter.ToString();
    }

    private byte[] GeneratePdfContent(IEnumerable<Transaction> transactions, Account account, TransactionExportRequestDto request)
    {
        using var stream = new MemoryStream();
        var document = new Document(PageSize.A4, 36, 36, 54, 54);
        var writer = PdfWriter.GetInstance(document, stream);

        document.Open();

        // Add header
        var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, BaseColor.Black);
        var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.Black);
        var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.Black);

        // Title
        var title = new Paragraph("CONTOSO BANK - Transaction Statement", titleFont)
        {
            Alignment = Element.ALIGN_CENTER,
            SpacingAfter = 20
        };
        document.Add(title);

        // Account info
        var accountInfo = new Paragraph($"Account: {MaskAccountNumber(account.AccountNumber)}\nGenerated: {DateTime.Now:yyyy-MM-dd HH:mm:ss} UTC", normalFont)
        {
            SpacingAfter = 20
        };
        document.Add(accountInfo);

        // Create table
        var table = new PdfPTable(7) { WidthPercentage = 100 };
        table.SetWidths([1.5f, 1f, 1f, 1.5f, 2f, 1f, 1f]);

        // Table headers
        var headers = new[] { "Date", "Type", "Amount", "Description", "Recipient", "Status", "Fee" };
        foreach (var header in headers)
        {
            var cell = new PdfPCell(new Phrase(header, headerFont))
            {
                BackgroundColor = new BaseColor(240, 240, 240),
                Padding = 8,
                HorizontalAlignment = Element.ALIGN_CENTER
            };
            table.AddCell(cell);
        }

        // Add transaction data
        foreach (var transaction in transactions)
        {
            table.AddCell(new PdfPCell(new Phrase(FormatDate(transaction.CreatedAt, request.DateFormat), normalFont)) { Padding = 5 });
            table.AddCell(new PdfPCell(new Phrase(transaction.Type, normalFont)) { Padding = 5 });
            table.AddCell(new PdfPCell(new Phrase(FormatCurrency(transaction.Amount, request.CurrencyFormat), normalFont)) { Padding = 5, HorizontalAlignment = Element.ALIGN_RIGHT });
            table.AddCell(new PdfPCell(new Phrase(transaction.Description, normalFont)) { Padding = 5 });
            table.AddCell(new PdfPCell(new Phrase(request.IncludeSensitiveData ? transaction.RecipientName ?? "" : MaskSensitiveData(transaction.RecipientName), normalFont)) { Padding = 5 });
            table.AddCell(new PdfPCell(new Phrase(transaction.Status, normalFont)) { Padding = 5 });
            table.AddCell(new PdfPCell(new Phrase(FormatCurrency(transaction.Fee, request.CurrencyFormat), normalFont)) { Padding = 5, HorizontalAlignment = Element.ALIGN_RIGHT });
        }

        document.Add(table);

        // Add summary
        var summary = GetTransactionSummaryForPeriod(transactions);
        var summaryParagraph = new Paragraph($"\n\nSummary:\nTotal Credits: {summary.TotalCredits} ({FormatCurrency(summary.TotalCreditAmount, request.CurrencyFormat)})\nTotal Debits: {summary.TotalDebits} ({FormatCurrency(summary.TotalDebitAmount, request.CurrencyFormat)})\nNet Amount: {FormatCurrency(summary.NetAmount, request.CurrencyFormat)}", normalFont);
        document.Add(summaryParagraph);

        document.Close();
        return stream.ToArray();
    }

    private async Task<Account?> GetUserAccountAsync(Guid userId)
    {
        var account = await _unitOfWork.Accounts.GetByUserIdAsync(userId);
        return account?.IsActive == true ? account : null;
    }

    private string FormatDate(DateTime date, string format)
    {
        return format.ToUpper() switch
        {
            "ISO8601" => date.ToString("yyyy-MM-dd HH:mm:ss"),
            "US" => date.ToString("MM/dd/yyyy HH:mm:ss"),
            "EU" => date.ToString("dd/MM/yyyy HH:mm:ss"),
            _ => date.ToString("yyyy-MM-dd HH:mm:ss")
        };
    }

    private string FormatCurrency(decimal amount, string currencyFormat)
    {
        return currencyFormat.ToUpper() switch
        {
            "USD" => amount.ToString("C", new CultureInfo("en-US")),
            "EUR" => amount.ToString("C", new CultureInfo("de-DE")),
            "BRL" => amount.ToString("C", new CultureInfo("pt-BR")),
            _ => amount.ToString("C", CultureInfo.InvariantCulture)
        };
    }

    private string MaskSensitiveData(string? data)
    {
        if (string.IsNullOrEmpty(data)) return "";
        if (data.Length <= 4) return new string('*', data.Length);
        return data[..2] + new string('*', data.Length - 4) + data[^2..];
    }

    private string MaskAccountNumber(string accountNumber)
    {
        if (string.IsNullOrEmpty(accountNumber)) return "";
        if (accountNumber.Length <= 4) return new string('*', accountNumber.Length);
        return accountNumber[..4] + new string('*', accountNumber.Length - 8) + accountNumber[^4..];
    }

    private string GenerateFileName(Guid userId, string format, DateTime? startDate, DateTime? endDate)
    {
        var dateRange = "";
        if (startDate.HasValue && endDate.HasValue)
        {
            dateRange = $"_{startDate.Value:yyyyMMdd}-{endDate.Value:yyyyMMdd}";
        }
        else if (startDate.HasValue)
        {
            dateRange = $"_from_{startDate.Value:yyyyMMdd}";
        }
        else if (endDate.HasValue)
        {
            dateRange = $"_until_{endDate.Value:yyyyMMdd}";
        }

        return $"transactions_{userId.ToString()[..8]}{dateRange}_{DateTime.UtcNow:yyyyMMddHHmmss}.{format.ToLower()}";
    }

    private Task LogExportActivity(Guid userId, string format, int transactionCount, string fileName)
    {
        try
        {
            _logger.LogInformation("Export completed: User={UserId}, Format={Format}, Count={Count}, File={FileName}", 
                userId, format, transactionCount, fileName);
            
            // Here you could add additional audit logging to database if required
            // await _auditService.LogExportAsync(userId, format, transactionCount, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log export activity for user {UserId}", userId);
        }
        
        return Task.CompletedTask;
    }

    private TransactionSummaryDto GetTransactionSummaryForPeriod(IEnumerable<Transaction> transactions)
    {
        var credits = transactions.Where(t => t.Type.ToUpper() == "CREDIT").ToList();
        var debits = transactions.Where(t => t.Type.ToUpper() == "DEBIT").ToList();

        return new TransactionSummaryDto
        {
            TotalCredits = credits.Count,
            TotalCreditAmount = credits.Sum(t => t.Amount),
            TotalDebits = debits.Count,
            TotalDebitAmount = debits.Sum(t => t.Amount),
            EarliestTransaction = transactions.Any() ? transactions.Min(t => t.CreatedAt) : null,
            LatestTransaction = transactions.Any() ? transactions.Max(t => t.CreatedAt) : null
        };
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

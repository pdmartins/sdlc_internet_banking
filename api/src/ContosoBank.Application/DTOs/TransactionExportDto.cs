namespace ContosoBank.Application.DTOs;

/// <summary>
/// DTO for transaction export filtering and formatting options
/// </summary>
public class TransactionExportRequestDto
{
    /// <summary>
    /// Export format (CSV or PDF)
    /// </summary>
    public string Format { get; set; } = "CSV"; // CSV, PDF

    /// <summary>
    /// Start date for filtering transactions
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date for filtering transactions
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Transaction type filter (CREDIT, DEBIT, or null for all)
    /// </summary>
    public string? TransactionType { get; set; }

    /// <summary>
    /// Minimum amount filter
    /// </summary>
    public decimal? MinAmount { get; set; }

    /// <summary>
    /// Maximum amount filter
    /// </summary>
    public decimal? MaxAmount { get; set; }

    /// <summary>
    /// Transaction status filter
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Free text search in transaction descriptions
    /// </summary>
    public string? SearchText { get; set; }

    /// <summary>
    /// Include sensitive data in export (requires additional permissions)
    /// </summary>
    public bool IncludeSensitiveData { get; set; } = false;

    /// <summary>
    /// Date format for export (ISO8601, US, EU, etc.)
    /// </summary>
    public string DateFormat { get; set; } = "ISO8601";

    /// <summary>
    /// Currency format for amounts
    /// </summary>
    public string CurrencyFormat { get; set; } = "USD";
}

/// <summary>
/// DTO for transaction export result
/// </summary>
public class TransactionExportResultDto
{
    /// <summary>
    /// Indicates if export was successful
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Export file content as base64 string
    /// </summary>
    public string? FileContent { get; set; }

    /// <summary>
    /// Suggested filename for download
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// MIME type of the exported file
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Number of transactions exported
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// Export generation timestamp
    /// </summary>
    public DateTime ExportedAt { get; set; }

    /// <summary>
    /// Error message if export failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSizeBytes { get; set; }
}

/// <summary>
/// DTO for enhanced transaction filtering
/// </summary>
public class TransactionFilterDto
{
    /// <summary>
    /// Page number for pagination (1-based)
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Start date for filtering transactions
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date for filtering transactions
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Transaction type filter (CREDIT, DEBIT, or null for all)
    /// </summary>
    public string? TransactionType { get; set; }

    /// <summary>
    /// Minimum amount filter
    /// </summary>
    public decimal? MinAmount { get; set; }

    /// <summary>
    /// Maximum amount filter
    /// </summary>
    public decimal? MaxAmount { get; set; }

    /// <summary>
    /// Transaction status filter
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Free text search in transaction descriptions
    /// </summary>
    public string? SearchText { get; set; }

    /// <summary>
    /// Sort field (Date, Amount, Type, Description)
    /// </summary>
    public string SortBy { get; set; } = "Date";

    /// <summary>
    /// Sort direction (ASC, DESC)
    /// </summary>
    public string SortDirection { get; set; } = "DESC";
}

/// <summary>
/// DTO for paginated transaction results
/// </summary>
public class PaginatedTransactionResultDto
{
    /// <summary>
    /// List of transactions for current page
    /// </summary>
    public IEnumerable<TransactionResponseDto> Transactions { get; set; } = [];

    /// <summary>
    /// Current page number
    /// </summary>
    public int CurrentPage { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Total number of transactions matching filter
    /// </summary>
    public int TotalTransactions { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Indicates if there is a next page
    /// </summary>
    public bool HasNextPage => CurrentPage < TotalPages;

    /// <summary>
    /// Indicates if there is a previous page
    /// </summary>
    public bool HasPreviousPage => CurrentPage > 1;

    /// <summary>
    /// Summary statistics for filtered transactions
    /// </summary>
    public TransactionSummaryDto? Summary { get; set; }
}

/// <summary>
/// DTO for transaction summary statistics
/// </summary>
public class TransactionSummaryDto
{
    /// <summary>
    /// Total number of credit transactions
    /// </summary>
    public int TotalCredits { get; set; }

    /// <summary>
    /// Total amount of credit transactions
    /// </summary>
    public decimal TotalCreditAmount { get; set; }

    /// <summary>
    /// Total number of debit transactions
    /// </summary>
    public int TotalDebits { get; set; }

    /// <summary>
    /// Total amount of debit transactions
    /// </summary>
    public decimal TotalDebitAmount { get; set; }

    /// <summary>
    /// Net amount (credits - debits)
    /// </summary>
    public decimal NetAmount => TotalCreditAmount - TotalDebitAmount;

    /// <summary>
    /// Date range of transactions
    /// </summary>
    public DateTime? EarliestTransaction { get; set; }

    /// <summary>
    /// Latest transaction date
    /// </summary>
    public DateTime? LatestTransaction { get; set; }
}

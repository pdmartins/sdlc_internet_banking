namespace ContosoBank.Application.DTOs;

/// <summary>
/// DTO for security alert information
/// </summary>
public class SecurityAlertDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object> Details { get; set; } = new();
    public string DeliveryMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public bool RequiresAction { get; set; }
    public string? ActionUrl { get; set; }
    public string? ActionText { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime? ActionTakenAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}

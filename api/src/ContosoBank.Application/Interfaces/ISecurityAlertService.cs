using ContosoBank.Application.DTOs;

namespace ContosoBank.Application.Interfaces;

/// <summary>
/// Service interface for security alert operations
/// </summary>
public interface ISecurityAlertService
{
    /// <summary>
    /// Creates and sends a security alert to a user
    /// </summary>
    Task CreateAndSendAlertAsync(SecurityAlertRequest request);

    /// <summary>
    /// Gets security alerts for a user
    /// </summary>
    Task<IEnumerable<SecurityAlertDto>> GetUserAlertsAsync(Guid userId, bool unreadOnly = false);

    /// <summary>
    /// Marks an alert as read
    /// </summary>
    Task MarkAlertAsReadAsync(Guid alertId, Guid userId);

    /// <summary>
    /// Processes action taken on an alert
    /// </summary>
    Task ProcessAlertActionAsync(Guid alertId, Guid userId);

    /// <summary>
    /// Gets unread alert count for a user
    /// </summary>
    Task<int> GetUnreadAlertCountAsync(Guid userId);

    /// <summary>
    /// Sends pending alerts that haven't been delivered
    /// </summary>
    Task ProcessPendingAlertsAsync();
}

/// <summary>
/// Request to create a security alert
/// </summary>
public class SecurityAlertRequest
{
    public Guid UserId { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object>? Details { get; set; }
    public string DeliveryMethod { get; set; } = "Email";
    public bool RequiresAction { get; set; }
    public string? ActionUrl { get; set; }
    public string? ActionText { get; set; }
    public Guid? LoginAttemptId { get; set; }
    public Guid? AnomalyDetectionId { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

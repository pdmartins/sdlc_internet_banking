namespace ContosoBank.Application.DTOs;

/// <summary>
/// DTO for anomaly detection information
/// </summary>
public class AnomalyDetectionDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public Guid LoginAttemptId { get; set; }
    public string AnomalyType { get; set; } = string.Empty;
    public int Severity { get; set; }
    public int RiskScore { get; set; }
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Details { get; set; } = new();
    public string Status { get; set; } = string.Empty;
    public string ResponseAction { get; set; } = string.Empty;
    public bool IsResolved { get; set; }
    public string? ResolutionNotes { get; set; }
    public Guid? ResolvedByUserId { get; set; }
    public string? ResolvedByUserName { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime DetectedAt { get; set; }
    public LoginAttemptDto? LoginAttempt { get; set; }
}

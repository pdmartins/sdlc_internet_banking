using ContosoBank.Application.DTOs;

namespace ContosoBank.Application.Interfaces;

/// <summary>
/// Service interface for anomaly detection operations
/// </summary>
public interface IAnomalyDetectionService
{
    /// <summary>
    /// Analyzes a login attempt for anomalies and returns risk assessment
    /// </summary>
    Task<AnomalyAssessmentResult> AnalyzeLoginAttemptAsync(LoginAttemptData loginData);

    /// <summary>
    /// Updates user login patterns based on successful login
    /// </summary>
    Task UpdateUserLoginPatternAsync(Guid userId, LoginAttemptData loginData);

    /// <summary>
    /// Gets unresolved anomalies for review
    /// </summary>
    Task<IEnumerable<AnomalyDetectionDto>> GetUnresolvedAnomaliesAsync();

    /// <summary>
    /// Resolves an anomaly with notes
    /// </summary>
    Task ResolveAnomalyAsync(Guid anomalyId, string resolutionNotes, Guid resolvedByUserId);

    /// <summary>
    /// Gets anomaly statistics for a date range
    /// </summary>
    Task<AnomalyStatisticsDto> GetAnomalyStatisticsAsync(DateTime from, DateTime to);
}

/// <summary>
/// Data class for login attempt information
/// </summary>
public class LoginAttemptData
{
    public Guid? UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string? Country { get; set; }
    public string? Region { get; set; }
    public string? City { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? DeviceFingerprint { get; set; }
    public string? DeviceType { get; set; }
    public string? OperatingSystem { get; set; }
    public string? Browser { get; set; }
    public DateTime AttemptedAt { get; set; }
    public bool IsSuccessful { get; set; }
    public string? FailureReason { get; set; }
}

/// <summary>
/// Result of anomaly assessment
/// </summary>
public class AnomalyAssessmentResult
{
    public bool IsAnomalous { get; set; }
    public int RiskScore { get; set; } // 0-100
    public List<string> AnomalyReasons { get; set; } = new();
    public string RecommendedAction { get; set; } = string.Empty; // Allow, StepUp, Block, Lock
    public Dictionary<string, object> Details { get; set; } = new();
}

namespace ContosoBank.Application.DTOs;

/// <summary>
/// DTO for login attempt information
/// </summary>
public class LoginAttemptDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string? Country { get; set; }
    public string? Region { get; set; }
    public string? City { get; set; }
    public string? DeviceFingerprint { get; set; }
    public string? DeviceType { get; set; }
    public string? OperatingSystem { get; set; }
    public string? Browser { get; set; }
    public DateTime AttemptedAt { get; set; }
    public bool IsSuccessful { get; set; }
    public string? FailureReason { get; set; }
    public bool IsAnomalous { get; set; }
    public List<string> AnomalyReasons { get; set; } = new();
    public int RiskScore { get; set; }
    public string? ResponseAction { get; set; }
}

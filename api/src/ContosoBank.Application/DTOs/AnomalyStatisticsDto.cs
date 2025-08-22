namespace ContosoBank.Application.DTOs;

/// <summary>
/// DTO for anomaly detection statistics
/// </summary>
public class AnomalyStatisticsDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalAnomalies { get; set; }
    public int ResolvedAnomalies { get; set; }
    public int PendingAnomalies { get; set; }
    public int HighSeverityAnomalies { get; set; }
    public int CriticalSeverityAnomalies { get; set; }
    public double AverageRiskScore { get; set; }
    public Dictionary<string, int> AnomaliesByType { get; set; } = new();
    public Dictionary<string, int> AnomaliesByDay { get; set; } = new();
    public Dictionary<int, int> AnomaliesBySeverity { get; set; } = new();
    public List<TopRiskyUserDto> TopRiskyUsers { get; set; } = new();
    public List<AnomalyTrendDto> DailyTrends { get; set; } = new();
}

/// <summary>
/// DTO for top risky users in statistics
/// </summary>
public class TopRiskyUserDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int AnomalyCount { get; set; }
    public double AverageRiskScore { get; set; }
    public DateTime LastAnomalyAt { get; set; }
}

/// <summary>
/// DTO for anomaly trends over time
/// </summary>
public class AnomalyTrendDto
{
    public DateTime Date { get; set; }
    public int TotalCount { get; set; }
    public int HighSeverityCount { get; set; }
    public double AverageRiskScore { get; set; }
    public Dictionary<string, int> TypeBreakdown { get; set; } = new();
}

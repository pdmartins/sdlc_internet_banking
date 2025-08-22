using System.ComponentModel.DataAnnotations;

namespace ContosoBank.Domain.Entities;

/// <summary>
/// Entity for tracking user's typical login patterns to detect anomalies
/// </summary>
public class UserLoginPattern
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [MaxLength(500)]
    public string TypicalIpAddresses { get; set; } = string.Empty; // JSON array of frequent IPs

    [MaxLength(500)]
    public string TypicalLocations { get; set; } = string.Empty; // JSON array of frequent locations

    [MaxLength(500)]
    public string TypicalDevices { get; set; } = string.Empty; // JSON array of device fingerprints

    [MaxLength(200)]
    public string TypicalLoginHours { get; set; } = string.Empty; // JSON array of hour ranges (0-23)

    [MaxLength(50)]
    public string TypicalDaysOfWeek { get; set; } = string.Empty; // JSON array of days (0-6)

    [MaxLength(100)]
    public string PreferredTimeZone { get; set; } = string.Empty;

    public DateTime FirstLoginAt { get; set; }
    public DateTime LastLoginAt { get; set; }
    public DateTime LastUpdatedAt { get; set; }

    public int TotalSuccessfulLogins { get; set; }
    public int TotalFailedLogins { get; set; }

    // Risk thresholds
    public int LocationRiskThreshold { get; set; } = 50; // 0-100
    public int TimeRiskThreshold { get; set; } = 30; // 0-100
    public int DeviceRiskThreshold { get; set; } = 70; // 0-100

    // Navigation properties
    public User User { get; set; } = null!;
}

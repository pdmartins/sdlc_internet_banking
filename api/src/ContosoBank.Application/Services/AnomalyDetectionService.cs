using ContosoBank.Application.DTOs;
using ContosoBank.Application.Interfaces;
using ContosoBank.Domain.Entities;
using ContosoBank.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ContosoBank.Application.Services;

/// <summary>
/// Service implementation for anomaly detection operations
/// </summary>
public class AnomalyDetectionService : IAnomalyDetectionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGeolocationService _geolocationService;
    private readonly ISecurityAlertService _securityAlertService;
    private readonly ILogger<AnomalyDetectionService> _logger;

    public AnomalyDetectionService(
        IUnitOfWork unitOfWork,
        IGeolocationService geolocationService,
        ISecurityAlertService securityAlertService,
        ILogger<AnomalyDetectionService> logger)
    {
        _unitOfWork = unitOfWork;
        _geolocationService = geolocationService;
        _securityAlertService = securityAlertService;
        _logger = logger;
    }

    public async Task<AnomalyAssessmentResult> AnalyzeLoginAttemptAsync(LoginAttemptData loginData)
    {
        try
        {
            _logger.LogInformation("Starting anomaly analysis for user {UserId}", loginData.UserId);

            // Create login attempt record
            var loginAttempt = await CreateLoginAttemptAsync(loginData);

            // Get user's login patterns
            var userPattern = await _unitOfWork.UserLoginPatterns.GetByUserIdAsync(loginData.UserId ?? Guid.Empty);
            
            // Perform risk analysis
            var riskAssessment = await PerformRiskAnalysisAsync(loginAttempt, userPattern, loginData);

            // If anomalous, create anomaly detection record
            if (riskAssessment.IsAnomalous)
            {
                await CreateAnomalyDetectionAsync(loginAttempt, riskAssessment);
            }

            // Update user patterns for future analysis
            if (loginData.UserId.HasValue)
            {
                await UpdateUserLoginPatternAsync(loginData.UserId.Value, loginData);
            }

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Anomaly analysis completed for user {UserId}. Risk score: {RiskScore}", 
                loginData.UserId, riskAssessment.RiskScore);

            return riskAssessment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing login attempt for user {UserId}", loginData.UserId);
            throw;
        }
    }

    public async Task UpdateUserLoginPatternAsync(Guid userId, LoginAttemptData loginData)
    {
        try
        {
            var pattern = await _unitOfWork.UserLoginPatterns.GetByUserIdAsync(userId);
            
            if (pattern == null)
            {
                // Create new pattern for first-time user
                pattern = new UserLoginPattern
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    FirstLoginAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow,
                    LastUpdatedAt = DateTime.UtcNow,
                    TotalSuccessfulLogins = loginData.IsSuccessful ? 1 : 0,
                    TotalFailedLogins = loginData.IsSuccessful ? 0 : 1,
                    TypicalIpAddresses = $"[\"{loginData.IpAddress}\"]",
                    TypicalLocations = string.IsNullOrEmpty(loginData.Country) ? "[]" : $"[\"{loginData.Country},{loginData.Region},{loginData.City}\"]",
                    TypicalDevices = string.IsNullOrEmpty(loginData.DeviceFingerprint) ? "[]" : $"[\"{loginData.DeviceFingerprint}\"]",
                    TypicalLoginHours = $"[{DateTime.UtcNow.Hour}]",
                    TypicalDaysOfWeek = $"[{(int)DateTime.UtcNow.DayOfWeek}]",
                    PreferredTimeZone = "UTC"
                };

                await _unitOfWork.UserLoginPatterns.AddAsync(pattern);
            }
            else
            {
                // Update existing pattern
                await UpdateExistingPatternAsync(pattern, loginData);
            }

            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating login pattern for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<AnomalyDetectionDto>> GetUnresolvedAnomaliesAsync()
    {
        try
        {
            var anomalies = await _unitOfWork.AnomalyDetections.GetUnresolvedAnomaliesAsync();
            
            return anomalies.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unresolved anomalies");
            throw;
        }
    }

    public async Task ResolveAnomalyAsync(Guid anomalyId, string resolutionNotes, Guid resolvedByUserId)
    {
        try
        {
            var anomaly = await _unitOfWork.AnomalyDetections.GetByIdAsync(anomalyId);
            if (anomaly == null)
            {
                throw new ArgumentException($"Anomaly with ID {anomalyId} not found");
            }

            anomaly.IsResolved = true;
            anomaly.ResolvedAt = DateTime.UtcNow;
            anomaly.ResolutionNotes = resolutionNotes;
            anomaly.ResolvedByUserId = resolvedByUserId;
            anomaly.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.AnomalyDetections.Update(anomaly);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Anomaly {AnomalyId} resolved by user {UserId}", anomalyId, resolvedByUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving anomaly {AnomalyId}", anomalyId);
            throw;
        }
    }

    public async Task<AnomalyStatisticsDto> GetAnomalyStatisticsAsync(DateTime from, DateTime to)
    {
        try
        {
            var anomalies = await _unitOfWork.AnomalyDetections.GetBySeverityAsync(1, from, to);
            
            var stats = new AnomalyStatisticsDto
            {
                FromDate = from,
                ToDate = to,
                TotalAnomalies = anomalies.Count(),
                ResolvedAnomalies = anomalies.Count(a => a.IsResolved),
                PendingAnomalies = anomalies.Count(a => !a.IsResolved),
                HighSeverityAnomalies = anomalies.Count(a => a.Severity >= 4),
                CriticalSeverityAnomalies = anomalies.Count(a => a.Severity == 5),
                AverageRiskScore = anomalies.Any() ? anomalies.Average(a => a.RiskScore) : 0,
                AnomaliesByType = anomalies.GroupBy(a => a.AnomalyType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                AnomaliesByDay = anomalies.GroupBy(a => a.DetectedAt.Date.ToString("yyyy-MM-dd"))
                    .ToDictionary(g => g.Key, g => g.Count()),
                AnomaliesBySeverity = anomalies.GroupBy(a => a.Severity)
                    .ToDictionary(g => g.Key, g => g.Count()),
                TopRiskyUsers = anomalies.GroupBy(a => a.UserId)
                    .Select(g => new TopRiskyUserDto
                    {
                        UserId = g.Key,
                        Email = g.First().User?.Email ?? "",
                        FullName = g.First().User?.FullName ?? "",
                        AnomalyCount = g.Count(),
                        AverageRiskScore = g.Average(a => a.RiskScore),
                        LastAnomalyAt = g.Max(a => a.DetectedAt)
                    })
                    .OrderByDescending(u => u.AverageRiskScore)
                    .Take(10)
                    .ToList(),
                DailyTrends = anomalies.GroupBy(a => a.DetectedAt.Date)
                    .Select(g => new AnomalyTrendDto
                    {
                        Date = g.Key,
                        TotalCount = g.Count(),
                        HighSeverityCount = g.Count(a => a.Severity >= 4),
                        AverageRiskScore = g.Average(a => a.RiskScore),
                        TypeBreakdown = g.GroupBy(a => a.AnomalyType)
                            .ToDictionary(tg => tg.Key, tg => tg.Count())
                    })
                    .OrderBy(t => t.Date)
                    .ToList()
            };

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating anomaly statistics for period {From} to {To}", from, to);
            throw;
        }
    }

    #region Private Methods

    private async Task<LoginAttempt> CreateLoginAttemptAsync(LoginAttemptData loginData)
    {
        var loginAttempt = new LoginAttempt
        {
            Id = Guid.NewGuid(),
            UserId = loginData.UserId,
            Email = loginData.Email,
            IpAddress = loginData.IpAddress,
            UserAgent = loginData.UserAgent,
            Country = loginData.Country,
            Region = loginData.Region,
            City = loginData.City,
            Latitude = loginData.Latitude,
            Longitude = loginData.Longitude,
            DeviceFingerprint = loginData.DeviceFingerprint,
            DeviceType = loginData.DeviceType,
            OperatingSystem = loginData.OperatingSystem,
            Browser = loginData.Browser,
            AttemptedAt = DateTime.UtcNow,
            IsSuccessful = loginData.IsSuccessful,
            FailureReason = loginData.FailureReason,
            IsAnomalous = false, // Will be updated after analysis
            RiskScore = 0, // Will be updated after analysis
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.LoginAttempts.AddAsync(loginAttempt);
        return loginAttempt;
    }

    private async Task<AnomalyAssessmentResult> PerformRiskAnalysisAsync(
        LoginAttempt loginAttempt, 
        UserLoginPattern? userPattern, 
        LoginAttemptData loginData)
    {
        var riskFactors = new List<string>();
        var riskScore = 0;

        // New user analysis
        if (userPattern == null)
        {
            riskScore += 20; // New users have inherent risk
            riskFactors.Add("new_user");
        }
        else
        {
            // Location analysis
            var locationRisk = AnalyzeLocationRisk(loginAttempt, userPattern);
            riskScore += locationRisk.Score;
            riskFactors.AddRange(locationRisk.Factors);

            // Time pattern analysis
            var timeRisk = AnalyzeTimeRisk(loginAttempt, userPattern);
            riskScore += timeRisk.Score;
            riskFactors.AddRange(timeRisk.Factors);

            // Device analysis
            var deviceRisk = AnalyzeDeviceRisk(loginAttempt, userPattern);
            riskScore += deviceRisk.Score;
            riskFactors.AddRange(deviceRisk.Factors);

            // Velocity analysis (rapid successive logins)
            var velocityRisk = await AnalyzeVelocityRiskAsync(loginAttempt);
            riskScore += velocityRisk.Score;
            riskFactors.AddRange(velocityRisk.Factors);
        }

        // Failed login analysis
        if (!loginAttempt.IsSuccessful)
        {
            var failureRisk = await AnalyzeFailurePatternAsync(loginAttempt);
            riskScore += failureRisk.Score;
            riskFactors.AddRange(failureRisk.Factors);
        }

        // Determine if anomalous
        var isAnomalous = riskScore >= 50; // Threshold for anomaly
        var responseAction = DetermineResponseAction(riskScore, riskFactors);

        // Update login attempt with analysis results
        loginAttempt.IsAnomalous = isAnomalous;
        loginAttempt.RiskScore = Math.Min(riskScore, 100);
        loginAttempt.AnomalyReasons = string.Join(",", riskFactors);
        loginAttempt.ResponseAction = responseAction;

        return new AnomalyAssessmentResult
        {
            IsAnomalous = isAnomalous,
            RiskScore = Math.Min(riskScore, 100),
            AnomalyReasons = riskFactors,
            RecommendedAction = responseAction,
            Details = new Dictionary<string, object>
            {
                ["severity"] = CalculateSeverity(riskScore),
                ["factors"] = riskFactors,
                ["recommendations"] = GenerateRecommendations(riskScore, riskFactors)
            }
        };
    }

    private RiskAnalysisResult AnalyzeLocationRisk(LoginAttempt loginAttempt, UserLoginPattern userPattern)
    {
        var factors = new List<string>();
        var score = 0;

        // Parse typical locations
        var typicalLocations = ParseJsonArray(userPattern.TypicalLocations);
        var currentLocation = $"{loginAttempt.Country},{loginAttempt.Region},{loginAttempt.City}";

        if (!typicalLocations.Contains(currentLocation) && !string.IsNullOrEmpty(loginAttempt.Country))
        {
            score += userPattern.LocationRiskThreshold;
            factors.Add("unusual_location");

            // Check if it's a completely new country
            var typicalCountries = typicalLocations
                .Where(l => !string.IsNullOrEmpty(l))
                .Select(l => l.Split(',').FirstOrDefault())
                .Distinct();

            if (!typicalCountries.Contains(loginAttempt.Country))
            {
                score += 30;
                factors.Add("new_country");
            }
        }

        return new RiskAnalysisResult { Score = score, Factors = factors };
    }

    private RiskAnalysisResult AnalyzeTimeRisk(LoginAttempt loginAttempt, UserLoginPattern userPattern)
    {
        var factors = new List<string>();
        var score = 0;

        var typicalHours = ParseJsonArray(userPattern.TypicalLoginHours)
            .Where(h => int.TryParse(h, out _))
            .Select(int.Parse)
            .ToList();

        var currentHour = loginAttempt.AttemptedAt.Hour;

        if (typicalHours.Any() && !typicalHours.Contains(currentHour))
        {
            // Check if it's during typical "off hours" (late night/early morning)
            if (currentHour >= 0 && currentHour <= 5)
            {
                score += userPattern.TimeRiskThreshold + 20;
                factors.Add("unusual_time_late_night");
            }
            else
            {
                score += userPattern.TimeRiskThreshold;
                factors.Add("unusual_time");
            }
        }

        return new RiskAnalysisResult { Score = score, Factors = factors };
    }

    private RiskAnalysisResult AnalyzeDeviceRisk(LoginAttempt loginAttempt, UserLoginPattern userPattern)
    {
        var factors = new List<string>();
        var score = 0;

        if (!string.IsNullOrEmpty(loginAttempt.DeviceFingerprint))
        {
            var typicalDevices = ParseJsonArray(userPattern.TypicalDevices);

            if (!typicalDevices.Contains(loginAttempt.DeviceFingerprint))
            {
                score += userPattern.DeviceRiskThreshold;
                factors.Add("new_device");
            }
        }

        return new RiskAnalysisResult { Score = score, Factors = factors };
    }

    private async Task<RiskAnalysisResult> AnalyzeVelocityRiskAsync(LoginAttempt loginAttempt)
    {
        var factors = new List<string>();
        var score = 0;

        // Check for rapid successive login attempts in the last 5 minutes
        var recentAttempts = await _unitOfWork.LoginAttempts
            .GetByUserIdAndDateRangeAsync(loginAttempt.UserId ?? Guid.Empty, DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow);

        if (recentAttempts.Count() > 3)
        {
            score += 40;
            factors.Add("high_velocity");
        }
        else if (recentAttempts.Count() > 1)
        {
            score += 20;
            factors.Add("moderate_velocity");
        }

        return new RiskAnalysisResult { Score = score, Factors = factors };
    }

    private async Task<RiskAnalysisResult> AnalyzeFailurePatternAsync(LoginAttempt loginAttempt)
    {
        var factors = new List<string>();
        var score = 0;

        // Check recent failed attempts from this IP
        var recentFailures = await _unitOfWork.LoginAttempts
            .GetByIpAddressAsync(loginAttempt.IpAddress, DateTime.UtcNow.AddHours(-1), DateTime.UtcNow);

        var failureCount = recentFailures.Count(a => !a.IsSuccessful);

        if (failureCount > 5)
        {
            score += 60;
            factors.Add("brute_force_pattern");
        }
        else if (failureCount > 3)
        {
            score += 30;
            factors.Add("multiple_failures");
        }

        return new RiskAnalysisResult { Score = score, Factors = factors };
    }

    private async Task CreateAnomalyDetectionAsync(LoginAttempt loginAttempt, AnomalyAssessmentResult riskAssessment)
    {
        var severity = (int)(riskAssessment.Details.TryGetValue("severity", out var sev) ? sev : 1);
        
        var anomaly = new AnomalyDetection
        {
            Id = Guid.NewGuid(),
            UserId = loginAttempt.UserId ?? Guid.Empty,
            LoginAttemptId = loginAttempt.Id,
            AnomalyType = DetermineAnomalyType(riskAssessment.AnomalyReasons),
            Severity = severity,
            RiskScore = riskAssessment.RiskScore,
            Description = GenerateAnomalyDescription(riskAssessment),
            Details = string.Join(", ", riskAssessment.AnomalyReasons),
            Status = "Pending",
            ResponseAction = riskAssessment.RecommendedAction,
            IsResolved = false,
            DetectedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.AnomalyDetections.AddAsync(anomaly);

        // Create security alert if severity is high enough
        if (anomaly.Severity >= 3)
        {
            await _securityAlertService.CreateAndSendAlertAsync(new SecurityAlertRequest
            {
                UserId = anomaly.UserId,
                AlertType = "LoginAnomaly",
                Severity = GetSeverityString(anomaly.Severity),
                Title = $"Suspicious login activity detected",
                Message = anomaly.Description,
                AnomalyDetectionId = anomaly.Id,
                LoginAttemptId = loginAttempt.Id,
                RequiresAction = anomaly.Severity >= 4
            });
        }
    }

    private async Task UpdateExistingPatternAsync(UserLoginPattern pattern, LoginAttemptData loginData)
    {
        // Update login counts
        if (loginData.IsSuccessful)
            pattern.TotalSuccessfulLogins++;
        else
            pattern.TotalFailedLogins++;

        // Update last login time
        pattern.LastLoginAt = DateTime.UtcNow;
        pattern.LastUpdatedAt = DateTime.UtcNow;

        // Update typical patterns (simplified - in production, use more sophisticated learning)
        await UpdateTypicalPatterns(pattern, loginData);

        _unitOfWork.UserLoginPatterns.Update(pattern);
    }

    private async Task UpdateTypicalPatterns(UserLoginPattern pattern, LoginAttemptData loginData)
    {
        // Update IP addresses
        var ips = ParseJsonArray(pattern.TypicalIpAddresses).ToList();
        if (!ips.Contains(loginData.IpAddress))
        {
            ips.Add(loginData.IpAddress);
            if (ips.Count > 10) ips.RemoveAt(0); // Keep only last 10
            pattern.TypicalIpAddresses = $"[{string.Join(",", ips.Select(ip => $"\"{ip}\""))}]";
        }

        // Update locations
        if (!string.IsNullOrEmpty(loginData.Country))
        {
            var locations = ParseJsonArray(pattern.TypicalLocations).ToList();
            var newLocation = $"{loginData.Country},{loginData.Region},{loginData.City}";
            if (!locations.Contains(newLocation))
            {
                locations.Add(newLocation);
                if (locations.Count > 5) locations.RemoveAt(0); // Keep only last 5
                pattern.TypicalLocations = $"[{string.Join(",", locations.Select(loc => $"\"{loc}\""))}]";
            }
        }

        // Update devices
        if (!string.IsNullOrEmpty(loginData.DeviceFingerprint))
        {
            var devices = ParseJsonArray(pattern.TypicalDevices).ToList();
            if (!devices.Contains(loginData.DeviceFingerprint))
            {
                devices.Add(loginData.DeviceFingerprint);
                if (devices.Count > 5) devices.RemoveAt(0); // Keep only last 5
                pattern.TypicalDevices = $"[{string.Join(",", devices.Select(dev => $"\"{dev}\""))}]";
            }
        }

        // Update typical hours
        var hours = ParseJsonArray(pattern.TypicalLoginHours)
            .Where(h => int.TryParse(h, out _))
            .Select(int.Parse)
            .ToList();
        var currentHour = DateTime.UtcNow.Hour;
        if (!hours.Contains(currentHour))
        {
            hours.Add(currentHour);
            if (hours.Count > 8) hours.RemoveAt(0); // Keep only last 8 typical hours
            pattern.TypicalLoginHours = $"[{string.Join(",", hours)}]";
        }

        await Task.CompletedTask; // For async consistency
    }

    private static List<string> ParseJsonArray(string jsonArray)
    {
        if (string.IsNullOrEmpty(jsonArray) || jsonArray == "[]")
            return new List<string>();

        try
        {
            // Simple JSON array parsing (in production, use System.Text.Json)
            return jsonArray.Trim('[', ']')
                .Split(',')
                .Select(s => s.Trim().Trim('"'))
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static int CalculateSeverity(int riskScore)
    {
        return riskScore switch
        {
            >= 90 => 5, // Critical
            >= 70 => 4, // High
            >= 50 => 3, // Medium
            >= 30 => 2, // Low
            _ => 1      // Minimal
        };
    }

    private static string DetermineResponseAction(int riskScore, List<string> riskFactors)
    {
        if (riskScore >= 90 || riskFactors.Contains("brute_force_pattern"))
            return "Block";
        if (riskScore >= 70)
            return "StepUp";
        if (riskScore >= 50)
            return "Challenge";
        return "Allow";
    }

    private static string DetermineAnomalyType(List<string> riskFactors)
    {
        if (riskFactors.Contains("new_country") || riskFactors.Contains("unusual_location"))
            return "Location";
        if (riskFactors.Contains("unusual_time") || riskFactors.Contains("unusual_time_late_night"))
            return "Time";
        if (riskFactors.Contains("new_device"))
            return "Device";
        if (riskFactors.Contains("high_velocity") || riskFactors.Contains("brute_force_pattern"))
            return "Velocity";
        return "General";
    }

    private static string GenerateAnomalyDescription(AnomalyAssessmentResult assessment)
    {
        var descriptions = new List<string>();

        foreach (var factor in assessment.AnomalyReasons)
        {
            descriptions.Add(factor switch
            {
                "new_country" => "Login from a new country",
                "unusual_location" => "Login from an unusual location",
                "unusual_time" => "Login at an unusual time",
                "unusual_time_late_night" => "Login during late night hours",
                "new_device" => "Login from a new device",
                "high_velocity" => "Multiple rapid login attempts",
                "brute_force_pattern" => "Potential brute force attack pattern",
                "multiple_failures" => "Multiple failed login attempts",
                _ => factor.Replace("_", " ")
            });
        }

        return $"Anomalous login detected: {string.Join(", ", descriptions)}";
    }

    private static List<string> GenerateRecommendations(int riskScore, List<string> riskFactors)
    {
        var recommendations = new List<string>();

        if (riskFactors.Contains("new_country"))
            recommendations.Add("Consider requiring additional verification for international logins");

        if (riskFactors.Contains("new_device"))
            recommendations.Add("Implement device registration and trusted device management");

        if (riskFactors.Contains("brute_force_pattern"))
            recommendations.Add("Consider implementing progressive delay or account lockout");

        if (riskScore >= 70)
            recommendations.Add("Enable additional monitoring for this user account");

        if (recommendations.Count == 0)
            recommendations.Add("Monitor user activity closely");

        return recommendations;
    }

    private static string GetSeverityString(int severity)
    {
        return severity switch
        {
            5 => "Critical",
            4 => "High",
            3 => "Medium",
            2 => "Low",
            _ => "Minimal"
        };
    }

    private static AnomalyDetectionDto MapToDto(AnomalyDetection anomaly)
    {
        return new AnomalyDetectionDto
        {
            Id = anomaly.Id,
            UserId = anomaly.UserId,
            UserEmail = anomaly.User?.Email ?? "",
            LoginAttemptId = anomaly.LoginAttemptId,
            AnomalyType = anomaly.AnomalyType,
            Severity = anomaly.Severity,
            RiskScore = anomaly.RiskScore,
            Description = anomaly.Description,
            Details = new Dictionary<string, object> { ["raw"] = anomaly.Details },
            Status = anomaly.Status,
            ResponseAction = anomaly.ResponseAction,
            IsResolved = anomaly.IsResolved,
            ResolutionNotes = anomaly.ResolutionNotes,
            ResolvedByUserId = anomaly.ResolvedByUserId,
            ResolvedByUserName = anomaly.ResolvedByUser?.Email ?? anomaly.ResolvedByUser?.FullName,
            ResolvedAt = anomaly.ResolvedAt,
            DetectedAt = anomaly.DetectedAt,
            LoginAttempt = anomaly.LoginAttempt != null ? new LoginAttemptDto
            {
                Id = anomaly.LoginAttempt.Id,
                UserId = anomaly.LoginAttempt.UserId,
                Email = anomaly.LoginAttempt.Email,
                IpAddress = anomaly.LoginAttempt.IpAddress,
                UserAgent = anomaly.LoginAttempt.UserAgent,
                Country = anomaly.LoginAttempt.Country,
                Region = anomaly.LoginAttempt.Region,
                City = anomaly.LoginAttempt.City,
                DeviceFingerprint = anomaly.LoginAttempt.DeviceFingerprint,
                AttemptedAt = anomaly.LoginAttempt.AttemptedAt,
                IsSuccessful = anomaly.LoginAttempt.IsSuccessful,
                FailureReason = anomaly.LoginAttempt.FailureReason
            } : null
        };
    }

    #endregion
}

/// <summary>
/// Internal class for risk analysis results
/// </summary>
internal class RiskAnalysisResult
{
    public int Score { get; set; }
    public List<string> Factors { get; set; } = new();
}

namespace ContosoBank.Application.DTOs;

/// <summary>
/// DTO for session information
/// </summary>
public class SessionDto
{
    public Guid Id { get; set; }
    public string DeviceInfo { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public bool IsTrustedDevice { get; set; }
    public bool IsCurrentSession { get; set; }
}

/// <summary>
/// DTO for logout all devices request
/// </summary>
public class LogoutAllDevicesRequestDto
{
    public bool ConfirmLogoutAll { get; set; } = true;
}

/// <summary>
/// DTO for logout all devices response
/// </summary>
public class LogoutAllDevicesResponseDto
{
    public bool Success { get; set; }
    public int SessionsRevoked { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// DTO for session activity update
/// </summary>
public class SessionActivityDto
{
    public string SessionToken { get; set; } = string.Empty;
    public DateTime LastActivity { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO for session validation response
/// </summary>
public class SessionValidationDto
{
    public bool IsValid { get; set; }
    public bool IsExpired { get; set; }
    public bool IsInactive { get; set; }
    public int MinutesUntilTimeout { get; set; }
    public string Message { get; set; } = string.Empty;
}

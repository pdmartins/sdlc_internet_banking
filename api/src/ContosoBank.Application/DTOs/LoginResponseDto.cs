namespace ContosoBank.Application.DTOs;

/// <summary>
/// DTO for successful login response
/// </summary>
public class LoginResponseDto
{
    /// <summary>
    /// User identifier
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// User's full name
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// User's email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Authentication token (for future use)
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration time
    /// </summary>
    public DateTime TokenExpiresAt { get; set; }

    /// <summary>
    /// Whether MFA is required for this login
    /// </summary>
    public bool RequiresMfa { get; set; }

    /// <summary>
    /// MFA method configured for this user
    /// </summary>
    public string MfaMethod { get; set; } = string.Empty;

    /// <summary>
    /// Account information
    /// </summary>
    public AccountResponseDto? Account { get; set; }

    /// <summary>
    /// Success message
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

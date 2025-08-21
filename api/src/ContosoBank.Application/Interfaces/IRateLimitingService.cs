namespace ContosoBank.Application.Interfaces;

public interface IRateLimitingService
{
    /// <summary>
    /// Checks if registration attempts are within allowed limits
    /// </summary>
    Task<bool> CanAttemptRegistrationAsync(string clientIdentifier);
    
    /// <summary>
    /// Records a registration attempt
    /// </summary>
    Task RecordRegistrationAttemptAsync(string clientIdentifier, bool isSuccessful);
    
    /// <summary>
    /// Checks if login attempts are within allowed limits
    /// </summary>
    Task<bool> CanAttemptLoginAsync(string clientIdentifier);
    
    /// <summary>
    /// Records a login attempt
    /// </summary>
    Task RecordLoginAttemptAsync(string clientIdentifier, bool isSuccessful);
    
    /// <summary>
    /// Gets remaining attempts for a client
    /// </summary>
    Task<int> GetRemainingAttemptsAsync(string clientIdentifier, string attemptType);
    
    /// <summary>
    /// Gets time until rate limit reset
    /// </summary>
    Task<TimeSpan?> GetTimeUntilResetAsync(string clientIdentifier, string attemptType);
    
    /// <summary>
    /// Manually resets rate limit for a client (admin function)
    /// </summary>
    Task ResetRateLimitAsync(string clientIdentifier, string attemptType);
}

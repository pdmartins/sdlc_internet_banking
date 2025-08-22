using ContosoBank.Application.Interfaces;
using ContosoBank.Domain.Entities;
using ContosoBank.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ContosoBank.Application.Services;

public class RateLimitingService : IRateLimitingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RateLimitingService> _logger;
    private readonly IConfiguration _configuration;

    // Rate limiting configuration
    private readonly int _maxRegistrationAttempts;
    private readonly int _maxLoginAttempts;
    private readonly TimeSpan _rateLimitWindow;
    private readonly TimeSpan _blockDuration;

    public RateLimitingService(
        IUnitOfWork unitOfWork, 
        ILogger<RateLimitingService> logger,
        IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _configuration = configuration;

        // Load configuration or use defaults
        _maxRegistrationAttempts = _configuration.GetValue<int>("RateLimit:MaxRegistrationAttempts", 5);
        _maxLoginAttempts = _configuration.GetValue<int>("RateLimit:MaxLoginAttempts", 5);
        _rateLimitWindow = TimeSpan.FromMinutes(_configuration.GetValue<int>("RateLimit:WindowMinutes", 15));
        _blockDuration = TimeSpan.FromMinutes(_configuration.GetValue<int>("RateLimit:BlockDurationMinutes", 30));
    }

    public async Task<bool> CanAttemptRegistrationAsync(string clientIdentifier)
    {
        return await CanAttemptAsync(clientIdentifier, "REGISTRATION", _maxRegistrationAttempts);
    }

    public async Task RecordRegistrationAttemptAsync(string clientIdentifier, bool isSuccessful)
    {
        await RecordAttemptAsync(clientIdentifier, "REGISTRATION", isSuccessful);
    }

    public async Task<bool> CanAttemptLoginAsync(string clientIdentifier)
    {
        return await CanAttemptAsync(clientIdentifier, "LOGIN", _maxLoginAttempts);
    }

    public async Task RecordLoginAttemptAsync(string clientIdentifier, bool isSuccessful)
    {
        await RecordAttemptAsync(clientIdentifier, "LOGIN", isSuccessful);
    }

    public async Task<int> GetRemainingAttemptsAsync(string clientIdentifier, string attemptType)
    {
        try
        {
            var entry = await _unitOfWork.RateLimits.GetByClientAndTypeAsync(clientIdentifier, attemptType);
            
            if (entry == null || IsWindowExpired(entry))
                return GetMaxAttempts(attemptType);

            if (entry.IsBlocked && entry.BlockedUntil > DateTime.UtcNow)
                return 0;

            var maxAttempts = GetMaxAttempts(attemptType);
            return Math.Max(0, maxAttempts - entry.AttemptCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting remaining attempts for {ClientId}, type: {Type}", 
                clientIdentifier, attemptType);
            return 0;
        }
    }

    public async Task<TimeSpan?> GetTimeUntilResetAsync(string clientIdentifier, string attemptType)
    {
        try
        {
            var entry = await _unitOfWork.RateLimits.GetByClientAndTypeAsync(clientIdentifier, attemptType);
            
            if (entry == null)
                return null;

            if (entry.IsBlocked && entry.BlockedUntil.HasValue)
            {
                var timeUntilUnblock = entry.BlockedUntil.Value - DateTime.UtcNow;
                return timeUntilUnblock > TimeSpan.Zero ? timeUntilUnblock : TimeSpan.Zero;
            }

            if (!IsWindowExpired(entry))
            {
                var timeUntilWindowReset = entry.FirstAttempt.Add(_rateLimitWindow) - DateTime.UtcNow;
                return timeUntilWindowReset > TimeSpan.Zero ? timeUntilWindowReset : TimeSpan.Zero;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting time until reset for {ClientId}, type: {Type}", 
                clientIdentifier, attemptType);
            return null;
        }
    }

    public async Task ResetRateLimitAsync(string clientIdentifier, string attemptType)
    {
        try
        {
            _logger.LogInformation("Manually resetting rate limit for {ClientId}, type: {Type}", 
                clientIdentifier, attemptType);

            var entry = await _unitOfWork.RateLimits.GetByClientAndTypeAsync(clientIdentifier, attemptType);
            
            if (entry != null)
            {
                _unitOfWork.RateLimits.Remove(entry);
                await _unitOfWork.SaveChangesAsync();
            }

            _logger.LogInformation("Rate limit reset successfully for {ClientId}", clientIdentifier);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting rate limit for {ClientId}", clientIdentifier);
            throw;
        }
    }

    public async Task<bool> CanAttemptAsync(string clientIdentifier, string attemptType, int maxAttempts)
    {
        try
        {
            var entry = await _unitOfWork.RateLimits.GetByClientAndTypeAsync(clientIdentifier, attemptType);
            
            // No previous attempts
            if (entry == null)
                return true;

            // Check if currently blocked
            if (entry.IsBlocked && entry.BlockedUntil > DateTime.UtcNow)
            {
                _logger.LogWarning("Client {ClientId} is blocked for {Type} until {BlockedUntil}", 
                    clientIdentifier, attemptType, entry.BlockedUntil);
                return false;
            }

            // Check if rate limit window has expired
            if (IsWindowExpired(entry))
            {
                // Reset the entry for a new window
                ResetEntryForNewWindow(entry);
                return true;
            }

            // Check if within rate limit
            if (entry.AttemptCount >= maxAttempts)
            {
                _logger.LogWarning("Client {ClientId} exceeded rate limit for {Type}: {Count}/{Max}", 
                    clientIdentifier, attemptType, entry.AttemptCount, maxAttempts);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking rate limit for {ClientId}, type: {Type}", 
                clientIdentifier, attemptType);
            return false; // Fail safe - deny on error
        }
    }

    public async Task RecordAttemptAsync(string clientIdentifier, string attemptType, bool isSuccessful)
    {
        const int maxRetries = 3;
        var delayMs = 100;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                await RecordAttemptInternalAsync(clientIdentifier, attemptType, isSuccessful);
                return; // Success, exit retry loop
            }
            catch (Exception ex) when (attempt < maxRetries - 1 && IsConcurrencyException(ex))
            {
                _logger.LogWarning("Concurrency exception on attempt {Attempt} for {ClientId}/{Type}: {Error}", 
                    attempt + 1, clientIdentifier, attemptType, ex.Message);
                
                // Wait with exponential backoff before retrying
                await Task.Delay(delayMs);
                delayMs *= 2;
            }
        }
        
        // If we get here, all retries failed
        _logger.LogError("Failed to record attempt for {ClientId}/{Type} after {MaxRetries} retries", 
            clientIdentifier, attemptType, maxRetries);
    }

    private bool IsConcurrencyException(Exception ex)
    {
        return ex.GetType().Name.Contains("DbUpdateConcurrencyException") ||
               ex.Message.Contains("concurrency") ||
               ex.Message.Contains("The database operation was expected to affect 1 row(s), but actually affected 0 row(s)") ||
               ex.Message.Contains("unique index") ||
               ex.Message.Contains("duplicate key");
    }

    private async Task RecordAttemptInternalAsync(string clientIdentifier, string attemptType, bool isSuccessful)
    {
        var now = DateTime.UtcNow;
        var maxAttempts = GetMaxAttempts(attemptType);

        // Try to get existing entry first
        var entry = await _unitOfWork.RateLimits.GetByClientAndTypeAsync(clientIdentifier, attemptType);

        if (entry == null)
        {
            // Create new entry
            entry = new RateLimitEntry
            {
                Id = Guid.NewGuid(),
                ClientIdentifier = clientIdentifier,
                AttemptType = attemptType,
                AttemptCount = 1,
                SuccessfulCount = isSuccessful ? 1 : 0,
                FailedCount = isSuccessful ? 0 : 1,
                FirstAttempt = now,
                LastAttempt = now,
                IsBlocked = false,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _unitOfWork.RateLimits.AddAsync(entry);
        }
        else
        {
            // Update existing entry
            if (IsWindowExpired(entry))
            {
                // Reset for new window
                entry.AttemptCount = 1;
                entry.SuccessfulCount = isSuccessful ? 1 : 0;
                entry.FailedCount = isSuccessful ? 0 : 1;
                entry.FirstAttempt = now;
                entry.IsBlocked = false;
                entry.BlockedUntil = null;
                entry.BlockReason = null;
            }
            else
            {
                // Increment counters
                entry.AttemptCount++;
                if (isSuccessful)
                    entry.SuccessfulCount++;
                else
                    entry.FailedCount++;
            }

            entry.LastAttempt = now;
            entry.UpdatedAt = now;

            // Check if should be blocked
            if (!isSuccessful && entry.FailedCount >= maxAttempts)
            {
                entry.IsBlocked = true;
                entry.BlockedUntil = now.Add(_blockDuration);
                entry.BlockReason = $"Exceeded {maxAttempts} failed {attemptType.ToLower()} attempts";
                
                _logger.LogWarning("Client {ClientId} blocked for {Type} until {BlockedUntil} - {Reason}", 
                    clientIdentifier, attemptType, entry.BlockedUntil, entry.BlockReason);
            }

            _unitOfWork.RateLimits.Update(entry);
        }

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Recorded {Type} attempt for {ClientId}: Success={Success}, Total={Total}", 
            attemptType, clientIdentifier, isSuccessful, entry.AttemptCount);
    }

    private bool IsWindowExpired(RateLimitEntry entry)
    {
        return DateTime.UtcNow > entry.FirstAttempt.Add(_rateLimitWindow);
    }

    private void ResetEntryForNewWindow(RateLimitEntry entry)
    {
        var now = DateTime.UtcNow;
        
        entry.AttemptCount = 0;
        entry.SuccessfulCount = 0;
        entry.FailedCount = 0;
        entry.FirstAttempt = now;
        entry.LastAttempt = now;
        entry.IsBlocked = false;
        entry.BlockedUntil = null;
        entry.BlockReason = null;
        entry.UpdatedAt = now;

        _unitOfWork.RateLimits.Update(entry);
    }

    private int GetMaxAttempts(string attemptType)
    {
        return attemptType.ToUpper() switch
        {
            "REGISTRATION" => _maxRegistrationAttempts,
            "LOGIN" => _maxLoginAttempts,
            _ => 5 // Default
        };
    }
}

namespace ContosoBank.Domain.Interfaces;

public interface IUnitOfWork
{
    IUserRepository Users { get; }
    IAccountRepository Accounts { get; }
    ISecurityEventRepository SecurityEvents { get; }
    IGdprConsentRepository GdprConsents { get; }
    IDataProcessingLogRepository DataProcessingLogs { get; }
    IRateLimitRepository RateLimits { get; }
    IMfaSessionRepository MfaSessions { get; }
    IPasswordResetRepository PasswordResets { get; }
    IUserSessionRepository UserSessions { get; }
    ILoginAttemptRepository LoginAttempts { get; }
    IUserLoginPatternRepository UserLoginPatterns { get; }
    IAnomalyDetectionRepository AnomalyDetections { get; }
    ISecurityAlertRepository SecurityAlerts { get; }
    ITransactionRepository Transactions { get; }
    
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}

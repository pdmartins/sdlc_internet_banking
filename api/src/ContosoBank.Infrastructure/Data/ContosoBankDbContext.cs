using ContosoBank.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ContosoBank.Infrastructure.Data;

public class ContosoBankDbContext : DbContext
{
    public ContosoBankDbContext(DbContextOptions<ContosoBankDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<SecurityEvent> SecurityEvents { get; set; }
    public DbSet<RateLimitEntry> RateLimitEntries { get; set; }
    public DbSet<MfaSession> MfaSessions { get; set; }
    public DbSet<PasswordReset> PasswordResets { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }
    public DbSet<LoginAttempt> LoginAttempts { get; set; }
    public DbSet<UserLoginPattern> UserLoginPatterns { get; set; }
    public DbSet<AnomalyDetection> AnomalyDetections { get; set; }
    public DbSet<SecurityAlert> SecurityAlerts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Phone).IsRequired().HasMaxLength(100); // Increased to accommodate encrypted data
            entity.Property(e => e.CPF).IsRequired().HasMaxLength(14);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
            entity.Property(e => e.SecurityQuestion).IsRequired().HasMaxLength(255);
            entity.Property(e => e.SecurityAnswerHash).IsRequired().HasMaxLength(500);
            entity.Property(e => e.MfaOption).IsRequired().HasMaxLength(20);
            entity.Property(e => e.CreatedAt).IsRequired();

            // Unique constraints
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.CPF).IsUnique();
            entity.HasIndex(e => e.Phone).IsUnique();

            // Relationships
            entity.HasOne(e => e.Account)
                  .WithOne(a => a.User)
                  .HasForeignKey<Account>(a => a.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.SecurityEvents)
                  .WithOne(se => se.User)
                  .HasForeignKey(se => se.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Account configuration
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AccountNumber).IsRequired().HasMaxLength(20);
            entity.Property(e => e.BranchCode).IsRequired().HasMaxLength(10);
            entity.Property(e => e.AccountType).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Balance).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.DailyLimit).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.MonthlyLimit).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.CreatedAt).IsRequired();

            // Unique constraints
            entity.HasIndex(e => e.AccountNumber).IsUnique();

            // Relationships
            entity.HasMany(e => e.Transactions)
                  .WithOne(t => t.Account)
                  .HasForeignKey(t => t.AccountId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Transaction configuration
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Amount).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.BalanceAfter).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Reference).HasMaxLength(100);
            entity.Property(e => e.RecipientAccount).HasMaxLength(255);
            entity.Property(e => e.RecipientName).HasMaxLength(100);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Fee).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CreatedAt).IsRequired();

            // Indexes
            entity.HasIndex(e => e.AccountId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Status);
        });

        // SecurityEvent configuration
        modelBuilder.Entity<SecurityEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Severity).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.Location).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).IsRequired();

            // Indexes
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.EventType);
        });

        // RateLimitEntry configuration
        modelBuilder.Entity<RateLimitEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ClientIdentifier).IsRequired().HasMaxLength(100);
            entity.Property(e => e.AttemptType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.AttemptCount).IsRequired();
            entity.Property(e => e.SuccessfulCount).IsRequired();
            entity.Property(e => e.FailedCount).IsRequired();
            entity.Property(e => e.FirstAttempt).IsRequired();
            entity.Property(e => e.LastAttempt).IsRequired();
            entity.Property(e => e.BlockedUntil).IsRequired(false);
            entity.Property(e => e.IsBlocked).IsRequired();
            entity.Property(e => e.BlockReason).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            // Indexes for efficient querying
            entity.HasIndex(e => new { e.ClientIdentifier, e.AttemptType }).IsUnique();
            entity.HasIndex(e => e.LastAttempt);
            entity.HasIndex(e => e.BlockedUntil);
            entity.HasIndex(e => e.IsBlocked);
        });

        // MfaSession configuration
        modelBuilder.Entity<MfaSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.CodeHash).IsRequired().HasMaxLength(6);
            entity.Property(e => e.Method).IsRequired().HasMaxLength(20);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.ExpiresAt).IsRequired();
            entity.Property(e => e.IsUsed).IsRequired();
            entity.Property(e => e.UsedAt).IsRequired(false);
            entity.Property(e => e.AttemptCount).IsRequired();
            entity.Property(e => e.MaxAttempts).IsRequired();
            entity.Property(e => e.IsBlocked).IsRequired();
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);

            // Indexes for efficient querying
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => e.IsUsed);
            
            // Relationship with User
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // PasswordReset configuration
        modelBuilder.Entity<PasswordReset>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(255);
            entity.Property(e => e.TokenHash).IsRequired().HasMaxLength(255);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.ExpiresAt).IsRequired();
            entity.Property(e => e.IsUsed).IsRequired();
            entity.Property(e => e.UsedAt).IsRequired(false);
            entity.Property(e => e.AttemptCount).IsRequired();
            entity.Property(e => e.MaxAttempts).IsRequired();
            entity.Property(e => e.IsBlocked).IsRequired();
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);

            // Indexes for efficient querying
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => e.IsUsed);
            
            // Relationship with User
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // UserSession configuration
        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SessionToken).IsRequired().HasMaxLength(512);
            entity.Property(e => e.IpAddress).IsRequired().HasMaxLength(45);
            entity.Property(e => e.UserAgent).IsRequired().HasMaxLength(500);
            entity.Property(e => e.DeviceFingerprint).HasMaxLength(100);
            entity.Property(e => e.Location).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.ExpiresAt).IsRequired();
            entity.Property(e => e.LastActivityAt).IsRequired(false);
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.IsRevoked).IsRequired();
            entity.Property(e => e.RevokedAt).IsRequired(false);
            entity.Property(e => e.RevokedReason).HasMaxLength(100);
            entity.Property(e => e.IsTrustedDevice).IsRequired();
            entity.Property(e => e.InactivityTimeoutMinutes).IsRequired();

            // Indexes for efficient querying
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.SessionToken).IsUnique();
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.IsRevoked);
            entity.HasIndex(e => e.LastActivityAt);
            entity.HasIndex(e => e.DeviceFingerprint);
            
            // Relationship with User
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // LoginAttempt configuration
        modelBuilder.Entity<LoginAttempt>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.IpAddress).IsRequired().HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.Region).HasMaxLength(100);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.DeviceFingerprint).HasMaxLength(100);
            entity.Property(e => e.DeviceType).HasMaxLength(50);
            entity.Property(e => e.OperatingSystem).HasMaxLength(100);
            entity.Property(e => e.Browser).HasMaxLength(100);
            entity.Property(e => e.AttemptedAt).IsRequired();
            entity.Property(e => e.IsSuccessful).IsRequired();
            entity.Property(e => e.FailureReason).HasMaxLength(255);
            entity.Property(e => e.IsAnomalous).IsRequired();
            entity.Property(e => e.AnomalyReasons).HasMaxLength(500);
            entity.Property(e => e.RiskScore).IsRequired();
            entity.Property(e => e.ResponseAction).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).IsRequired();

            // Indexes for efficient querying
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.IpAddress);
            entity.HasIndex(e => e.AttemptedAt);
            entity.HasIndex(e => e.IsSuccessful);
            entity.HasIndex(e => e.DeviceFingerprint);
            entity.HasIndex(e => e.IsAnomalous);

            // Relationship with User
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // UserLoginPattern configuration
        modelBuilder.Entity<UserLoginPattern>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TypicalIpAddresses).HasMaxLength(500);
            entity.Property(e => e.TypicalLocations).HasMaxLength(500);
            entity.Property(e => e.TypicalDevices).HasMaxLength(500);
            entity.Property(e => e.TypicalLoginHours).HasMaxLength(200);
            entity.Property(e => e.TypicalDaysOfWeek).HasMaxLength(50);
            entity.Property(e => e.PreferredTimeZone).HasMaxLength(100);
            entity.Property(e => e.FirstLoginAt).IsRequired();
            entity.Property(e => e.LastLoginAt).IsRequired();
            entity.Property(e => e.LastUpdatedAt).IsRequired();
            entity.Property(e => e.TotalSuccessfulLogins).IsRequired();
            entity.Property(e => e.TotalFailedLogins).IsRequired();
            entity.Property(e => e.LocationRiskThreshold).IsRequired();
            entity.Property(e => e.TimeRiskThreshold).IsRequired();
            entity.Property(e => e.DeviceRiskThreshold).IsRequired();

            // Indexes for efficient querying
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasIndex(e => e.LastUpdatedAt);

            // Relationship with User (one-to-one)
            entity.HasOne(e => e.User)
                  .WithOne()
                  .HasForeignKey<UserLoginPattern>(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // AnomalyDetection configuration
        modelBuilder.Entity<AnomalyDetection>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AnomalyType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Severity).IsRequired();
            entity.Property(e => e.RiskScore).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Details).HasMaxLength(2000);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ResponseAction).HasMaxLength(50);
            entity.Property(e => e.IsResolved).IsRequired();
            entity.Property(e => e.ResolutionNotes).HasMaxLength(1000);
            entity.Property(e => e.ResolvedAt).IsRequired(false);
            entity.Property(e => e.DetectedAt).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            // Indexes for efficient querying
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.LoginAttemptId);
            entity.HasIndex(e => e.AnomalyType);
            entity.HasIndex(e => e.DetectedAt);
            entity.HasIndex(e => e.IsResolved);
            entity.HasIndex(e => e.Severity);

            // Relationships
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.LoginAttempt)
                  .WithMany()
                  .HasForeignKey(e => e.LoginAttemptId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ResolvedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.ResolvedByUserId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // SecurityAlert configuration
        modelBuilder.Entity<SecurityAlert>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AlertType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Severity).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Details).HasMaxLength(2000);
            entity.Property(e => e.DeliveryMethod).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.IsRead).IsRequired();
            entity.Property(e => e.RequiresAction).IsRequired();
            entity.Property(e => e.ActionUrl).HasMaxLength(500);
            entity.Property(e => e.ActionText).HasMaxLength(100);
            entity.Property(e => e.ReadAt).IsRequired(false);
            entity.Property(e => e.ActionTakenAt).IsRequired(false);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.SentAt).IsRequired(false);
            entity.Property(e => e.DeliveredAt).IsRequired(false);
            entity.Property(e => e.ExpiresAt).IsRequired();

            // Indexes for efficient querying
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.LoginAttemptId);
            entity.HasIndex(e => e.AnomalyDetectionId);
            entity.HasIndex(e => e.AlertType);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.IsRead);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Severity);

            // Relationships
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.LoginAttempt)
                  .WithMany()
                  .HasForeignKey(e => e.LoginAttemptId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.AnomalyDetection)
                  .WithMany()
                  .HasForeignKey(e => e.AnomalyDetectionId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }
}

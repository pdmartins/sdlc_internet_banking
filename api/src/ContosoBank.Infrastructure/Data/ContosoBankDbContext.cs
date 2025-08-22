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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Phone).IsRequired().HasMaxLength(20);
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
    }
}

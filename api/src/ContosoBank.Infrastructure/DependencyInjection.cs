using ContosoBank.Application.Interfaces;
using ContosoBank.Application.Services;
using ContosoBank.Domain.Interfaces;
using ContosoBank.Infrastructure.Data;
using ContosoBank.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ContosoBank.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Database Context
        services.AddDbContext<ContosoBankDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ContosoBankDbContext).Assembly.FullName)));

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<ISecurityEventRepository, SecurityEventRepository>();
        services.AddScoped<IGdprConsentRepository, GdprConsentRepository>();
        services.AddScoped<IDataProcessingLogRepository, DataProcessingLogRepository>();
        services.AddScoped<IRateLimitRepository, RateLimitRepository>();
        services.AddScoped<IMfaSessionRepository, MfaSessionRepository>();
        services.AddScoped<IPasswordResetRepository, PasswordResetRepository>();

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Application Services
        services.AddScoped<IRegistrationService, RegistrationService>();
        services.AddScoped<IEnhancedRegistrationService, EnhancedRegistrationService>();
        services.AddScoped<IGdprComplianceService, GdprComplianceService>();
        services.AddScoped<IRateLimitingService, RateLimitingService>();
        services.AddScoped<IIdentityVerificationService, IdentityVerificationService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IMfaService, MfaService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();

        return services;
    }
}

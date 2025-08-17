using Microsoft.EntityFrameworkCore;
using InternetBankingAPI.Models;

namespace InternetBankingAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<PersonalInfo> PersonalInfos { get; set; }
        public DbSet<IdentityVerification> IdentityVerifications { get; set; }
    }
}

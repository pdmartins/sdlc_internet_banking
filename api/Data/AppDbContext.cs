using Microsoft.EntityFrameworkCore;

namespace InternetBankingAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<PersonalInfo> PersonalInfos { get; set; }
    }
}

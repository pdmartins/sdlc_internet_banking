using System.Threading.Tasks;
using InternetBankingAPI.Models;
using Microsoft.EntityFrameworkCore;
using InternetBankingAPI.Data;

namespace InternetBankingAPI.Services
{
    public class IdentityVerificationService
    {
        private readonly AppDbContext _context;

        public IdentityVerificationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IdentityVerification> CreateVerificationAsync(IdentityVerification verification)
        {
            _context.IdentityVerifications.Add(verification);
            await _context.SaveChangesAsync();
            return verification;
        }

        public async Task<IdentityVerification> GetVerificationByIdAsync(int id)
        {
            return await _context.IdentityVerifications.FindAsync(id);
        }

        public async Task UpdateVerificationStatusAsync(int id, string status)
        {
            var verification = await _context.IdentityVerifications.FindAsync(id);
            if (verification != null)
            {
                verification.Status = status;
                await _context.SaveChangesAsync();
            }
        }
    }
}

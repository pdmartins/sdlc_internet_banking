using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace InternetBankingAPI.Services
{
    public interface IBiometricVerificationService
    {
        Task<(bool IsSuccess, string Status, string ErrorMessage)> MatchBiometricAsync(IFormFile biometricData, string userId);
        Task<(bool IsSuccess, string Status, string ErrorMessage)> RetryBiometricAsync(string userId);
    }

    public class BiometricVerificationService : IBiometricVerificationService
    {
        private readonly ILogger<BiometricVerificationService> _logger;

        public BiometricVerificationService(ILogger<BiometricVerificationService> logger)
        {
            _logger = logger;
        }

        public async Task<(bool IsSuccess, string Status, string ErrorMessage)> MatchBiometricAsync(IFormFile biometricData, string userId)
        {
            try
            {
                // Simulate saving the biometric data to a storage location
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/biometric-uploads");
                Directory.CreateDirectory(uploadsFolder);

                var filePath = Path.Combine(uploadsFolder, biometricData.FileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await biometricData.CopyToAsync(stream);
                }

                // Simulate biometric matching logic
                var isMatched = true; // Replace with actual matching logic

                if (isMatched)
                {
                    _logger.LogInformation("Biometric match successful for user {UserId}", userId);
                    return (true, "Matched", null);
                }

                _logger.LogWarning("Biometric match failed for user {UserId}", userId);
                return (false, "Not Matched", "Biometric match failed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error matching biometric data for user {UserId}", userId);
                return (false, null, ex.Message);
            }
        }

        public async Task<(bool IsSuccess, string Status, string ErrorMessage)> RetryBiometricAsync(string userId)
        {
            try
            {
                // Simulate retry logic
                await Task.Delay(100); // Simulate async operation
                _logger.LogInformation("Retry initiated for user {UserId}", userId);
                return (true, "Retry Initiated", null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating retry for user {UserId}", userId);
                return (false, null, ex.Message);
            }
        }
    }
}

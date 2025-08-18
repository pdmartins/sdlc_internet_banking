using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using InternetBankingAPI.Services;

namespace InternetBankingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BiometricVerificationController : ControllerBase
    {
        private readonly IBiometricVerificationService _biometricService;
        private readonly ILogger<BiometricVerificationController> _logger;

        public BiometricVerificationController(IBiometricVerificationService biometricService, ILogger<BiometricVerificationController> logger)
        {
            _biometricService = biometricService;
            _logger = logger;
        }

        [HttpPost("match")]
        public async Task<IActionResult> MatchBiometric([FromForm] IFormFile biometricData, [FromForm] string userId)
        {
            _logger.LogInformation("Received request for biometric match with userId: {UserId}", userId);
            _logger.LogInformation("Biometric file name: {FileName}", biometricData?.FileName);

            if (biometricData == null || string.IsNullOrEmpty(userId))
            {
                return BadRequest("Biometric data and user ID are required.");
            }

            var result = await _biometricService.MatchBiometricAsync(biometricData, userId);

            if (result.IsSuccess)
            {
                return Ok(new { message = "Biometric match successful.", status = result.Status });
            }

            return BadRequest(new { message = result.ErrorMessage });
        }

        [HttpPost("retry")]
        public async Task<IActionResult> RetryBiometric([FromBody] string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("User ID is required for retry.");
            }

            var retryResult = await _biometricService.RetryBiometricAsync(userId);

            if (retryResult.IsSuccess)
            {
                return Ok(new { message = "Retry initiated successfully.", status = retryResult.Status });
            }

            return BadRequest(new { message = retryResult.ErrorMessage });
        }
    }
}

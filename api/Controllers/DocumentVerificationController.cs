using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace InternetBankingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentVerificationController : ControllerBase
    {
        private readonly IDocumentVerificationService _verificationService;

        public DocumentVerificationController(IDocumentVerificationService verificationService)
        {
            _verificationService = verificationService;
        }

        [HttpPost("verify")]
        public async Task<IActionResult> VerifyDocument([FromForm] IFormFile document, [FromForm] string userId)
        {
            if (document == null || string.IsNullOrEmpty(userId))
            {
                return BadRequest("Document and user ID are required.");
            }

            var result = await _verificationService.VerifyDocumentAsync(document, userId);

            if (result.IsSuccess)
            {
                return Ok(new { message = "Document verification successful.", status = result.Status });
            }

            return BadRequest(new { message = result.ErrorMessage });
        }

        [HttpGet("status/{userId}")]
        public async Task<IActionResult> GetVerificationStatus(string userId)
        {
            var status = await _verificationService.GetVerificationStatusAsync(userId);

            if (status == null)
            {
                return NotFound("Verification status not found.");
            }

            return Ok(new { status });
        }

        [HttpPost("appeal")]
        public async Task<IActionResult> AppealVerification([FromBody] string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("User ID is required for an appeal.");
            }

            // Simulate appeal logic
            var appealResult = await _verificationService.AppealVerificationAsync(userId);

            if (appealResult.IsSuccess)
            {
                return Ok(new { message = "Appeal submitted successfully.", status = appealResult.Status });
            }

            return BadRequest(new { message = appealResult.ErrorMessage });
        }
    }
}

using System;
using System.Threading.Tasks;
using InternetBankingAPI.Models;
using InternetBankingAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace InternetBankingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IdentityVerificationController : ControllerBase
    {
        private readonly IdentityVerificationService _service;

        public IdentityVerificationController(IdentityVerificationService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> UploadVerification([FromForm] IFormFile idFile, [FromForm] IFormFile selfieFile, [FromForm] string userId)
        {
            if (idFile == null || selfieFile == null || string.IsNullOrEmpty(userId))
            {
                return BadRequest("ID file, selfie file, and user ID are required.");
            }

            // Save files to the server
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
            Directory.CreateDirectory(uploadsFolder);

            var idFilePath = Path.Combine(uploadsFolder, idFile.FileName);
            var selfieFilePath = Path.Combine(uploadsFolder, selfieFile.FileName);

            using (var idStream = new FileStream(idFilePath, FileMode.Create))
            {
                await idFile.CopyToAsync(idStream);
            }

            using (var selfieStream = new FileStream(selfieFilePath, FileMode.Create))
            {
                await selfieFile.CopyToAsync(selfieStream);
            }

            // Create IdentityVerification object
            var verification = new IdentityVerification
            {
                UserId = userId,
                DocumentPath = idFilePath,
                SelfiePath = selfieFilePath,
                Status = "Pending",
                UploadedAt = DateTime.UtcNow
            };

            var createdVerification = await _service.CreateVerificationAsync(verification);
            return CreatedAtAction(nameof(GetVerification), new { id = createdVerification.Id }, createdVerification);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetVerification(int id)
        {
            var verification = await _service.GetVerificationByIdAsync(id);
            if (verification == null)
            {
                return NotFound();
            }

            return Ok(verification);
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
        {
            await _service.UpdateVerificationStatusAsync(id, status);
            return NoContent();
        }
    }
}

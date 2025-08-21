using ContosoBank.Application.DTOs;
using ContosoBank.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ContosoBank.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RegistrationController : ControllerBase
{
    private readonly IRegistrationService _registrationService;
    private readonly ILogger<RegistrationController> _logger;

    public RegistrationController(
        IRegistrationService registrationService,
        ILogger<RegistrationController> logger)
    {
        _registrationService = registrationService;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new user in the system
    /// </summary>
    /// <param name="request">User registration data</param>
    /// <returns>User registration response with account information</returns>
    [HttpPost("register")]
    public async Task<ActionResult<UserRegistrationResponseDto>> RegisterUser(
        [FromBody] RegisterUserRequestDto request)
    {
        try
        {
            _logger.LogInformation("Starting user registration for email: {Email}", request.Email);

            var result = await _registrationService.RegisterUserAsync(request);

            _logger.LogInformation("User registration completed successfully for email: {Email}", request.Email);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Registration validation failed: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Registration business rule violation: {Message}", ex.Message);
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during user registration");
            return StatusCode(500, new { message = "An unexpected error occurred during registration" });
        }
    }

    /// <summary>
    /// Sets up security information for a registered user
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="request">Security setup data</param>
    /// <returns>Complete registration response</returns>
    [HttpPost("{userId:guid}/security")]
    public async Task<ActionResult<RegistrationCompleteResponseDto>> SetupSecurity(
        Guid userId,
        [FromBody] SetupSecurityRequestDto request)
    {
        try
        {
            _logger.LogInformation("Starting security setup for user: {UserId}", userId);

            var result = await _registrationService.SetupSecurityAsync(userId, request);

            _logger.LogInformation("Security setup completed successfully for user: {UserId}", userId);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Security setup validation failed for user {UserId}: {Message}", userId, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Security setup business rule violation for user {UserId}: {Message}", userId, ex.Message);
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during security setup for user: {UserId}", userId);
            return StatusCode(500, new { message = "An unexpected error occurred during security setup" });
        }
    }

    /// <summary>
    /// Validates email uniqueness
    /// </summary>
    /// <param name="email">Email to validate</param>
    /// <returns>True if email is available</returns>
    [HttpGet("validate/email")]
    public async Task<ActionResult<bool>> ValidateEmail([FromQuery] string email)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new { message = "Email is required" });
            }

            var isAvailable = await _registrationService.IsEmailAvailableAsync(email);
            return Ok(new { available = isAvailable });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating email: {Email}", email);
            return StatusCode(500, new { message = "An error occurred while validating email" });
        }
    }

    /// <summary>
    /// Validates CPF uniqueness
    /// </summary>
    /// <param name="cpf">CPF to validate</param>
    /// <returns>True if CPF is available</returns>
    [HttpGet("validate/cpf")]
    public async Task<ActionResult<bool>> ValidateCPF([FromQuery] string cpf)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(cpf))
            {
                return BadRequest(new { message = "CPF is required" });
            }

            var isAvailable = await _registrationService.IsCPFAvailableAsync(cpf);
            return Ok(new { available = isAvailable });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating CPF: {CPF}", cpf);
            return StatusCode(500, new { message = "An error occurred while validating CPF" });
        }
    }

    /// <summary>
    /// Validates phone uniqueness
    /// </summary>
    /// <param name="phone">Phone to validate</param>
    /// <returns>True if phone is available</returns>
    [HttpGet("validate/phone")]
    public async Task<ActionResult<bool>> ValidatePhone([FromQuery] string phone)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return BadRequest(new { message = "Phone is required" });
            }

            var isAvailable = await _registrationService.IsPhoneAvailableAsync(phone);
            return Ok(new { available = isAvailable });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating phone: {Phone}", phone);
            return StatusCode(500, new { message = "An error occurred while validating phone" });
        }
    }
}

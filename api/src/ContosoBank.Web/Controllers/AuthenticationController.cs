using ContosoBank.Application.DTOs;
using ContosoBank.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ContosoBank.Web.Controllers;

/// <summary>
/// Authentication controller for user login operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthenticationController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<AuthenticationController> _logger;

    public AuthenticationController(
        IAuthenticationService authenticationService,
        ILogger<AuthenticationController> logger)
    {
        _authenticationService = authenticationService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates a user with email and password
    /// </summary>
    /// <param name="request">Login request data</param>
    /// <returns>Login response with user information and token</returns>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Get client IP address
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            
            _logger.LogInformation("Processing login request for email: {Email}", request.Email);

            var result = await _authenticationService.LoginAsync(request, clientIp);

            _logger.LogInformation("Login successful for user: {UserId} ({Email})", result.UserId, result.Email);

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized login attempt for email {Email}: {Message}", request.Email, ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Invalid login operation for email {Email}: {Message}", request.Email, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Login validation failed for email {Email}: {Message}", request.Email, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login for email: {Email}", request.Email);
            return StatusCode(500, new { message = "Ocorreu um erro inesperado durante o login. Tente novamente." });
        }
    }

    /// <summary>
    /// Validates user credentials without performing full login
    /// </summary>
    /// <param name="email">User's email</param>
    /// <param name="password">User's password</param>
    /// <returns>True if credentials are valid</returns>
    [HttpPost("validate")]
    public async Task<ActionResult<bool>> ValidateCredentials([FromBody] LoginRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var isValid = await _authenticationService.ValidateCredentialsAsync(request.Email, request.Password);
            
            return Ok(new { valid = isValid });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating credentials for email: {Email}", request.Email);
            return StatusCode(500, new { message = "Erro ao validar credenciais" });
        }
    }

    /// <summary>
    /// Checks if an account is locked
    /// </summary>
    /// <param name="email">User's email to check</param>
    /// <returns>True if account is locked</returns>
    [HttpGet("lockstatus")]
    public async Task<ActionResult<bool>> CheckLockStatus([FromQuery] string email)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new { message = "Email é obrigatório" });
            }

            var isLocked = await _authenticationService.IsAccountLockedAsync(email);
            
            return Ok(new { locked = isLocked });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking lock status for email: {Email}", email);
            return StatusCode(500, new { message = "Erro ao verificar status da conta" });
        }
    }
}

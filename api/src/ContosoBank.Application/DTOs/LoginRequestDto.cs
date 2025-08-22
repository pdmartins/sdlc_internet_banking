using System.ComponentModel.DataAnnotations;

namespace ContosoBank.Application.DTOs;

/// <summary>
/// DTO for user login request
/// </summary>
public class LoginRequestDto
{
    /// <summary>
    /// User's email address
    /// </summary>
    [Required(ErrorMessage = "Email é obrigatório")]
    [EmailAddress(ErrorMessage = "Formato de email inválido")]
    [MaxLength(255, ErrorMessage = "Email não pode exceder 255 caracteres")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's password
    /// </summary>
    [Required(ErrorMessage = "Senha é obrigatória")]
    [MinLength(1, ErrorMessage = "Senha não pode estar vazia")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Whether to remember this device for future logins
    /// </summary>
    public bool RememberDevice { get; set; } = false;
}

namespace ContosoBank.Application.DTOs;

public class RegistrationCompleteResponseDto
{
    public UserRegistrationResponseDto User { get; set; } = null!;
    public AccountResponseDto Account { get; set; } = null!;
    public string SecurityConfigured { get; set; } = string.Empty;
    public string MfaOption { get; set; } = string.Empty;
    public int PasswordStrength { get; set; }
    public DateTime CompletedAt { get; set; }
}

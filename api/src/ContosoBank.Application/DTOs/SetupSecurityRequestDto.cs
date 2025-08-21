using System.ComponentModel.DataAnnotations;

namespace ContosoBank.Application.DTOs;

public class SetupSecurityRequestDto
{
    [Required(ErrorMessage = "ID do usuário é obrigatório")]
    public Guid UserId { get; set; }
    
    [Required(ErrorMessage = "Senha é obrigatória")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Senha deve ter entre 8 e 100 caracteres")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*(),.?"":{}<>]).{8,}$",
        ErrorMessage = "Senha deve conter pelo menos: 1 maiúscula, 1 minúscula, 1 número e 1 caractere especial")]
    public string Password { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Pergunta de segurança é obrigatória")]
    [StringLength(255, ErrorMessage = "Pergunta de segurança não pode exceder 255 caracteres")]
    public string SecurityQuestion { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Resposta de segurança é obrigatória")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Resposta deve ter entre 2 e 100 caracteres")]
    public string SecurityAnswer { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Opção de MFA é obrigatória")]
    [RegularExpression(@"^(sms|authenticator)$", ErrorMessage = "Opção de MFA deve ser 'sms' ou 'authenticator'")]
    public string MfaOption { get; set; } = string.Empty;
    
    [Range(1, 5, ErrorMessage = "Força da senha deve estar entre 1 e 5")]
    public int PasswordStrength { get; set; }
}

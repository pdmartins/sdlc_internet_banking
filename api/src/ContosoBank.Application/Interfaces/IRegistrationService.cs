using ContosoBank.Application.DTOs;

namespace ContosoBank.Application.Interfaces;

public interface IRegistrationService
{
    Task<UserRegistrationResponseDto> RegisterUserAsync(RegisterUserRequestDto request);
    Task<RegistrationCompleteResponseDto> SetupSecurityAsync(Guid userId, SetupSecurityRequestDto request);
    Task<bool> IsEmailAvailableAsync(string email);
    Task<bool> IsCPFAvailableAsync(string cpf);
    Task<bool> IsPhoneAvailableAsync(string phone);
}

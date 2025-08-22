using ContosoBank.Application.DTOs;

namespace ContosoBank.Application.Interfaces;

public interface IPasswordResetService
{
    Task<PasswordResetResponseDto> RequestPasswordResetAsync(PasswordResetRequestDto request, string clientIpAddress, string userAgent);
    Task<PasswordResetResponseDto> ResetPasswordAsync(PasswordResetDto request, string clientIpAddress, string userAgent);
    Task<PasswordResetResponseDto> ValidateResetTokenAsync(string token);
}

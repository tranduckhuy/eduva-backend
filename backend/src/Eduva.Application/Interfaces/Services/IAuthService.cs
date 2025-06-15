using Eduva.Application.Features.Auth.DTOs;
using Eduva.Shared.Enums;

namespace Eduva.Application.Interfaces.Services
{
    public interface IAuthService
    {
        Task<CustomCode> RegisterAsync(RegisterRequestDto request);
        Task<(CustomCode, AuthResultDto)> LoginAsync(LoginRequestDto request);
        Task<CustomCode> ForgotPasswordAsync(ForgotPasswordRequestDto request);
        Task<CustomCode> ResetPasswordAsync(ResetPasswordRequestDto request);
        Task<CustomCode> ConfirmEmailAsync(ConfirmEmailRequestDto request);
        Task<CustomCode> ResendConfirmationEmailAsync(ResendConfirmationEmailRequestDto request);
        Task<(CustomCode, AuthResultDto)> RefreshTokenAsync(RefreshTokenRequestDto request);
        Task LogoutAsync(string userId, string accessToken);
        Task<CustomCode> ChangePasswordAsync(ChangePasswordRequestDto request);
        Task InvalidateAllUserTokensAsync(string userId);
    }
}

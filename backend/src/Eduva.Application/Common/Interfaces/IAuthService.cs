using Eduva.Application.Features.Auth.DTOs;
using Eduva.Shared.Enums;

namespace Eduva.Application.Common.Interfaces
{
    public interface IAuthService
    {
        Task<CustomCode> RegisterAsync(RegisterRequestDto request);
        Task<(CustomCode, AuthResultDto)> LoginAsync(LoginRequestDto request);
        Task<CustomCode> ForgotPasswordAsync(ForgotPasswordRequestDto forgotPasswordRequestDto);
        Task<CustomCode> ResetPasswordAsync(ResetPasswordRequestDto resetPasswordRequestDto);
        Task<CustomCode> ConfirmEmailAsync(ConfirmEmailRequestDto confirmEmailRequestDto);
        Task<CustomCode> ResendConfirmationEmailAsync(ResendConfirmationEmailRequestDto resendConfirmationEmailRequestDto);
        Task<(CustomCode, AuthResultDto)> RefreshTokenAsync(RefreshTokenRequestDto request);
        Task LogoutAsync(string userId, string accessToken);
        Task<CustomCode> ChangePasswordAsync(ChangePasswordRequestDto dto);
        Task InvalidateAllUserTokensAsync(string userId);
    }
}

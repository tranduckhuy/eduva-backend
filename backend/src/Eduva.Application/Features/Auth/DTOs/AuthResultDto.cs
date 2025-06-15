namespace Eduva.Application.Features.Auth.DTOs
{
    public class AuthResultDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public bool Requires2FA { get; set; } = false;
        public string Email { get; set; } = string.Empty;
    }
}
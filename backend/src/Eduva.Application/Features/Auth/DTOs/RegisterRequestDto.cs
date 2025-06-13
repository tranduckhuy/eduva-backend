namespace Eduva.Application.Features.Auth.DTOs
{
    public class RegisterRequestDto
    {
        public string? FullName { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string ClientUrl { get; set; } = string.Empty;
    }
}

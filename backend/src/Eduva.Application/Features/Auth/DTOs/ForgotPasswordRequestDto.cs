using System.ComponentModel.DataAnnotations;

namespace Eduva.Application.Features.Auth.DTOs
{
    public class ForgotPasswordRequestDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Email is not valid")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Client URL is required")]
        [MaxLength(255, ErrorMessage = "Client URL must be less than 255 characters")]
        public string ClientUrl { get; set; } = string.Empty;
    }
}
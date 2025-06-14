using System.ComponentModel.DataAnnotations;

namespace Eduva.Application.Features.Auth.DTOs
{
    public class ConfirmEmailRequestDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Email is not valid.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Token is required")]
        public string Token { get; set; } = string.Empty;
    }
}

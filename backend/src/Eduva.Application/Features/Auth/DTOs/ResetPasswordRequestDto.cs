using System.ComponentModel.DataAnnotations;

namespace Eduva.Application.Features.Auth.DTOs
{
    public class ResetPasswordRequestDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Email is not valid")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Token is required")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(255, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm Password is required.")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}

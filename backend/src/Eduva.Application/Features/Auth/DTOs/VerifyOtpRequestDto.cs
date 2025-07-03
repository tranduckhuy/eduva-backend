using System.ComponentModel.DataAnnotations;

namespace Eduva.Application.Features.Auth.DTOs
{
    public class VerifyOtpRequestDto
    {
        [Required(ErrorMessage = "OTP code is required.")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "OTP must be exactly 6 digits.")]
        public string OtpCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Email is not valid")]
        public string Email { get; set; } = string.Empty;
    }
}
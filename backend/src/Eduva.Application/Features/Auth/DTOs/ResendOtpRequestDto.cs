using Eduva.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Eduva.Application.Features.Auth.DTOs
{
    public class ResendOtpRequestDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Email is not valid.")]
        public string Email { get; set; } = default!;

        [Required(ErrorMessage = "OTP purpose is required.")]
        [EnumDataType(typeof(OtpPurpose), ErrorMessage = "OTP purpose is not valid.")]
        public OtpPurpose Purpose { get; set; }
    }
}
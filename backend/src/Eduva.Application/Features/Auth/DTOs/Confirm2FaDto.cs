using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.Auth.DTOs
{
    public class Confirm2FaDto
    {
        [JsonIgnore]
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "OTP code is required.")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "OTP must be exactly 6 digits.")]
        public string OtpCode { get; set; } = string.Empty;
    }
}
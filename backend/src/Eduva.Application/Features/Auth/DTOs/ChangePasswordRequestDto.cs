using Eduva.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.Auth.DTOs
{
    public class ChangePasswordRequestDto
    {
        [JsonIgnore]
        public Guid UserId { get; set; }

        [JsonIgnore]
        public string CurrentAccessToken { get; set; } = string.Empty;

        [Required(ErrorMessage = "Current Password is required")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New Password is required")]
        [StringLength(255, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [EnumDataType(typeof(LogoutBehavior), ErrorMessage = "Invalid logout behavior.")]
        public LogoutBehavior LogoutBehavior { get; set; } = LogoutBehavior.KeepAllSessions;
    }
}

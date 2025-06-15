using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.Auth.DTOs
{
    public class Request2FaDto
    {
        [JsonIgnore]
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "Current password is required.")]
        public string CurrentPassword { get; set; } = string.Empty;
    }
}
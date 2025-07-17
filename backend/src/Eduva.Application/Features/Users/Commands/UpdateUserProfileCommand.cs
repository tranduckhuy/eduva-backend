using Eduva.Application.Features.Users.Responses;
using MediatR;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.Users.Commands
{
    public class UpdateUserProfileCommand : IRequest<UserResponse>
    {
        [JsonIgnore]
        public Guid UserId { get; set; }

        public string? FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; } = string.Empty;
    }
}

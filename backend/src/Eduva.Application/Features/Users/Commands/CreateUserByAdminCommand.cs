using Eduva.Domain.Enums;
using MediatR;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.Users.Commands
{
    public class CreateUserByAdminCommand : IRequest<Unit>
    {
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public Role Role { get; set; }
        public string InitialPassword { get; set; } = string.Empty;
        [JsonIgnore]
        public Guid CreatorId { get; set; }
    }
}

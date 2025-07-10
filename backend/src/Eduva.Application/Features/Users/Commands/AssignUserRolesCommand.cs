using Eduva.Domain.Enums;
using MediatR;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.Users.Commands
{
    public class AssignUserRolesCommand : IRequest<Unit>
    {
        public Guid TargetUserId { get; set; }
        public List<Role> Roles { get; set; } = [];
        [JsonIgnore]
        public Guid SchoolAdminId { get; set; }
    }
}
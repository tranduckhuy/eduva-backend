using Eduva.Domain.Enums;

namespace Eduva.Application.Features.Users.Requests
{
    public class AssignUserRolesRequest
    {
        public List<Role> Roles { get; set; } = [];
    }
}
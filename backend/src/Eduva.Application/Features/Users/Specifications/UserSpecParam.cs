using Eduva.Application.Common.Specifications;
using Eduva.Domain.Enums;

namespace Eduva.Application.Features.Users.Specifications
{
    public class UserSpecParam : BaseSpecParam
    {
        public int? SchoolId { get; set; }
        public Role? Role { get; set; }
        public EntityStatus? Status { get; set; }
    }
}
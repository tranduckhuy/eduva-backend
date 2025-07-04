using Eduva.Application.Features.Users.Responses;
using MediatR;

namespace Eduva.Application.Features.Users.Queries
{
    public record GetSchoolUserQuery(Guid TargetUserId, Guid SchoolAdminId) : IRequest<UserResponse>;
}
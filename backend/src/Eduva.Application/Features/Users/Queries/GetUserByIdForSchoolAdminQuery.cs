using Eduva.Application.Features.Users.Responses;
using MediatR;

namespace Eduva.Application.Features.Users.Queries
{
    public record GetUserByIdForSchoolAdminQuery(Guid RequesterId, Guid TargetUserId) : IRequest<UserResponse>;
}
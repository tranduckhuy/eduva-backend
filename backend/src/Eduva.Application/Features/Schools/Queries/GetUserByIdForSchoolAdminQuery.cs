using Eduva.Application.Features.Users.Responses;
using MediatR;

namespace Eduva.Application.Features.Schools.Queries
{
    public record GetUserByIdForSchoolAdminQuery(Guid RequesterId, Guid TargetUserId) : IRequest<UserResponse>;
}
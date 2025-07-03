using Eduva.Application.Features.Users.Responses;
using MediatR;

namespace Eduva.Application.Features.Users.Queries
{
    public record GetUserProfileQuery(Guid UserId) : IRequest<UserResponse>;
}

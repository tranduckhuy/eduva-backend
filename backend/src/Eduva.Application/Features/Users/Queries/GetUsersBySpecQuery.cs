using Eduva.Application.Common.Models;
using Eduva.Application.Features.Users.Responses;
using Eduva.Application.Features.Users.Specifications;
using MediatR;

namespace Eduva.Application.Features.Users.Queries
{
    public record GetUsersBySpecQuery(UserSpecParam Param) : IRequest<Pagination<UserResponse>>;
}
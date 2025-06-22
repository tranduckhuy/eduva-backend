using Eduva.Application.Common.Models;
using Eduva.Application.Features.AICreditPacks.Responses;
using Eduva.Application.Features.AICreditPacks.Specifications;
using MediatR;

namespace Eduva.Application.Features.AICreditPacks.Queries
{
    public record GetAICreditPacksQuery(AICreditPackSpecParam Param)
        : IRequest<Pagination<AICreditPackResponse>>;
}
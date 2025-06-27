using Eduva.Application.Features.AICreditPacks.Responses;
using MediatR;

namespace Eduva.Application.Features.AICreditPacks.Queries
{
    public class GetAICreditPackByIdQuery : IRequest<AICreditPackResponse>
    {
        public int Id { get; set; }

        public GetAICreditPackByIdQuery(int id)
        {
            Id = id;
        }
    }
}
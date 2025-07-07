using Eduva.Application.Common.Mappings;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.AICreditPacks.Responses;
using Eduva.Application.Features.AICreditPacks.Specifications;
using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using MediatR;

namespace Eduva.Application.Features.AICreditPacks.Queries
{
    public class GetAICreditPacksQueryHandler : IRequestHandler<GetAICreditPacksQuery, Pagination<AICreditPackResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetAICreditPacksQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Pagination<AICreditPackResponse>> Handle(GetAICreditPacksQuery request, CancellationToken cancellationToken)
        {
            var spec = new AICreditPackSpecification(request.Param);

            var result = await _unitOfWork
                .GetRepository<AICreditPack, int>()
                .GetWithSpecAsync(spec);

            return AppMapper<AppMappingProfile>.Mapper.Map<Pagination<AICreditPackResponse>>(result);
        }
    }
}
using AutoMapper;
using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.AICreditPacks.Responses;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Shared.Enums;
using MediatR;

namespace Eduva.Application.Features.AICreditPacks.Queries
{
    public class GetAICreditPackByIdQueryHandler : IRequestHandler<GetAICreditPackByIdQuery, AICreditPackResponse>
    {
        private readonly IGenericRepository<AICreditPack, int> _creditPackRepository;
        private readonly IMapper _mapper;

        public GetAICreditPackByIdQueryHandler(
            IGenericRepository<AICreditPack, int> creditPackRepository,
            IMapper mapper)
        {
            _creditPackRepository = creditPackRepository;
            _mapper = mapper;
        }

        public async Task<AICreditPackResponse> Handle(GetAICreditPackByIdQuery request, CancellationToken cancellationToken)
        {
            var pack = await _creditPackRepository.GetByIdAsync(request.Id) ?? throw new AppException(CustomCode.AICreditPackNotFound);

            return _mapper.Map<AICreditPackResponse>(pack);
        }
    }
}
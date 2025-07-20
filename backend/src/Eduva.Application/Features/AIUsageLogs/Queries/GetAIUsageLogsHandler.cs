using Eduva.Application.Common.Mappings;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.AIUsageLogs.Specifications;
using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using MediatR;

namespace Eduva.Application.Features.AIUsageLogs.Queries
{
    public class GetAIUsageLogsHandler : IRequestHandler<GetAIUsageLogsQuery, Pagination<AIUsageLogResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetAIUsageLogsHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Pagination<AIUsageLogResponse>> Handle(GetAIUsageLogsQuery request, CancellationToken cancellationToken)
        {
            var spec = new GetAIUsageLogsSpecification(request.SpecParam, request.UserId);

            var repository = _unitOfWork.GetRepository<AIUsageLog, Guid>();

            var aiUsageLogs = await repository.GetWithSpecAsync(spec);

            return AppMapper<AppMappingProfile>.Mapper.Map<Pagination<AIUsageLogResponse>>(aiUsageLogs);
        }
    }
}

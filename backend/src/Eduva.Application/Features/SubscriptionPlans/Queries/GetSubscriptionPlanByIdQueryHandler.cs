using Eduva.Application.Common.Mappings;
using Eduva.Application.Exceptions.SubscriptionPlan;
using Eduva.Application.Features.SubscriptionPlans.Responses;
using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using MediatR;

namespace Eduva.Application.Features.SubscriptionPlans.Queries
{
    public class GetSubscriptionPlanByIdQueryHandler : IRequestHandler<GetSubscriptionPlanByIdQuery, SubscriptionPlanResponse>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetSubscriptionPlanByIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<SubscriptionPlanResponse> Handle(GetSubscriptionPlanByIdQuery request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<SubscriptionPlan, int>();
            var plan = await repo.GetByIdAsync(request.Id) ?? throw new PlanNotFoundException();

            return AppMapper<AppMappingProfile>.Mapper.Map<SubscriptionPlanResponse>(plan);
        }
    }
}
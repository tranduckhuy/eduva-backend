using Eduva.Application.Common.Mappings;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.SubscriptionPlans.Responses;
using Eduva.Application.Features.SubscriptionPlans.Specifications;
using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using MediatR;

namespace Eduva.Application.Features.SubscriptionPlans.Queries
{
    public class GetSubscriptionPlansHandler : IRequestHandler<GetSubscriptionPlansQuery, Pagination<SubscriptionPlanResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetSubscriptionPlansHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Pagination<SubscriptionPlanResponse>> Handle(GetSubscriptionPlansQuery request, CancellationToken cancellationToken)
        {
            var spec = new SubscriptionPlanSpecification(request.Param);

            var result = await _unitOfWork
                .GetRepository<SubscriptionPlan, int>()
                .GetWithSpecAsync(spec);

            return AppMapper.Mapper.Map<Pagination<SubscriptionPlanResponse>>(result);
        }
    }
}
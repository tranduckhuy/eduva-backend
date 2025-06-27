using Eduva.Application.Exceptions.SubscriptionPlan;
using Eduva.Application.Features.SubscriptionPlans.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using MediatR;

namespace Eduva.Application.Features.SubscriptionPlans.Queries
{
    public class GetSubscriptionPlanDetailQueryHandler : IRequestHandler<GetSubscriptionPlanDetailQuery, SubscriptionPlanDetailResponse>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetSubscriptionPlanDetailQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<SubscriptionPlanDetailResponse> Handle(GetSubscriptionPlanDetailQuery request, CancellationToken cancellationToken)
        {
            var planRepo = _unitOfWork.GetRepository<SubscriptionPlan, int>();
            var schoolSubRepo = _unitOfWork.GetCustomRepository<ISchoolSubscriptionRepository>();

            var plan = await planRepo.GetByIdAsync(request.Id) ?? throw new PlanNotFoundException();

            var usageCount = await schoolSubRepo.CountSchoolsUsingPlanAsync(plan.Id, cancellationToken);

            return new SubscriptionPlanDetailResponse
            {
                Id = plan.Id,
                Name = plan.Name,
                Description = plan.Description,
                MaxUsers = plan.MaxUsers,
                StorageLimitGB = plan.StorageLimitGB,
                PriceMonthly = plan.PriceMonthly,
                PricePerYear = plan.PricePerYear,
                Status = plan.Status,
                IsRecommended = plan.IsRecommended,
                NumberOfSchoolsUsing = usageCount
            };
        }
    }
}
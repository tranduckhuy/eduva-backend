using Eduva.Application.Common.Mappings;
using Eduva.Application.Exceptions.SubscriptionPlan;
using Eduva.Application.Features.SubscriptionPlans.Responses;
using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using MediatR;

namespace Eduva.Application.Features.SubscriptionPlans.Commands.UpdatePlan
{
    public class UpdateSubscriptionPlanCommandHandler : IRequestHandler<UpdateSubscriptionPlanCommand, SubscriptionPlanResponse>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdateSubscriptionPlanCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<SubscriptionPlanResponse> Handle(UpdateSubscriptionPlanCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<SubscriptionPlan, int>();

            var plan = await repo.GetByIdAsync(request.Id) ?? throw new PlanNotFoundException();

            plan.Name = request.Name;
            plan.Description = request.Description;
            plan.MaxUsers = request.MaxUsers;
            plan.StorageLimitGB = request.StorageLimitGB;
            plan.MaxMinutesPerMonth = request.MaxMinutesPerMonth;
            plan.PriceMonthly = request.PriceMonthly;
            plan.PricePerYear = request.PricePerYear;
            plan.LastModifiedAt = DateTimeOffset.UtcNow;

            repo.Update(plan);
            await _unitOfWork.CommitAsync();

            return AppMapper.Mapper.Map<SubscriptionPlanResponse>(plan);
        }
    }
}
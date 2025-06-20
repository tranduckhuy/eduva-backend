using Eduva.Application.Exceptions.SubscriptionPlan;
using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using MediatR;

namespace Eduva.Application.Features.SubscriptionPlans.Commands.ActivatePlan
{
    public class ActivateSubscriptionPlanCommandHandler : IRequestHandler<ActivateSubscriptionPlanCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;

        public ActivateSubscriptionPlanCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(ActivateSubscriptionPlanCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<SubscriptionPlan, int>();

            var plan = await repo.GetByIdAsync(request.Id) ?? throw new PlanNotFoundException();

            if (plan.Status == EntityStatus.Active)
            {
                throw new PlanAlreadyActiveException();
            }

            plan.Status = EntityStatus.Active;
            plan.LastModifiedAt = DateTimeOffset.UtcNow;

            repo.Update(plan);
            await _unitOfWork.CommitAsync();

            return Unit.Value;
        }
    }
}
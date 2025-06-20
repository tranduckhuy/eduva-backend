using Eduva.Application.Exceptions.SubscriptionPlan;
using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using MediatR;

namespace Eduva.Application.Features.SubscriptionPlans.Commands.ArchivePlan
{
    public class ArchiveSubscriptionPlanCommandHandler : IRequestHandler<ArchiveSubscriptionPlanCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;

        public ArchiveSubscriptionPlanCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(ArchiveSubscriptionPlanCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<SubscriptionPlan, int>();
            var schoolSubRepo = _unitOfWork.GetRepository<SchoolSubscription, int>();

            var plan = await repo.GetByIdAsync(request.Id) ?? throw new PlanNotFoundException();

            if (plan.Status == EntityStatus.Archived)
            {
                throw new PlanAlreadyArchivedException();
            }

            var isInUse = await schoolSubRepo
                .ExistsAsync(s => s.PlanId == plan.Id && s.SubscriptionStatus == SubscriptionStatus.Active);

            if (isInUse)
            {
                throw new PlanInUseException();
            }

            plan.Status = EntityStatus.Archived;
            plan.LastModifiedAt = DateTimeOffset.UtcNow;

            repo.Update(plan);
            await _unitOfWork.CommitAsync();

            return Unit.Value;
        }
    }
}
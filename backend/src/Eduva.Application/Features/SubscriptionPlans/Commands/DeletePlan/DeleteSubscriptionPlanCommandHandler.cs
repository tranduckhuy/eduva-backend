using Eduva.Application.Exceptions.SubscriptionPlan;
using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using MediatR;

namespace Eduva.Application.Features.SubscriptionPlans.Commands.DeletePlan
{
    public class DeleteSubscriptionPlanCommandHandler : IRequestHandler<DeleteSubscriptionPlanCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;

        public DeleteSubscriptionPlanCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(DeleteSubscriptionPlanCommand request, CancellationToken cancellationToken)
        {
            var planRepo = _unitOfWork.GetRepository<SubscriptionPlan, int>();
            var plan = await planRepo.GetByIdAsync(request.Id) ?? throw new PlanNotFoundException();

            if (plan.Status != EntityStatus.Archived)
            {
                throw new SubscriptionPlanMustBeArchivedException();

            }

            planRepo.Remove(plan);
            await _unitOfWork.CommitAsync();

            return Unit.Value;
        }
    }
}
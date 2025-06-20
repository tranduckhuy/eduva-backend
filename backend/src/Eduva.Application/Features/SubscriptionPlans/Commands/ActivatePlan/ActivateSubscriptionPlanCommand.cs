using MediatR;

namespace Eduva.Application.Features.SubscriptionPlans.Commands.ActivatePlan
{
    public class ActivateSubscriptionPlanCommand : IRequest<Unit>
    {
        public int Id { get; set; }

        public ActivateSubscriptionPlanCommand(int id)
        {
            Id = id;
        }
    }
}
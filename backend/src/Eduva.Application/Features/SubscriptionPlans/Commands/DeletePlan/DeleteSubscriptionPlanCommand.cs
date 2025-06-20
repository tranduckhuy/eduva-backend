using MediatR;

namespace Eduva.Application.Features.SubscriptionPlans.Commands.DeletePlan
{
    public class DeleteSubscriptionPlanCommand : IRequest<Unit>
    {
        public int Id { get; set; }

        public DeleteSubscriptionPlanCommand(int id)
        {
            Id = id;
        }
    }
}
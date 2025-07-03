using MediatR;

namespace Eduva.Application.Features.SubscriptionPlans.Commands.ArchivePlan
{
    public class ArchiveSubscriptionPlanCommand : IRequest<Unit>
    {
        public int Id { get; set; }

        public ArchiveSubscriptionPlanCommand(int id)
        {
            Id = id;
        }
    }
}
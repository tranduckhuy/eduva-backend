using Eduva.Application.Features.SubscriptionPlans.Responses;
using MediatR;

namespace Eduva.Application.Features.SubscriptionPlans.Queries
{
    public class GetSubscriptionPlanByIdQuery : IRequest<SubscriptionPlanResponse>
    {
        public int Id { get; set; }

        public GetSubscriptionPlanByIdQuery(int id)
        {
            Id = id;
        }
    }
}
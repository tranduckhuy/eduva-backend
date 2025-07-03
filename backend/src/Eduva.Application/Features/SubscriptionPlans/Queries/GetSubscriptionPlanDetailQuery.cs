using Eduva.Application.Features.SubscriptionPlans.Responses;
using MediatR;

namespace Eduva.Application.Features.SubscriptionPlans.Queries
{
    public class GetSubscriptionPlanDetailQuery : IRequest<SubscriptionPlanDetailResponse>
    {
        public int Id { get; set; }

        public GetSubscriptionPlanDetailQuery(int id)
        {
            Id = id;
        }
    }
}
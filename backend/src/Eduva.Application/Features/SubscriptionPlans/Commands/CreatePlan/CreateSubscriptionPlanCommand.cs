using Eduva.Application.Features.SubscriptionPlans.Responses;
using MediatR;

namespace Eduva.Application.Features.SubscriptionPlans.Commands.CreatePlan
{
    public class CreateSubscriptionPlanCommand : IRequest<SubscriptionPlanResponse>
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int MaxUsers { get; set; }
        public decimal StorageLimitGB { get; set; }
        public decimal PriceMonthly { get; set; }
        public decimal PricePerYear { get; set; }
        public bool IsRecommended { get; set; } = false;
    }
}
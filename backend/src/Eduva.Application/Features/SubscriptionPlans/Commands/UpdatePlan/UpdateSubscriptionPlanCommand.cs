using Eduva.Application.Features.SubscriptionPlans.Responses;
using MediatR;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.SubscriptionPlans.Commands.UpdatePlan
{
    public class UpdateSubscriptionPlanCommand : IRequest<SubscriptionPlanResponse>
    {
        [JsonIgnore]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int MaxUsers { get; set; }
        public decimal StorageLimitGB { get; set; }
        public decimal MaxMinutesPerMonth { get; set; }
        public decimal PriceMonthly { get; set; }
        public decimal PricePerYear { get; set; }
    }
}
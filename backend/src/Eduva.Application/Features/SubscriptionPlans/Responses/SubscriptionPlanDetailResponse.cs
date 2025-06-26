using Eduva.Domain.Enums;

namespace Eduva.Application.Features.SubscriptionPlans.Responses
{
    public class SubscriptionPlanDetailResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int MaxUsers { get; set; }
        public decimal StorageLimitGB { get; set; }
        public decimal PriceMonthly { get; set; }
        public decimal PricePerYear { get; set; }
        public bool IsRecommended { get; set; } = false;
        public EntityStatus Status { get; set; }

        public int NumberOfSchoolsUsing { get; set; }
    }
}
using Eduva.Domain.Enums;

namespace Eduva.Application.Features.SchoolSubscriptions.Responses
{
    public class MySchoolSubscriptionResponse
    {
        public string PlanName { get; set; } = string.Empty;
        public string? Description { get; set; }

        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }

        public SubscriptionStatus SubscriptionStatus { get; set; }
        public BillingCycle BillingCycle { get; set; }

        public int MaxUsers { get; set; }
        public decimal StorageLimitGB { get; set; }
        public decimal AmountPaid { get; set; }
    }
}

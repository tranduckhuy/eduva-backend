using Eduva.Domain.Common;
using Eduva.Domain.Enums;

namespace Eduva.Domain.Entities
{
    public class SchoolSubscription : BaseEntity<int>
    {
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public SubscriptionStatus SubscriptionStatus { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public decimal AmountPaid { get; set; }
        public decimal CurrentPeriodAIUsageMinutes { get; set; }
        public DateTimeOffset LastUsageResetDate { get; set; }
        public DateTimeOffset PurchasedAt { get; set; }

        public int SchoolId { get; set; }
        public virtual School School { get; set; } = null!;

        public int PlanId { get; set; }
        public virtual SubscriptionPlan Plan { get; set; } = null!;
    }
}
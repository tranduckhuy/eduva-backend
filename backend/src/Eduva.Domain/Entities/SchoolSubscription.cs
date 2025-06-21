using Eduva.Domain.Common;
using Eduva.Domain.Enums;

namespace Eduva.Domain.Entities
{
    public class SchoolSubscription : BaseEntity<int>
    {
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public SubscriptionStatus SubscriptionStatus { get; set; }
        public BillingCycle BillingCycle { get; set; }

        public int SchoolId { get; set; }
        public virtual School School { get; set; } = null!;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public int PlanId { get; set; }
        public virtual SubscriptionPlan Plan { get; set; } = null!;

        public Guid PaymentTransactionId { get; set; } // Reference to the payment transaction
        public virtual PaymentTransaction PaymentTransaction { get; set; } = null!;
    }
}
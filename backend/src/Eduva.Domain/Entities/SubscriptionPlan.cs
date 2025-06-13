using Eduva.Domain.Common;

namespace Eduva.Domain.Entities
{
    public class SubscriptionPlan : BaseTimestampedEntity<int>
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int MaxUsers { get; set; }
        public decimal StorageLimitGB { get; set; }
        public decimal MaxMinutesPerMonth { get; set; }
        public decimal PriceMonthly { get; set; }
        public decimal PricePerYear { get; set; }

        // Navigation properties
        public virtual ICollection<SchoolSubscription> SchoolSubscriptions { get; set; } = [];
    }
}

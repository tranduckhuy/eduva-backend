using Eduva.Domain.Enums;

namespace Eduva.Application.Features.SchoolSubscriptions.Responses
{
    public class SchoolSubscriptionResponse
    {
        public Guid Id { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public SubscriptionStatus SubscriptionStatus { get; set; }
        public BillingCycle BillingCycle { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public decimal AmountPaid { get; set; }

        public SchoolInfo School { get; set; } = null!;
        public SubscriptionPlanInfo Plan { get; set; } = null!;
    }

    public class SchoolInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string ContactEmail { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public string? WebsiteUrl { get; set; }
    }

    public class SubscriptionPlanInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int MaxUsers { get; set; }
        public decimal StorageLimitGB { get; set; }
        public decimal Price { get; set; }
    }
}
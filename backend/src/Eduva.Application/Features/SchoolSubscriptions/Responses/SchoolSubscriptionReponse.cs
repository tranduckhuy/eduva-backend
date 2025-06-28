using Eduva.Application.Features.Payments.Responses;
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

        public SchoolInfo School { get; set; } = null!;
        public SubscriptionPlanInfo Plan { get; set; } = null!;
        public PaymentTransactionInfo PaymentTransaction { get; set; } = null!;

        public UserInfo User { get; set; } = null!;

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
        public bool IsRecommended { get; set; } = false;
    }

    public class PaymentTransactionInfo
    {
        public Guid UserId { get; set; }
        public PaymentPurpose PaymentPurpose { get; set; }
        public int PaymentItemId { get; set; }
        public string? RelatedId { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
        public string TransactionCode { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
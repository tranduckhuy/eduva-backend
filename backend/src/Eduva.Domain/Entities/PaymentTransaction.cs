using Eduva.Domain.Common;
using Eduva.Domain.Enums;

namespace Eduva.Domain.Entities
{
    public class PaymentTransaction : BaseEntity<Guid>
    {
        public Guid UserId { get; set; }
        public PaymentPurpose PaymentPurpose { get; set; }
        public string? RelatedId { get; set; } // Optional: could be used for linking to an order or subscription
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
        public string TransactionCode { get; set; } = string.Empty; // Unique identifier for the transaction
        public decimal Amount { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation property
        public ApplicationUser User { get; set; } = null!;
    }
}

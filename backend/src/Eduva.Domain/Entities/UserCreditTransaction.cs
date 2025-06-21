using Eduva.Domain.Common;

namespace Eduva.Domain.Entities
{
    public class UserCreditTransaction : BaseEntity<int>
    {
        public Guid UserId { get; set; }

        public int AICreditPackId { get; set; }
        public Guid PaymentTransactionId { get; set; } // Reference to the payment transaction
        public int Credits { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation properties
        public virtual ApplicationUser User { get; set; } = null!;
        public virtual AICreditPack AICreditPack { get; set; } = null!;
        public virtual PaymentTransaction PaymentTransaction { get; set; } = null!;

    }
}

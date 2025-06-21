using Eduva.Domain.Common;
using Eduva.Domain.Enums;

namespace Eduva.Domain.Entities
{
    public class UserCreditTransaction : BaseEntity<int>
    {
        public Guid UserId { get; set; }

        public int AICreditPackID { get; set; }
        public int Credits { get; set; }
        public decimal MoneyAmount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
        public string TransactionId { get; set; } = string.Empty;

        // Navigation properties
        public virtual ApplicationUser User { get; set; } = null!;
        public virtual AICreditPack AICreditPack { get; set; } = null!;
    }
}

using Eduva.Domain.Common;

namespace Eduva.Domain.Entities
{
    public class AICreditPack : BaseTimestampedEntity<int>
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Credits { get; set; }
        public int BonusCredits { get; set; } = 0;

        // Navigation properties
        public virtual ICollection<UserCreditTransaction> UserCreditTransactions { get; set; } = new HashSet<UserCreditTransaction>();
    }
}

using Eduva.Domain.Common;
using Eduva.Domain.Enums;

namespace Eduva.Domain.Entities
{
    public class AIUsageLog : BaseEntity<Guid>
    {
        public Guid UserId { get; set; }
        public AIServiceType AIServiceType { get; set; }
        public decimal DurationMinutes { get; set; }
        public int CreditsCharged { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation properties
        public virtual ApplicationUser User { get; set; } = null!;
    }
}
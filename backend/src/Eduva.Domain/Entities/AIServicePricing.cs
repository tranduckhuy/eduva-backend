using Eduva.Domain.Common;
using Eduva.Domain.Enums;

namespace Eduva.Domain.Entities
{
    public class AIServicePricing : BaseTimestampedEntity<int>
    {
        public AIServiceType ServiceType { get; set; }
        public decimal PricePerMinuteCredits { get; set; }
    }
}

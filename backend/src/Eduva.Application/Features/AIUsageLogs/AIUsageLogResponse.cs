using Eduva.Domain.Enums;

namespace Eduva.Application.Features.AIUsageLogs
{
    public class AIUsageLogResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public AIServiceType AIServiceType { get; set; }
        public decimal DurationMinutes { get; set; }
        public int CreditsCharged { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}

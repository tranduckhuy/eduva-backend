using Eduva.Domain.Common;
using Eduva.Domain.Enums;

namespace Eduva.Domain.Entities
{
    public class AIUsageLog : BaseEntity<int>
    {
        public Guid UserId { get; set; } 
        public string? LessonTitleAtCreation { get; set; }
        public ContentType ContentType { get; set; }
        public decimal CostMinutes { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation properties
        public virtual ApplicationUser User { get; set; } = null!;
    }
}

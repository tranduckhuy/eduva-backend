using Eduva.Domain.Common;

namespace Eduva.Domain.Entities
{
    public class Notification : BaseEntity<int>
    {
        public string Type { get; set; } = default!;
        public string Payload { get; set; } = default!;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation properties
        public virtual ICollection<UserNotification> UserNotifications { get; set; } = [];
    }
}

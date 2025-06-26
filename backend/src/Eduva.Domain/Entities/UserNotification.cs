using Eduva.Domain.Common;

namespace Eduva.Domain.Entities
{
    public class UserNotification : BaseEntity<Guid>
    {
        public Guid TargetUserID { get; set; }
        public int NotificationID { get; set; }
        public bool IsRead { get; set; }

        // Navigation properties
        public virtual ApplicationUser TargetUser { get; set; } = default!;
        public virtual Notification Notification { get; set; } = default!;
    }
}
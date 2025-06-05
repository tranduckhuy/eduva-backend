using Eduva.Domain.Entities;

namespace Eduva.Domain.Common
{
    public class BaseAuditableEntity<TKey> : BaseEntity<TKey>, IAuditable, ISoftDeletable
    {
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public Guid CreatedBy { get; set; }

        public DateTimeOffset? LastModifiedAt { get; set; }
        public Guid? LastModifiedBy { get; set; }

        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        public virtual User Creator { get; set; } = null!;
        public virtual User? Modifier { get; set; }
    }
}

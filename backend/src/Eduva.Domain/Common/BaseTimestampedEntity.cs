using Eduva.Domain.Enums;

namespace Eduva.Domain.Common
{
    public abstract class BaseTimestampedEntity<TKey> : BaseEntity<TKey>
    {
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? LastModifiedAt { get; set; }
        public EntityStatus Status { get; set; } = EntityStatus.Active;
    }
}

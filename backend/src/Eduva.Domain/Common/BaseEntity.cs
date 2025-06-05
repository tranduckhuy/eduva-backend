namespace Eduva.Domain.Common
{
    public abstract class BaseEntity<TKey> : IEntity<TKey>
    {
        public TKey ID { get; set; } = default!;
    }
}

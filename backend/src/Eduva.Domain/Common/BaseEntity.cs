using System.ComponentModel.DataAnnotations;

namespace Eduva.Domain.Common
{
    public abstract class BaseEntity<TKey> : IEntity<TKey>
    {
        [Key]
        public TKey Id { get; set; } = default!;
    }
}
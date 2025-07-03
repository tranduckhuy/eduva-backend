using Eduva.Domain.Common;

namespace Eduva.Domain.Entities
{
    public class SystemConfig : BaseTimestampedEntity<int>
    {
        public string Key { get; set; } = default!;
        public string Value { get; set; } = default!;
        public string? Description { get; set; }
    }
}

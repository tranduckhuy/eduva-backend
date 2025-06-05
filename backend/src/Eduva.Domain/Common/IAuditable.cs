using Eduva.Domain.Entities;

namespace Eduva.Domain.Common
{
    public interface IAuditable
    {
        DateTimeOffset CreatedAt { get; set; }
        Guid CreatedBy { get; set; }

        DateTimeOffset? LastModifiedAt { get; set; }
        Guid? LastModifiedBy { get; set; }
    }
}

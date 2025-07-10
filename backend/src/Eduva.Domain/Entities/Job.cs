using Eduva.Domain.Common;
using Eduva.Domain.Enums;

namespace Eduva.Domain.Entities;

public class Job : BaseTimestampedEntity<Guid>
{
    public Job()
    {
        Id = Guid.NewGuid();
        JobStatus = JobStatus.Processing;
        CreatedAt = DateTimeOffset.UtcNow;
        LastModifiedAt = DateTimeOffset.UtcNow;
    }

    public AIServiceType? Type { get; set; }
    public JobStatus JobStatus { get; set; }
    public string Topic { get; set; } = string.Empty;
    public List<string> SourceBlobNames { get; set; } = [];
    public string? ContentBlobName { get; set; }
    public string? ProductBlobName { get; set; }
    public int? WordCount { get; set; }
    public string? FailureReason { get; set; }
    public Guid UserId { get; set; }

    // Navigation properties
    public virtual ApplicationUser User { get; set; } = null!;
}

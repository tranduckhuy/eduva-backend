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

    public JobStatus JobStatus { get; set; }
    public string Topic { get; set; } = string.Empty;
    public List<string> SourceBlobNames { get; set; } = [];
    public string? ContentBlobName { get; set; }
    public string? VideoOutputBlobName { get; set; }
    public string? AudioOutputBlobName { get; set; }
    public string? PreviewContent { get; set; }
    public decimal? EstimatedDurationMinutes { get; set; }
    public int AudioCost { get; set; } = 0;
    public int VideoCost { get; set; } = 0;
    public int? WordCount { get; set; }
    public string? FailureReason { get; set; }
    public Guid UserId { get; set; }

    // Navigation properties
    public virtual ApplicationUser User { get; set; } = null!;
}

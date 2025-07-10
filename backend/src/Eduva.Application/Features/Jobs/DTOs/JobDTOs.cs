using Eduva.Application.Features.Jobs.Commands.ConfirmJob;
using Eduva.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Eduva.Application.Features.Jobs.DTOs;

public class CreateJobRequest
{
    public List<IFormFile> File { get; set; } = [];
    public string Topic { get; set; } = string.Empty;
}

public class CreateJobResponse
{
    public Guid JobId { get; set; }
    public JobStatus Status { get; set; }
}

public class JobResponse
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public List<string> SourceBlobNames { get; set; } = new();
    public string? ContentBlobName { get; set; }
    public string? ProductBlobName { get; set; }
    public int? WordCount { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastModifiedAt { get; set; }
}

public class UpdateJobRequest
{
    public JobStatus? Status { get; set; }
    public string? ContentBlobName { get; set; }
    public string? ProductBlobName { get; set; }
    public int? WordCount { get; set; }
    public string? FailureReason { get; set; }
}

public class ConfirmJobRequest
{
    public AIServiceType Type { get; set; }
    public VoiceConfigDto VoiceConfig { get; set; } = default!;
}

public class UpdateJobProgressRequest
{
    public JobStatus JobStatus { get; set; }
    public int? WordCount { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public string? ContentBlobName { get; set; }
    public string? ProductBlobName { get; set; }
    public string? FailureReason { get; set; }
}

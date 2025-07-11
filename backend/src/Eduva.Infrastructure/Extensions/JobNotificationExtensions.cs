using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Enums;

namespace Eduva.Infrastructure.Extensions;

public static class JobNotificationExtensions
{
    /// <summary>
    /// Job started notification
    /// </summary>
    public static async Task NotifyJobStartedAsync(this IJobNotificationService service,
        Guid jobId,
        Guid userId,
        AIServiceType jobType = AIServiceType.GenAudio,
        CancellationToken cancellationToken = default)
    {
        var data = new
        {
            JobId = jobId,
            Status = JobStatus.Processing.ToString(),
            Type = jobType.ToString(),
            Message = $"Your {jobType} job has started processing...",
            StartedAt = DateTime.UtcNow
        };

        await service.NotifyUserAsync(userId, "JobStarted", data, cancellationToken);
    }

    /// <summary>
    /// Progress notification for a job
    /// </summary>
    public static async Task NotifyJobProgressAsync(this IJobNotificationService service,
        Guid jobId,
        Guid userId,
        int progress,
        string? message = null,
        CancellationToken cancellationToken = default)
    {
        var data = new
        {
            JobId = jobId,
            Progress = progress,
            Message = message ?? $"Processing... {progress}%",
            UpdatedAt = DateTime.UtcNow
        };

        await service.NotifyUserAsync(userId, "JobProgress", data, cancellationToken);
    }

    /// <summary>
    /// Completed job notification
    /// </summary>
    public static async Task NotifyJobCompletedAsync(this IJobNotificationService service,
        Guid jobId,
        Guid userId,
        string? downloadUrl = null,
        CancellationToken cancellationToken = default)
    {
        var data = new
        {
            JobId = jobId,
            Status = JobStatus.Completed.ToString(),
            DownloadUrl = downloadUrl,
            Message = "Your job has been completed successfully!",
            CompletedAt = DateTime.UtcNow
        };

        await service.NotifyUserAsync(userId, "JobCompleted", data, cancellationToken);
    }

    /// <summary>
    /// Failed job notification
    /// </summary>
    public static async Task NotifyJobFailedAsync(this IJobNotificationService service,
        Guid jobId,
        Guid userId,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        var data = new
        {
            JobId = jobId,
            Status = JobStatus.Failed.ToString(),
            ErrorMessage = errorMessage,
            FailedAt = DateTime.UtcNow
        };

        await service.NotifyUserAsync(userId, "JobFailed", data, cancellationToken);
    }
}

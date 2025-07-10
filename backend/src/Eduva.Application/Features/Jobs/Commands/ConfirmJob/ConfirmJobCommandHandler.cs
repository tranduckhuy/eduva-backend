using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Application.Models.Jobs;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.Jobs.Commands.ConfirmJob;

public class ConfirmJobCommand : IRequest<CustomCode>
{
    public Guid Id { get; set; }
    public AIServiceType Type { get; set; } // User's choice: audio or video
    public VoiceConfigDto VoiceConfig { get; set; } = default!;
}

public class VoiceConfigDto
{
    [JsonPropertyName("language_code")]
    public string LanguageCode { get; set; } = default!;

    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("speaking_rate")]
    public float SpeakingRate { get; set; }
}

public class ConfirmJobCommandHandler : IRequestHandler<ConfirmJobCommand, CustomCode>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRabbitMQService _rabbitMQService;
    private readonly IJobNotificationService _notificationService;
    private readonly ILogger<ConfirmJobCommandHandler> _logger;

    public ConfirmJobCommandHandler(
        IUnitOfWork unitOfWork,
        IRabbitMQService rabbitMQService,
        IJobNotificationService notificationService,
        ILogger<ConfirmJobCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _rabbitMQService = rabbitMQService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<CustomCode> Handle(ConfirmJobCommand request, CancellationToken cancellationToken)
    {
        // Validate job type choice
        if (request.Type != AIServiceType.GenVideo && request.Type != AIServiceType.GenAudio)
        {
            return CustomCode.ModelInvalid;
        }

        var jobRepository = _unitOfWork.GetCustomRepository<IJobRepository>();
        var job = await jobRepository.GetByIdAsync(request.Id, cancellationToken);
        if (job == null)
        {
            return CustomCode.UserNotFound;
        }

        if (job.JobStatus != JobStatus.ContentGenerated)
        {
            return CustomCode.ProvidedInformationIsInValid;
        }

        // Update job with user's choice and status
        job.Type = request.Type; // Set the chosen type (audio or video)
        job.JobStatus = JobStatus.CreatingProduct;
        job.LastModifiedAt = DateTimeOffset.UtcNow;

        _unitOfWork.GetRepository<Job, Guid>().Update(job);
        await _unitOfWork.CommitAsync();

        // Create job message for product creation
        var createProductMessage = new CreateProductMessage
        {
            JobId = job.Id,
            JobType = request.Type, // Use the user's chosen type
            TaskType = TaskType.CreateProduct,
            ContentBlobName = job.ContentBlobName ?? string.Empty,
            VoiceConfig = request.VoiceConfig
        };

        // Publish message to RabbitMQ
        await _rabbitMQService.PublishAsync(createProductMessage);

        // Send real-time update via SignalR
        var statusData = new
        {
            JobId = job.Id,
            Status = job.JobStatus,
            job.LastModifiedAt
        };

        await _notificationService.NotifyUserAsync(job.UserId, "JobStatusUpdated", statusData, cancellationToken);

        _logger.LogInformation("Job {JobId} confirmed successfully. Status updated to {Status}", job.Id, job.JobStatus);

        return CustomCode.Updated;
    }
}

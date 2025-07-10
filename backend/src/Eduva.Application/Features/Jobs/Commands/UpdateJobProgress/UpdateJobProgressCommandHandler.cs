using Eduva.Application.Exceptions.Job;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Eduva.Application.Features.Jobs.Commands.UpdateJobProgress;

public class UpdateJobProgressCommand : IRequest<Unit>
{
    public Guid JobId { get; set; }
    public JobStatus JobStatus { get; set; }
    public int? WordCount { get; set; }
    public string? PreviewContent { get; set; }
    public string? ContentBlobName { get; set; }
    public string? ProductBlobName { get; set; }
    public string? FailureReason { get; set; }
}

public class UpdateJobProgressCommandHandler : IRequestHandler<UpdateJobProgressCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJobNotificationService _notificationService;
    private readonly ILogger<UpdateJobProgressCommandHandler> _logger;
    private const int WORDS_PER_MINUTE = 200;

    public UpdateJobProgressCommandHandler(
        IUnitOfWork unitOfWork,
        IJobNotificationService notificationService,
        ILogger<UpdateJobProgressCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<Unit> Handle(UpdateJobProgressCommand request, CancellationToken cancellationToken)
    {
        var jobRepository = _unitOfWork.GetCustomRepository<IJobRepository>();
        var job = await jobRepository.GetByIdAsync(request.JobId, cancellationToken) ?? throw new JobNotFoundException();

        // Update job status and relevant fields
        job.JobStatus = request.JobStatus;

        if (request.WordCount.HasValue)
            job.WordCount = request.WordCount.Value;

        if (!string.IsNullOrEmpty(request.ContentBlobName))
            job.ContentBlobName = request.ContentBlobName;

        if (!string.IsNullOrEmpty(request.ProductBlobName))
            job.ProductBlobName = request.ProductBlobName;

        if (!string.IsNullOrEmpty(request.FailureReason))
            job.FailureReason = request.FailureReason;

        _unitOfWork.GetRepository<Job, Guid>().Update(job);

        // Calculate credit costs based on estimated duration
        var (audioCost, videoCost) = await CalculateCreditCostAsync(request.WordCount ?? 0, cancellationToken);

        // Send real-time update via SignalR
        var statusData = new
        {
            JobId = job.Id,
            Status = job.JobStatus,
            request.PreviewContent,
            AudioCost = audioCost,
            VideoCost = videoCost,
            job.ContentBlobName,
            job.ProductBlobName,
            job.FailureReason,
            job.LastModifiedAt
        };

        await _notificationService.NotifyUserAsync(job.UserId, "JobStatusUpdated", statusData, cancellationToken);

        _logger.LogInformation("Job {JobId} progress updated successfully. Status: {Status}", job.Id, job.JobStatus);

        await _unitOfWork.CommitAsync();

        return Unit.Value;
    }

    private async Task<(int audioCost, int videoCost)> CalculateCreditCostAsync(int wordCount, CancellationToken cancellationToken = default)
    {
        var aiServicePricingRepository = _unitOfWork.GetRepository<AIServicePricing, int>();

        var pricingRecords = await aiServicePricingRepository.GetAllAsync();

        if (pricingRecords == null || !pricingRecords.Any())
        {
            throw new InvalidOperationException("No AI service pricing records found.");
        }

        var audioPricing = pricingRecords.FirstOrDefault(p => p.ServiceType == AIServiceType.GenAudio);
        var videoPricing = pricingRecords.FirstOrDefault(p => p.ServiceType == AIServiceType.GenVideo);

        if (audioPricing == null || videoPricing == null)
        {
            throw new InvalidOperationException("Audio or Video pricing not found.");
        }

        var estimatedDurationMinutes = (decimal)wordCount / WORDS_PER_MINUTE;

        var audioCost = (int)Math.Ceiling(estimatedDurationMinutes * audioPricing.PricePerMinuteCredits);
        var videoCost = (int)Math.Ceiling(estimatedDurationMinutes * videoPricing.PricePerMinuteCredits);

        // Calculate cost based on estimated duration
        return (audioCost, videoCost);
    }
}

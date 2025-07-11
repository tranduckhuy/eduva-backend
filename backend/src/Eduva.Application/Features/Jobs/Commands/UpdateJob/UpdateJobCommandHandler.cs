using Eduva.Application.Exceptions.Job;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Eduva.Application.Features.Jobs.Commands.UpdateJob;

public class UpdateJobCommand : IRequest<CustomCode>
{
    public Guid Id { get; set; }
    public JobStatus? Status { get; set; }
    public string? ContentBlobName { get; set; }
    public string? ProductBlobName { get; set; }
    public int? WordCount { get; set; }
    public string? FailureReason { get; set; }
}

public class UpdateJobCommandHandler : IRequestHandler<UpdateJobCommand, CustomCode>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJobNotificationService _notificationService;
    private readonly ILogger<UpdateJobCommandHandler> _logger;

    public UpdateJobCommandHandler(
        IUnitOfWork unitOfWork,
        IJobNotificationService notificationService,
        ILogger<UpdateJobCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<CustomCode> Handle(UpdateJobCommand request, CancellationToken cancellationToken)
    {
        var jobRepository = _unitOfWork.GetCustomRepository<IJobRepository>();

        var job = await jobRepository.GetByIdAsync(request.Id, cancellationToken) ?? throw new JobNotFoundException();

        // Update job properties
        var updated = false;

        if (request.Status.HasValue)
        {
            job.JobStatus = request.Status.Value;
            updated = true;
        }

        if (!string.IsNullOrEmpty(request.ContentBlobName))
        {
            job.ContentBlobName = request.ContentBlobName;
            updated = true;
        }

        if (!string.IsNullOrEmpty(request.ProductBlobName))
        {
            job.ProductBlobName = request.ProductBlobName;
            updated = true;
        }

        if (request.WordCount.HasValue)
        {
            job.WordCount = request.WordCount.Value;
            updated = true;
        }

        if (!string.IsNullOrEmpty(request.FailureReason))
        {
            job.FailureReason = request.FailureReason;
            updated = true;
        }

        if (updated)
        {
            _unitOfWork.GetRepository<Job, Guid>().Update(job);
            await _unitOfWork.CommitAsync();

            // Send real-time update via SignalR
            var statusData = new
            {
                JobId = job.Id,
                Status = job.JobStatus,
                ContentBlobName = job.ContentBlobName,
                ProductBlobName = job.ProductBlobName,
                WordCount = job.WordCount,
                FailureReason = job.FailureReason,
                LastModifiedAt = job.LastModifiedAt
            };

            await _notificationService.NotifyUserAsync(job.UserId, "JobStatusUpdated", statusData, cancellationToken);

            _logger.LogInformation("Job {JobId} updated successfully. New status: {Status}", job.Id, job.JobStatus);
        }

        return CustomCode.Updated;
    }
}

using Eduva.Application.Exceptions.Job;
using Eduva.Application.Features.Jobs.Commands.ConfirmJob;
using Eduva.Application.Features.Jobs.DTOs;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Eduva.Application.Features.Jobs.Services
{
    public class JobConfirmationService : IJobConfirmationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRabbitMQService _rabbitMQService;
        private readonly IJobNotificationService _notificationService;
        private readonly ILogger<JobConfirmationService> _logger;

        private const int WORDS_PER_MINUTE = 250;

        public JobConfirmationService(
            IUnitOfWork unitOfWork,
            IRabbitMQService rabbitMQService,
            IJobNotificationService notificationService,
            ILogger<JobConfirmationService> logger)
        {
            _unitOfWork = unitOfWork;
            _rabbitMQService = rabbitMQService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task ConfirmJobAsync(ConfirmJobCommand request, CancellationToken cancellationToken)
        {
            var jobRepo = _unitOfWork.GetCustomRepository<IJobRepository>();
            var job = await jobRepo.GetByIdAsync(request.Id, cancellationToken) ?? throw new JobNotFoundException();

            if (job.JobStatus != JobStatus.ContentGenerated && job.JobStatus == JobStatus.Processing)
            {
                throw new JobContentNotGeneratedException([
                    $"Job {job.Id} is not in a state that can be confirmed. Current status: {job.JobStatus}."
                ]);
            }

            job.JobStatus = JobStatus.CreatingProduct;
            job.LastModifiedAt = DateTimeOffset.UtcNow;
            _unitOfWork.GetRepository<Job, Guid>().Update(job);

            await ChargeUserCreditsAsync(job, request.Type, cancellationToken);
            await _unitOfWork.CommitAsync();

            await PublishCreateProductMessage(job, request);
            await NotifyUserAsync(job, cancellationToken);

            _logger.LogInformation("Job {JobId} confirmed successfully. Status updated to {Status}", job.Id, job.JobStatus);
        }

        private async Task ChargeUserCreditsAsync(Job job, AIServiceType type, CancellationToken cancellationToken)
        {
            var userRepo = _unitOfWork.GetCustomRepository<IUserRepository>();

            var creditsToCharge = type == AIServiceType.GenAudio ? job.AudioCost : job.VideoCost;

            await userRepo.UpdateCreditBalanceAsync(job.UserId, -creditsToCharge, cancellationToken);

            var aiUsageLogRepository = _unitOfWork.GetRepository<AIUsageLog, Guid>();
            var usage = new AIUsageLog
            {
                UserId = job.UserId,
                AIServiceType = type,
                DurationMinutes = job.WordCount.HasValue ? (decimal)job.WordCount / WORDS_PER_MINUTE : 0,
                CreditsCharged = creditsToCharge,
            };

            await aiUsageLogRepository.AddAsync(usage);
        }

        private async Task PublishCreateProductMessage(Job job, ConfirmJobCommand request)
        {
            var message = new CreateProductMessage
            {
                JobId = job.Id,
                JobType = request.Type,
                TaskType = TaskType.CreateProduct,
                ContentBlobName = job.ContentBlobName ?? string.Empty,
                VoiceConfig = request.VoiceConfig
            };

            await _rabbitMQService.PublishAsync(message, TaskType.CreateProduct);
        }

        private async Task NotifyUserAsync(Job job, CancellationToken cancellationToken)
        {
            var statusData = new
            {
                JobId = job.Id,
                Status = job.JobStatus,
                job.LastModifiedAt
            };

            await _notificationService.NotifyUserAsync(job.UserId, "JobStatusUpdated", statusData, cancellationToken);
        }
    }
}

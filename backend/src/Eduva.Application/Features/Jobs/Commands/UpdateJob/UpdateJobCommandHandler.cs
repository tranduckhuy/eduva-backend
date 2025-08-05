using Eduva.Application.Exceptions.Auth;
using Eduva.Application.Exceptions.Job;
using Eduva.Application.Features.Jobs.DTOs;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Eduva.Application.Features.Jobs.Commands.UpdateJob;

public class UpdateJobCommand : IRequest<Unit>
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public List<IFormFile>? File { get; set; }
    public string Topic { get; set; } = string.Empty;
}

public class UpdateJobCommandHandler : IRequestHandler<UpdateJobCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateJobCommandHandler> _logger;
    private readonly IStorageService _storageService;
    private readonly IRabbitMQService _rabbitMQService;

    public UpdateJobCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<UpdateJobCommandHandler> logger,
        IStorageService storageService,
        IRabbitMQService rabbitMQService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _storageService = storageService;
        _rabbitMQService = rabbitMQService;
    }

    public async Task<Unit> Handle(UpdateJobCommand request, CancellationToken cancellationToken)
    {
        var jobRepository = _unitOfWork.GetCustomRepository<IJobRepository>();

        var job = await jobRepository.GetByIdAsync(request.Id, cancellationToken) ?? throw new JobNotFoundException();

        if (job.UserId != request.UserId)
        {
            throw new ForbiddenException(["You do not have permission to update this job. It does not belong to your account."]);
        }

        // Check if files are provided, if so, remove old files and upload new ones
        if (request.File != null && request.File.Count > 0)
        {
            // Delete old files
            if (job.SourceBlobNames != null && job.SourceBlobNames.Count > 0)
            {
                var deletedBlobNames = job.SourceBlobNames.ToList();

                if (!string.IsNullOrEmpty(job.ContentBlobName))
                {
                    deletedBlobNames.Add(job.ContentBlobName);
                }

                await _storageService.DeleteRangeTempFileAsync(deletedBlobNames);
                job.SourceBlobNames = [];
                job.ContentBlobName = null;
            }

            var uploadTasks = request.File.Select(async file =>
            {
                var fileExtension = Path.GetExtension(file.FileName);
                var blobName = $"jobs/input/{Guid.NewGuid()}{fileExtension}";
                await _storageService.UploadFileToTempContainerAsync(file, blobName);
                return blobName;
            });

            var blobNames = await Task.WhenAll(uploadTasks);

            job.SourceBlobNames = blobNames.ToList();
        }

        // Update job topic
        if (!string.IsNullOrWhiteSpace(request.Topic))
        {
            job.Topic = request.Topic;
        }

        // Save changes to the database
        jobRepository.Update(job);
        await _unitOfWork.CommitAsync();

        // Create and send message to AI worker for content generation
        var generateContentMessage = new GenerateContentMessage
        {
            JobId = job.Id,
            TaskType = TaskType.GenerateContent,
            Topic = request.Topic,
            SourceBlobNames = job.SourceBlobNames.ToList()
        };

        // Publish message to RabbitMQ
        await _rabbitMQService.PublishAsync(generateContentMessage, TaskType.GenerateContent);

        _logger.LogInformation("Job {JobId} created successfully for content generation with topic '{Topic}'",
            job.Id, request.Topic);

        return Unit.Value;
    }
}

using Eduva.Application.Features.Jobs.DTOs;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Eduva.Application.Features.Jobs.Commands.CreateJob;

public class CreateJobCommand : IRequest<CreateJobResponse>
{
    public Guid UserId { get; set; }
    public List<IFormFile> File { get; set; } = [];
    public string Topic { get; set; } = string.Empty;
}


public class CreateJobCommandHandler : IRequestHandler<CreateJobCommand, CreateJobResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStorageService _storageService;
    private readonly IRabbitMQService _rabbitMQService;
    private readonly ILogger<CreateJobCommandHandler> _logger;

    public CreateJobCommandHandler(
        IUnitOfWork unitOfWork,
        IStorageService storageService,
        IRabbitMQService rabbitMQService,
        ILogger<CreateJobCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _storageService = storageService;
        _rabbitMQService = rabbitMQService;
        _logger = logger;
    }

    public async Task<CreateJobResponse> Handle(CreateJobCommand request, CancellationToken cancellationToken)
    {
        // Upload files to temporary container
        var uploadTasks = request.File.Select(async file =>
        {
            var fileExtension = Path.GetExtension(file.FileName);
            var blobName = $"jobs/input/{Guid.NewGuid()}{fileExtension}";
            await _storageService.UploadFileToTempContainerAsync(file, blobName);
            return blobName;
        });

        var sourceBlobNames = await Task.WhenAll(uploadTasks);

        // Create job entity (Type will be set later when user confirms)
        var job = new Job
        {
            JobStatus = JobStatus.Processing,
            Topic = request.Topic,
            SourceBlobNames = sourceBlobNames.ToList(),
            UserId = request.UserId
        };

        // Save job to database
        await _unitOfWork.GetRepository<Job, Guid>().AddAsync(job);
        await _unitOfWork.CommitAsync();

        // Create and send message to AI worker for content generation
        var generateContentMessage = new GenerateContentMessage
        {
            JobId = job.Id,
            TaskType = TaskType.GenerateContent,
            Topic = request.Topic,
            SourceBlobNames = sourceBlobNames.ToList()
        };

        // Publish message to RabbitMQ
        await _rabbitMQService.PublishAsync(generateContentMessage);

        _logger.LogInformation("Job {JobId} created successfully for content generation with topic '{Topic}'",
            job.Id, request.Topic);

        return new CreateJobResponse
        {
            JobId = job.Id,
            Status = job.JobStatus
        };
    }
}
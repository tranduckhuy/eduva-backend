using Eduva.API.Attributes;
using Eduva.API.Controllers.Base;
using Eduva.Application.Features.Jobs.Commands.ConfirmJob;
using Eduva.Application.Features.Jobs.Commands.CreateJob;
using Eduva.Application.Features.Jobs.Commands.UpdateJobProgress;
using Eduva.Application.Features.Jobs.DTOs;
using Eduva.Application.Features.Jobs.Queries.GetJob;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eduva.API.Controllers.Jobs;

[Route("api/ai-jobs")]
public class AIJobsController : BaseController<AIJobsController>
{
    private readonly IMediator _mediator;

    public AIJobsController(IMediator mediator, ILogger<AIJobsController> logger) : base(logger)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Create a new job for content generation
    /// </summary>
    /// <param name="request">Job creation request with files and topic</param>
    /// <returns>Job creation response with jobId</returns>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateJob([FromForm] CreateJobRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userId, out var id))
        {
            return Respond(CustomCode.UserIdNotFound);
        }

        var command = new CreateJobCommand
        {
            UserId = id,
            File = request.File,
            Topic = request.Topic
        };

        return await HandleRequestAsync(async () =>
        {
            var response = await _mediator.Send(command);
            return (CustomCode.Success, response);
        });
    }

    /// <summary>
    /// Update job progress (Webhook endpoint for AI worker)
    /// </summary>
    /// <param name="id">Job ID</param>
    /// <param name="request">Update request with progress data</param>
    /// <returns>Success or error response</returns>
    [HttpPut("{id:guid}/progress")]
    [ApiKeyAuth]
    public async Task<IActionResult> UpdateJobProgress(Guid id, [FromBody] UpdateJobProgressRequest request)
    {
        var command = new UpdateJobProgressCommand
        {
            JobId = id,
            JobStatus = request.JobStatus,
            ContentBlobName = request.ContentBlobName,
            ProductBlobName = request.ProductBlobName,
            WordCount = request.WordCount,
            PreviewContent = request.PreviewContent,
            FailureReason = request.FailureReason
        };

        return await HandleRequestAsync(async () =>
        {
            await _mediator.Send(command);
        });
    }

    /// <summary>
    /// Confirm job and trigger product creation
    /// </summary>
    /// <param name="id">Job ID</param>
    /// <param name="request">Confirmation request with voice config</param>
    /// <returns>Success or error response</returns>
    [HttpPost("{id:guid}/confirm")]
    [Authorize]
    public async Task<IActionResult> ConfirmJob(Guid id, [FromBody] ConfirmJobRequest request)
    {
        var command = new ConfirmJobCommand
        {
            Id = id,
            Type = request.Type,
            VoiceConfig = request.VoiceConfig
        };

        return await HandleRequestAsync(async () =>
        {
            await _mediator.Send(command);
        });
    }

    /// <summary>
    /// Get job by ID
    /// </summary>
    /// <param name="id">Job ID</param>
    /// <returns>Job details</returns>
    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetJob(Guid id)
    {
        var query = new GetJobQuery { Id = id };

        return await HandleRequestAsync(async () =>
        {
            var response = await _mediator.Send(query);
            return (CustomCode.Success, response);
        });
    }
}

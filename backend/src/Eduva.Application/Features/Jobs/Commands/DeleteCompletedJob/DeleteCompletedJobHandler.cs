using Eduva.Application.Exceptions.Auth;
using Eduva.Application.Exceptions.Job;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Eduva.Application.Features.Jobs.Commands.DeleteCompletedJob
{
    public class DeleteCompletedJobHandler : IRequestHandler<DeleteCompletedJobCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteCompletedJobHandler> _logger;

        public DeleteCompletedJobHandler(IUnitOfWork unitOfWork, ILogger<DeleteCompletedJobHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Unit> Handle(DeleteCompletedJobCommand request, CancellationToken cancellationToken)
        {
            var jobRepository = _unitOfWork.GetRepository<Domain.Entities.Job, Guid>();
            var job = await jobRepository.GetByIdAsync(request.JobId)
                ?? throw new JobNotFoundException();

            if (job.UserId != request.UserId)
            {
                throw new ForbiddenException(["You are not authorized to delete this job because it does not belong to you."]);
            }

            if (request.Permanent)
            {
                jobRepository.Remove(job);
                _logger.LogInformation("Deleting job {JobId} permanently.", job.Id);
            }
            else
            {
                job.Status = EntityStatus.Deleted;
                jobRepository.Update(job);
            }

            await _unitOfWork.CommitAsync();
            return Unit.Value;
        }
    }
}

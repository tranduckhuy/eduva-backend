using Eduva.Application.Common.Mappings;
using Eduva.Application.Exceptions.Job;
using Eduva.Application.Features.Jobs.DTOs;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using MediatR;

namespace Eduva.Application.Features.Jobs.Queries.GetJob;

public class GetJobQuery : IRequest<JobResponse>
{
    public Guid Id { get; set; }
}

public class GetJobQueryHandler : IRequestHandler<GetJobQuery, JobResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStorageService _storageService;

    public GetJobQueryHandler(IUnitOfWork unitOfWork, IStorageService storageService)
    {
        _unitOfWork = unitOfWork;
        _storageService = storageService;
    }

    public async Task<JobResponse> Handle(GetJobQuery request, CancellationToken cancellationToken)
    {
        var jobRepository = _unitOfWork.GetRepository<Job, Guid>();
        var job = await jobRepository.GetByIdAsync(request.Id) ?? throw new JobNotFoundException();

        if (job.ProductBlobName != null)
        {
            job.ProductBlobName = _storageService.GetReadableUrl(job.ProductBlobName);
        }

        var response = AppMapper<AppMappingProfile>.Mapper.Map<JobResponse>(job);
        return response;
    }
}

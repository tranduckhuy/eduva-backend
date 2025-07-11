using Eduva.Application.Common.Mappings;
using Eduva.Application.Exceptions.Job;
using Eduva.Application.Features.Jobs.DTOs;
using Eduva.Application.Interfaces;
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

    public GetJobQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<JobResponse> Handle(GetJobQuery request, CancellationToken cancellationToken)
    {
        var jobRepository = _unitOfWork.GetRepository<Job, Guid>();
        var job = await jobRepository.GetByIdAsync(request.Id) ?? throw new JobNotFoundException();

        var response = AppMapper<AppMappingProfile>.Mapper.Map<JobResponse>(job);
        return response;
    }
}

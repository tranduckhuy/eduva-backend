using Eduva.Application.Common.Mappings;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.Jobs.DTOs;
using Eduva.Application.Features.Jobs.Specifications;
using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using MediatR;

namespace Eduva.Application.Features.Jobs.Queries.GetCompletedJobs
{
    public class GetCompletedJobsHandler : IRequestHandler<GetCompletedJobsQuery, Pagination<JobResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetCompletedJobsHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Pagination<JobResponse>> Handle(GetCompletedJobsQuery request, CancellationToken cancellationToken)
        {
            var spec = new GetJobsSpecification(request.SpecParam, request.UserId);

            var repository = _unitOfWork.GetRepository<Job, Guid>();

            var jobs = await repository.GetWithSpecAsync(spec);

            return AppMapper<AppMappingProfile>.Mapper.Map<Pagination<JobResponse>>(jobs);
        }
    }
}

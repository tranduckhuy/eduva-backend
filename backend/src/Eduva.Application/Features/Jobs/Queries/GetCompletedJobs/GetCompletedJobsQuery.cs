using Eduva.Application.Common.Models;
using Eduva.Application.Features.Jobs.DTOs;
using Eduva.Application.Features.Jobs.Specifications;
using MediatR;

namespace Eduva.Application.Features.Jobs.Queries.GetCompletedJobs
{
    public record GetCompletedJobsQuery(
        JobSpecParam SpecParam,
        Guid UserId
    ) : IRequest<Pagination<JobResponse>>;
}

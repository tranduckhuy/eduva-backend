using Eduva.Application.Common.Models;
using Eduva.Application.Features.SchoolSubscriptions.Responses;
using Eduva.Application.Features.SchoolSubscriptions.Specifications;
using MediatR;

namespace Eduva.Application.Features.SchoolSubscriptions.Queries
{
    public record GetSchoolSubscriptionQuery(SchoolSubscriptionSpecParam Param) : IRequest<Pagination<SchoolSubscriptionResponse>>;
}
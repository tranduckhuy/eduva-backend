using Eduva.Application.Common.Models;
using Eduva.Application.Features.Schools.Responses;
using Eduva.Application.Features.Schools.Specifications;
using MediatR;

namespace Eduva.Application.Features.Schools.Queries
{
    public record GetSchoolQuery(SchoolSpecParam Param) : IRequest<Pagination<SchoolResponse>>;
}
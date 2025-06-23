using Eduva.Application.Features.Schools.Responses;
using MediatR;

namespace Eduva.Application.Features.Schools.Queries
{
    public record GetMySchoolQuery(Guid SchoolAdminId) : IRequest<SchoolResponse>;
}
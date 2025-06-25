using Eduva.Application.Common.Models;
using Eduva.Application.Features.Classes.Responses;
using Eduva.Application.Features.Classes.Specifications;
using MediatR;

namespace Eduva.Application.Features.Classes.Queries.GetClasses
{
    public record GetClassesQuery(ClassSpecParam ClassSpecParam, Guid UserId)
        : IRequest<Pagination<ClassResponse>>;
}

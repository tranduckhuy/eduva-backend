using Eduva.Application.Common.Models;
using Eduva.Application.Features.LessonMaterials.Responses;
using Eduva.Application.Features.LessonMaterials.Specifications;
using MediatR;

namespace Eduva.Application.Features.LessonMaterials.Queries
{
    public record GetSchoolPublicLessonMaterialsQuery(
        LessonMaterialSpecParam LessonMaterialSpecParam,
        Guid UserId
    )
    : IRequest<Pagination<LessonMaterialResponse>>;
}

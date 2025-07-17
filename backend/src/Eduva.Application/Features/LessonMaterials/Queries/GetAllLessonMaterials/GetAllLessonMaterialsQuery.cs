using Eduva.Application.Features.LessonMaterials.Responses;
using MediatR;

namespace Eduva.Application.Features.LessonMaterials.Queries.GetAllLessonMaterials
{
    public record GetAllLessonMaterialsQuery(Guid UserId, bool IsStudent, int? SchoolId = null, Guid? ClassId = null, Guid? FolderId = null) : IRequest<IReadOnlyList<LessonMaterialResponse>>;
}
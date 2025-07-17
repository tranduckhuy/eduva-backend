using Eduva.Application.Features.LessonMaterials.DTOs;
using Eduva.Application.Features.LessonMaterials.Responses;
using MediatR;

namespace Eduva.Application.Features.LessonMaterials.Queries.GetLessonMaterialsByFolder
{
    public record GetLessonMaterialsByFolderQuery(
        Guid FolderId,
        Guid UserId,
        int SchoolId,
        IList<string> UserRoles,
        LessonMaterialFilterOptions? FilterOptions = null
    ) : IRequest<IReadOnlyList<LessonMaterialResponse>>;
}

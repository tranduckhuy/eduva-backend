using Eduva.Application.Features.LessonMaterials.Responses;
using Eduva.Domain.Enums;
using MediatR;

namespace Eduva.Application.Features.LessonMaterials.Queries.GetFoldersWithLessonMaterialsByClassId
{
    public record GetFoldersWithLessonMaterialsByClassIdQuery
    (
        Guid ClassId,
        int SchoolId,
        Guid UserId,
        IList<string> UserRoles,
        LessonMaterialStatus? LessonStatus,
        EntityStatus? Status
    ) : IRequest<IReadOnlyList<FolderWithLessonMaterialsResponse>>;
}
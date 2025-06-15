using Eduva.Domain.Enums;

namespace Eduva.Application.Features.LessonMaterials.Responses
{
    public record LessonMaterialResponse(
        int Id,
        int? SchoolId,
        string Title,
        string? Description,
        ContentType ContentType,
        string? Tag,
        LessonMaterialStatus LessonStatus,
        int Duration,
        bool IsAIContent,
        string SourceUrl,
        LessonMaterialVisibility Visibility,
        DateTimeOffset CreatedAt,
        DateTimeOffset? LastModifiedAt,
        EntityStatus Status
    );
}

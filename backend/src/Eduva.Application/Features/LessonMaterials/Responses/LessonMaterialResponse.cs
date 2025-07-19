using Eduva.Domain.Enums;

namespace Eduva.Application.Features.LessonMaterials.Responses
{
    public class LessonMaterialResponse
    {
        public Guid Id { get; set; }
        public int? SchoolId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public ContentType ContentType { get; set; }
        public LessonMaterialStatus LessonStatus { get; set; }
        public int Duration { get; set; }
        public int FileSize { get; set; } // Size in bytes
        public bool IsAIContent { get; set; }
        public string SourceUrl { get; set; } = string.Empty;
        public LessonMaterialVisibility Visibility { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? LastModifiedAt { get; set; }
        public EntityStatus Status { get; set; }
        public Guid CreatedById { get; set; }
        public string? CreatedByName { get; set; }
    }
}
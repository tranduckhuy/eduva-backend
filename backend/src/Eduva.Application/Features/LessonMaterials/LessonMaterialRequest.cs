using Eduva.Domain.Enums;

namespace Eduva.Application.Features.LessonMaterials
{
    public class LessonMaterialRequest
    {
        public string? Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public ContentType ContentType { get; set; }
        public int? Duration { get; set; }
        public int FileSize { get; set; } // Size in bytes
        public bool IsAIContent { get; set; }
        public string SourceUrl { get; set; } = string.Empty;
    }
}

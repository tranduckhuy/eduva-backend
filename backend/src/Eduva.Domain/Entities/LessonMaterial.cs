using Eduva.Domain.Common;
using Eduva.Domain.Enums;

namespace Eduva.Domain.Entities
{
    public class LessonMaterial : BaseTimestampedEntity<Guid>
    {
        public int? SchoolId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; } = string.Empty;
        public ContentType ContentType { get; set; }
        public string? Tag { get; set; } = string.Empty;
        public LessonMaterialStatus LessonStatus { get; set; }
        public int? Duration { get; set; }
        public int FileSize { get; set; } // Size in bytes
        public bool IsAIContent { get; set; }
        public string SourceUrl { get; set; } = string.Empty;
        public LessonMaterialVisibility Visibility { get; set; }
        public Guid CreatedByUserId { get; set; } // User ID of the creator

        // Navigation Properties
        public virtual ApplicationUser CreatedByUser { get; set; } = default!;
        public virtual School? School { get; set; } // Nullable
        public virtual ICollection<FolderLessonMaterial> FolderLessonMaterials { get; set; } = [];
        public virtual ICollection<LessonMaterialApproval> LessonMaterialApprovals { get; set; } = [];
        public virtual ICollection<LessonMaterialQuestion> LessonMaterialQuestions { get; set; } = [];
    }
}
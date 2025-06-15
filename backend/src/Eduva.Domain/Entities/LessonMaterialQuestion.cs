using Eduva.Domain.Common;

namespace Eduva.Domain.Entities
{
    public class LessonMaterialQuestion : BaseTimestampedEntity<int>
    {
        public int LessonMaterialId { get; set; }
        public string Title { get; set; } = default!;
        public string Content { get; set; } = default!;
        public Guid CreatedBy { get; set; } // User ID of the creator

        // Navigation properties
        public virtual ApplicationUser CreatedByUser { get; set; } = default!;
        public virtual LessonMaterial LessonMaterial { get; set; } = default!;
        public virtual ICollection<QuestionComment> Comments { get; set; } = [];
    }
}
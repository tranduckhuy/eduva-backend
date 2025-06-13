using Eduva.Domain.Common;

namespace Eduva.Domain.Entities
{
    public class QuestionComment : BaseTimestampedEntity<int>
    {
        public int QuestionID { get; set; }
        public string Content { get; set; } = default!;
        public int? ParentCommentId { get; set; }
        public Guid CreatedBy { get; set; } // User ID of the creator

        // Navigation properties
        public virtual ApplicationUser CreatedByUser { get; set; } = default!;
        public virtual LessonMaterialQuestion Question { get; set; } = default!;
        public virtual QuestionComment? ParentComment { get; set; }
        public virtual ICollection<QuestionComment> Replies { get; set; } = [];
    }
}

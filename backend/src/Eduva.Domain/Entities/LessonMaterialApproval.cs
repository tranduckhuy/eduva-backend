using Eduva.Domain.Common;
using Eduva.Domain.Enums;

namespace Eduva.Domain.Entities
{
    public class LessonMaterialApproval : BaseEntity<int>
    {
        public int LessonMaterialId { get; set; }
        public Guid ApproverId { get; set; }
        public LessonMaterialStatus StatusChangeTo { get; set; }
        public string? RequesterNote { get; set; }
        public string? Feedback {  get; set; }

        // Navigation properties
        public virtual User Approver { get; set; } = default!;
        public virtual LessonMaterial LessonMaterial { get; set; } = default!;
    }
}

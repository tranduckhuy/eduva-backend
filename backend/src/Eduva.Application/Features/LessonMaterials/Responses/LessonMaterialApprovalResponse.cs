using Eduva.Domain.Enums;

namespace Eduva.Application.Features.LessonMaterials.Responses
{
    public class LessonMaterialApprovalResponse
    {
        public Guid Id { get; set; }
        public Guid LessonMaterialId { get; set; }
        public string LessonMaterialTitle { get; set; } = string.Empty;
        public Guid ApproverId { get; set; }
        public string ApproverName { get; set; } = string.Empty;
        public string ApproverAvatarUrl { get; set; } = string.Empty;
        public LessonMaterialStatus StatusChangeTo { get; set; }
        public string? Feedback { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        // Additional useful information
        public string CreatorName { get; set; } = string.Empty;
        public int? SchoolId { get; set; }
        public string SchoolName { get; set; } = string.Empty;
    }
}

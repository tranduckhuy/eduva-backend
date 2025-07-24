using Eduva.Domain.Enums;

namespace Eduva.Application.Common.Models.Notifications
{
    public class LessonMaterialApprovalNotification
    {
        public Guid LessonMaterialId { get; set; }
        public string LessonMaterialTitle { get; set; } = string.Empty;
        public LessonMaterialStatus Status { get; set; }
        public string? Feedback { get; set; }
        public DateTimeOffset ApprovedAt { get; set; }
        public Guid PerformedByUserId { get; set; }
        public string? PerformedByName { get; set; }
        public string? PerformedByAvatar { get; set; }
    }
}
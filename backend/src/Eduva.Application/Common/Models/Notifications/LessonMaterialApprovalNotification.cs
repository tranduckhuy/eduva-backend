namespace Eduva.Application.Common.Models.Notifications
{
    public class LessonMaterialApprovalNotification : BaseNotification
    {
        public Guid LessonMaterialId { get; set; }
        public string LessonMaterialTitle { get; set; } = string.Empty;
        public DateTimeOffset ApprovedAt { get; set; }
    }
}
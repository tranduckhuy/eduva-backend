using Eduva.Domain.Enums;

namespace Eduva.Application.Common.Models.Notifications
{
    public class QuestionCommentNotification
    {
        public Guid NotificationId { get; set; }
        public Guid CommentId { get; set; }
        public Guid QuestionId { get; set; }
        public string? Title { get; set; }
        public Guid LessonMaterialId { get; set; }
        public string? LessonMaterialTitle { get; set; }
        public string Content { get; set; } = default!;
        public DateTimeOffset CreatedAt { get; set; }
        public Guid CreatedByUserId { get; set; }
        public Guid? PerformedByUserId { get; set; }
        public string? PerformedByName { get; set; }
        public string? PerformedByAvatar { get; set; }
        public string? CreatedByName { get; set; }
        public string? CreatedByAvatar { get; set; }
        public string? CreatedByRole { get; set; }
        public Guid? ParentCommentId { get; set; }
        public bool IsReply { get; set; }
        public QuestionActionType ActionType { get; set; }
    }
}
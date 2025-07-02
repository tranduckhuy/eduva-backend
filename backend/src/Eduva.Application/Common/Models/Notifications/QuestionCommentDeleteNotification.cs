using Eduva.Domain.Enums;

namespace Eduva.Application.Common.Models.Notifications
{
    public class QuestionCommentDeleteNotification
    {
        public Guid CommentId { get; set; }
        public Guid QuestionId { get; set; }
        public Guid LessonMaterialId { get; set; }
        public DateTimeOffset DeletedAt { get; set; }
        public int DeletedRepliesCount { get; set; }
        public QuestionActionType ActionType { get; set; }
    }
}
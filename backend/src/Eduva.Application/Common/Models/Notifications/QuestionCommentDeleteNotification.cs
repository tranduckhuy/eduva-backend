using Eduva.Domain.Enums;

namespace Eduva.Application.Common.Models.Notifications
{
    public class QuestionCommentDeleteNotification
    {
        public Guid CommentId { get; set; }
        public Guid QuestionId { get; set; }
        public string? Title { get; set; }
        public Guid LessonMaterialId { get; set; }
        public string? LessonMaterialTitle { get; set; }
        public DateTimeOffset DeletedAt { get; set; }
        public int DeletedRepliesCount { get; set; }
        public Guid CreatedByUserId { get; set; }
        public string? CreatedByName { get; set; }
        public string? CreatedByAvatar { get; set; }
        public string? CreatedByRole { get; set; }
        public QuestionActionType ActionType { get; set; }
    }
}
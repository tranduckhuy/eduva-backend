using Eduva.Domain.Enums;

namespace Eduva.Application.Common.Models.Notifications
{
    public class QuestionNotification
    {
        public Guid QuestionId { get; set; }
        public Guid LessonMaterialId { get; set; }
        public string? Title { get; set; }
        public string? LessonMaterialTitle { get; set; }
        public string? Content { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? LastModifiedAt { get; set; }
        public Guid CreatedByUserId { get; set; }
        public Guid? ExecutorByUserId { get; set; }
        public string? CreatedByName { get; set; }
        public string? CreatedByAvatar { get; set; }
        public string? CreatedByRole { get; set; }
        public int CommentCount { get; set; }
        public QuestionActionType ActionType { get; set; }
    }

    public class QuestionDeleteNotification
    {
        public Guid QuestionId { get; set; }
        public string? Title { get; set; }
        public Guid LessonMaterialId { get; set; }
        public string? LessonMaterialTitle { get; set; }
        public DateTimeOffset DeletedAt { get; set; }
        public Guid CreatedByUserId { get; set; }
        public Guid ExecutorByUserId { get; set; }
        public string? CreatedByName { get; set; }
        public string? CreatedByAvatar { get; set; }
        public string? CreatedByRole { get; set; }
        public QuestionActionType ActionType { get; set; }
    }
}
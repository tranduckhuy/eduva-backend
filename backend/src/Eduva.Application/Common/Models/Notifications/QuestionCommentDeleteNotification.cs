namespace Eduva.Application.Common.Models.Notifications
{
    public class QuestionCommentDeleteNotification : BaseCommentNotification
    {
        public DateTimeOffset DeletedAt { get; set; }
    }
}
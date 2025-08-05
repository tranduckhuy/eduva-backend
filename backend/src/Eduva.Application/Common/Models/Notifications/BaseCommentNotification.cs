namespace Eduva.Application.Common.Models.Notifications
{
    public abstract class BaseCommentNotification : BaseQuestionNotification
    {
        public Guid CommentId { get; set; }
    }
}
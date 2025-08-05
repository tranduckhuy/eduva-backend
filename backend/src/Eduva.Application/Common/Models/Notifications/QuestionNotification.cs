namespace Eduva.Application.Common.Models.Notifications
{
    public class QuestionNotification : BaseQuestionNotification
    {
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class QuestionDeleteNotification : BaseQuestionNotification
    {
        public DateTimeOffset DeletedAt { get; set; }
    }
}
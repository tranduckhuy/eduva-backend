namespace Eduva.Application.Common.Models.Notifications
{
    public abstract class BaseQuestionNotification : BaseNotification
    {
        public Guid QuestionId { get; set; }
        public Guid LessonMaterialId { get; set; }
        public string? Title { get; set; }
        public string? LessonMaterialTitle { get; set; }
    }
}
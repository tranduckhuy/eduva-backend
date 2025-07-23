using Eduva.Application.Features.Questions.Responses;

namespace Eduva.Application.Interfaces.Services
{
    public interface IHubNotificationService
    {
        Task NotifyQuestionCreatedAsync(QuestionResponse question, Guid lessonMaterialId, Guid? executorUserId = null);
        Task NotifyQuestionUpdatedAsync(QuestionResponse question, Guid lessonMaterialId, Guid? executorUserId = null);
        Task NotifyQuestionDeletedAsync(QuestionResponse question, Guid lessonMaterialId, Guid? executorUserId = null);

        Task NotifyQuestionCommentedAsync(QuestionCommentResponse comment, Guid lessonMaterialId, string title, string lessonMaterialTitle, Guid? executorUserId = null);
        Task NotifyQuestionCommentUpdatedAsync(QuestionCommentResponse comment, Guid lessonMaterialId, string title, string lessonMaterialTitle, Guid? executorUserId = null);
        Task NotifyQuestionCommentDeletedAsync(QuestionCommentResponse comment, Guid lessonMaterialId, string title, string lessonMaterialTitle, int deletedRepliesCount = 0, Guid? executorUserId = null);

    }
}
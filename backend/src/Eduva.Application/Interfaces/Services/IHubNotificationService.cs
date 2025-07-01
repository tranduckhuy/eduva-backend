using Eduva.Application.Features.Questions.Responses;

namespace Eduva.Application.Interfaces.Services
{
    public interface IHubNotificationService
    {
        Task NotifyQuestionCreatedAsync(QuestionResponse question, Guid lessonMaterialId);
        Task NotifyQuestionUpdatedAsync(QuestionResponse question, Guid lessonMaterialId);
        Task NotifyQuestionDeletedAsync(Guid questionId, Guid lessonMaterialId);
    }
}
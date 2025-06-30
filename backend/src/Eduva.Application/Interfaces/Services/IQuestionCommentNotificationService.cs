using Eduva.Application.Features.Questions.Responses;

namespace Eduva.Application.Interfaces.Services
{
    public interface IQuestionCommentNotificationService
    {
        Task NotifyQuestionCreatedAsync(CreateQuestionResponse question, Guid lessonMaterialId);
    }
}
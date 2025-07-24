using Eduva.Application.Features.Questions.Responses;
using Eduva.Domain.Entities;

namespace Eduva.Application.Interfaces.Services
{
    public interface IHubNotificationService
    {

        #region Question/Comment Notifications

        Task NotifyQuestionCreatedAsync(QuestionResponse question, Guid lessonMaterialId, ApplicationUser? user = null);
        Task NotifyQuestionUpdatedAsync(QuestionResponse question, Guid lessonMaterialId, ApplicationUser? user = null);
        Task NotifyQuestionDeletedAsync(QuestionResponse question, Guid lessonMaterialId, ApplicationUser? user = null, List<Guid>? targetUserIds = null);

        Task NotifyQuestionCommentedAsync(QuestionCommentResponse comment, Guid lessonMaterialId, string title, string lessonMaterialTitle, ApplicationUser? user = null);
        Task NotifyQuestionCommentUpdatedAsync(QuestionCommentResponse comment, Guid lessonMaterialId, string title, string lessonMaterialTitle, ApplicationUser? user = null);
        Task NotifyQuestionCommentDeletedAsync(QuestionCommentResponse comment, Guid lessonMaterialId, string title, string lessonMaterialTitle, int deletedRepliesCount = 0, ApplicationUser? user = null, List<Guid>? targetUserIds = null);

        #endregion

    }
}
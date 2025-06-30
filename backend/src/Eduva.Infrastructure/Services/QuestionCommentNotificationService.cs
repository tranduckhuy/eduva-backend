using Eduva.Application.Features.Questions.Responses;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Enums;
using Eduva.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Eduva.Infrastructure.Services
{
    public class QuestionCommentNotificationService : IQuestionCommentNotificationService
    {
        private readonly IHubContext<QuestionCommentHub> _hubContext;
        private readonly ILogger<QuestionCommentNotificationService> _logger;

        public QuestionCommentNotificationService(
            IHubContext<QuestionCommentHub> hubContext,
            ILogger<QuestionCommentNotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task NotifyQuestionCreatedAsync(QuestionResponse question, Guid lessonMaterialId)
        {
            await NotifyQuestionActionAsync(question, lessonMaterialId, QuestionActionType.Created);
        }

        public async Task NotifyQuestionUpdatedAsync(QuestionResponse question, Guid lessonMaterialId)
        {
            await NotifyQuestionActionAsync(question, lessonMaterialId, QuestionActionType.Updated);
        }

        public async Task NotifyQuestionDeletedAsync(Guid questionId, Guid lessonMaterialId)
        {
            await NotifyQuestionDeleteActionAsync(questionId, lessonMaterialId, QuestionActionType.Deleted);
        }

        private async Task NotifyQuestionActionAsync(QuestionResponse question, Guid lessonMaterialId, QuestionActionType actionType)
        {
            try
            {
                var groupName = $"Lesson_{lessonMaterialId}";
                var eventName = $"Question{actionType}";
                var actionDescription = actionType.ToString().ToLower();

                // Prepare notification data
                var notificationData = new
                {
                    questionId = question.Id,
                    lessonMaterialId,
                    title = question.Title,
                    content = question.Content,
                    createdAt = question.CreatedAt,
                    updatedAt = question.UpdatedAt,
                    createdByUserId = question.CreatedByUserId,
                    createdByName = question.CreatedByName,
                    createdByAvatar = question.CreatedByAvatar,
                    createdByRole = question.CreatedByRole,
                    commentCount = question.CommentCount,
                    actionType
                };

                _logger.LogInformation("[SignalR] Starting notification for question {ActionDescription}. " +
                    "QuestionId: {QuestionId}, LessonId: {LessonId}, GroupName: {GroupName}, " +
                    "Title: {Title}, CreatedBy: {CreatedBy}",
                    actionDescription, question.Id, lessonMaterialId, groupName, question.Title, question.CreatedByName);

                // Send to all users in the lesson group
                await _hubContext.Clients.Group(groupName)
                    .SendAsync(eventName, notificationData);

                _logger.LogInformation("[SignalR] Question {ActionDescription} notification sent successfully! " +
                    "Event: {EventName}, Group: {GroupName}, QuestionId: {QuestionId}",
                    actionDescription, eventName, groupName, question.Id);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SignalR] Failed to send question {ActionDescription} notification. " +
                    "QuestionId: {QuestionId}, LessonId: {LessonId}, Error: {ErrorMessage}",
                    actionType.ToString().ToLower(), question.Id, lessonMaterialId, ex.Message);
            }
        }

        private async Task NotifyQuestionDeleteActionAsync(Guid questionId, Guid lessonMaterialId, QuestionActionType actionType)
        {
            try
            {
                var groupName = $"Lesson_{lessonMaterialId}";
                var eventName = $"Question{actionType}";
                var actionDescription = actionType.ToString().ToLower();

                var notificationData = new
                {
                    questionId,
                    lessonMaterialId,
                    deletedAt = DateTimeOffset.UtcNow,
                    actionType
                };

                _logger.LogInformation("[SignalR] Starting notification for question {ActionDescription}. " +
                    "QuestionId: {QuestionId}, LessonId: {LessonId}, GroupName: {GroupName}",
                    actionDescription, questionId, lessonMaterialId, groupName);

                // Send to all users in the lesson group
                await _hubContext.Clients.Group(groupName)
                    .SendAsync(eventName, notificationData);

                _logger.LogInformation("[SignalR] Question {ActionDescription} notification sent successfully! " +
                    "Event: {EventName}, Group: {GroupName}, QuestionId: {QuestionId}",
                    actionDescription, eventName, groupName, questionId);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SignalR] Failed to send question {ActionDescription} notification. " +
                    "QuestionId: {QuestionId}, LessonId: {LessonId}, Error: {ErrorMessage}",
                    actionType.ToString().ToLower(), questionId, lessonMaterialId, ex.Message);
            }
        }
    }
}
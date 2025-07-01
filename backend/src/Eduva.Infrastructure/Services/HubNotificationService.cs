using Eduva.Application.Common.Models.Notifications;
using Eduva.Application.Contracts.Hubs;
using Eduva.Application.Features.Questions.Responses;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Eduva.Infrastructure.Services
{
    public class HubNotificationService : IHubNotificationService
    {
        private readonly INotificationHub _notificationHub;
        private readonly ILogger<HubNotificationService> _logger;

        public HubNotificationService(
            INotificationHub notificationHub,
            ILogger<HubNotificationService> logger)
        {
            _notificationHub = notificationHub;
            _logger = logger;
        }

        public async Task NotifyQuestionCreatedAsync(QuestionResponse question, Guid lessonMaterialId)
        {
            var notification = new QuestionNotification
            {
                QuestionId = question.Id,
                LessonMaterialId = lessonMaterialId,
                Title = question.Title,
                LessonMaterialTitle = question.LessonMaterialTitle,
                Content = question.Content,
                CreatedAt = question.CreatedAt,
                LastModifiedAt = question.LastModifiedAt,
                CreatedByUserId = question.CreatedByUserId,
                CreatedByName = question.CreatedByName,
                CreatedByAvatar = question.CreatedByAvatar,
                CreatedByRole = question.CreatedByRole,
                CommentCount = question.CommentCount,
                ActionType = QuestionActionType.Created
            };

            await SendNotificationAsync(notification, "QuestionCreated");
        }

        public async Task NotifyQuestionUpdatedAsync(QuestionResponse question, Guid lessonMaterialId)
        {
            var notification = new QuestionNotification
            {
                QuestionId = question.Id,
                LessonMaterialId = lessonMaterialId,
                Title = question.Title,
                LessonMaterialTitle = question.LessonMaterialTitle,
                Content = question.Content,
                CreatedAt = question.CreatedAt,
                LastModifiedAt = question.LastModifiedAt,
                CreatedByUserId = question.CreatedByUserId,
                CreatedByName = question.CreatedByName,
                CreatedByAvatar = question.CreatedByAvatar,
                CreatedByRole = question.CreatedByRole,
                CommentCount = question.CommentCount,
                ActionType = QuestionActionType.Updated
            };

            await SendNotificationAsync(notification, "QuestionUpdated");
        }

        public async Task NotifyQuestionDeletedAsync(Guid questionId, Guid lessonMaterialId)
        {
            var notification = new QuestionDeleteNotification
            {
                QuestionId = questionId,
                LessonMaterialId = lessonMaterialId,
                DeletedAt = DateTimeOffset.UtcNow,
                ActionType = QuestionActionType.Deleted
            };

            var groupName = $"Lesson_{lessonMaterialId}";

            try
            {
                _logger.LogInformation("[SignalR] Starting notification for question deleted. " +
                    "QuestionId: {QuestionId}, LessonId: {LessonId}, GroupName: {GroupName}",
                    questionId, lessonMaterialId, groupName);

                await _notificationHub.SendNotificationToGroupAsync(groupName, "QuestionDeleted", notification);

                _logger.LogInformation("[SignalR] Question deleted notification sent successfully! " +
                    "Event: QuestionDeleted, Group: {GroupName}, QuestionId: {QuestionId}",
                    groupName, questionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SignalR] Failed to send question deleted notification. " +
                    "QuestionId: {QuestionId}, LessonId: {LessonId}, Error: {ErrorMessage}",
                    questionId, lessonMaterialId, ex.Message);
            }
        }

        private async Task SendNotificationAsync(QuestionNotification notification, string eventName)
        {
            var groupName = $"Lesson_{notification.LessonMaterialId}";

            try
            {
                _logger.LogInformation("[SignalR] Starting notification for question {ActionType}. " +
                     "QuestionId: {QuestionId}, LessonId: {LessonId}, GroupName: {GroupName}, " +
                     "Title: {Title}, LessonTitle: {LessonTitle}, CreatedBy: {CreatedBy}",
                     notification.ActionType.ToString().ToLower(), notification.QuestionId,
                     notification.LessonMaterialId, groupName, notification.Title,
                     notification.LessonMaterialTitle, notification.CreatedByName);

                await _notificationHub.SendNotificationToGroupAsync(groupName, eventName, notification);

                _logger.LogInformation("[SignalR] Question {ActionType} notification sent successfully! " +
                    "Event: {EventName}, Group: {GroupName}, QuestionId: {QuestionId}, LessonTitle: {LessonTitle}",
                    notification.ActionType.ToString().ToLower(), eventName, groupName,
                    notification.QuestionId, notification.LessonMaterialTitle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SignalR] Failed to send question {ActionType} notification. " +
                    "QuestionId: {QuestionId}, LessonId: {LessonId}, Error: {ErrorMessage}",
                    notification.ActionType.ToString().ToLower(), notification.QuestionId,
                    notification.LessonMaterialId, ex.Message);
            }
        }
    }
}
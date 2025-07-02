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

        public async Task NotifyQuestionCommentedAsync(QuestionCommentResponse comment, Guid lessonMaterialId)
        {
            var notification = new QuestionCommentNotification
            {
                CommentId = comment.Id,
                QuestionId = comment.QuestionId,
                LessonMaterialId = lessonMaterialId,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                CreatedByUserId = comment.CreatedByUserId,
                CreatedByName = comment.CreatedByName,
                CreatedByAvatar = comment.CreatedByAvatar,
                CreatedByRole = comment.CreatedByRole,
                ParentCommentId = comment.ParentCommentId,
                IsReply = comment.ParentCommentId.HasValue,
                ActionType = QuestionActionType.Commented
            };

            await SendCommentNotificationAsync(notification, "QuestionCommented");
        }

        public async Task NotifyQuestionCommentUpdatedAsync(QuestionCommentResponse comment, Guid lessonMaterialId)
        {
            var notification = new QuestionCommentNotification
            {
                CommentId = comment.Id,
                QuestionId = comment.QuestionId,
                LessonMaterialId = lessonMaterialId,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                CreatedByUserId = comment.CreatedByUserId,
                CreatedByName = comment.CreatedByName,
                CreatedByAvatar = comment.CreatedByAvatar,
                CreatedByRole = comment.CreatedByRole,
                ParentCommentId = comment.ParentCommentId,
                IsReply = comment.ParentCommentId.HasValue,
                ActionType = QuestionActionType.Updated
            };

            await SendCommentNotificationAsync(notification, "QuestionCommentUpdated");
        }

        public async Task NotifyQuestionCommentDeletedAsync(Guid commentId, Guid questionId, Guid lessonMaterialId, int deletedRepliesCount = 0)
        {
            var notification = new QuestionCommentDeleteNotification
            {
                CommentId = commentId,
                QuestionId = questionId,
                LessonMaterialId = lessonMaterialId,
                DeletedAt = DateTimeOffset.UtcNow,
                DeletedRepliesCount = deletedRepliesCount,
                ActionType = QuestionActionType.Deleted
            };

            var groupName = $"Lesson_{lessonMaterialId}";

            try
            {
                _logger.LogInformation("[SignalR] Starting notification for comment deleted. " +
                    "CommentId: {CommentId}, QuestionId: {QuestionId}, LessonId: {LessonId}, " +
                    "GroupName: {GroupName}, DeletedReplies: {DeletedReplies}",
                    commentId, questionId, lessonMaterialId, groupName, deletedRepliesCount);

                await _notificationHub.SendNotificationToGroupAsync(groupName, "QuestionCommentDeleted", notification);

                _logger.LogInformation("[SignalR] Comment deleted notification sent successfully! " +
                    "Event: QuestionCommentDeleted, Group: {GroupName}, CommentId: {CommentId}, " +
                    "QuestionId: {QuestionId}, DeletedReplies: {DeletedReplies}",
                    groupName, commentId, questionId, deletedRepliesCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SignalR] Failed to send comment deleted notification. " +
                    "CommentId: {CommentId}, QuestionId: {QuestionId}, LessonId: {LessonId}, Error: {ErrorMessage}",
                    commentId, questionId, lessonMaterialId, ex.Message);
            }
        }

        private async Task SendCommentNotificationAsync(QuestionCommentNotification notification, string eventName)
        {
            var groupName = $"Lesson_{notification.LessonMaterialId}";

            try
            {
                _logger.LogInformation("[SignalR] Starting notification for comment {ActionType}. " +
                     "CommentId: {CommentId}, QuestionId: {QuestionId}, LessonId: {LessonId}, " +
                     "GroupName: {GroupName}, IsReply: {IsReply}, CreatedBy: {CreatedBy}",
                     notification.ActionType.ToString().ToLower(), notification.CommentId,
                     notification.QuestionId, notification.LessonMaterialId, groupName,
                     notification.IsReply, notification.CreatedByName);

                await _notificationHub.SendNotificationToGroupAsync(groupName, eventName, notification);

                _logger.LogInformation("[SignalR] Comment {ActionType} notification sent successfully! " +
                    "Event: {EventName}, Group: {GroupName}, CommentId: {CommentId}, QuestionId: {QuestionId}",
                    notification.ActionType.ToString().ToLower(), eventName, groupName,
                    notification.CommentId, notification.QuestionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SignalR] Failed to send comment {ActionType} notification. " +
                    "CommentId: {CommentId}, QuestionId: {QuestionId}, LessonId: {LessonId}, Error: {ErrorMessage}",
                    notification.ActionType.ToString().ToLower(), notification.CommentId,
                    notification.QuestionId, notification.LessonMaterialId, ex.Message);
            }
        }
    }
}
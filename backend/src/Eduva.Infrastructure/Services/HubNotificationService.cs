using Eduva.Application.Common.Models.Notifications;
using Eduva.Application.Contracts.Hubs;
using Eduva.Application.Features.Questions.Responses;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace Eduva.Infrastructure.Services
{
    public class HubNotificationService : IHubNotificationService
    {
        private const string QuestionCreatedNotificationType = "QuestionCreated";
        private const string QuestionUpdatedNotificationType = "QuestionUpdated";
        private const string QuestionDeletedNotificationType = "QuestionDeleted";
        private const string QuestionCommentedNotificationType = "QuestionCommented";
        private const string QuestionCommentUpdatedNotificationType = "QuestionCommentUpdated";
        private const string QuestionCommentDeletedNotificationType = "QuestionCommentDeleted";

        private readonly INotificationHub _notificationHub;
        private readonly INotificationService _notificationService;
        private readonly ILogger<HubNotificationService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public HubNotificationService(
            INotificationHub notificationHub,
            INotificationService notificationService,
            ILogger<HubNotificationService> logger)
        {
            _notificationHub = notificationHub;
            _notificationService = notificationService;
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

            await SendNotificationAsync(notification, QuestionCreatedNotificationType);

            // Save the notification to the database for persistence
            await SaveNotificationToDatabase(QuestionCreatedNotificationType, notification, lessonMaterialId);
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

            await SendNotificationAsync(notification, QuestionUpdatedNotificationType);

            // Save the notification to the database for persistence
            await SaveNotificationToDatabase(QuestionUpdatedNotificationType, notification, lessonMaterialId);
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

            try
            {
                _logger.LogInformation("[SignalR] Starting notification for question deleted. " +
                    "QuestionId: {QuestionId}, LessonId: {LessonId}",
                    questionId, lessonMaterialId);

                // Get target users for question deletion notification
                var targetUserIds = await _notificationService.GetUsersForQuestionCommentNotificationAsync(questionId, lessonMaterialId);

                // Send to each user individually
                foreach (var userId in targetUserIds)
                {
                    await _notificationHub.SendNotificationToUserAsync(userId.ToString(), QuestionDeletedNotificationType, notification);
                }

                _logger.LogInformation("[SignalR] Question deleted notification sent successfully! " +
                    "Event: QuestionDeleted, TargetUsers: {UserCount}, QuestionId: {QuestionId}",
                    targetUserIds.Count, questionId);

                // Save the notification to the database for persistence
                await SaveNotificationToDatabase(QuestionDeletedNotificationType, notification, lessonMaterialId);
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
            try
            {
                _logger.LogInformation("[SignalR] Starting notification for question {ActionType}. " +
                     "QuestionId: {QuestionId}, LessonId: {LessonId}, " +
                     "Title: {Title}, LessonTitle: {LessonTitle}, CreatedBy: {CreatedBy}",
                     notification.ActionType.ToString().ToLower(), notification.QuestionId,
                     notification.LessonMaterialId, notification.Title,
                     notification.LessonMaterialTitle, notification.CreatedByName);

                // Get target users for real-time notification
                List<Guid> targetUserIds;
                if (eventName == QuestionCreatedNotificationType)
                {
                    targetUserIds = await _notificationService.GetUsersForNewQuestionNotificationAsync(notification.LessonMaterialId);
                }
                else
                {
                    targetUserIds = await _notificationService.GetUsersForQuestionCommentNotificationAsync(notification.QuestionId, notification.LessonMaterialId);
                }

                // Exclude the creator from receiving their own notification
                targetUserIds.Remove(notification.CreatedByUserId);

                // Send to each user individually
                foreach (var userId in targetUserIds)
                {
                    await _notificationHub.SendNotificationToUserAsync(userId.ToString(), eventName, notification);
                }

                _logger.LogInformation("[SignalR] Question {ActionType} notification sent successfully! " +
                    "Event: {EventName}, TargetUsers: {UserCount}, QuestionId: {QuestionId}, LessonTitle: {LessonTitle}",
                    notification.ActionType.ToString().ToLower(), eventName, targetUserIds.Count,
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

            await SendCommentNotificationAsync(notification, QuestionCommentedNotificationType);

            // Save the notification to the database for persistence
            await SaveNotificationToDatabase(QuestionCommentedNotificationType, notification, lessonMaterialId);
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

            await SendCommentNotificationAsync(notification, QuestionCommentUpdatedNotificationType);

            // Save the notification to the database for persistence
            await SaveNotificationToDatabase(QuestionCommentUpdatedNotificationType, notification, lessonMaterialId);
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

            try
            {
                _logger.LogInformation("[SignalR] Starting notification for comment deleted. " +
                    "CommentId: {CommentId}, QuestionId: {QuestionId}, LessonId: {LessonId}, " +
                    "DeletedReplies: {DeletedReplies}",
                    commentId, questionId, lessonMaterialId, deletedRepliesCount);

                // Get target users for comment deletion notification
                var targetUserIds = await _notificationService.GetUsersForQuestionCommentNotificationAsync(questionId, lessonMaterialId);

                // Send to each user individually
                foreach (var userId in targetUserIds)
                {
                    await _notificationHub.SendNotificationToUserAsync(userId.ToString(), QuestionCommentDeletedNotificationType, notification);
                }

                _logger.LogInformation("[SignalR] Comment deleted notification sent successfully! " +
                    "Event: QuestionCommentDeleted, TargetUsers: {UserCount}, CommentId: {CommentId}, " +
                    "QuestionId: {QuestionId}, DeletedReplies: {DeletedReplies}",
                    targetUserIds.Count, commentId, questionId, deletedRepliesCount);

                // Save the notification to the database for persistence
                await SaveNotificationToDatabase(QuestionCommentDeletedNotificationType, notification, lessonMaterialId);
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
            try
            {
                _logger.LogInformation("[SignalR] Starting notification for comment {ActionType}. " +
                     "CommentId: {CommentId}, QuestionId: {QuestionId}, LessonId: {LessonId}, " +
                     "IsReply: {IsReply}, CreatedBy: {CreatedBy}",
                     notification.ActionType.ToString().ToLower(), notification.CommentId,
                     notification.QuestionId, notification.LessonMaterialId,
                     notification.IsReply, notification.CreatedByName);

                // Get target users for comment notification
                var targetUserIds = await _notificationService.GetUsersForQuestionCommentNotificationAsync(
                    notification.QuestionId, notification.LessonMaterialId);

                // Exclude the creator from receiving their own notification
                targetUserIds.Remove(notification.CreatedByUserId);

                // Send to each user individually
                foreach (var userId in targetUserIds)
                {
                    await _notificationHub.SendNotificationToUserAsync(userId.ToString(), eventName, notification);
                }

                _logger.LogInformation("[SignalR] Comment {ActionType} notification sent successfully! " +
                    "Event: {EventName}, TargetUsers: {UserCount}, CommentId: {CommentId}, QuestionId: {QuestionId}",
                    notification.ActionType.ToString().ToLower(), eventName, targetUserIds.Count,
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

        private async Task SaveNotificationToDatabase(string notificationType, object notificationData, Guid lessonMaterialId)
        {
            try
            {
                // Serialize notification data to JSON
                var payload = JsonSerializer.Serialize(notificationData, JsonOptions);

                // Create persistent notification
                var persistentNotification = await _notificationService.CreateNotificationAsync(notificationType, payload);

                List<Guid> targetUserIds;

                // CASE 1: New question - only notify teachers + users with access to the lesson
                if (notificationType == QuestionCreatedNotificationType)
                {
                    targetUserIds = await _notificationService.GetUsersForNewQuestionNotificationAsync(lessonMaterialId);
                }
                else if (IsQuestionSpecificNotification(notificationType, notificationData, out var questionId))
                {
                    // CASE 2: All other operations related to the specific question
                    targetUserIds = await _notificationService.GetUsersForQuestionCommentNotificationAsync(questionId, lessonMaterialId);
                }
                else
                {
                    // CASE 3: Keep the old logic for other cases - ensure safety
                    targetUserIds = await _notificationService.GetUsersInLessonAsync(lessonMaterialId);
                }

                _logger.LogInformation("Notification target users determined - Found {Count} users for {NotificationType}",
                    targetUserIds.Count, notificationType);

                // Exclude the creator from receiving their own notification
                if (notificationData is QuestionNotification qn)
                {
                    targetUserIds.Remove(qn.CreatedByUserId);
                }
                else if (notificationData is QuestionCommentNotification qcn)
                {
                    targetUserIds.Remove(qcn.CreatedByUserId);
                }

                // Create user notifications
                if (targetUserIds.Count != 0)
                {
                    await _notificationService.CreateUserNotificationsAsync(persistentNotification.Id, targetUserIds);
                }

                _logger.LogInformation("Saved persistent notification: {NotificationType}, " +
                    "NotificationId: {NotificationId}, TargetUsers: {UserCount}",
                    notificationType, persistentNotification.Id, targetUserIds.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save persistent notification: {NotificationType}, Error: {ErrorMessage}, StackTrace: {StackTrace}",
                    notificationType, ex.Message, ex.StackTrace);
            }
        }

        #region Helper Methods

        private static bool IsQuestionSpecificNotification(string notificationType, object notificationData, out Guid questionId)
        {
            questionId = Guid.Empty;

            return notificationType switch
            {
                // Question operations related to specific questions
                QuestionUpdatedNotificationType when notificationData is QuestionNotification qn =>
                    SetQuestionId(out questionId, qn.QuestionId),

                QuestionDeletedNotificationType when notificationData is QuestionDeleteNotification qdn =>
                    SetQuestionId(out questionId, qdn.QuestionId),

                // Comment operation - always related to specific question
                QuestionCommentedNotificationType when notificationData is QuestionCommentNotification qcn =>
                    SetQuestionId(out questionId, qcn.QuestionId),

                QuestionCommentUpdatedNotificationType when notificationData is QuestionCommentNotification qcu =>
                    SetQuestionId(out questionId, qcu.QuestionId),

                QuestionCommentDeletedNotificationType when notificationData is QuestionCommentDeleteNotification qcdn =>
                    SetQuestionId(out questionId, qcdn.QuestionId),

                _ => false
            };
        }

        private static bool SetQuestionId(out Guid questionId, Guid id)
        {
            questionId = id;
            return true;
        }

        #endregion

    }
}
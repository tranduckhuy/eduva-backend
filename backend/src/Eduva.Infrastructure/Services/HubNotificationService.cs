using Eduva.Application.Common.Models.Notifications;
using Eduva.Application.Contracts.Hubs;
using Eduva.Application.Features.Questions.Responses;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Constants;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace Eduva.Infrastructure.Services
{
    public class HubNotificationService : IHubNotificationService
    {
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

        #region Question/Comment Notification Methods

        #region Question Notifications 

        public async Task NotifyQuestionCreatedAsync(QuestionResponse question, Guid lessonMaterialId, ApplicationUser? user = null)
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

            // Save the notification to the database for persistence
            var userNotificationId = await SaveNotificationToDatabase(NotificationTypes.QuestionCreated, notification, lessonMaterialId, question.CreatedByUserId, user);

            notification.UserNotificationId = userNotificationId;

            await SendNotificationAsync(notification, NotificationTypes.QuestionCreated, user);

        }

        public async Task NotifyQuestionUpdatedAsync(QuestionResponse question, Guid lessonMaterialId, ApplicationUser? user = null)
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
                PerformedByUserId = user?.Id ?? Guid.Empty,
                PerformedByName = user?.FullName,
                PerformedByAvatar = user?.AvatarUrl,
                CreatedByName = question.CreatedByName,
                CreatedByAvatar = question.CreatedByAvatar,
                CreatedByRole = question.CreatedByRole,
                CommentCount = question.CommentCount,
                ActionType = QuestionActionType.Updated
            };


            // Save the notification to the database for persistence
            var userNotificationId = await SaveNotificationToDatabase(NotificationTypes.QuestionUpdated, notification, lessonMaterialId, question.CreatedByUserId, user);
            notification.UserNotificationId = userNotificationId;

            await SendNotificationAsync(notification, NotificationTypes.QuestionUpdated, user);
        }

        public async Task NotifyQuestionDeletedAsync(QuestionResponse question, Guid lessonMaterialId, ApplicationUser? user = null, List<Guid>? targetUserIds = null)
        {
            var notification = new QuestionDeleteNotification
            {
                QuestionId = question.Id,
                Title = question.Title,
                LessonMaterialId = lessonMaterialId,
                LessonMaterialTitle = question.LessonMaterialTitle,
                CreatedByUserId = question.CreatedByUserId,
                PerformedByUserId = user?.Id ?? Guid.Empty,
                PerformedByName = user?.FullName,
                PerformedByAvatar = user?.AvatarUrl,
                CreatedByName = question.CreatedByName,
                CreatedByAvatar = question.CreatedByAvatar,
                CreatedByRole = question.CreatedByRole,
                DeletedAt = DateTimeOffset.UtcNow,
                ActionType = QuestionActionType.Deleted
            };

            try
            {
                _logger.LogInformation("[SignalR] Starting notification for question deleted. " +
                    "QuestionId: {QuestionId}, LessonId: {LessonId}",
                    question.Id, lessonMaterialId);

                // Get target users for question deletion notification
                targetUserIds ??= await _notificationService.GetUsersForQuestionCommentNotificationAsync(
                        question.Id, lessonMaterialId, question.CreatedByUserId);

                // Exclude the creator from receiving their own notification
                if (user?.Id != null && user.Id != Guid.Empty)
                {
                    targetUserIds.Remove(user.Id);
                }

                // Save the notification to the database for persistence
                var userNotificationId = await SaveNotificationToDatabase(NotificationTypes.QuestionDeleted, notification, lessonMaterialId, question.CreatedByUserId, user, targetUserIds);
                notification.UserNotificationId = userNotificationId;

                // Send to each user individually
                foreach (var userId in targetUserIds)
                {
                    await _notificationHub.SendNotificationToUserAsync(userId.ToString(), NotificationTypes.QuestionDeleted, notification);
                }

                _logger.LogInformation("[SignalR] Question deleted notification sent successfully! " +
                    "Event: QuestionDeleted, TargetUsers: {UserCount}, QuestionId: {QuestionId}",
                    targetUserIds.Count, question.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SignalR] Failed to send question deleted notification. " +
                    "QuestionId: {QuestionId}, LessonId: {LessonId}, Error: {ErrorMessage}",
                    question.Id, lessonMaterialId, ex.Message);
            }
        }

        private async Task SendNotificationAsync(QuestionNotification notification, string eventName, ApplicationUser? user = null)
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
                if (eventName == NotificationTypes.QuestionCreated)
                {
                    targetUserIds = await _notificationService.GetUsersForNewQuestionNotificationAsync(notification.LessonMaterialId);
                }
                else
                {
                    targetUserIds = await _notificationService.GetUsersForQuestionCommentNotificationAsync(notification.QuestionId, notification.LessonMaterialId, notification.CreatedByUserId);
                }

                // Exclude the creator from receiving their own notification
                if (user?.Id != null && user.Id != Guid.Empty)
                {
                    targetUserIds.Remove(user.Id);
                }

                // Send to each user individually
                foreach (var userId in targetUserIds)
                {
                    await _notificationHub.SendNotificationToUserAsync(userId.ToString(), eventName, notification);
                }

                _logger.LogInformation("[SignalR] Question {ActionType} notification sent successfully! " +
                   "UserNotificationId: {UserNotificationId}, Event: {EventName}, TargetUsers: {UserCount}, QuestionId: {QuestionId}, LessonTitle: {LessonTitle}",
                   notification.ActionType.ToString().ToLower(), notification.UserNotificationId, eventName, targetUserIds.Count,
                   notification.QuestionId, notification.LessonMaterialTitle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SignalR] Failed to send question {ActionType} notification. " +
                   "UserNotificationId: {UserNotificationId}, QuestionId: {QuestionId}, LessonId: {LessonId}, Error: {ErrorMessage}",
                   notification.ActionType.ToString().ToLower(), notification.UserNotificationId, notification.QuestionId,
                   notification.LessonMaterialId, ex.Message);
            }
        }

        #endregion

        #region Question Comment Notifications

        public async Task NotifyQuestionCommentedAsync(QuestionCommentResponse comment, Guid lessonMaterialId, string title, string lessonMaterialTitle, ApplicationUser? user = null)
        {
            var notification = new QuestionCommentNotification
            {
                CommentId = comment.Id,
                QuestionId = comment.QuestionId,
                Title = title,
                LessonMaterialId = lessonMaterialId,
                LessonMaterialTitle = lessonMaterialTitle,
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

            // Save the notification to the database for persistence
            var userNotificationId = await SaveNotificationToDatabase(NotificationTypes.QuestionCommented, notification, lessonMaterialId, null, user);
            notification.UserNotificationId = userNotificationId;

            await SendCommentNotificationAsync(notification, NotificationTypes.QuestionCommented, user);
        }

        public async Task NotifyQuestionCommentUpdatedAsync(QuestionCommentResponse comment, Guid lessonMaterialId, string title, string lessonMaterialTitle, ApplicationUser? user = null)
        {
            var notification = new QuestionCommentNotification
            {
                CommentId = comment.Id,
                QuestionId = comment.QuestionId,
                Title = title,
                LessonMaterialId = lessonMaterialId,
                LessonMaterialTitle = lessonMaterialTitle,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                CreatedByUserId = comment.CreatedByUserId,
                PerformedByUserId = user?.Id ?? Guid.Empty,
                PerformedByName = user?.FullName,
                PerformedByAvatar = user?.AvatarUrl,
                CreatedByName = comment.CreatedByName,
                CreatedByAvatar = comment.CreatedByAvatar,
                CreatedByRole = comment.CreatedByRole,
                ParentCommentId = comment.ParentCommentId,
                IsReply = comment.ParentCommentId.HasValue,
                ActionType = QuestionActionType.Updated
            };
            // Save the notification to the database for persistence
            var userNotificationId = await SaveNotificationToDatabase(NotificationTypes.QuestionCommentUpdated, notification, lessonMaterialId, comment.CreatedByUserId, user);
            notification.UserNotificationId = userNotificationId;

            await SendCommentNotificationAsync(notification, NotificationTypes.QuestionCommentUpdated, user);
        }

        public async Task NotifyQuestionCommentDeletedAsync(QuestionCommentResponse comment, Guid lessonMaterialId, string title, string lessonMaterialTitle, int deletedRepliesCount = 0, ApplicationUser? user = null, List<Guid>? targetUserIds = null)
        {
            var notification = new QuestionCommentDeleteNotification
            {
                CommentId = comment.Id,
                QuestionId = comment.QuestionId,
                Title = title,
                LessonMaterialId = lessonMaterialId,
                LessonMaterialTitle = lessonMaterialTitle,
                DeletedAt = DateTimeOffset.UtcNow,
                DeletedRepliesCount = deletedRepliesCount,
                CreatedByUserId = comment.CreatedByUserId,
                PerformedByUserId = user?.Id ?? Guid.Empty,
                PerformedByName = user?.FullName,
                PerformedByAvatar = user?.AvatarUrl,
                CreatedByName = comment.CreatedByName,
                CreatedByAvatar = comment.CreatedByAvatar,
                CreatedByRole = comment.CreatedByRole,
                ActionType = QuestionActionType.Deleted
            };

            try
            {
                _logger.LogInformation("[SignalR] Starting notification for comment deleted. " +
                    "CommentId: {CommentId}, QuestionId: {QuestionId}, LessonId: {LessonId}, " +
                    "DeletedReplies: {DeletedReplies}",
                    comment.Id, comment.QuestionId, lessonMaterialId, deletedRepliesCount);

                // Get target users for comment deletion notification
                targetUserIds ??= await _notificationService.GetUsersForQuestionCommentNotificationAsync(
                            comment.QuestionId, lessonMaterialId, comment.CreatedByUserId);

                // Exclude the creator from receiving their own notification
                if (user?.Id != null && user.Id != Guid.Empty)
                {
                    targetUserIds.Remove(user.Id);
                }

                // Save the notification to the database for persistence
                var userNotificationId = await SaveNotificationToDatabase(NotificationTypes.QuestionCommentDeleted, notification, lessonMaterialId, comment.CreatedByUserId, user, targetUserIds);
                notification.UserNotificationId = userNotificationId;

                // Send to each user individually
                foreach (var userId in targetUserIds)
                {
                    await _notificationHub.SendNotificationToUserAsync(userId.ToString(), NotificationTypes.QuestionCommentDeleted, notification);
                }

                _logger.LogInformation("[SignalR] Comment deleted notification sent successfully! " +
                    "Event: QuestionCommentDeleted, TargetUsers: {UserCount}, CommentId: {CommentId}, " +
                    "QuestionId: {QuestionId}, DeletedReplies: {DeletedReplies}",
                    targetUserIds.Count, comment.Id, comment.QuestionId, deletedRepliesCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SignalR] Failed to send comment deleted notification. " +
                    "CommentId: {CommentId}, QuestionId: {QuestionId}, LessonId: {LessonId}, Error: {ErrorMessage}",
                    comment.Id, comment.QuestionId, lessonMaterialId, ex.Message);
            }
        }

        private async Task SendCommentNotificationAsync(QuestionCommentNotification notification, string eventName, ApplicationUser? user = null)
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
                    notification.QuestionId, notification.LessonMaterialId, null);

                // Exclude the creator from receiving their own notification
                if (user?.Id != null && user.Id != Guid.Empty)
                {
                    targetUserIds.Remove(user.Id);
                }

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

        #endregion



        #region Helper Methods

        private static bool IsQuestionSpecificNotification(string notificationType, object notificationData, out Guid questionId)
        {
            questionId = Guid.Empty;

            return notificationType switch
            {
                // Question operations related to specific questions
                NotificationTypes.QuestionUpdated when notificationData is QuestionNotification qn =>
                    SetQuestionId(out questionId, qn.QuestionId),

                NotificationTypes.QuestionDeleted when notificationData is QuestionDeleteNotification qdn =>
                    SetQuestionId(out questionId, qdn.QuestionId),

                // Comment operation - always related to specific question
                NotificationTypes.QuestionCommented when notificationData is QuestionCommentNotification qcn =>
                    SetQuestionId(out questionId, qcn.QuestionId),

                NotificationTypes.QuestionCommentUpdated when notificationData is QuestionCommentNotification qcu =>
                    SetQuestionId(out questionId, qcu.QuestionId),

                NotificationTypes.QuestionCommentDeleted when notificationData is QuestionCommentDeleteNotification qcdn =>
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

        #endregion

        #region Lesson Material Approval Notifications

        public async Task NotifyLessonMaterialApprovalAsync(
            LessonMaterialApprovalNotification notification,
            string eventType,
            Guid targetUserId,
            ApplicationUser? performedByUser = null)
        {
            if (performedByUser != null)
            {
                notification.PerformedByUserId = performedByUser.Id;
                notification.PerformedByName = performedByUser.FullName;
                notification.PerformedByAvatar = performedByUser.AvatarUrl;
            }

            var notificationId = await SaveNotificationToDatabase(eventType, notification, notification.LessonMaterialId, targetUserId, performedByUser, new List<Guid> { targetUserId });
            notification.UserNotificationId = notificationId;

            await _notificationHub.SendNotificationToUserAsync(targetUserId.ToString(), eventType, notification);

        }

        #endregion

        #region Save Notification to Database

        private async Task<Guid> SaveNotificationToDatabase(string notificationType, object notificationData, Guid lessonMaterialId, Guid? createdUserId = null, ApplicationUser? user = null, List<Guid>? targetUserIds = null)
        {
            try
            {
                // Serialize notification data to JSON
                var payload = JsonSerializer.Serialize(notificationData, JsonOptions);

                // Create persistent notification
                var persistentNotification = await _notificationService.CreateNotificationAsync(notificationType, payload);

                if (targetUserIds == null)
                {
                    if (notificationType == NotificationTypes.QuestionCreated)
                    {
                        targetUserIds = await _notificationService.GetUsersForNewQuestionNotificationAsync(lessonMaterialId);
                    }
                    else if (IsQuestionSpecificNotification(notificationType, notificationData, out var questionId))
                    {
                        targetUserIds = await _notificationService.GetUsersForQuestionCommentNotificationAsync(
                            questionId, lessonMaterialId, createdUserId);
                    }
                    else
                    {
                        targetUserIds = await _notificationService.GetUsersInLessonAsync(lessonMaterialId);
                    }
                }

                _logger.LogInformation("Notification target users determined - Found {Count} users for {NotificationType}",
                    targetUserIds.Count, notificationType);

                // Exclude the creator from receiving their own notification
                if (user?.Id != null && user.Id != Guid.Empty)
                {
                    targetUserIds.Remove(user.Id);
                }

                // Create user notifications
                Guid userNotificationId = Guid.Empty;

                if (targetUserIds.Count != 0)
                {
                    var userNotifications = await _notificationService.CreateUserNotificationsAsync(persistentNotification.Id, targetUserIds);

                    if (userNotifications != null && userNotifications.Count != 0)
                    {
                        userNotificationId = userNotifications.First();
                    }
                }

                _logger.LogInformation("Saved persistent notification: {NotificationType}, " +
                    "NotificationId: {NotificationId}, TargetUsers: {UserCount}",
                    notificationType, persistentNotification.Id, targetUserIds.Count);

                return userNotificationId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save persistent notification: {NotificationType}, Error: {ErrorMessage}, StackTrace: {StackTrace}",
                    notificationType, ex.Message, ex.StackTrace);

                return Guid.Empty;
            }
        }

        #endregion

    }
}
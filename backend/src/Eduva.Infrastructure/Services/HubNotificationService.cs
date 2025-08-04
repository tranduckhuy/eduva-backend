using Eduva.Application.Common.Models.Notifications;
using Eduva.Application.Contracts.Hubs;
using Eduva.Application.Features.Questions.Responses;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Constants;
using Eduva.Domain.Entities;
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
                CreatedAt = question.CreatedAt,
                CreatedByUserId = question.CreatedByUserId,
                CreatedByName = question.CreatedByName,
                CreatedByAvatar = question.CreatedByAvatar,
                CreatedByRole = question.CreatedByRole,
            };

            // Save the notification to the database for persistence
            var userNotificationIds = await SaveNotificationToDatabase(NotificationTypes.QuestionCreated, notification, lessonMaterialId, question.CreatedByUserId, user);

            if (userNotificationIds.Count != 0)
            {
                foreach (var kvp in userNotificationIds)
                {
                    var userId = kvp.Key;
                    var userNotificationId = kvp.Value;

                    notification.UserNotificationId = userNotificationId;

                    _logger.LogInformation("[SignalR] Sending question created notification - UserId: {UserId}, UserNotificationId: {UserNotificationId}, QuestionId: {QuestionId}",
              userId, userNotificationId, question.Id);

                    await _notificationHub.SendNotificationToUserAsync(userId.ToString(), NotificationTypes.QuestionCreated, notification);
                }
            }
            else
            {
                _logger.LogWarning("No target users found for question created notification. QuestionId: {QuestionId}", question.Id);
            }
        }

        public async Task NotifyQuestionUpdatedAsync(QuestionResponse question, Guid lessonMaterialId, ApplicationUser? user = null)
        {
            var notification = new QuestionNotification
            {
                QuestionId = question.Id,
                LessonMaterialId = lessonMaterialId,
                Title = question.Title,
                LessonMaterialTitle = question.LessonMaterialTitle,
                CreatedAt = question.CreatedAt,
                CreatedByUserId = question.CreatedByUserId,
                PerformedByUserId = user?.Id ?? Guid.Empty,
                PerformedByName = user?.FullName,
                PerformedByAvatar = user?.AvatarUrl,
                CreatedByName = question.CreatedByName,
                CreatedByAvatar = question.CreatedByAvatar,
                CreatedByRole = question.CreatedByRole,
            };


            // Save the notification to the database for persistence
            var userNotificationIds = await SaveNotificationToDatabase(NotificationTypes.QuestionUpdated, notification, lessonMaterialId, question.CreatedByUserId, user);

            if (userNotificationIds.Count != 0)
            {
                foreach (var kvp in userNotificationIds)
                {
                    var userId = kvp.Key;
                    var userNotificationId = kvp.Value;

                    notification.UserNotificationId = userNotificationId;

                    _logger.LogInformation("[SignalR] Sending question updated notification - UserId: {UserId}, UserNotificationId: {UserNotificationId}, QuestionId: {QuestionId}",
                        userId, userNotificationId, question.Id);

                    await _notificationHub.SendNotificationToUserAsync(userId.ToString(), NotificationTypes.QuestionUpdated, notification);
                }
            }
            else
            {
                _logger.LogWarning("No target users found for question updated notification. QuestionId: {QuestionId}", question.Id);
            }

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
                DeletedAt = DateTimeOffset.UtcNow
            };

            try
            {
                _logger.LogInformation("[SignalR] Starting notification for question deleted. " +
                    "QuestionId: {QuestionId}, LessonId: {LessonId}",
                    question.Id, lessonMaterialId);

                // Get target users for question deletion notification
                targetUserIds ??= await _notificationService.GetUsersForQuestionCommentNotificationAsync(
                        question.Id, lessonMaterialId, null);

                // Exclude the creator from receiving their own notification
                if (user?.Id != null && user.Id != Guid.Empty)
                {
                    targetUserIds.Remove(user.Id);
                }

                // Save the notification to the database for persistence
                var userNotificationIds = await SaveNotificationToDatabase(NotificationTypes.QuestionDeleted, notification, lessonMaterialId, null, user, targetUserIds);

                if (userNotificationIds.Count != 0)
                {
                    foreach (var kvp in userNotificationIds)
                    {
                        var userId = kvp.Key;
                        var userNotificationId = kvp.Value;

                        notification.UserNotificationId = userNotificationId;

                        _logger.LogInformation("[SignalR] Sending question deleted notification - UserId: {UserId}, UserNotificationId: {UserNotificationId}, QuestionId: {QuestionId}",
          userId, userNotificationId, question.Id);

                        await _notificationHub.SendNotificationToUserAsync(userId.ToString(), NotificationTypes.QuestionDeleted, notification);
                    }
                }
                else
                {
                    _logger.LogWarning("No target users found for question deleted notification. QuestionId: {QuestionId}", question.Id);
                }

                _logger.LogInformation("[SignalR] Question deleted notification sent successfully! " +
                    "Event: QuestionDeleted, TargetUsers: {UserCount}, QuestionId: {QuestionId}",
                    userNotificationIds.Count, question.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SignalR] Failed to send question deleted notification. " +
                    "QuestionId: {QuestionId}, LessonId: {LessonId}, Error: {ErrorMessage}",
                    question.Id, lessonMaterialId, ex.Message);
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
                CreatedAt = comment.CreatedAt,
                CreatedByUserId = comment.CreatedByUserId,
                CreatedByName = comment.CreatedByName,
                CreatedByAvatar = comment.CreatedByAvatar,
                CreatedByRole = comment.CreatedByRole,
            };

            // Save the notification to the database for persistence
            var userNotificationIds = await SaveNotificationToDatabase(NotificationTypes.QuestionCommented, notification, lessonMaterialId, null, user);

            if (userNotificationIds.Count != 0)
            {
                foreach (var kvp in userNotificationIds)
                {
                    var userId = kvp.Key;
                    var userNotificationId = kvp.Value;

                    notification.UserNotificationId = userNotificationId;

                    _logger.LogInformation("[SignalR] Sending comment created notification - UserId: {UserId}, UserNotificationId: {UserNotificationId}, CommentId: {CommentId}, QuestionId: {QuestionId}",
         userId, userNotificationId, comment.Id, comment.QuestionId);

                    await _notificationHub.SendNotificationToUserAsync(userId.ToString(), NotificationTypes.QuestionCommented, notification);
                }
            }
            else
            {
                _logger.LogWarning("No target users found for comment created notification. CommentId: {CommentId}", comment.Id);
            }
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
                CreatedAt = comment.CreatedAt,
                CreatedByUserId = comment.CreatedByUserId,
                PerformedByUserId = user?.Id ?? Guid.Empty,
                PerformedByName = user?.FullName,
                PerformedByAvatar = user?.AvatarUrl,
                CreatedByName = comment.CreatedByName,
                CreatedByAvatar = comment.CreatedByAvatar,
                CreatedByRole = comment.CreatedByRole,
            };

            // Save the notification to the database for persistence
            var userNotificationIds = await SaveNotificationToDatabase(NotificationTypes.QuestionCommentUpdated, notification, lessonMaterialId, null, user);

            if (userNotificationIds.Count != 0)
            {
                foreach (var kvp in userNotificationIds)
                {
                    var userId = kvp.Key;
                    var userNotificationId = kvp.Value;

                    notification.UserNotificationId = userNotificationId;

                    _logger.LogInformation("[SignalR] Sending comment updated notification - UserId: {UserId}, UserNotificationId: {UserNotificationId}, CommentId: {CommentId}, QuestionId: {QuestionId}",
         userId, userNotificationId, comment.Id, comment.QuestionId);

                    await _notificationHub.SendNotificationToUserAsync(userId.ToString(), NotificationTypes.QuestionCommentUpdated, notification);
                }
            }
            else
            {
                _logger.LogWarning("No target users found for comment updated notification. CommentId: {CommentId}", comment.Id);
            }
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
                CreatedByUserId = comment.CreatedByUserId,
                PerformedByUserId = user?.Id ?? Guid.Empty,
                PerformedByName = user?.FullName,
                PerformedByAvatar = user?.AvatarUrl,
                CreatedByName = comment.CreatedByName,
                CreatedByAvatar = comment.CreatedByAvatar,
                CreatedByRole = comment.CreatedByRole,
            };

            try
            {
                _logger.LogInformation("[SignalR] Starting notification for comment deleted. " +
                    "CommentId: {CommentId}, QuestionId: {QuestionId}, LessonId: {LessonId}, " +
                    "DeletedReplies: {DeletedReplies}",
                    comment.Id, comment.QuestionId, lessonMaterialId, deletedRepliesCount);

                // Get target users for comment deletion notification
                targetUserIds ??= await _notificationService.GetUsersForQuestionCommentNotificationAsync(
                            comment.QuestionId, lessonMaterialId, null);

                // Exclude the creator from receiving their own notification
                if (user?.Id != null && user.Id != Guid.Empty)
                {
                    targetUserIds.Remove(user.Id);
                }

                // Save the notification to the database for persistence
                var userNotificationIds = await SaveNotificationToDatabase(NotificationTypes.QuestionCommentDeleted, notification, lessonMaterialId, null, user, targetUserIds);
                if (userNotificationIds.Count != 0)
                {
                    foreach (var kvp in userNotificationIds)
                    {
                        var userId = kvp.Key;
                        var userNotificationId = kvp.Value;

                        notification.UserNotificationId = userNotificationId;

                        _logger.LogInformation("[SignalR] Sending comment deleted notification - UserId: {UserId}, UserNotificationId: {UserNotificationId}, CommentId: {CommentId}, QuestionId: {QuestionId}",
        userId, userNotificationId, comment.Id, comment.QuestionId);

                        await _notificationHub.SendNotificationToUserAsync(userId.ToString(), NotificationTypes.QuestionCommentDeleted, notification);
                    }
                }
                else
                {
                    _logger.LogWarning("No target users found for comment deleted notification. CommentId: {CommentId}", comment.Id);
                }

                _logger.LogInformation("[SignalR] Comment deleted notification sent successfully! " +
                    "Event: QuestionCommentDeleted, TargetUsers: {UserCount}, CommentId: {CommentId}, " +
                    "QuestionId: {QuestionId}, DeletedReplies: {DeletedReplies}",
                    userNotificationIds.Count, comment.Id, comment.QuestionId, deletedRepliesCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SignalR] Failed to send comment deleted notification. " +
                    "CommentId: {CommentId}, QuestionId: {QuestionId}, LessonId: {LessonId}, Error: {ErrorMessage}",
                    comment.Id, comment.QuestionId, lessonMaterialId, ex.Message);
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

            var userNotificationIds = await SaveNotificationToDatabase(eventType, notification, notification.LessonMaterialId, targetUserId, performedByUser, new List<Guid> { targetUserId });
            var userNotificationId = userNotificationIds.GetValueOrDefault(targetUserId, Guid.Empty);
            notification.UserNotificationId = userNotificationId;

            _logger.LogInformation("[SignalR] Sending lesson material approval notification - UserId: {UserId}, UserNotificationId: {UserNotificationId}, EventType: {EventType}",
    targetUserId, userNotificationId, eventType);

            await _notificationHub.SendNotificationToUserAsync(targetUserId.ToString(), eventType, notification);

        }

        #endregion

        #region Save Notification to Database

        private async Task<Dictionary<Guid, Guid>> SaveNotificationToDatabase(string notificationType, object notificationData, Guid lessonMaterialId, Guid? createdUserId = null, ApplicationUser? user = null, List<Guid>? targetUserIds = null)
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
                var userNotificationIds = new Dictionary<Guid, Guid>();

                if (targetUserIds.Count != 0)
                {
                    var userNotifications = await _notificationService.CreateUserNotificationsAsync(persistentNotification.Id, targetUserIds);

                    if (userNotifications.Count == targetUserIds.Count)
                    {
                        for (int i = 0; i < targetUserIds.Count; i++)
                        {
                            userNotificationIds[targetUserIds[i]] = userNotifications[i];
                        }
                    }
                    else
                    {
                        _logger.LogError("Mismatch between targetUserIds count ({TargetCount}) and userNotifications count ({NotificationCount})",
                            targetUserIds.Count, userNotifications.Count);

                        return [];
                    }
                }

                _logger.LogInformation("Saved persistent notification: {NotificationType}, " +
                    "NotificationId: {NotificationId}, TargetUsers: {UserCount}",
                    notificationType, persistentNotification.Id, targetUserIds.Count);

                return userNotificationIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save persistent notification: {NotificationType}, Error: {ErrorMessage}, StackTrace: {StackTrace}",
                    notificationType, ex.Message, ex.StackTrace);

                return [];
            }
        }

        #endregion

    }
}
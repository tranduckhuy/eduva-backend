using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Eduva.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(IUnitOfWork unitOfWork, ILogger<NotificationService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Notification> CreateNotificationAsync(string type, string payload, CancellationToken cancellationToken = default)
        {
            var notificationRepo = _unitOfWork.GetCustomRepository<INotificationRepository>();

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                Type = type,
                Payload = payload,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await notificationRepo.AddAsync(notification);
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Created notification with ID: {NotificationId}, Type: {Type}", notification.Id, type);

            return notification;
        }

        public async Task CreateUserNotificationsAsync(Guid notificationId, List<Guid> targetUserIds, CancellationToken cancellationToken = default)
        {
            if (targetUserIds.Count == 0)
            {
                _logger.LogWarning("No target users provided for notification: {NotificationId}", notificationId);
                return;
            }

            var userNotificationRepo = _unitOfWork.GetCustomRepository<IUserNotificationRepository>();

            var userNotifications = targetUserIds.Select(userId => new UserNotification
            {
                Id = Guid.NewGuid(),
                TargetUserId = userId,
                NotificationId = notificationId,
                IsRead = false
            }).ToList();

            await userNotificationRepo.AddRangeAsync(userNotifications);
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Created {Count} user notifications for notification: {NotificationId}",
                userNotifications.Count, notificationId);
        }

        public async Task<List<UserNotification>> GetUserNotificationsAsync(Guid userId, int skip, int take, CancellationToken cancellationToken = default)
        {
            var userNotificationRepo = _unitOfWork.GetCustomRepository<IUserNotificationRepository>();
            return await userNotificationRepo.GetByUserIdAsync(userId, skip, take, cancellationToken);
        }

        public async Task<List<UserNotification>> GetUnreadNotificationsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var userNotificationRepo = _unitOfWork.GetCustomRepository<IUserNotificationRepository>();
            return await userNotificationRepo.GetUnreadByUserIdAsync(userId, cancellationToken);
        }

        public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var userNotificationRepo = _unitOfWork.GetCustomRepository<IUserNotificationRepository>();
            return await userNotificationRepo.GetUnreadCountByUserIdAsync(userId, cancellationToken);
        }

        public async Task<int> GetTotalCountAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var userNotificationRepo = _unitOfWork.GetCustomRepository<IUserNotificationRepository>();
            return await userNotificationRepo.GetTotalCountByUserIdAsync(userId, cancellationToken);
        }

        public async Task MarkAsReadAsync(Guid userNotificationId, CancellationToken cancellationToken = default)
        {
            var userNotificationRepo = _unitOfWork.GetCustomRepository<IUserNotificationRepository>();
            await userNotificationRepo.MarkAsReadAsync(userNotificationId, cancellationToken);
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Marked user notification as read: {UserNotificationId}", userNotificationId);
        }

        public async Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var userNotificationRepo = _unitOfWork.GetCustomRepository<IUserNotificationRepository>();
            await userNotificationRepo.MarkAllAsReadAsync(userId, cancellationToken);
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Marked all notifications as read for user: {UserId}", userId);
        }

        public async Task<List<Guid>> GetUsersInLessonAsync(Guid lessonMaterialId, CancellationToken cancellationToken = default)
        {
            try
            {
                var lessonRepo = _unitOfWork.GetRepository<LessonMaterial, Guid>();
                var lesson = await lessonRepo.GetByIdAsync(lessonMaterialId);

                if (lesson == null)
                {
                    _logger.LogWarning("Lesson material not found: {LessonMaterialId}", lessonMaterialId);
                    return [];
                }

                var userIds = new HashSet<Guid>();

                // 1. Add lesson creator
                userIds.Add(lesson.CreatedByUserId);
                _logger.LogInformation("Added lesson creator: {UserId}", lesson.CreatedByUserId);

                // 2. Add users who have interacted with this lesson (questions, comments)
                await AddUsersWhoInteractedWithLessonAsync(lessonMaterialId, userIds);

                // 3. Add users with access based on folder structure
                await AddUsersWithFolderAccessAsync(lessonMaterialId, userIds, cancellationToken);

                // 4. For lessons not in folders, add users with access based on visibility
                if (userIds.Count <= 1) // Only creator found
                {
                    await AddUsersBasedOnVisibilityAsync(lesson, userIds);
                }

                var result = userIds.ToList();
                _logger.LogInformation("Total users found for lesson {LessonMaterialId}: {UserCount}",
                    lessonMaterialId, result.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users for lesson: {LessonMaterialId}", lessonMaterialId);
                return new List<Guid>();
            }
        }

        #region Helper Methods

        private async Task AddUsersWhoInteractedWithLessonAsync(Guid lessonMaterialId, HashSet<Guid> userIds)
        {
            try
            {
                var initialCount = userIds.Count;

                // Add users who have asked questions on this lesson
                var questionRepo = _unitOfWork.GetRepository<LessonMaterialQuestion, Guid>();
                var allQuestions = await questionRepo.GetAllAsync();
                var lessonQuestions = allQuestions.Where(q => q.LessonMaterialId == lessonMaterialId).ToList();

                foreach (var question in lessonQuestions)
                {
                    userIds.Add(question.CreatedByUserId);
                }

                // Add users who have commented on questions of this lesson
                if (lessonQuestions.Count != 0)
                {
                    var commentRepo = _unitOfWork.GetRepository<QuestionComment, Guid>();
                    var allComments = await commentRepo.GetAllAsync();

                    var questionIds = lessonQuestions.Select(q => q.Id).ToList();
                    var lessonComments = allComments.Where(c => questionIds.Contains(c.QuestionId)).ToList();

                    foreach (var comment in lessonComments)
                    {
                        userIds.Add(comment.CreatedByUserId);
                    }
                }

                var addedCount = userIds.Count - initialCount;
                _logger.LogInformation("Added {Count} users who have interacted with lesson", addedCount);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to add interacted users for lesson: {LessonMaterialId}", lessonMaterialId);
            }
        }

        private async Task AddUsersWithFolderAccessAsync(Guid lessonMaterialId, HashSet<Guid> userIds, CancellationToken cancellationToken = default)
        {
            try
            {
                var folderLessonRepo = _unitOfWork.GetRepository<FolderLessonMaterial, Guid>();
                var folderLesson = await folderLessonRepo.FirstOrDefaultAsync(fl => fl.LessonMaterialId == lessonMaterialId, cancellationToken: cancellationToken);

                if (folderLesson == null)
                {
                    _logger.LogInformation("Lesson not in any folder");
                    return;
                }

                var folderRepo = _unitOfWork.GetRepository<Folder, Guid>();
                var folder = await folderRepo.GetByIdAsync(folderLesson.FolderId);

                if (folder == null)
                {
                    return;
                }

                _logger.LogInformation("Found folder: {FolderId}, Type: {FolderType}", folder.Id,
                    folder.UserId.HasValue ? "Personal" : "Class");

                if (folder.UserId.HasValue)
                {
                    // Personal folder - add owner
                    userIds.Add(folder.UserId.Value);
                    _logger.LogInformation("Added folder owner: {UserId}", folder.UserId.Value);
                }
                else if (folder.ClassId.HasValue)
                {
                    // Class folder - add teacher and students with access
                    await AddClassUsersWithAccessAsync(folder.ClassId.Value, lessonMaterialId, userIds);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to add folder access users for lesson: {LessonMaterialId}", lessonMaterialId);
            }
        }

        private async Task AddClassUsersWithAccessAsync(Guid classId, Guid lessonMaterialId, HashSet<Guid> userIds)
        {
            try
            {
                var classRepo = _unitOfWork.GetRepository<Classroom, Guid>();
                var classroom = await classRepo.GetByIdAsync(classId);

                if (classroom == null)
                {
                    return;
                }

                var initialCount = userIds.Count;

                // Add teacher
                userIds.Add(classroom.TeacherId);

                // Get ALL students in this class (not just first one!)
                var studentClassRepo = _unitOfWork.GetRepository<StudentClass, Guid>();
                var allStudentClasses = await studentClassRepo.GetAllAsync();
                var studentsInClass = allStudentClasses
                    .Where(sc => sc.ClassId == classId)
                    .Select(sc => sc.StudentId)
                    .ToList();

                // Use StudentClassRepository to check access for each student
                var studentClassCustomRepo = _unitOfWork.GetCustomRepository<IStudentClassRepository>();

                foreach (var studentId in studentsInClass)
                {
                    // Check if student actually has access to this specific material
                    var hasAccess = await studentClassCustomRepo.HasAccessToMaterialAsync(studentId, lessonMaterialId);
                    if (hasAccess)
                    {
                        userIds.Add(studentId);
                    }
                }

                var addedCount = userIds.Count - initialCount;
                _logger.LogInformation("Added teacher and {Count} students with access from class: {ClassId}",
                    addedCount, classId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to add class users for class: {ClassId}", classId);
            }
        }

        private async Task AddUsersBasedOnVisibilityAsync(LessonMaterial lesson, HashSet<Guid> userIds)
        {
            try
            {
                // Only add users if lesson has school visibility
                if (lesson.Visibility != LessonMaterialVisibility.School)
                {
                    _logger.LogInformation("Lesson visibility is {Visibility}, not adding school users", lesson.Visibility);
                    return;
                }

                var userRepo = _unitOfWork.GetRepository<ApplicationUser, Guid>();
                var allUsers = await userRepo.GetAllAsync();

                // Add active users from the same school
                var schoolUsers = allUsers.Where(u =>
                    u.SchoolId == lesson.SchoolId &&
                    u.Status == EntityStatus.Active)
                    .Select(u => u.Id)
                    .ToList();

                var initialCount = userIds.Count;
                foreach (var userId in schoolUsers)
                {
                    userIds.Add(userId);
                }

                var addedCount = userIds.Count - initialCount;
                _logger.LogInformation("Added {Count} users from school: {SchoolId} with visibility School",
                    addedCount, lesson.SchoolId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to add school users for lesson");
            }
        }

        #endregion
    }
}
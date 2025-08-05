using Eduva.Application.Common.Models.Notifications;
using Eduva.Application.Contracts.Hubs;
using Eduva.Application.Features.Questions.Responses;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Eduva.Infrastructure.Test.Services
{
    [TestFixture]
    public class HubNotificationServiceTests
    {
        private Mock<INotificationHub> _mockNotificationHub = null!;
        private Mock<INotificationService> _mockNotificationService = null!;
        private Mock<ILogger<HubNotificationService>> _mockLogger = null!;
        private HubNotificationService _hubNotificationService = null!;

        #region SetUp

        [SetUp]
        public void SetUp()
        {
            _mockNotificationHub = new Mock<INotificationHub>();
            _mockNotificationService = new Mock<INotificationService>();
            _mockLogger = new Mock<ILogger<HubNotificationService>>();

            _hubNotificationService = new HubNotificationService(
                _mockNotificationHub.Object,
                _mockNotificationService.Object,
                _mockLogger.Object);
        }

        #endregion

        #region NotifyQuestionCreatedAsync Tests

        [Test]
        public async Task NotifyQuestionCreated_ShouldSendNotificationAndSaveToDatabase_WhenSuccessful()
        {
            var lessonMaterialId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var targetUser1 = Guid.NewGuid();
            var targetUser2 = Guid.NewGuid();
            var targetUsers = new List<Guid> { targetUser1, targetUser2, creatorId };

            var question = CreateSampleQuestionResponse(creatorId);
            var persistentNotification = CreateSamplePersistentNotification();

            var creatorUser = new ApplicationUser
            {
                Id = creatorId,
                FullName = "Creator Name",
                AvatarUrl = "avatar.png"
            };

            SetupNotificationServiceMocks(targetUsers, persistentNotification, lessonMaterialId, question.Id);

            await _hubNotificationService.NotifyQuestionCreatedAsync(question, lessonMaterialId, creatorUser);

            VerifyUserNotificationSent(targetUser1.ToString(), "QuestionCreated");
            VerifyUserNotificationSent(targetUser2.ToString(), "QuestionCreated");
            VerifyUserNotificationNotSent(creatorId.ToString());
            VerifyPersistentNotificationCreated("QuestionCreated");
        }

        [Test]
        public async Task NotifyQuestionCreated_ShouldNotSendNotifications_WhenOnlyCreatorInTargetList()
        {
            await Task.Yield();
            var lessonMaterialId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var targetUsers = new List<Guid> { creatorId };

            var question = CreateSampleQuestionResponse(creatorId);
            var persistentNotification = CreateSamplePersistentNotification();

            var creatorUser = new ApplicationUser
            {
                Id = creatorId,
                FullName = "Creator Name",
                AvatarUrl = "avatar.png"
            };

            SetupNotificationServiceMocks(targetUsers, persistentNotification, lessonMaterialId, question.Id);

            await _hubNotificationService.NotifyQuestionCreatedAsync(question, lessonMaterialId, creatorUser);

            VerifyNoUserNotificationsSent();
            VerifyPersistentNotificationCreated("QuestionCreated", false);
        }

        #endregion

        #region NotifyQuestionUpdatedAsync Tests

        [Test]
        public async Task NotifyQuestionUpdated_ShouldSendNotificationAndSaveToDatabase_WhenSuccessful()
        {
            var lessonMaterialId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var targetUser1 = Guid.NewGuid();
            var targetUser2 = Guid.NewGuid();
            var targetUsers = new List<Guid> { targetUser1, targetUser2, creatorId };

            var question = CreateSampleQuestionResponse(creatorId);
            var persistentNotification = CreateSamplePersistentNotification();

            var creatorUser = new ApplicationUser
            {
                Id = creatorId,
                FullName = "Creator Name",
                AvatarUrl = "avatar.png"
            };

            SetupNotificationServiceMocks(targetUsers, persistentNotification, lessonMaterialId, question.Id);

            await _hubNotificationService.NotifyQuestionUpdatedAsync(question, lessonMaterialId, creatorUser);

            VerifyUserNotificationSent(targetUser1.ToString(), "QuestionUpdated");
            VerifyUserNotificationSent(targetUser2.ToString(), "QuestionUpdated");
            VerifyUserNotificationNotSent(creatorId.ToString());
            VerifyPersistentNotificationCreated("QuestionUpdated");
        }

        #endregion

        #region NotifyQuestionDeletedAsync Tests

        [Test]
        public async Task NotifyQuestionDeleted_ShouldSendNotificationAndSaveToDatabase_WhenSuccessful()
        {
            var questionId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();
            var targetUser1 = Guid.NewGuid();
            var targetUsers = new List<Guid> { targetUser1 };
            var persistentNotification = CreateSamplePersistentNotification();

            SetupNotificationServiceMocks(targetUsers, persistentNotification, lessonMaterialId, questionId);

            var questionResponse = new QuestionResponse
            {
                Id = questionId,
                Title = "Test Question",
                Content = "Test Content",
                LessonMaterialTitle = "Test Lesson",
                CreatedAt = DateTimeOffset.UtcNow,
                LastModifiedAt = DateTimeOffset.UtcNow,
                CreatedByUserId = Guid.NewGuid(),
                CreatedByName = "Test User",
                CreatedByAvatar = "avatar.jpg",
                CreatedByRole = "Student",
                CommentCount = 0
            };

            await _hubNotificationService.NotifyQuestionDeletedAsync(
                questionResponse,
                lessonMaterialId
            );

            VerifyUserNotificationSent(targetUser1.ToString(), "QuestionDeleted");
            VerifyPersistentNotificationCreated("QuestionDeleted");
        }

        [Test]
        public async Task NotifyQuestionDeleted_ShouldLogError_WhenServiceThrows()
        {
            await Task.Yield();
            var questionId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();
            var targetUser1 = Guid.NewGuid();

            _mockNotificationService.Setup(x => x.GetUsersForQuestionCommentNotificationAsync(
                questionId,
                lessonMaterialId,
                targetUser1,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service error"));

            var questionResponse = new QuestionResponse
            {
                Id = questionId,
                Title = "Test Question",
                Content = "Test Content",
                LessonMaterialTitle = "Test Lesson",
                CreatedAt = DateTimeOffset.UtcNow,
                LastModifiedAt = DateTimeOffset.UtcNow,
                CreatedByUserId = Guid.NewGuid(),
                CreatedByName = "Test User",
                CreatedByAvatar = "avatar.jpg",
                CreatedByRole = "Student",
                CommentCount = 0
            };

            Assert.DoesNotThrowAsync(async () =>
                await _hubNotificationService.NotifyQuestionDeletedAsync(
                    questionResponse,
                    lessonMaterialId
                ));

            VerifyErrorLogged();
        }

        #endregion

        #region NotifyQuestionCommentedAsync Tests

        [Test]
        public async Task NotifyQuestionCommented_ShouldSendNotificationAndSaveToDatabase_WhenSuccessful()
        {
            var lessonMaterialId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var targetUser1 = Guid.NewGuid();
            var targetUsers = new List<Guid> { targetUser1, creatorId };

            var comment = CreateSampleQuestionCommentResponse(creatorId);
            var persistentNotification = CreateSamplePersistentNotification();

            var creatorUser = new ApplicationUser
            {
                Id = creatorId,
                FullName = "Creator Name",
                AvatarUrl = "avatar.png",
            };

            SetupNotificationServiceMocks(targetUsers, persistentNotification, lessonMaterialId, comment.QuestionId);

            await _hubNotificationService.NotifyQuestionCommentedAsync(
                comment,
                lessonMaterialId,
                "Test Question Title",
                "Test Lesson Material Title",
                creatorUser
            );
            VerifyUserNotificationSent(targetUser1.ToString(), "QuestionCommented");
            VerifyUserNotificationNotSent(creatorId.ToString());
            VerifyPersistentNotificationCreated("QuestionCommented");
        }

        [Test]
        public async Task NotifyQuestionCommentCreated_ShouldSetCorrectReplyFlag_WhenHasParentComment()
        {
            await Task.Yield();
            var lessonMaterialId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var parentCommentId = Guid.NewGuid();
            var targetUser1 = Guid.NewGuid();
            var targetUsers = new List<Guid> { targetUser1, creatorId };

            var comment = CreateSampleQuestionCommentResponse(creatorId, parentCommentId);
            var persistentNotification = CreateSamplePersistentNotification();

            SetupNotificationServiceMocks(targetUsers, persistentNotification, lessonMaterialId, comment.QuestionId);

            await _hubNotificationService.NotifyQuestionCommentedAsync(
                comment,
                lessonMaterialId,
                "Test Question Title",
                "Test Lesson Material Title"
            );
            VerifyUserNotificationSent(targetUser1.ToString(), "QuestionCommented");
        }

        #endregion

        #region NotifyQuestionCommentUpdatedAsync Tests

        [Test]
        public async Task NotifyQuestionCommentUpdated_ShouldSendNotificationAndSaveToDatabase_WhenSuccessful()
        {
            var lessonMaterialId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var targetUser1 = Guid.NewGuid();
            var targetUsers = new List<Guid> { targetUser1, creatorId };

            var comment = CreateSampleQuestionCommentResponse(creatorId);
            var persistentNotification = CreateSamplePersistentNotification();

            SetupNotificationServiceMocks(targetUsers, persistentNotification, lessonMaterialId, comment.QuestionId);

            await _hubNotificationService.NotifyQuestionCommentUpdatedAsync(comment, lessonMaterialId, "Test Title", "Test Lesson Material Title");

            VerifyUserNotificationSent(targetUser1.ToString(), "QuestionCommentUpdated");
            VerifyPersistentNotificationCreated("QuestionCommentUpdated");
        }

        #endregion

        #region NotifyQuestionCommentDeletedAsync Tests

        [Test]
        public async Task NotifyQuestionCommentDeleted_ShouldSendNotificationAndSaveToDatabase_WhenSuccessful()
        {
            var commentId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();
            var deletedRepliesCount = 3;
            var targetUser1 = Guid.NewGuid();
            var targetUsers = new List<Guid> { targetUser1 };
            var persistentNotification = CreateSamplePersistentNotification();

            SetupNotificationServiceMocks(targetUsers, persistentNotification, lessonMaterialId, questionId);

            var commentResponse = new QuestionCommentResponse
            {
                Id = commentId,
                QuestionId = questionId,
                Content = "Test Comment",
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedByUserId = Guid.NewGuid(),
                CreatedByName = "Test User",
                CreatedByAvatar = "avatar.jpg",
                CreatedByRole = "Student",
                ParentCommentId = null,
                ReplyCount = deletedRepliesCount
            };

            await _hubNotificationService.NotifyQuestionCommentDeletedAsync(
                commentResponse,
                lessonMaterialId,
                "Test Question Title",
                "Test Lesson Material Title",
                deletedRepliesCount
            );
            VerifyUserNotificationSent(targetUser1.ToString(), "QuestionCommentDeleted");
            VerifyPersistentNotificationCreated("QuestionCommentDeleted");
        }

        [Test]
        public async Task NotifyQuestionCommentDeleted_ShouldLogError_WhenServiceThrows()
        {
            await Task.Yield();
            var commentId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();
            var targetUser1 = Guid.NewGuid();

            _mockNotificationService.Setup(x => x.GetUsersForQuestionCommentNotificationAsync(
                questionId,
                lessonMaterialId,
                targetUser1,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service error"));

            var commentResponse = new QuestionCommentResponse
            {
                Id = commentId,
                QuestionId = questionId,
                Content = "Test Comment",
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedByUserId = Guid.NewGuid(),
                CreatedByName = "Test User",
                CreatedByAvatar = "avatar.jpg",
                CreatedByRole = "Student",
                ParentCommentId = null,
            };

            Assert.DoesNotThrowAsync(async () =>
                await _hubNotificationService.NotifyQuestionCommentDeletedAsync(
                    commentResponse,
                    lessonMaterialId,
                    "Test Question Title",
                    "Test Lesson Material Title",
                    0
                ));

            VerifyErrorLogged();
        }

        #endregion

        #region NotifyLessonMaterialApprovalAsync Tests

        [Test]
        public async Task NotifyLessonMaterialApprovalAsync_WithPerformedByUser_ShouldSetUserInfoAndSendNotification()
        {
            // Arrange
            var lessonMaterialId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
            var performedByUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                FullName = "John Doe",
                AvatarUrl = "https://example.com/avatar.jpg"
            };

            var notification = new LessonMaterialApprovalNotification
            {
                LessonMaterialId = lessonMaterialId,
                LessonMaterialTitle = "Math Lesson",
            };

            var eventType = "LessonMaterialApproved";
            var userNotificationId = Guid.NewGuid();

            _mockNotificationService.Setup(x => x.CreateNotificationAsync(eventType, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Domain.Entities.Notification { Id = Guid.NewGuid() });

            _mockNotificationService.Setup(x => x.CreateUserNotificationsAsync(It.IsAny<Guid>(), It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([userNotificationId]);

            // Act
            await _hubNotificationService.NotifyLessonMaterialApprovalAsync(notification, eventType, targetUserId, performedByUser);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(notification.PerformedByUserId, Is.EqualTo(performedByUser.Id));
                Assert.That(notification.PerformedByName, Is.EqualTo(performedByUser.FullName));
                Assert.That(notification.PerformedByAvatar, Is.EqualTo(performedByUser.AvatarUrl));
                Assert.That(notification.UserNotificationId, Is.EqualTo(userNotificationId));
            });

            _mockNotificationService.Verify(x => x.CreateNotificationAsync(eventType, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockNotificationService.Verify(x => x.CreateUserNotificationsAsync(It.IsAny<Guid>(), It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockNotificationHub.Verify(x => x.SendNotificationToUserAsync(targetUserId.ToString(), eventType, notification), Times.Once);
        }

        [Test]
        public async Task NotifyLessonMaterialApprovalAsync_WithoutPerformedByUser_ShouldNotSetUserInfo()
        {
            // Arrange
            var lessonMaterialId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();

            var notification = new LessonMaterialApprovalNotification
            {
                LessonMaterialId = lessonMaterialId,
                LessonMaterialTitle = "Science Lesson",
            };

            var eventType = "LessonMaterialRejected";
            var userNotificationId = Guid.NewGuid();

            _mockNotificationService.Setup(x => x.CreateNotificationAsync(eventType, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Domain.Entities.Notification { Id = Guid.NewGuid() });

            _mockNotificationService.Setup(x => x.CreateUserNotificationsAsync(It.IsAny<Guid>(), It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([userNotificationId]);

            // Act
            await _hubNotificationService.NotifyLessonMaterialApprovalAsync(notification, eventType, targetUserId, null);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(notification.PerformedByUserId, Is.Null);
                Assert.That(notification.PerformedByName, Is.Null);
                Assert.That(notification.PerformedByAvatar, Is.Null);
                Assert.That(notification.UserNotificationId, Is.EqualTo(userNotificationId));
            });

            _mockNotificationService.Verify(x => x.CreateNotificationAsync(eventType, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockNotificationService.Verify(x => x.CreateUserNotificationsAsync(It.IsAny<Guid>(), It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockNotificationHub.Verify(x => x.SendNotificationToUserAsync(targetUserId.ToString(), eventType, notification), Times.Once);
        }

        [Test]
        public async Task NotifyLessonMaterialApprovalAsync_WithEmptyUserNotificationId_ShouldHandleGracefully()
        {
            // Arrange
            var lessonMaterialId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();

            var notification = new LessonMaterialApprovalNotification
            {
                LessonMaterialId = lessonMaterialId,
                LessonMaterialTitle = "History Lesson",
            };

            var eventType = "LessonMaterialApproved";

            _mockNotificationService.Setup(x => x.CreateNotificationAsync(eventType, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Domain.Entities.Notification { Id = Guid.NewGuid() });

            _mockNotificationService.Setup(x => x.CreateUserNotificationsAsync(It.IsAny<Guid>(), It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([Guid.Empty]);

            // Act
            await _hubNotificationService.NotifyLessonMaterialApprovalAsync(notification, eventType, targetUserId);

            // Assert
            Assert.That(notification.UserNotificationId, Is.EqualTo(Guid.Empty));

            _mockNotificationService.Verify(x => x.CreateNotificationAsync(eventType, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockNotificationService.Verify(x => x.CreateUserNotificationsAsync(It.IsAny<Guid>(), It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockNotificationHub.Verify(x => x.SendNotificationToUserAsync(targetUserId.ToString(), eventType, notification), Times.Once);
        }

        [Test]
        public async Task NotifyLessonMaterialApprovalAsync_WithEmptyEventType_ShouldHandleGracefully()
        {
            // Arrange
            var lessonMaterialId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();

            var notification = new LessonMaterialApprovalNotification
            {
                LessonMaterialId = lessonMaterialId,
                LessonMaterialTitle = "Test Lesson",
            };

            var eventType = string.Empty;
            var userNotificationId = Guid.NewGuid();

            _mockNotificationService.Setup(x => x.CreateNotificationAsync(eventType, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Domain.Entities.Notification { Id = Guid.NewGuid() });

            _mockNotificationService.Setup(x => x.CreateUserNotificationsAsync(It.IsAny<Guid>(), It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([userNotificationId]);

            // Act
            await _hubNotificationService.NotifyLessonMaterialApprovalAsync(notification, eventType, targetUserId);

            // Assert
            _mockNotificationService.Verify(x => x.CreateNotificationAsync(eventType, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockNotificationHub.Verify(x => x.SendNotificationToUserAsync(targetUserId.ToString(), eventType, notification), Times.Once);
        }

        [Test]
        public async Task NotifyLessonMaterialApprovalAsync_WithPerformedByUserHavingNullProperties_ShouldHandleGracefully()
        {
            // Arrange
            var lessonMaterialId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
            var performedByUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                FullName = null,
                AvatarUrl = null!
            };

            var notification = new LessonMaterialApprovalNotification
            {
                LessonMaterialId = lessonMaterialId,
                LessonMaterialTitle = "Test Lesson",
            };

            var eventType = "LessonMaterialApproved";
            var userNotificationId = Guid.NewGuid();

            _mockNotificationService.Setup(x => x.CreateNotificationAsync(eventType, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Domain.Entities.Notification { Id = Guid.NewGuid() });

            _mockNotificationService.Setup(x => x.CreateUserNotificationsAsync(It.IsAny<Guid>(), It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([userNotificationId]);

            // Act
            await _hubNotificationService.NotifyLessonMaterialApprovalAsync(notification, eventType, targetUserId, performedByUser);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(notification.PerformedByUserId, Is.EqualTo(performedByUser.Id));
                Assert.That(notification.PerformedByName, Is.Null);
                Assert.That(notification.PerformedByAvatar, Is.Null);
            });

            _mockNotificationService.Verify(x => x.CreateNotificationAsync(eventType, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockNotificationHub.Verify(x => x.SendNotificationToUserAsync(targetUserId.ToString(), eventType, notification), Times.Once);
        }

        #endregion


        #region Error Handling Tests

        [Test]
        public async Task NotifyComment_ShouldLogError_WhenHubException()
        {
            await Task.Yield();
            var lessonMaterialId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var targetUsers = new List<Guid> { Guid.NewGuid() };

            _mockNotificationService.Setup(s => s.GetUsersForQuestionCommentNotificationAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetUsers);
            _mockNotificationHub.Setup(h => h.SendNotificationToUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
                .ThrowsAsync(new Exception("Hub error"));

            var comment = CreateSampleQuestionCommentResponse(creatorId);

            Assert.DoesNotThrowAsync(async () =>
                 await _hubNotificationService.NotifyQuestionCommentedAsync(
                     comment,
                     lessonMaterialId,
                     "Test Question Title",
                     "Test Lesson Material Title"
                 ));

            VerifyErrorLogged();
        }

        #endregion

        #region Helper Methods

        private void SetupNotificationServiceMocks(List<Guid> targetUsers, Notification persistentNotification, Guid lessonMaterialId, Guid questionId)
        {
            _mockNotificationService.Setup(s => s.GetUsersForNewQuestionNotificationAsync(
            lessonMaterialId,
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(targetUsers);

            _mockNotificationService
        .Setup(x => x.GetUsersForQuestionCommentNotificationAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(targetUsers);

            _mockNotificationService.Setup(s => s.CreateNotificationAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(persistentNotification);

            _mockNotificationService.Setup(s => s.CreateUserNotificationsAsync(
                It.IsAny<Guid>(),
                It.IsAny<List<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid notificationId, List<Guid> targetUserIds, CancellationToken token) =>
            {
                return targetUserIds.Select(_ => Guid.NewGuid()).ToList();
            });
        }

        private void VerifyUserNotificationSent(string userId, string eventName)
        {
            _mockNotificationHub.Verify(h => h.SendNotificationToUserAsync(userId, eventName, It.IsAny<object>()), Times.Once);
        }

        private void VerifyUserNotificationNotSent(string userId)
        {
            _mockNotificationHub.Verify(h => h.SendNotificationToUserAsync(userId, It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        }

        private void VerifyNoUserNotificationsSent()
        {
            _mockNotificationHub.Verify(h => h.SendNotificationToUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        }

        private void VerifyPersistentNotificationCreated(string notificationType, bool expectUserNotifications = true)
        {
            _mockNotificationService.Verify(
                s => s.CreateNotificationAsync(
                    notificationType,
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _mockNotificationService.Verify(
                s => s.CreateUserNotificationsAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<List<Guid>>(),
                    It.IsAny<CancellationToken>()),
                expectUserNotifications ? Times.Once() : Times.Never());
        }

        private void VerifyErrorLogged()
        {
            _mockLogger.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
        }

        private static QuestionResponse CreateSampleQuestionResponse(Guid creatorId)
        {
            return new QuestionResponse
            {
                Id = Guid.NewGuid(),
                Title = "Test Question",
                Content = "Test Content",
                LessonMaterialTitle = "Test Lesson",
                CreatedAt = DateTimeOffset.UtcNow,
                LastModifiedAt = DateTimeOffset.UtcNow,
                CreatedByUserId = creatorId,
                CreatedByName = "Test User",
                CreatedByAvatar = "avatar.jpg",
                CreatedByRole = "Student",
                CommentCount = 0
            };
        }

        private static QuestionCommentResponse CreateSampleQuestionCommentResponse(Guid creatorId, Guid? parentCommentId = null)
        {
            return new QuestionCommentResponse
            {
                Id = Guid.NewGuid(),
                QuestionId = Guid.NewGuid(),
                Content = "Test Comment",
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedByUserId = creatorId,
                CreatedByName = "Test User",
                CreatedByAvatar = "avatar.jpg",
                CreatedByRole = "Student",
                ParentCommentId = parentCommentId
            };
        }

        private static Notification CreateSamplePersistentNotification()
        {
            return new Notification
            {
                Id = Guid.NewGuid(),
                Type = "TestNotification",
                Payload = "{}",
                CreatedAt = DateTimeOffset.UtcNow
            };
        }

        #endregion
    }
}
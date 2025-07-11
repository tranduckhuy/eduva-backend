using Eduva.Application.Common.Models.Notifications;
using Eduva.Application.Contracts.Hubs;
using Eduva.Application.Features.Questions.Responses;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Eduva.Infrastructure.Test.Services
{
    [TestFixture]
    public class HubNotificationServiceTests
    {
        private Mock<INotificationHub> _notificationHubMock;
        private Mock<INotificationService> _notificationServiceMock;
        private Mock<ILogger<HubNotificationService>> _loggerMock;
        private HubNotificationService _service;

        private readonly Guid _questionId = Guid.NewGuid();
        private readonly Guid _commentId = Guid.NewGuid();
        private readonly Guid _lessonMaterialId = Guid.NewGuid();
        private readonly Guid _userId = Guid.NewGuid();

        #region Setup

        [SetUp]
        public void Setup()
        {
            _notificationHubMock = new Mock<INotificationHub>();
            _notificationServiceMock = new Mock<INotificationService>();
            _loggerMock = new Mock<ILogger<HubNotificationService>>();
            _service = new HubNotificationService(_notificationHubMock.Object, _notificationServiceMock.Object, _loggerMock.Object);
        }

        #endregion

        #region Helper 

        private QuestionResponse CreateTestQuestion()
        {
            return new QuestionResponse
            {
                Id = _questionId,
                Title = "Test Question",
                Content = "Test Content",
                LessonMaterialTitle = "Test Lesson",
                CreatedAt = DateTimeOffset.UtcNow,
                LastModifiedAt = DateTimeOffset.UtcNow,
                CreatedByUserId = _userId,
                CreatedByName = "Test User",
                CreatedByAvatar = "avatar.jpg",
                CreatedByRole = "Student",
                CommentCount = 5
            };
        }

        private QuestionCommentResponse CreateTestComment(Guid? parentCommentId = null)
        {
            return new QuestionCommentResponse
            {
                Id = _commentId,
                QuestionId = _questionId,
                Content = "Test Comment",
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedByUserId = _userId,
                CreatedByName = "Test User",
                CreatedByAvatar = "avatar.jpg",
                CreatedByRole = "Student",
                ParentCommentId = parentCommentId
            };
        }

        #endregion

        #region Question Notification Tests

        [TestCase("QuestionCreated")]
        [TestCase("QuestionUpdated")]
        public async Task NotifyQuestion_ShouldSendNotificationAndSaveToDatabase_WhenSuccessful(string eventType)
        {
            // Arrange
            var question = CreateTestQuestion();
            var notification = new Notification { Id = Guid.NewGuid() };
            var userIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

            _notificationServiceMock.Setup(x => x.CreateNotificationAsync(eventType, It.IsAny<string>(), default))
                .ReturnsAsync(notification);
            _notificationServiceMock.Setup(x => x.GetUsersInLessonAsync(_lessonMaterialId, default))
                .ReturnsAsync(userIds);

            // Act
            if (eventType == "QuestionCreated")
                await _service.NotifyQuestionCreatedAsync(question, _lessonMaterialId);
            else
                await _service.NotifyQuestionUpdatedAsync(question, _lessonMaterialId);

            // Assert
            _notificationHubMock.Verify(x => x.SendNotificationToGroupAsync(
                $"Lesson_{_lessonMaterialId}", eventType, It.IsAny<QuestionNotification>()), Times.Once);

            _notificationServiceMock.Verify(x => x.CreateNotificationAsync(eventType, It.IsAny<string>(), default), Times.Once);
            _notificationServiceMock.Verify(x => x.GetUsersInLessonAsync(_lessonMaterialId, default), Times.Once);
            _notificationServiceMock.Verify(x => x.CreateUserNotificationsAsync(notification.Id, It.IsAny<List<Guid>>(), default), Times.Once);
        }

        [Test]
        public async Task NotifyQuestionDeleted_ShouldSendNotificationAndSaveToDatabase_WhenSuccessful()
        {
            // Arrange
            var notification = new Notification { Id = Guid.NewGuid() };
            var userIds = new List<Guid> { Guid.NewGuid() };

            _notificationServiceMock.Setup(x => x.CreateNotificationAsync("QuestionDeleted", It.IsAny<string>(), default))
                .ReturnsAsync(notification);
            _notificationServiceMock.Setup(x => x.GetUsersInLessonAsync(_lessonMaterialId, default))
                .ReturnsAsync(userIds);

            // Act
            await _service.NotifyQuestionDeletedAsync(_questionId, _lessonMaterialId);

            // Assert
            _notificationHubMock.Verify(x => x.SendNotificationToGroupAsync(
                $"Lesson_{_lessonMaterialId}", "QuestionDeleted", It.Is<QuestionDeleteNotification>(n =>
                n.QuestionId == _questionId && n.LessonMaterialId == _lessonMaterialId)), Times.Once);

            _notificationServiceMock.Verify(x => x.CreateNotificationAsync("QuestionDeleted", It.IsAny<string>(), default), Times.Once);
        }

        [TestCase("QuestionCreated")]
        [TestCase("QuestionUpdated")]
        [TestCase("QuestionDeleted")]
        public async Task NotifyQuestion_ShouldLogError_WhenHubException(string eventType)
        {
            // Arrange
            var question = CreateTestQuestion();
            _notificationHubMock.Setup(x => x.SendNotificationToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
                .ThrowsAsync(new Exception("Hub connection failed"));

            // Act & Assert - Should not throw
            if (eventType == "QuestionCreated")
                await _service.NotifyQuestionCreatedAsync(question, _lessonMaterialId);
            else if (eventType == "QuestionUpdated")
                await _service.NotifyQuestionUpdatedAsync(question, _lessonMaterialId);
            else
                await _service.NotifyQuestionDeletedAsync(_questionId, _lessonMaterialId);

            _notificationHubMock.Verify(x => x.SendNotificationToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()), Times.Once);
        }

        #endregion

        #region Comment Notification Tests

        [TestCase("QuestionCommented")]
        [TestCase("QuestionCommentUpdated")]
        public async Task NotifyComment_ShouldSendNotificationAndSaveToDatabase_WhenSuccessful(string eventType)
        {
            // Arrange
            var comment = CreateTestComment();
            var notification = new Notification { Id = Guid.NewGuid() };
            var userIds = new List<Guid> { Guid.NewGuid() };

            _notificationServiceMock.Setup(x => x.CreateNotificationAsync(eventType, It.IsAny<string>(), default))
                .ReturnsAsync(notification);
            _notificationServiceMock.Setup(x => x.GetUsersInLessonAsync(_lessonMaterialId, default))
                .ReturnsAsync(userIds);

            // Act
            if (eventType == "QuestionCommented")
                await _service.NotifyQuestionCommentedAsync(comment, _lessonMaterialId);
            else
                await _service.NotifyQuestionCommentUpdatedAsync(comment, _lessonMaterialId);

            // Assert
            _notificationHubMock.Verify(x => x.SendNotificationToGroupAsync(
                $"Lesson_{_lessonMaterialId}", eventType, It.IsAny<QuestionCommentNotification>()), Times.Once);

            _notificationServiceMock.Verify(x => x.CreateNotificationAsync(eventType, It.IsAny<string>(), default), Times.Once);
        }

        [Test]
        public async Task NotifyQuestionCommented_ShouldSetIsReplyTrue_WhenParentCommentExists()
        {
            // Arrange
            var parentCommentId = Guid.NewGuid();
            var comment = CreateTestComment(parentCommentId);

            // Act
            await _service.NotifyQuestionCommentedAsync(comment, _lessonMaterialId);

            // Assert
            _notificationHubMock.Verify(x => x.SendNotificationToGroupAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.Is<QuestionCommentNotification>(n =>
                n.IsReply && n.ParentCommentId == parentCommentId)), Times.Once);
        }

        [TestCase(0)]
        [TestCase(5)]
        public async Task NotifyQuestionCommentDeleted_ShouldSendNotificationWithCorrectDeletedRepliesCount(int deletedRepliesCount)
        {
            // Arrange
            var notification = new Notification { Id = Guid.NewGuid() };
            _notificationServiceMock.Setup(x => x.CreateNotificationAsync("QuestionCommentDeleted", It.IsAny<string>(), default))
                .ReturnsAsync(notification);
            _notificationServiceMock.Setup(x => x.GetUsersInLessonAsync(_lessonMaterialId, default))
                .ReturnsAsync(new List<Guid>());

            // Act
            await _service.NotifyQuestionCommentDeletedAsync(_commentId, _questionId, _lessonMaterialId, deletedRepliesCount);

            // Assert
            _notificationHubMock.Verify(x => x.SendNotificationToGroupAsync(
                $"Lesson_{_lessonMaterialId}", "QuestionCommentDeleted", It.Is<QuestionCommentDeleteNotification>(n =>
                n.DeletedRepliesCount == deletedRepliesCount)), Times.Once);
        }

        [TestCase("QuestionCommented")]
        [TestCase("QuestionCommentUpdated")]
        [TestCase("QuestionCommentDeleted")]
        public async Task NotifyComment_ShouldLogError_WhenHubException(string eventType)
        {
            // Arrange
            var comment = CreateTestComment();
            _notificationHubMock.Setup(x => x.SendNotificationToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
                .ThrowsAsync(new Exception("Hub connection failed"));

            // Act & Assert - Should not throw
            if (eventType == "QuestionCommented")
                await _service.NotifyQuestionCommentedAsync(comment, _lessonMaterialId);
            else if (eventType == "QuestionCommentUpdated")
                await _service.NotifyQuestionCommentUpdatedAsync(comment, _lessonMaterialId);
            else
                await _service.NotifyQuestionCommentDeletedAsync(_commentId, _questionId, _lessonMaterialId, 2);

            _notificationHubMock.Verify(x => x.SendNotificationToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()), Times.Once);
        }

        #endregion

        #region Database Persistence Tests

        [Test]
        public async Task SaveNotificationToDatabase_ShouldExcludeCreatorFromTargetUsers_WhenQuestionNotification()
        {
            // Arrange
            var question = CreateTestQuestion();
            var notification = new Notification { Id = Guid.NewGuid() };
            var userIds = new List<Guid> { _userId, Guid.NewGuid(), Guid.NewGuid() }; // Creator + others

            _notificationServiceMock.Setup(x => x.CreateNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), default))
                .ReturnsAsync(notification);
            _notificationServiceMock.Setup(x => x.GetUsersInLessonAsync(_lessonMaterialId, default))
                .ReturnsAsync(userIds);

            // Act
            await _service.NotifyQuestionCreatedAsync(question, _lessonMaterialId);

            // Assert
            _notificationServiceMock.Verify(x => x.CreateUserNotificationsAsync(
                notification.Id, It.Is<List<Guid>>(list => !list.Contains(_userId) && list.Count == 2), default), Times.Once);
        }

        [Test]
        public async Task SaveNotificationToDatabase_ShouldExcludeCreatorFromTargetUsers_WhenCommentNotification()
        {
            // Arrange
            var comment = CreateTestComment();
            var notification = new Notification { Id = Guid.NewGuid() };
            var userIds = new List<Guid> { _userId, Guid.NewGuid() }; // Creator + other

            _notificationServiceMock.Setup(x => x.CreateNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), default))
                .ReturnsAsync(notification);
            _notificationServiceMock.Setup(x => x.GetUsersInLessonAsync(_lessonMaterialId, default))
                .ReturnsAsync(userIds);

            // Act
            await _service.NotifyQuestionCommentedAsync(comment, _lessonMaterialId);

            // Assert
            _notificationServiceMock.Verify(x => x.CreateUserNotificationsAsync(
                notification.Id, It.Is<List<Guid>>(list => !list.Contains(_userId) && list.Count == 1), default), Times.Once);
        }

        [Test]
        public async Task SaveNotificationToDatabase_ShouldNotCreateUserNotifications_WhenNoTargetUsers()
        {
            // Arrange
            var question = CreateTestQuestion();
            var notification = new Notification { Id = Guid.NewGuid() };
            var userIds = new List<Guid> { _userId }; // Only creator

            _notificationServiceMock.Setup(x => x.CreateNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), default))
                .ReturnsAsync(notification);
            _notificationServiceMock.Setup(x => x.GetUsersInLessonAsync(_lessonMaterialId, default))
                .ReturnsAsync(userIds);

            // Act
            await _service.NotifyQuestionCreatedAsync(question, _lessonMaterialId);

            // Assert
            _notificationServiceMock.Verify(x => x.CreateUserNotificationsAsync(
                It.IsAny<Guid>(), It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task SaveNotificationToDatabase_ShouldLogError_WhenExceptionThrown()
        {
            // Arrange
            var question = CreateTestQuestion();
            _notificationServiceMock.Setup(x => x.CreateNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), default))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert - Should not throw
            await _service.NotifyQuestionCreatedAsync(question, _lessonMaterialId);

            _notificationServiceMock.Verify(x => x.CreateNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), default), Times.Once);
        }

        #endregion

        #region Edge Cases and Integration Tests

        [Test]
        public async Task AllMethods_ShouldCallCorrectServices_WhenValidInput()
        {
            // Arrange
            var question = CreateTestQuestion();
            var comment = CreateTestComment();
            var notification = new Notification { Id = Guid.NewGuid() };

            _notificationServiceMock.Setup(x => x.CreateNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), default))
                .ReturnsAsync(notification);
            _notificationServiceMock.Setup(x => x.GetUsersInLessonAsync(_lessonMaterialId, default))
                .ReturnsAsync(new List<Guid>());

            // Act
            await _service.NotifyQuestionCreatedAsync(question, _lessonMaterialId);
            await _service.NotifyQuestionUpdatedAsync(question, _lessonMaterialId);
            await _service.NotifyQuestionDeletedAsync(_questionId, _lessonMaterialId);
            await _service.NotifyQuestionCommentedAsync(comment, _lessonMaterialId);
            await _service.NotifyQuestionCommentUpdatedAsync(comment, _lessonMaterialId);
            await _service.NotifyQuestionCommentDeletedAsync(_commentId, _questionId, _lessonMaterialId);

            // Assert
            _notificationHubMock.Verify(x => x.SendNotificationToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()), Times.Exactly(6));
            _notificationServiceMock.Verify(x => x.CreateNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), default), Times.Exactly(6));
            _notificationServiceMock.Verify(x => x.GetUsersInLessonAsync(_lessonMaterialId, default), Times.Exactly(6));
        }

        [TestCase(QuestionActionType.Created)]
        [TestCase(QuestionActionType.Updated)]
        [TestCase(QuestionActionType.Deleted)]
        [TestCase(QuestionActionType.Commented)]
        public async Task NotificationObjects_ShouldHaveCorrectActionType(QuestionActionType actionType)
        {
            // Arrange
            var question = CreateTestQuestion();
            var comment = CreateTestComment();

            // Act & Assert
            switch (actionType)
            {
                case QuestionActionType.Created:
                    await _service.NotifyQuestionCreatedAsync(question, _lessonMaterialId);
                    _notificationHubMock.Verify(x => x.SendNotificationToGroupAsync(
                        It.IsAny<string>(), It.IsAny<string>(), It.Is<QuestionNotification>(n => n.ActionType == QuestionActionType.Created)), Times.Once);
                    break;
                case QuestionActionType.Updated:
                    await _service.NotifyQuestionUpdatedAsync(question, _lessonMaterialId);
                    _notificationHubMock.Verify(x => x.SendNotificationToGroupAsync(
                        It.IsAny<string>(), It.IsAny<string>(), It.Is<QuestionNotification>(n => n.ActionType == QuestionActionType.Updated)), Times.Once);
                    break;
                case QuestionActionType.Deleted:
                    await _service.NotifyQuestionDeletedAsync(_questionId, _lessonMaterialId);
                    _notificationHubMock.Verify(x => x.SendNotificationToGroupAsync(
                        It.IsAny<string>(), It.IsAny<string>(), It.Is<QuestionDeleteNotification>(n => n.ActionType == QuestionActionType.Deleted)), Times.Once);
                    break;
                case QuestionActionType.Commented:
                    await _service.NotifyQuestionCommentedAsync(comment, _lessonMaterialId);
                    _notificationHubMock.Verify(x => x.SendNotificationToGroupAsync(
                        It.IsAny<string>(), It.IsAny<string>(), It.Is<QuestionCommentNotification>(n => n.ActionType == QuestionActionType.Commented)), Times.Once);
                    break;
            }
        }

        #endregion
    }
}
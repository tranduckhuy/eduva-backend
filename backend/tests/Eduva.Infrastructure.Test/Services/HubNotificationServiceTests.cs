using Eduva.Application.Common.Models.Notifications;
using Eduva.Application.Contracts.Hubs;
using Eduva.Application.Features.Questions.Responses;
using Eduva.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Eduva.Infrastructure.Test.Services
{
    [TestFixture]
    public class HubNotificationServiceTests
    {
        #region Setup

        private Mock<INotificationHub> _notificationHubMock = null!;
        private Mock<ILogger<HubNotificationService>> _loggerMock = null!;
        private HubNotificationService _service = null!;

        [SetUp]
        public void Setup()
        {
            _notificationHubMock = new Mock<INotificationHub>();
            _loggerMock = new Mock<ILogger<HubNotificationService>>();
            _service = new HubNotificationService(_notificationHubMock.Object, _loggerMock.Object);
        }

        #endregion

        #region NotifyQuestionCreatedAsync Tests

        [Test]
        public async Task NotifyQuestionCreatedAsync_ShouldSendNotification_WhenValidData()
        {
            // Arrange
            var questionId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();
            var question = new QuestionResponse
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
                CommentCount = 5
            };

            _notificationHubMock.Setup(x => x.SendNotificationToGroupAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.NotifyQuestionCreatedAsync(question, lessonMaterialId);

            // Assert
            _notificationHubMock.Verify(x => x.SendNotificationToGroupAsync(
                $"Lesson_{lessonMaterialId}", "QuestionCreated", It.IsAny<QuestionNotification>()), Times.Once);
        }

        [Test]
        public async Task NotifyQuestionCreatedAsync_ShouldLogError_WhenExceptionThrown()
        {
            // Arrange
            var questionId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();
            var question = new QuestionResponse
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
                CommentCount = 5
            };

            _notificationHubMock.Setup(x => x.SendNotificationToGroupAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            await _service.NotifyQuestionCreatedAsync(question, lessonMaterialId);

            // Assert
            _notificationHubMock.Verify(x => x.SendNotificationToGroupAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()), Times.Once);
        }

        #endregion

        #region NotifyQuestionUpdatedAsync Tests

        [Test]
        public async Task NotifyQuestionUpdatedAsync_ShouldSendNotification_WhenValidData()
        {
            // Arrange
            var questionId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();
            var question = new QuestionResponse
            {
                Id = questionId,
                Title = "Updated Question",
                Content = "Updated Content",
                LessonMaterialTitle = "Updated Lesson",
                CreatedAt = DateTimeOffset.UtcNow,
                LastModifiedAt = DateTimeOffset.UtcNow,
                CreatedByUserId = Guid.NewGuid(),
                CreatedByName = "Test User",
                CreatedByAvatar = "avatar.jpg",
                CreatedByRole = "Student",
                CommentCount = 10
            };

            _notificationHubMock.Setup(x => x.SendNotificationToGroupAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.NotifyQuestionUpdatedAsync(question, lessonMaterialId);

            // Assert
            _notificationHubMock.Verify(x => x.SendNotificationToGroupAsync(
                $"Lesson_{lessonMaterialId}", "QuestionUpdated", It.IsAny<QuestionNotification>()), Times.Once);
        }

        #endregion

        #region NotifyQuestionDeletedAsync Tests

        [Test]
        public async Task NotifyQuestionDeletedAsync_ShouldSendNotification_WhenValidData()
        {
            // Arrange
            var questionId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();

            _notificationHubMock.Setup(x => x.SendNotificationToGroupAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.NotifyQuestionDeletedAsync(questionId, lessonMaterialId);

            // Assert
            _notificationHubMock.Verify(x => x.SendNotificationToGroupAsync(
                $"Lesson_{lessonMaterialId}", "QuestionDeleted", It.IsAny<QuestionDeleteNotification>()), Times.Once);
        }

        [Test]
        public async Task NotifyQuestionDeletedAsync_ShouldLogError_WhenExceptionThrown()
        {
            // Arrange
            var questionId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();

            _notificationHubMock.Setup(x => x.SendNotificationToGroupAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            await _service.NotifyQuestionDeletedAsync(questionId, lessonMaterialId);

            // Assert
            _notificationHubMock.Verify(x => x.SendNotificationToGroupAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()), Times.Once);
        }

        #endregion

        #region NotifyQuestionCommentedAsync Tests

        [Test]
        public async Task NotifyQuestionCommentedAsync_ShouldSendNotification_WhenValidData()
        {
            // Arrange
            var commentId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();
            var comment = new QuestionCommentResponse
            {
                Id = commentId,
                QuestionId = questionId,
                Content = "Test Comment",
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedByUserId = Guid.NewGuid(),
                CreatedByName = "Test User",
                CreatedByAvatar = "avatar.jpg",
                CreatedByRole = "Student",
                ParentCommentId = null
            };

            _notificationHubMock.Setup(x => x.SendNotificationToGroupAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.NotifyQuestionCommentedAsync(comment, lessonMaterialId);

            // Assert
            _notificationHubMock.Verify(x => x.SendNotificationToGroupAsync(
                $"Lesson_{lessonMaterialId}", "QuestionCommented", It.IsAny<QuestionCommentNotification>()), Times.Once);
        }

        [Test]
        public async Task NotifyQuestionCommentedAsync_ShouldSetIsReplyTrue_WhenParentCommentIdExists()
        {
            // Arrange
            var commentId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();
            var parentCommentId = Guid.NewGuid();
            var comment = new QuestionCommentResponse
            {
                Id = commentId,
                QuestionId = questionId,
                Content = "Test Reply",
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedByUserId = Guid.NewGuid(),
                CreatedByName = "Test User",
                CreatedByAvatar = "avatar.jpg",
                CreatedByRole = "Student",
                ParentCommentId = parentCommentId
            };

            _notificationHubMock.Setup(x => x.SendNotificationToGroupAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.NotifyQuestionCommentedAsync(comment, lessonMaterialId);

            // Assert
            _notificationHubMock.Verify(x => x.SendNotificationToGroupAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.Is<QuestionCommentNotification>(n => n.IsReply)), Times.Once);
        }

        #endregion

        #region NotifyQuestionCommentUpdatedAsync Tests

        [Test]
        public async Task NotifyQuestionCommentUpdatedAsync_ShouldSendNotification_WhenValidData()
        {
            // Arrange
            var commentId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();
            var comment = new QuestionCommentResponse
            {
                Id = commentId,
                QuestionId = questionId,
                Content = "Updated Comment",
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedByUserId = Guid.NewGuid(),
                CreatedByName = "Test User",
                CreatedByAvatar = "avatar.jpg",
                CreatedByRole = "Student",
                ParentCommentId = null
            };

            _notificationHubMock.Setup(x => x.SendNotificationToGroupAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.NotifyQuestionCommentUpdatedAsync(comment, lessonMaterialId);

            // Assert
            _notificationHubMock.Verify(x => x.SendNotificationToGroupAsync(
                $"Lesson_{lessonMaterialId}", "QuestionCommentUpdated", It.IsAny<QuestionCommentNotification>()), Times.Once);
        }

        #endregion

        #region NotifyQuestionCommentDeletedAsync Tests

        [Test]
        public async Task NotifyQuestionCommentDeletedAsync_ShouldSendNotification_WhenValidData()
        {
            // Arrange
            var commentId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();
            var deletedRepliesCount = 5;

            _notificationHubMock.Setup(x => x.SendNotificationToGroupAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.NotifyQuestionCommentDeletedAsync(commentId, questionId, lessonMaterialId, deletedRepliesCount);

            // Assert
            _notificationHubMock.Verify(x => x.SendNotificationToGroupAsync(
                $"Lesson_{lessonMaterialId}", "QuestionCommentDeleted", It.IsAny<QuestionCommentDeleteNotification>()), Times.Once);
        }

        [Test]
        public async Task NotifyQuestionCommentDeletedAsync_ShouldLogError_WhenExceptionThrown()
        {
            // Arrange
            var commentId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();
            var deletedRepliesCount = 3;

            _notificationHubMock.Setup(x => x.SendNotificationToGroupAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            await _service.NotifyQuestionCommentDeletedAsync(commentId, questionId, lessonMaterialId, deletedRepliesCount);

            // Assert
            _notificationHubMock.Verify(x => x.SendNotificationToGroupAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()), Times.Once);
        }

        [Test]
        public async Task NotifyQuestionCommentDeletedAsync_ShouldUseDefaultDeletedRepliesCount_WhenNotProvided()
        {
            // Arrange
            var commentId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();

            _notificationHubMock.Setup(x => x.SendNotificationToGroupAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.NotifyQuestionCommentDeletedAsync(commentId, questionId, lessonMaterialId);

            // Assert
            _notificationHubMock.Verify(x => x.SendNotificationToGroupAsync(
                $"Lesson_{lessonMaterialId}", "QuestionCommentDeleted", It.Is<QuestionCommentDeleteNotification>(n => n.DeletedRepliesCount == 0)), Times.Once);
        }

        #endregion

        #region Private Methods Tests

        [Test]
        public async Task SendNotificationAsync_ShouldLogInformation_WhenNotificationSent()
        {
            // Arrange
            var questionId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();
            var question = new QuestionResponse
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
                CommentCount = 5
            };

            _notificationHubMock.Setup(x => x.SendNotificationToGroupAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.NotifyQuestionCreatedAsync(question, lessonMaterialId);

            // Assert
            _notificationHubMock.Verify(x => x.SendNotificationToGroupAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()), Times.Once);
        }

        [Test]
        public async Task SendCommentNotificationAsync_ShouldLogInformation_WhenNotificationSent()
        {
            // Arrange
            var commentId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();
            var comment = new QuestionCommentResponse
            {
                Id = commentId,
                QuestionId = questionId,
                Content = "Test Comment",
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedByUserId = Guid.NewGuid(),
                CreatedByName = "Test User",
                CreatedByAvatar = "avatar.jpg",
                CreatedByRole = "Student",
                ParentCommentId = null
            };

            _notificationHubMock.Setup(x => x.SendNotificationToGroupAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.NotifyQuestionCommentedAsync(comment, lessonMaterialId);

            // Assert
            _notificationHubMock.Verify(x => x.SendNotificationToGroupAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()), Times.Once);
        }

        #endregion
    }
}
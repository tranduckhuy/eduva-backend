using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.Questions.Commands.UpdateQuestion;
using Eduva.Application.Features.Questions.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Moq;
using System.Linq.Expressions;

namespace Eduva.Application.Test.Features.Questions.Commands.UpdateQuestion
{
    [TestFixture]
    public class UpdateQuestionHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = default!;
        private Mock<UserManager<ApplicationUser>> _userManagerMock = default!;
        private Mock<IHubNotificationService> _hubNotificationServiceMock = default!;
        private Mock<IQuestionPermissionService> _permissionServiceMock = default!;
        private Mock<IGenericRepository<LessonMaterialQuestion, Guid>> _questionRepositoryMock = default!;
        private Mock<IGenericRepository<ApplicationUser, Guid>> _userRepositoryMock = default!;
        private Mock<IGenericRepository<LessonMaterial, Guid>> _lessonRepositoryMock = default!;
        private Mock<IGenericRepository<QuestionComment, Guid>> _commentRepositoryMock = default!;
        private UpdateQuestionHandler _handler = default!;

        #region Setup

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);
            _hubNotificationServiceMock = new Mock<IHubNotificationService>();
            _permissionServiceMock = new Mock<IQuestionPermissionService>();
            _questionRepositoryMock = new Mock<IGenericRepository<LessonMaterialQuestion, Guid>>();
            _userRepositoryMock = new Mock<IGenericRepository<ApplicationUser, Guid>>();
            _lessonRepositoryMock = new Mock<IGenericRepository<LessonMaterial, Guid>>();
            _commentRepositoryMock = new Mock<IGenericRepository<QuestionComment, Guid>>();

            _unitOfWorkMock.Setup(x => x.GetRepository<LessonMaterialQuestion, Guid>())
                .Returns(_questionRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.GetRepository<ApplicationUser, Guid>())
                .Returns(_userRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.GetRepository<LessonMaterial, Guid>())
                .Returns(_lessonRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.GetRepository<QuestionComment, Guid>())
                .Returns(_commentRepositoryMock.Object);

            _handler = new UpdateQuestionHandler(
                _unitOfWorkMock.Object,
                _userManagerMock.Object,
                _hubNotificationServiceMock.Object,
                _permissionServiceMock.Object);
        }

        #endregion

        #region Question Validation Tests

        [Test]
        public void Handle_ShouldThrowQuestionNotFound_WhenQuestionDoesNotExist()
        {
            // Arrange
            var questionId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var command = new UpdateQuestionCommand
            {
                Id = questionId,
                Title = "Updated Title",
                Content = "Updated Content",
                UpdatedByUserId = userId
            };

            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync((LessonMaterialQuestion?)null);

            // Act & Assert
            var exception = Assert.ThrowsAsync<AppException>(() =>
                _handler.Handle(command, CancellationToken.None));

            Assert.That(exception.StatusCode, Is.EqualTo(CustomCode.QuestionNotFound));
        }

        [Test]
        public void Handle_ShouldThrowQuestionNotActive_WhenQuestionStatusIsNotActive()
        {
            // Arrange
            var questionId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var command = new UpdateQuestionCommand
            {
                Id = questionId,
                Title = "Updated Title",
                Content = "Updated Content",
                UpdatedByUserId = userId
            };
            var question = new LessonMaterialQuestion { Id = questionId, Status = EntityStatus.Archived };

            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(question);

            // Act & Assert
            var exception = Assert.ThrowsAsync<AppException>(() =>
                _handler.Handle(command, CancellationToken.None));

            Assert.That(exception.StatusCode, Is.EqualTo(CustomCode.QuestionNotActive));
        }

        #endregion

        #region User Validation Tests

        [Test]
        public void Handle_ShouldThrowUserNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            var questionId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var command = new UpdateQuestionCommand
            {
                Id = questionId,
                Title = "Updated Title",
                Content = "Updated Content",
                UpdatedByUserId = userId
            };
            var question = new LessonMaterialQuestion { Id = questionId, Status = EntityStatus.Active };

            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(question);
            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync((ApplicationUser?)null);

            // Act & Assert
            var exception = Assert.ThrowsAsync<AppException>(() =>
                _handler.Handle(command, CancellationToken.None));

            Assert.That(exception.StatusCode, Is.EqualTo(CustomCode.UserNotFound));
        }

        #endregion

        #region Permission Tests

        [Test]
        public void Handle_ShouldThrowInsufficientPermission_WhenUserIsNotOwnerAndNotSystemAdmin()
        {
            // Arrange
            var questionId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var command = new UpdateQuestionCommand
            {
                Id = questionId,
                Title = "Updated Title",
                Content = "Updated Content",
                UpdatedByUserId = userId
            };
            var question = new LessonMaterialQuestion { Id = questionId, Status = EntityStatus.Active, CreatedByUserId = creatorId };
            var user = new ApplicationUser { Id = userId };

            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(question);
            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Teacher" });
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Teacher");

            // Act & Assert
            var exception = Assert.ThrowsAsync<AppException>(() =>
                _handler.Handle(command, CancellationToken.None));

            Assert.That(exception.StatusCode, Is.EqualTo(CustomCode.InsufficientPermissionToUpdateQuestion));
        }

        [Test]
        public async Task Handle_ShouldAllowSystemAdmin_ToUpdateAnyQuestion()
        {
            // Arrange
            var questionId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();
            var command = new UpdateQuestionCommand
            {
                Id = questionId,
                Title = "Updated Title",
                Content = "Updated Content",
                UpdatedByUserId = userId
            };
            var question = new LessonMaterialQuestion
            {
                Id = questionId,
                Status = EntityStatus.Active,
                CreatedByUserId = creatorId,
                LessonMaterialId = lessonMaterialId,
                Title = "Original Title",
                Content = "Original Content"
            };
            var user = new ApplicationUser { Id = userId };
            var creator = new ApplicationUser { Id = creatorId, FullName = "Creator", AvatarUrl = "avatar.jpg" };
            var lessonMaterial = new LessonMaterial { Id = lessonMaterialId, Status = EntityStatus.Active, Title = "Test Lesson" };

            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(question);
            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userRepositoryMock.Setup(x => x.GetByIdAsync(creatorId))
                .ReturnsAsync(creator);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "SystemAdmin" });
            _userManagerMock.Setup(x => x.GetRolesAsync(creator))
                .ReturnsAsync(new List<string> { "Student" });
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.Is<IList<string>>(roles => roles.Contains("SystemAdmin"))))
                .Returns("SystemAdmin");
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.Is<IList<string>>(roles => roles.Contains("Student"))))
                .Returns("Student");
            _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lessonMaterialId))
                .ReturnsAsync(lessonMaterial);
            _commentRepositoryMock.Setup(x => x.CountAsync(It.IsAny<Expression<Func<QuestionComment, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);
            _unitOfWorkMock.Setup(x => x.CommitAsync())
                .ReturnsAsync(1);
            _hubNotificationServiceMock.Setup(x => x.NotifyQuestionUpdatedAsync(It.IsAny<QuestionResponse>(), lessonMaterialId, It.IsAny<ApplicationUser?>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Id, Is.EqualTo(questionId));
                Assert.That(result.Title, Is.EqualTo("Updated Title"));
                Assert.That(result.Content, Is.EqualTo("Updated Content"));
                Assert.That(result.CreatedByUserId, Is.EqualTo(creatorId));
                Assert.That(result.CreatedByName, Is.EqualTo("Creator"));
                Assert.That(result.CreatedByAvatar, Is.EqualTo("avatar.jpg"));
                Assert.That(result.CreatedByRole, Is.EqualTo("Student"));
                Assert.That(result.CommentCount, Is.EqualTo(0));
            });
            _questionRepositoryMock.Verify(x => x.Update(It.IsAny<LessonMaterialQuestion>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.CommitAsync(), Times.Once);
            _hubNotificationServiceMock.Verify(x => x.NotifyQuestionUpdatedAsync(It.IsAny<QuestionResponse>(), lessonMaterialId, It.IsAny<ApplicationUser?>()), Times.Once);
        }

        #endregion

        #region Lesson Material Validation Tests

        [Test]
        public void Handle_ShouldThrowLessonMaterialNotFound_WhenLessonMaterialDoesNotExist()
        {
            // Arrange
            var questionId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();
            var command = new UpdateQuestionCommand
            {
                Id = questionId,
                Title = "Updated Title",
                Content = "Updated Content",
                UpdatedByUserId = userId
            };
            var question = new LessonMaterialQuestion { Id = questionId, Status = EntityStatus.Active, CreatedByUserId = userId, LessonMaterialId = lessonMaterialId };
            var user = new ApplicationUser { Id = userId };

            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(question);
            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Student" });
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Student");
            _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lessonMaterialId))
                .ReturnsAsync((LessonMaterial?)null);

            // Act & Assert
            var exception = Assert.ThrowsAsync<AppException>(() =>
                _handler.Handle(command, CancellationToken.None));

            Assert.That(exception.StatusCode, Is.EqualTo(CustomCode.LessonMaterialNotFound));
        }

        [Test]
        public void Handle_ShouldThrowLessonMaterialNotActive_WhenLessonMaterialStatusIsNotActive()
        {
            // Arrange
            var questionId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();
            var command = new UpdateQuestionCommand
            {
                Id = questionId,
                Title = "Updated Title",
                Content = "Updated Content",
                UpdatedByUserId = userId
            };
            var question = new LessonMaterialQuestion { Id = questionId, Status = EntityStatus.Active, CreatedByUserId = userId, LessonMaterialId = lessonMaterialId };
            var user = new ApplicationUser { Id = userId };
            var lessonMaterial = new LessonMaterial { Id = lessonMaterialId, Status = EntityStatus.Archived };

            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(question);
            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Student" });
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Student");
            _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lessonMaterialId))
                .ReturnsAsync(lessonMaterial);

            // Act & Assert
            var exception = Assert.ThrowsAsync<AppException>(() =>
                _handler.Handle(command, CancellationToken.None));

            Assert.That(exception.StatusCode, Is.EqualTo(CustomCode.LessonMaterialNotActive));
        }

        #endregion

        #region Success Response Tests

        [Test]
        public async Task Handle_ShouldUpdateQuestionSuccessfully_WhenUserIsOwner()
        {
            // Arrange
            var questionId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();
            var command = new UpdateQuestionCommand
            {
                Id = questionId,
                Title = "Updated Title",
                Content = "Updated Content",
                UpdatedByUserId = userId
            };
            var question = new LessonMaterialQuestion
            {
                Id = questionId,
                Status = EntityStatus.Active,
                CreatedByUserId = userId,
                LessonMaterialId = lessonMaterialId,
                Title = "Original Title",
                Content = "Original Content",
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
            };
            var user = new ApplicationUser { Id = userId, FullName = "Test User", AvatarUrl = "avatar.jpg" };
            var lessonMaterial = new LessonMaterial { Id = lessonMaterialId, Status = EntityStatus.Active, Title = "Test Lesson" };

            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(question);
            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Student" });
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Student");
            _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lessonMaterialId))
                .ReturnsAsync(lessonMaterial);
            _commentRepositoryMock.Setup(x => x.CountAsync(It.IsAny<Expression<Func<QuestionComment, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(5);
            _unitOfWorkMock.Setup(x => x.CommitAsync())
                .ReturnsAsync(1);
            _hubNotificationServiceMock.Setup(x => x.NotifyQuestionUpdatedAsync(It.IsAny<QuestionResponse>(), lessonMaterialId, It.IsAny<ApplicationUser?>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Id, Is.EqualTo(questionId));
                Assert.That(result.Title, Is.EqualTo("Updated Title"));
                Assert.That(result.Content, Is.EqualTo("Updated Content"));
                Assert.That(result.CreatedByUserId, Is.EqualTo(userId));
                Assert.That(result.CreatedByName, Is.EqualTo("Test User"));
                Assert.That(result.CreatedByAvatar, Is.EqualTo("avatar.jpg"));
                Assert.That(result.CreatedByRole, Is.EqualTo("Student"));
                Assert.That(result.CommentCount, Is.EqualTo(5));
                Assert.That(result.LastModifiedAt, Is.GreaterThan(result.CreatedAt));
            });
            _questionRepositoryMock.Verify(x => x.Update(It.IsAny<LessonMaterialQuestion>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.CommitAsync(), Times.Once);
            _hubNotificationServiceMock.Verify(x => x.NotifyQuestionUpdatedAsync(It.IsAny<QuestionResponse>(), lessonMaterialId, It.IsAny<ApplicationUser?>()), Times.Once);
        }

        [Test]
        public async Task Handle_ShouldHandleQuestionWithNoCreator_WhenCreatorDoesNotExist()
        {
            // Arrange
            var questionId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();
            var command = new UpdateQuestionCommand
            {
                Id = questionId,
                Title = "Updated Title",
                Content = "Updated Content",
                UpdatedByUserId = userId
            };
            var question = new LessonMaterialQuestion
            {
                Id = questionId,
                Status = EntityStatus.Active,
                CreatedByUserId = creatorId,
                LessonMaterialId = lessonMaterialId,
                Title = "Original Title",
                Content = "Original Content"
            };
            var user = new ApplicationUser { Id = userId };
            var lessonMaterial = new LessonMaterial { Id = lessonMaterialId, Status = EntityStatus.Active, Title = "Test Lesson" };

            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(question);
            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userRepositoryMock.Setup(x => x.GetByIdAsync(creatorId))
                .ReturnsAsync((ApplicationUser?)null);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "SystemAdmin" });
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.Is<IList<string>>(roles => roles.Contains("SystemAdmin"))))
                .Returns("SystemAdmin");
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.Is<IList<string>>(roles => roles.Count == 0)))
                .Returns("SystemAdmin");
            _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lessonMaterialId))
                .ReturnsAsync(lessonMaterial);
            _commentRepositoryMock.Setup(x => x.CountAsync(It.IsAny<Expression<Func<QuestionComment, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);
            _unitOfWorkMock.Setup(x => x.CommitAsync())
                .ReturnsAsync(1);
            _hubNotificationServiceMock.Setup(x => x.NotifyQuestionUpdatedAsync(It.IsAny<QuestionResponse>(), lessonMaterialId, It.IsAny<ApplicationUser?>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.CreatedByName, Is.Null);
                Assert.That(result.CreatedByAvatar, Is.Null);
                Assert.That(result.CreatedByRole, Is.EqualTo("SystemAdmin"));
            });
            _questionRepositoryMock.Verify(x => x.Update(It.IsAny<LessonMaterialQuestion>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.CommitAsync(), Times.Once);
        }

        #endregion

        #region Exception Handling Tests

        [Test]
        public void Handle_ShouldThrowException_WhenRepositoryThrowsException()
        {
            // Arrange
            var questionId = Guid.NewGuid();
            var command = new UpdateQuestionCommand
            {
                Id = questionId,
                Title = "Updated Title",
                Content = "Updated Content",
                UpdatedByUserId = Guid.NewGuid()
            };

            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            var exception = Assert.ThrowsAsync<Exception>(() =>
                _handler.Handle(command, CancellationToken.None));

            Assert.That(exception.Message, Is.EqualTo("Database error"));
        }

        #endregion
    }
}
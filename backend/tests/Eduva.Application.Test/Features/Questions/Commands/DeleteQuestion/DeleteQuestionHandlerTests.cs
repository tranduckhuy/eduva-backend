using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.Questions.Commands.DeleteQuestion;
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

namespace Eduva.Application.Test.Features.Questions.Commands.DeleteQuestion
{
    [TestFixture]
    public class DeleteQuestionHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = default!;
        private Mock<UserManager<ApplicationUser>> _userManagerMock = default!;
        private Mock<IHubNotificationService> _hubNotificationServiceMock = default!;
        private Mock<IQuestionPermissionService> _permissionServiceMock = default!;
        private Mock<IGenericRepository<LessonMaterialQuestion, Guid>> _questionRepositoryMock = default!;
        private Mock<IGenericRepository<ApplicationUser, Guid>> _userRepositoryMock = default!;
        private Mock<IGenericRepository<QuestionComment, Guid>> _commentRepositoryMock = default!;
        private Mock<INotificationService> _notificationServiceMock = default!;

        private DeleteQuestionHandler _handler = default!;

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
            _commentRepositoryMock = new Mock<IGenericRepository<QuestionComment, Guid>>();
            _notificationServiceMock = new Mock<INotificationService>();

            _unitOfWorkMock.Setup(x => x.GetRepository<LessonMaterialQuestion, Guid>())
                .Returns(_questionRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.GetRepository<ApplicationUser, Guid>())
                .Returns(_userRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.GetRepository<QuestionComment, Guid>())
                .Returns(_commentRepositoryMock.Object);

            _handler = new DeleteQuestionHandler(
                _unitOfWorkMock.Object,
                _userManagerMock.Object,
                _hubNotificationServiceMock.Object,
                _permissionServiceMock.Object,
                 _notificationServiceMock.Object);
        }

        #endregion

        #region Question Validation Tests

        [Test]
        public void Handle_ShouldThrowQuestionNotFound_WhenQuestionDoesNotExist()
        {
            // Arrange
            var questionId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var command = new DeleteQuestionCommand { Id = questionId, DeletedByUserId = userId };

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
            var command = new DeleteQuestionCommand { Id = questionId, DeletedByUserId = userId };
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
            var command = new DeleteQuestionCommand { Id = questionId, DeletedByUserId = userId };
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
        public async Task Handle_ShouldAllowSystemAdmin_ToDeleteAnyQuestion()
        {
            // Arrange
            var questionId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();
            var lessonMaterialRepoMock = new Mock<IGenericRepository<LessonMaterial, Guid>>();
            _unitOfWorkMock.Setup(x => x.GetRepository<LessonMaterial, Guid>())
                .Returns(lessonMaterialRepoMock.Object);
            lessonMaterialRepoMock.Setup(x => x.GetByIdAsync(lessonMaterialId))
                .ReturnsAsync(new LessonMaterial { Id = lessonMaterialId, Title = "Test Lesson" });
            var command = new DeleteQuestionCommand { Id = questionId, DeletedByUserId = userId };
            var question = new LessonMaterialQuestion
            {
                Id = questionId,
                Status = EntityStatus.Active,
                Title = "Test Question",
                LessonMaterialId = lessonMaterialId,
                CreatedByUserId = creatorId
            };
            var user = new ApplicationUser { Id = userId };
            var creator = new ApplicationUser { Id = creatorId, FullName = "Creator" };

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
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("SystemAdmin");
            _unitOfWorkMock.Setup(x => x.CommitAsync())
                .ReturnsAsync(1);

            // Mock notification service
            _hubNotificationServiceMock.Setup(x => x.NotifyQuestionDeletedAsync(
                It.IsAny<QuestionResponse>(),
                It.IsAny<Guid>(), It.IsAny<ApplicationUser?>(), It.IsAny<List<Guid>>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result, Is.True);
            _questionRepositoryMock.Verify(x => x.Remove(question), Times.Once);
            _unitOfWorkMock.Verify(x => x.CommitAsync(), Times.Once);
            _hubNotificationServiceMock.Verify(x => x.NotifyQuestionDeletedAsync(
                It.IsAny<QuestionResponse>(),
                lessonMaterialId, It.IsAny<ApplicationUser?>(), It.IsAny<List<Guid>>()), Times.Once);
        }

        [Test]
        public async Task Handle_ShouldAllowQuestionOwner_ToDeleteOwnQuestion()
        {
            // Arrange
            var questionId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();
            var lessonMaterialRepoMock = new Mock<IGenericRepository<LessonMaterial, Guid>>();
            _unitOfWorkMock.Setup(x => x.GetRepository<LessonMaterial, Guid>())
                .Returns(lessonMaterialRepoMock.Object);
            lessonMaterialRepoMock.Setup(x => x.GetByIdAsync(lessonMaterialId))
                .ReturnsAsync(new LessonMaterial { Id = lessonMaterialId, Title = "Test Lesson" });

            var command = new DeleteQuestionCommand { Id = questionId, DeletedByUserId = userId };
            var question = new LessonMaterialQuestion
            {
                Id = questionId,
                Status = EntityStatus.Active,
                CreatedByUserId = userId,
                LessonMaterialId = lessonMaterialId,
                Title = "Test Question"
            };
            var user = new ApplicationUser { Id = userId };

            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(question);
            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Student" });
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Student");
            _commentRepositoryMock.Setup(x => x.CountAsync(It.IsAny<Expression<Func<QuestionComment, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);
            _unitOfWorkMock.Setup(x => x.CommitAsync())
                .ReturnsAsync(1);

            _hubNotificationServiceMock.Setup(x => x.NotifyQuestionDeletedAsync(
                It.IsAny<QuestionResponse>(),
                It.IsAny<Guid>(), It.IsAny<ApplicationUser?>(), It.IsAny<List<Guid>>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result, Is.True);
            _questionRepositoryMock.Verify(x => x.Remove(question), Times.Once);
            _unitOfWorkMock.Verify(x => x.CommitAsync(), Times.Once);
            _hubNotificationServiceMock.Verify(x => x.NotifyQuestionDeletedAsync(
                It.IsAny<QuestionResponse>(),
                lessonMaterialId, It.IsAny<ApplicationUser?>(), It.IsAny<List<Guid>>()), Times.Once);
        }

        [Test]
        public void Handle_ShouldThrowInsufficientPermission_WhenUserHasNoPermission()
        {
            // Arrange
            var questionId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var command = new DeleteQuestionCommand { Id = questionId, DeletedByUserId = userId };
            var question = new LessonMaterialQuestion { Id = questionId, Status = EntityStatus.Active, CreatedByUserId = creatorId };
            var user = new ApplicationUser { Id = userId };
            var creator = new ApplicationUser { Id = creatorId };

            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(question);
            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userRepositoryMock.Setup(x => x.GetByIdAsync(creatorId))
                .ReturnsAsync(creator);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Student" });
            _userManagerMock.Setup(x => x.GetRolesAsync(creator))
                .ReturnsAsync(new List<string> { "Student" });
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Student");

            // Act & Assert
            var exception = Assert.ThrowsAsync<AppException>(() =>
                _handler.Handle(command, CancellationToken.None));

            Assert.That(exception.StatusCode, Is.EqualTo(CustomCode.InsufficientPermissionToDeleteQuestion));
        }

        #endregion

        #region Comment Validation Tests

        [Test]
        public void Handle_ShouldThrowCannotDeleteQuestionWithComments_WhenStudentTriesToDeleteQuestionWithComments()
        {
            // Arrange
            var questionId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var command = new DeleteQuestionCommand { Id = questionId, DeletedByUserId = userId };
            var question = new LessonMaterialQuestion { Id = questionId, Status = EntityStatus.Active, CreatedByUserId = userId };
            var user = new ApplicationUser { Id = userId };

            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(question);
            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Student" });
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Student");
            _commentRepositoryMock.Setup(x => x.CountAsync(It.IsAny<Expression<Func<QuestionComment, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act & Assert
            var exception = Assert.ThrowsAsync<AppException>(() =>
                _handler.Handle(command, CancellationToken.None));

            Assert.That(exception.StatusCode, Is.EqualTo(CustomCode.CannotDeleteQuestionWithComments));
        }

        #endregion

        #region Success Response Tests

        [Test]
        public async Task Handle_ShouldDeleteQuestionSuccessfully_WhenAllConditionsAreMet()
        {
            // Arrange
            var questionId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();

            var lessonMaterialRepoMock = new Mock<IGenericRepository<LessonMaterial, Guid>>();
            _unitOfWorkMock.Setup(x => x.GetRepository<LessonMaterial, Guid>())
                .Returns(lessonMaterialRepoMock.Object);
            lessonMaterialRepoMock.Setup(x => x.GetByIdAsync(lessonMaterialId))
                .ReturnsAsync(new LessonMaterial { Id = lessonMaterialId, Title = "Test Lesson" });

            var command = new DeleteQuestionCommand { Id = questionId, DeletedByUserId = userId };
            var question = new LessonMaterialQuestion
            {
                Id = questionId,
                Status = EntityStatus.Active,
                CreatedByUserId = userId,
                LessonMaterialId = lessonMaterialId,
                Title = "Test Question"
            };
            var user = new ApplicationUser { Id = userId };

            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(question);
            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Student" });
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Student");
            _commentRepositoryMock.Setup(x => x.CountAsync(It.IsAny<Expression<Func<QuestionComment, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);
            _unitOfWorkMock.Setup(x => x.CommitAsync())
                .ReturnsAsync(1);

            // Mock notification service
            _hubNotificationServiceMock.Setup(x => x.NotifyQuestionDeletedAsync(
                It.IsAny<QuestionResponse>(),
                lessonMaterialId, It.IsAny<ApplicationUser?>(), It.IsAny<List<Guid>>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result, Is.True);
            _questionRepositoryMock.Verify(x => x.Remove(question), Times.Once);
            _unitOfWorkMock.Verify(x => x.CommitAsync(), Times.Once);
            _hubNotificationServiceMock.Verify(x => x.NotifyQuestionDeletedAsync(
                It.IsAny<QuestionResponse>(),
                lessonMaterialId, It.IsAny<ApplicationUser?>(), It.IsAny<List<Guid>>()), Times.Once);
        }

        #endregion

        #region Exception Handling Tests

        [Test]
        public void Handle_ShouldThrowException_WhenRepositoryThrowsException()
        {
            // Arrange
            var questionId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var command = new DeleteQuestionCommand { Id = questionId, DeletedByUserId = userId };

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
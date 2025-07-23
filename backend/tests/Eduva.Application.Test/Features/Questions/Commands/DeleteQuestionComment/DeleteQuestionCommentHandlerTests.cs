using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.Questions.Commands.DeleteQuestionComment;
using Eduva.Application.Features.Questions.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Eduva.Application.Test.Features.Questions.Commands.DeleteQuestionComment
{
    [TestFixture]
    public class DeleteQuestionCommentHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = default!;
        private Mock<UserManager<ApplicationUser>> _userManagerMock = default!;
        private Mock<IHubNotificationService> _hubNotificationServiceMock = default!;
        private Mock<IQuestionPermissionService> _permissionServiceMock = default!;
        private Mock<IGenericRepository<ApplicationUser, Guid>> _userRepositoryMock = default!;
        private Mock<IGenericRepository<QuestionComment, Guid>> _commentRepositoryMock = default!;
        private Mock<IGenericRepository<LessonMaterialQuestion, Guid>> _questionRepositoryMock = default!;
        private DeleteQuestionCommentHandler _handler = default!;

        #region Setup

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);
            _hubNotificationServiceMock = new Mock<IHubNotificationService>();
            _permissionServiceMock = new Mock<IQuestionPermissionService>();
            _userRepositoryMock = new Mock<IGenericRepository<ApplicationUser, Guid>>();
            _commentRepositoryMock = new Mock<IGenericRepository<QuestionComment, Guid>>();
            _questionRepositoryMock = new Mock<IGenericRepository<LessonMaterialQuestion, Guid>>();

            _unitOfWorkMock.Setup(x => x.GetRepository<ApplicationUser, Guid>())
                .Returns(_userRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.GetRepository<QuestionComment, Guid>())
                .Returns(_commentRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.GetRepository<LessonMaterialQuestion, Guid>())
                .Returns(_questionRepositoryMock.Object);

            _handler = new DeleteQuestionCommentHandler(
                _unitOfWorkMock.Object,
                _userManagerMock.Object,
                _hubNotificationServiceMock.Object,
                _permissionServiceMock.Object);
        }

        #endregion

        #region User Validation Tests

        [Test]
        public void Handle_ShouldThrowUserNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            var commentId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var command = new DeleteQuestionCommentCommand { Id = commentId, DeletedByUserId = userId };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync((ApplicationUser?)null);

            // Act & Assert
            var exception = Assert.ThrowsAsync<AppException>(() =>
                _handler.Handle(command, CancellationToken.None));

            Assert.That(exception.StatusCode, Is.EqualTo(CustomCode.UserNotFound));
        }

        #endregion

        #region Comment Validation Tests

        [Test]
        public void Handle_ShouldThrowCommentNotFound_WhenCommentDoesNotExist()
        {
            // Arrange
            var commentId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var command = new DeleteQuestionCommentCommand { Id = commentId, DeletedByUserId = userId };
            var user = new ApplicationUser { Id = userId };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _commentRepositoryMock.Setup(x => x.GetByIdAsync(commentId))
                .ReturnsAsync((QuestionComment?)null);

            // Act & Assert
            var exception = Assert.ThrowsAsync<AppException>(() =>
                _handler.Handle(command, CancellationToken.None));

            Assert.That(exception.StatusCode, Is.EqualTo(CustomCode.CommentNotFound));
        }

        [Test]
        public void Handle_ShouldThrowCommentNotActive_WhenCommentStatusIsNotActive()
        {
            // Arrange
            var commentId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var command = new DeleteQuestionCommentCommand { Id = commentId, DeletedByUserId = userId };
            var user = new ApplicationUser { Id = userId };
            var comment = new QuestionComment { Id = commentId, Status = EntityStatus.Archived };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _commentRepositoryMock.Setup(x => x.GetByIdAsync(commentId))
                .ReturnsAsync(comment);

            // Act & Assert
            var exception = Assert.ThrowsAsync<AppException>(() =>
                _handler.Handle(command, CancellationToken.None));

            Assert.That(exception.StatusCode, Is.EqualTo(CustomCode.CommentNotActive));
        }

        #endregion

        #region Question Validation Tests

        [Test]
        public void Handle_ShouldThrowQuestionNotFound_WhenQuestionDoesNotExist()
        {
            // Arrange
            var commentId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var command = new DeleteQuestionCommentCommand { Id = commentId, DeletedByUserId = userId };
            var user = new ApplicationUser { Id = userId };
            var comment = new QuestionComment { Id = commentId, Status = EntityStatus.Active, QuestionId = questionId };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _commentRepositoryMock.Setup(x => x.GetByIdAsync(commentId))
                .ReturnsAsync(comment);
            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync((LessonMaterialQuestion?)null);

            // Act & Assert
            var exception = Assert.ThrowsAsync<AppException>(() =>
                _handler.Handle(command, CancellationToken.None));

            Assert.That(exception.StatusCode, Is.EqualTo(CustomCode.QuestionNotFound));
        }

        #endregion

        #region Permission Tests

        [Test]
        public async Task Handle_ShouldAllowSystemAdmin_ToDeleteAnyComment()
        {
            // Arrange
            var commentId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();

            // Mock lesson material repo
            var lessonMaterialRepoMock = new Mock<IGenericRepository<LessonMaterial, Guid>>();
            _unitOfWorkMock.Setup(x => x.GetRepository<LessonMaterial, Guid>())
                .Returns(lessonMaterialRepoMock.Object);
            lessonMaterialRepoMock.Setup(x => x.GetByIdAsync(lessonMaterialId))
                .ReturnsAsync(new LessonMaterial { Id = lessonMaterialId, Title = "Test Lesson" });

            var command = new DeleteQuestionCommentCommand { Id = commentId, DeletedByUserId = userId };
            var user = new ApplicationUser { Id = userId };
            var comment = new QuestionComment { Id = commentId, Status = EntityStatus.Active, QuestionId = questionId };
            var question = new LessonMaterialQuestion { Id = questionId, LessonMaterialId = lessonMaterialId, Title = "Test Question" };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _commentRepositoryMock.Setup(x => x.GetByIdAsync(commentId))
                .ReturnsAsync(comment);
            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(question);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "SystemAdmin" });
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("SystemAdmin");
            _commentRepositoryMock.Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<QuestionComment>());
            _unitOfWorkMock.Setup(x => x.CommitAsync())
                .ReturnsAsync(1);
            _hubNotificationServiceMock.Setup(x => x.NotifyQuestionCommentDeletedAsync(
                It.IsAny<QuestionCommentResponse>(),
                lessonMaterialId,
                It.IsAny<string>(),
                It.IsAny<string>(),
                0, It.IsAny<Guid?>()))
            .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result, Is.True);
            _commentRepositoryMock.Verify(x => x.Remove(comment), Times.Once);
            _unitOfWorkMock.Verify(x => x.CommitAsync(), Times.Once);
            _hubNotificationServiceMock.Verify(x => x.NotifyQuestionCommentDeletedAsync(
                It.IsAny<QuestionCommentResponse>(),
                lessonMaterialId,
                It.IsAny<string>(),
                It.IsAny<string>(),
                0, It.IsAny<Guid?>()), Times.Once);
        }

        [Test]
        public async Task Handle_ShouldAllowCommentOwner_ToDeleteOwnComment()
        {
            // Arrange
            var commentId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();

            // Mock lesson material repo
            var lessonMaterialRepoMock = new Mock<IGenericRepository<LessonMaterial, Guid>>();
            _unitOfWorkMock.Setup(x => x.GetRepository<LessonMaterial, Guid>())
                .Returns(lessonMaterialRepoMock.Object);
            lessonMaterialRepoMock.Setup(x => x.GetByIdAsync(lessonMaterialId))
                .ReturnsAsync(new LessonMaterial { Id = lessonMaterialId, Title = "Test Lesson" });

            var command = new DeleteQuestionCommentCommand { Id = commentId, DeletedByUserId = userId };
            var user = new ApplicationUser { Id = userId };
            var comment = new QuestionComment { Id = commentId, Status = EntityStatus.Active, QuestionId = questionId, CreatedByUserId = userId };
            var question = new LessonMaterialQuestion { Id = questionId, LessonMaterialId = lessonMaterialId, Title = "Test Question" };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _commentRepositoryMock.Setup(x => x.GetByIdAsync(commentId))
                .ReturnsAsync(comment);
            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(question);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Student" });
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Student");
            _commentRepositoryMock.Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<QuestionComment>());
            _unitOfWorkMock.Setup(x => x.CommitAsync())
                .ReturnsAsync(1);
            _hubNotificationServiceMock.Setup(x => x.NotifyQuestionCommentDeletedAsync(
                 It.IsAny<QuestionCommentResponse>(),
                 lessonMaterialId,
                 It.IsAny<string>(),
                 It.IsAny<string>(),
                 0, It.IsAny<Guid?>()))
             .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result, Is.True);
            _commentRepositoryMock.Verify(x => x.Remove(comment), Times.Once);
            _unitOfWorkMock.Verify(x => x.CommitAsync(), Times.Once);
            _hubNotificationServiceMock.Verify(x => x.NotifyQuestionCommentDeletedAsync(
                 It.IsAny<QuestionCommentResponse>(),
                 lessonMaterialId,
                 It.IsAny<string>(),
                 It.IsAny<string>(),
                 0, It.IsAny<Guid?>()), Times.Once);
        }

        [Test]
        public void Handle_ShouldThrowCannotDeleteCommentWithReplies_WhenStudentTriesToDeleteCommentWithReplies()
        {
            // Arrange
            var commentId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();

            // Mock lesson material repo
            var lessonMaterialRepoMock = new Mock<IGenericRepository<LessonMaterial, Guid>>();
            _unitOfWorkMock.Setup(x => x.GetRepository<LessonMaterial, Guid>())
                .Returns(lessonMaterialRepoMock.Object);
            lessonMaterialRepoMock.Setup(x => x.GetByIdAsync(lessonMaterialId))
                .ReturnsAsync(new LessonMaterial { Id = lessonMaterialId, Title = "Test Lesson" });

            var command = new DeleteQuestionCommentCommand { Id = commentId, DeletedByUserId = userId };
            var user = new ApplicationUser { Id = userId };
            var comment = new QuestionComment { Id = commentId, Status = EntityStatus.Active, QuestionId = questionId };
            var question = new LessonMaterialQuestion { Id = questionId, LessonMaterialId = lessonMaterialId, Title = "Test Question" };
            var replies = new List<QuestionComment> { new QuestionComment { Id = Guid.NewGuid(), ParentCommentId = commentId, Status = EntityStatus.Active } };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _commentRepositoryMock.Setup(x => x.GetByIdAsync(commentId))
                .ReturnsAsync(comment);
            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(question);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Student" });
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Student");
            _commentRepositoryMock.Setup(x => x.GetAllAsync())
                .ReturnsAsync(replies);

            // Act & Assert
            var exception = Assert.ThrowsAsync<AppException>(() =>
                _handler.Handle(command, CancellationToken.None));

            Assert.That(exception.StatusCode, Is.EqualTo(CustomCode.CannotDeleteCommentWithReplies));
        }

        [Test]
        public void Handle_ShouldThrowInsufficientPermission_WhenUserHasNoPermission()
        {
            // Arrange
            var commentId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();

            // Mock lesson material repo
            var lessonMaterialRepoMock = new Mock<IGenericRepository<LessonMaterial, Guid>>();
            _unitOfWorkMock.Setup(x => x.GetRepository<LessonMaterial, Guid>())
                .Returns(lessonMaterialRepoMock.Object);
            lessonMaterialRepoMock.Setup(x => x.GetByIdAsync(lessonMaterialId))
                .ReturnsAsync(new LessonMaterial { Id = lessonMaterialId, Title = "Test Lesson" });

            var command = new DeleteQuestionCommentCommand { Id = commentId, DeletedByUserId = userId };
            var user = new ApplicationUser { Id = userId };
            var creator = new ApplicationUser { Id = creatorId };
            var comment = new QuestionComment { Id = commentId, Status = EntityStatus.Active, QuestionId = questionId, CreatedByUserId = creatorId };
            var question = new LessonMaterialQuestion { Id = questionId, LessonMaterialId = lessonMaterialId, Title = "Test Question" };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userRepositoryMock.Setup(x => x.GetByIdAsync(creatorId))
                .ReturnsAsync(creator);
            _commentRepositoryMock.Setup(x => x.GetByIdAsync(commentId))
                .ReturnsAsync(comment);
            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(question);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Student" });
            _userManagerMock.Setup(x => x.GetRolesAsync(creator))
                .ReturnsAsync(new List<string> { "Student" });
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Student");
            _commentRepositoryMock.Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<QuestionComment>());

            // Act & Assert
            var exception = Assert.ThrowsAsync<AppException>(() =>
                _handler.Handle(command, CancellationToken.None));

            Assert.That(exception.StatusCode, Is.EqualTo(CustomCode.InsufficientPermissionToDeleteComment));
        }

        #endregion

        #region Success Response Tests

        [Test]
        public async Task Handle_ShouldDeleteCommentWithReplies_WhenUserIsSystemAdmin()
        {
            // Arrange
            var commentId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();
            var replyId = Guid.NewGuid();
            var command = new DeleteQuestionCommentCommand { Id = commentId, DeletedByUserId = userId };
            var user = new ApplicationUser { Id = userId };
            var comment = new QuestionComment { Id = commentId, Status = EntityStatus.Active, QuestionId = questionId };
            var question = new LessonMaterialQuestion { Id = questionId, LessonMaterialId = lessonMaterialId, Title = "Test Question" };
            var reply = new QuestionComment { Id = replyId, ParentCommentId = commentId, Status = EntityStatus.Active };

            // Mock lesson material repo
            var lessonMaterialRepoMock = new Mock<IGenericRepository<LessonMaterial, Guid>>();
            _unitOfWorkMock.Setup(x => x.GetRepository<LessonMaterial, Guid>())
                .Returns(lessonMaterialRepoMock.Object);
            lessonMaterialRepoMock.Setup(x => x.GetByIdAsync(lessonMaterialId))
                .ReturnsAsync(new LessonMaterial { Id = lessonMaterialId, Title = "Test Lesson" });

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _commentRepositoryMock.Setup(x => x.GetByIdAsync(commentId))
                .ReturnsAsync(comment);
            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(question);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "SystemAdmin" });
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("SystemAdmin");
            _commentRepositoryMock.Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<QuestionComment> { reply });
            _unitOfWorkMock.Setup(x => x.CommitAsync())
                .ReturnsAsync(1);
            _hubNotificationServiceMock.Setup(x => x.NotifyQuestionCommentDeletedAsync(
                It.IsAny<QuestionCommentResponse>(),
                lessonMaterialId,
                It.IsAny<string>(),
                It.IsAny<string>(),
                1, It.IsAny<Guid?>()))
            .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result, Is.True);
            _commentRepositoryMock.Verify(x => x.Remove(reply), Times.Once);
            _commentRepositoryMock.Verify(x => x.Remove(comment), Times.Once);
            _unitOfWorkMock.Verify(x => x.CommitAsync(), Times.Once);
            _hubNotificationServiceMock.Verify(x => x.NotifyQuestionCommentDeletedAsync(
             It.IsAny<QuestionCommentResponse>(),
             lessonMaterialId,
             It.IsAny<string>(),
             It.IsAny<string>(),
             1, It.IsAny<Guid?>()), Times.Once);
        }

        #endregion

        #region Exception Handling Tests

        [Test]
        public void Handle_ShouldThrowException_WhenRepositoryThrowsException()
        {
            // Arrange
            var commentId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var command = new DeleteQuestionCommentCommand { Id = commentId, DeletedByUserId = userId };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            var exception = Assert.ThrowsAsync<Exception>(() =>
                _handler.Handle(command, CancellationToken.None));

            Assert.That(exception.Message, Is.EqualTo("Database error"));
        }

        #endregion
    }
}
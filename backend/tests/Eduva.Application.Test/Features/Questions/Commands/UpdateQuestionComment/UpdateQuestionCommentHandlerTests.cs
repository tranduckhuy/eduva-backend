using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.Questions.Commands.UpdateQuestionComment;
using Eduva.Application.Features.Questions.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Eduva.Application.Test.Features.Questions.Commands.UpdateQuestionComment
{
    [TestFixture]
    public class UpdateQuestionCommentHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;
        private Mock<IHubNotificationService> _hubNotificationServiceMock = null!;
        private Mock<IQuestionPermissionService> _permissionServiceMock = null!;
        private Mock<IGenericRepository<ApplicationUser, Guid>> _userRepositoryMock = null!;
        private Mock<IGenericRepository<QuestionComment, Guid>> _commentRepositoryMock = null!;
        private Mock<IGenericRepository<LessonMaterialQuestion, Guid>> _questionRepositoryMock = null!;
        private UpdateQuestionCommentHandler _handler = null!;

        #region Setup

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _userRepositoryMock = new Mock<IGenericRepository<ApplicationUser, Guid>>();
            _commentRepositoryMock = new Mock<IGenericRepository<QuestionComment, Guid>>();
            _questionRepositoryMock = new Mock<IGenericRepository<LessonMaterialQuestion, Guid>>();

            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            _hubNotificationServiceMock = new Mock<IHubNotificationService>();
            _permissionServiceMock = new Mock<IQuestionPermissionService>();

            _unitOfWorkMock.Setup(x => x.GetRepository<ApplicationUser, Guid>())
                .Returns(_userRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.GetRepository<QuestionComment, Guid>())
                .Returns(_commentRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.GetRepository<LessonMaterialQuestion, Guid>())
                .Returns(_questionRepositoryMock.Object);

            _handler = new UpdateQuestionCommentHandler(
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
            var command = new UpdateQuestionCommentCommand
            {
                Id = Guid.NewGuid(),
                Content = "Updated content",
                UpdatedByUserId = Guid.NewGuid()
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((ApplicationUser?)null);

            // Act & Assert
            Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(command, CancellationToken.None));
        }

        #endregion

        #region Comment Validation Tests

        [Test]
        public void Handle_ShouldThrowCommentNotFound_WhenCommentDoesNotExist()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var commentId = Guid.NewGuid();
            var command = new UpdateQuestionCommentCommand
            {
                Id = commentId,
                Content = "Updated content",
                UpdatedByUserId = userId
            };
            var user = new ApplicationUser { Id = userId };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(["Student"]);
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Student");
            _commentRepositoryMock.Setup(x => x.GetByIdAsync(commentId))
                .ReturnsAsync((QuestionComment?)null);

            // Act & Assert
            Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(command, CancellationToken.None));
        }

        [Test]
        public void Handle_ShouldThrowCommentNotActive_WhenCommentStatusIsNotActive()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var commentId = Guid.NewGuid();
            var command = new UpdateQuestionCommentCommand
            {
                Id = commentId,
                Content = "Updated content",
                UpdatedByUserId = userId
            };
            var user = new ApplicationUser { Id = userId };
            var comment = new QuestionComment
            {
                Id = commentId,
                Status = EntityStatus.Inactive,
                QuestionId = Guid.NewGuid()
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(["Student"]);
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Student");
            _commentRepositoryMock.Setup(x => x.GetByIdAsync(commentId))
                .ReturnsAsync(comment);
            _commentRepositoryMock.Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<QuestionComment>());

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(command, CancellationToken.None));
            Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.CommentNotActive));
        }

        #endregion

        #region Permission Validation Tests

        [Test]
        public void Handle_ShouldThrowInsufficientPermissionToUpdateComment_WhenUserIsNotOwnerAndNotSystemAdmin()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var commentId = Guid.NewGuid();
            var commentOwnerId = Guid.NewGuid();
            var command = new UpdateQuestionCommentCommand
            {
                Id = commentId,
                Content = "Updated content",
                UpdatedByUserId = userId
            };
            var user = new ApplicationUser { Id = userId };
            var comment = new QuestionComment
            {
                Id = commentId,
                Status = EntityStatus.Active,
                CreatedByUserId = commentOwnerId,
                QuestionId = Guid.NewGuid()
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(["Student"]);
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Student");
            _commentRepositoryMock.Setup(x => x.GetByIdAsync(commentId))
                .ReturnsAsync(comment);
            _commentRepositoryMock.Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<QuestionComment>());

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(command, CancellationToken.None));
            Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.InsufficientPermissionToUpdateComment));
        }

        [Test]
        public async Task Handle_ShouldAllowUpdate_WhenUserIsSystemAdmin()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var commentId = Guid.NewGuid();
            var commentOwnerId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();

            // Mock lesson material repo
            var lessonMaterialRepoMock = new Mock<IGenericRepository<LessonMaterial, Guid>>();
            _unitOfWorkMock.Setup(x => x.GetRepository<LessonMaterial, Guid>())
                .Returns(lessonMaterialRepoMock.Object);
            lessonMaterialRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new LessonMaterial { Id = lessonMaterialId, Title = "Test Lesson" });

            var commentCreator = new ApplicationUser { Id = commentOwnerId, FullName = "Owner", AvatarUrl = "avatar.jpg" };
            _userRepositoryMock.Setup(x => x.GetByIdAsync(commentOwnerId))
                .ReturnsAsync(commentCreator);
            _userManagerMock.Setup(x => x.GetRolesAsync(commentCreator))
                .ReturnsAsync(new List<string> { "Student" });
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Student");

            var command = new UpdateQuestionCommentCommand
            {
                Id = commentId,
                Content = "Updated content",
                UpdatedByUserId = userId
            };
            var user = new ApplicationUser { Id = userId };
            var comment = new QuestionComment
            {
                Id = commentId,
                Status = EntityStatus.Active,
                CreatedByUserId = commentOwnerId,
                QuestionId = Guid.NewGuid()
            };
            var question = new LessonMaterialQuestion
            {
                Id = comment.QuestionId,
                Title = "Test Question",
                LessonMaterialId = lessonMaterialId
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "SystemAdmin" });
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("SystemAdmin");
            _commentRepositoryMock.Setup(x => x.GetByIdAsync(commentId))
                .ReturnsAsync(comment);
            _commentRepositoryMock.Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<QuestionComment>());
            _questionRepositoryMock.Setup(x => x.GetByIdAsync(comment.QuestionId))
                .ReturnsAsync(question);
            _commentRepositoryMock.Setup(x => x.Update(It.IsAny<QuestionComment>()))
                .Verifiable();
            _unitOfWorkMock.Setup(x => x.CommitAsync())
                .ReturnsAsync(1);
            _permissionServiceMock.Setup(x => x.GetUserRoleSafelyAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync("SystemAdmin");
            _permissionServiceMock.Setup(x => x.CanUserUpdateComment(It.IsAny<QuestionComment>(), It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .Returns(true);
            _permissionServiceMock.Setup(x => x.CanUserDeleteCommentAsync(It.IsAny<QuestionComment>(), It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            _hubNotificationServiceMock.Setup(x => x.NotifyQuestionCommentUpdatedAsync(
                 It.IsAny<QuestionCommentResponse>(),
                 It.IsAny<Guid>(),
                 It.IsAny<string>(),
                 It.IsAny<string>(),
                 It.IsAny<Guid?>()))
             .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Content, Is.EqualTo("Updated content"));
            });

            _commentRepositoryMock.Verify(x => x.Update(It.IsAny<QuestionComment>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.CommitAsync(), Times.Once);
        }

        [Test]
        public async Task Handle_ShouldAllowUpdate_WhenUserIsCommentOwner()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var commentId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();

            var lessonMaterialRepoMock = new Mock<IGenericRepository<LessonMaterial, Guid>>();
            _unitOfWorkMock.Setup(x => x.GetRepository<LessonMaterial, Guid>())
                .Returns(lessonMaterialRepoMock.Object);
            lessonMaterialRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new LessonMaterial { Id = lessonMaterialId, Title = "Test Lesson" });

            var command = new UpdateQuestionCommentCommand
            {
                Id = commentId,
                Content = "Updated content",
                UpdatedByUserId = userId
            };
            var user = new ApplicationUser { Id = userId, FullName = "Test User", AvatarUrl = "avatar.jpg" };
            var comment = new QuestionComment
            {
                Id = commentId,
                Status = EntityStatus.Active,
                CreatedByUserId = userId,
                QuestionId = Guid.NewGuid()
            };
            var question = new LessonMaterialQuestion
            {
                Id = comment.QuestionId,
                Title = "Test Question",
                LessonMaterialId = lessonMaterialId
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Student" });
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Student");
            _commentRepositoryMock.Setup(x => x.GetByIdAsync(commentId))
                .ReturnsAsync(comment);
            _commentRepositoryMock.Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<QuestionComment>());
            _questionRepositoryMock.Setup(x => x.GetByIdAsync(comment.QuestionId))
                .ReturnsAsync(question);
            _commentRepositoryMock.Setup(x => x.Update(It.IsAny<QuestionComment>()))
                .Verifiable();
            _unitOfWorkMock.Setup(x => x.CommitAsync())
                .ReturnsAsync(1);
            _permissionServiceMock.Setup(x => x.GetUserRoleSafelyAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync("Student");
            _permissionServiceMock.Setup(x => x.CanUserUpdateComment(It.IsAny<QuestionComment>(), It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .Returns(true);
            _permissionServiceMock.Setup(x => x.CanUserDeleteCommentAsync(It.IsAny<QuestionComment>(), It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            _hubNotificationServiceMock.Setup(x => x.NotifyQuestionCommentUpdatedAsync(
                It.IsAny<QuestionCommentResponse>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Guid?>()))
            .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Content, Is.EqualTo("Updated content"));
                Assert.That(result.CreatedByName, Is.EqualTo("Test User"));
                Assert.That(result.CreatedByAvatar, Is.EqualTo("avatar.jpg"));
                Assert.That(result.CreatedByRole, Is.EqualTo("Student"));
                Assert.That(result.CanUpdate, Is.True);
                Assert.That(result.CanDelete, Is.True);
                Assert.That(result.ReplyCount, Is.EqualTo(0));
                Assert.That(result.Replies, Is.Empty);
            });

            _commentRepositoryMock.Verify(x => x.Update(It.IsAny<QuestionComment>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.CommitAsync(), Times.Once);
            _hubNotificationServiceMock.Verify(x => x.NotifyQuestionCommentUpdatedAsync(
                 It.IsAny<QuestionCommentResponse>(),
                 It.IsAny<Guid>(),
                 It.IsAny<string>(),
                 It.IsAny<string>(),
                 It.IsAny<Guid?>()), Times.Once);
        }

        #endregion

        #region Question Validation Tests

        [Test]
        public void Handle_ShouldThrowQuestionNotFound_WhenQuestionDoesNotExist()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var commentId = Guid.NewGuid();
            var command = new UpdateQuestionCommentCommand
            {
                Id = commentId,
                Content = "Updated content",
                UpdatedByUserId = userId
            };
            var user = new ApplicationUser { Id = userId };
            var comment = new QuestionComment
            {
                Id = commentId,
                Status = EntityStatus.Active,
                CreatedByUserId = userId,
                QuestionId = Guid.NewGuid()
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(["Student"]);
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Student");
            _commentRepositoryMock.Setup(x => x.GetByIdAsync(commentId))
                .ReturnsAsync(comment);
            _commentRepositoryMock.Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<QuestionComment>());
            _questionRepositoryMock.Setup(x => x.GetByIdAsync(comment.QuestionId))
                .ReturnsAsync((LessonMaterialQuestion?)null);

            // Act & Assert
            Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(command, CancellationToken.None));
        }

        #endregion

        #region Success Response Tests

        [Test]
        public async Task Handle_ShouldUpdateCommentSuccessfully_WhenUserIsCommentOwner()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var commentId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();

            // Mock lesson material repo
            var lessonMaterialRepoMock = new Mock<IGenericRepository<LessonMaterial, Guid>>();
            _unitOfWorkMock.Setup(x => x.GetRepository<LessonMaterial, Guid>())
                .Returns(lessonMaterialRepoMock.Object);
            lessonMaterialRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new LessonMaterial { Id = lessonMaterialId, Title = "Test Lesson" });

            var command = new UpdateQuestionCommentCommand
            {
                Id = commentId,
                Content = "Updated content with spaces  ",
                UpdatedByUserId = userId
            };
            var user = new ApplicationUser { Id = userId, FullName = "Test User", AvatarUrl = "avatar.jpg" };
            var comment = new QuestionComment
            {
                Id = commentId,
                Status = EntityStatus.Active,
                CreatedByUserId = userId,
                QuestionId = Guid.NewGuid(),
                Content = "Original content"
            };
            var question = new LessonMaterialQuestion
            {
                Id = comment.QuestionId,
                Title = "Test Question",
                LessonMaterialId = lessonMaterialId
            };
            var replies = new List<QuestionComment>
            {
                new() { Id = Guid.NewGuid(), Status = EntityStatus.Active, ParentCommentId = commentId },
                new() { Id = Guid.NewGuid(), Status = EntityStatus.Active, ParentCommentId = commentId }
            };

            comment.Replies = replies;

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Student" });
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Student");
            _commentRepositoryMock.Setup(x => x.GetByIdAsync(commentId))
                .ReturnsAsync(comment);
            _commentRepositoryMock.Setup(x => x.GetAllAsync())
                .ReturnsAsync(replies);
            _questionRepositoryMock.Setup(x => x.GetByIdAsync(comment.QuestionId))
                .ReturnsAsync(question);
            _commentRepositoryMock.Setup(x => x.Update(It.IsAny<QuestionComment>()))
                .Verifiable();
            _unitOfWorkMock.Setup(x => x.CommitAsync())
                .ReturnsAsync(1);
            _permissionServiceMock.Setup(x => x.GetUserRoleSafelyAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync("Student");
            _permissionServiceMock.Setup(x => x.CanUserUpdateComment(It.IsAny<QuestionComment>(), It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .Returns(true);
            _permissionServiceMock.Setup(x => x.CanUserDeleteCommentAsync(It.IsAny<QuestionComment>(), It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            _hubNotificationServiceMock.Setup(x => x.NotifyQuestionCommentUpdatedAsync(
                  It.IsAny<QuestionCommentResponse>(),
                  It.IsAny<Guid>(),
                  It.IsAny<string>(),
                  It.IsAny<string>(),
                  It.IsAny<Guid?>()))
              .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Content, Is.EqualTo("Updated content with spaces"));
                Assert.That(result.ReplyCount, Is.EqualTo(2));
                Assert.That(result.CanUpdate, Is.True);
                Assert.That(result.CanDelete, Is.True);
            });

            _commentRepositoryMock.Verify(x => x.Update(It.Is<QuestionComment>(c =>
                c.Content == "Updated content with spaces" &&
                c.LastModifiedAt.HasValue)), Times.Once);
            _unitOfWorkMock.Verify(x => x.CommitAsync(), Times.Once);
            _hubNotificationServiceMock.Verify(x => x.NotifyQuestionCommentUpdatedAsync(
                  It.IsAny<QuestionCommentResponse>(),
                  It.IsAny<Guid>(),
                  It.IsAny<string>(),
                  It.IsAny<string>(),
                  It.IsAny<Guid?>()), Times.Once);
        }

        [Test]
        public async Task Handle_ShouldHandleCommentWithReplies_WhenCommentHasActiveReplies()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var commentId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();

            // Mock lesson material repo
            var lessonMaterialRepoMock = new Mock<IGenericRepository<LessonMaterial, Guid>>();
            _unitOfWorkMock.Setup(x => x.GetRepository<LessonMaterial, Guid>())
                .Returns(lessonMaterialRepoMock.Object);
            lessonMaterialRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new LessonMaterial { Id = lessonMaterialId, Title = "Test Lesson" });

            var command = new UpdateQuestionCommentCommand
            {
                Id = commentId,
                Content = "Updated content",
                UpdatedByUserId = userId
            };
            var user = new ApplicationUser { Id = userId };
            var comment = new QuestionComment
            {
                Id = commentId,
                Status = EntityStatus.Active,
                CreatedByUserId = userId,
                QuestionId = Guid.NewGuid()
            };
            var question = new LessonMaterialQuestion
            {
                Id = comment.QuestionId,
                Title = "Test Question",
                LessonMaterialId = lessonMaterialId
            };
            var replies = new List<QuestionComment>
            {
                new() { Id = Guid.NewGuid(), Status = EntityStatus.Active, ParentCommentId = commentId },
                new() { Id = Guid.NewGuid(), Status = EntityStatus.Inactive, ParentCommentId = commentId }, // Should be filtered out
                new() { Id = Guid.NewGuid(), Status = EntityStatus.Active, ParentCommentId = commentId }
            };

            comment.Replies = replies.Where(r => r.Status == EntityStatus.Active).ToList();

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Student" });
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Student");
            _commentRepositoryMock.Setup(x => x.GetByIdAsync(commentId))
                .ReturnsAsync(comment);
            _commentRepositoryMock.Setup(x => x.GetAllAsync())
                .ReturnsAsync(replies);
            _questionRepositoryMock.Setup(x => x.GetByIdAsync(comment.QuestionId))
                .ReturnsAsync(question);
            _commentRepositoryMock.Setup(x => x.Update(It.IsAny<QuestionComment>()))
                .Verifiable();
            _unitOfWorkMock.Setup(x => x.CommitAsync())
                .ReturnsAsync(1);
            _permissionServiceMock.Setup(x => x.GetUserRoleSafelyAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync("Student");
            _permissionServiceMock.Setup(x => x.CanUserUpdateComment(It.IsAny<QuestionComment>(), It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .Returns(true);
            _permissionServiceMock.Setup(x => x.CanUserDeleteCommentAsync(It.IsAny<QuestionComment>(), It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            _hubNotificationServiceMock.Setup(x => x.NotifyQuestionCommentUpdatedAsync(
                 It.IsAny<QuestionCommentResponse>(),
                 It.IsAny<Guid>(),
                 It.IsAny<string>(),
                 It.IsAny<string>(),
                 It.IsAny<Guid?>()))
             .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.ReplyCount, Is.EqualTo(2)); // Only active replies should be counted
            });

            _hubNotificationServiceMock.Verify(x => x.NotifyQuestionCommentUpdatedAsync(
                  It.IsAny<QuestionCommentResponse>(),
                  It.IsAny<Guid>(),
                  It.IsAny<string>(),
                  It.IsAny<string>(),
                  It.IsAny<Guid?>()), Times.Once);
        }

        #endregion

    }
}
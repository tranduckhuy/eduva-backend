using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.Questions.Commands.CreateQuestionComment;
using Eduva.Application.Features.Questions.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Eduva.Application.Test.Features.Questions.Commands.CreateQuestionComment
{
    [TestFixture]
    public class CreateQuestionCommentHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;
        private Mock<IHubNotificationService> _hubNotificationServiceMock = null!;
        private Mock<IQuestionPermissionService> _permissionServiceMock = null!;
        private Mock<IGenericRepository<ApplicationUser, Guid>> _userRepositoryMock = null!;
        private Mock<IGenericRepository<LessonMaterialQuestion, Guid>> _questionRepositoryMock = null!;
        private Mock<IGenericRepository<LessonMaterial, Guid>> _lessonRepositoryMock = null!;
        private Mock<IGenericRepository<QuestionComment, Guid>> _commentRepositoryMock = null!;
        private Mock<IStudentClassRepository> _studentClassRepositoryMock = null!;
        private CreateQuestionCommentHandler _handler = null!;

        #region Setup

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _userRepositoryMock = new Mock<IGenericRepository<ApplicationUser, Guid>>();
            _questionRepositoryMock = new Mock<IGenericRepository<LessonMaterialQuestion, Guid>>();
            _lessonRepositoryMock = new Mock<IGenericRepository<LessonMaterial, Guid>>();
            _commentRepositoryMock = new Mock<IGenericRepository<QuestionComment, Guid>>();
            _studentClassRepositoryMock = new Mock<IStudentClassRepository>();

            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            _hubNotificationServiceMock = new Mock<IHubNotificationService>();
            _permissionServiceMock = new Mock<IQuestionPermissionService>();

            _unitOfWorkMock.Setup(x => x.GetRepository<ApplicationUser, Guid>())
                .Returns(_userRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.GetRepository<LessonMaterialQuestion, Guid>())
                .Returns(_questionRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.GetRepository<LessonMaterial, Guid>())
                .Returns(_lessonRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.GetRepository<QuestionComment, Guid>())
                .Returns(_commentRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.GetCustomRepository<IStudentClassRepository>())
                .Returns(_studentClassRepositoryMock.Object);

            _handler = new CreateQuestionCommentHandler(
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
            var command = new CreateQuestionCommentCommand
            {
                QuestionId = Guid.NewGuid(),
                Content = "Test comment",
                CreatedByUserId = Guid.NewGuid()
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((ApplicationUser?)null);

            // Act & Assert
            Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(command, CancellationToken.None));
        }

        #endregion

        #region Question Validation Tests

        [Test]
        public void Handle_ShouldThrowQuestionNotFound_WhenQuestionDoesNotExist()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var command = new CreateQuestionCommentCommand
            {
                QuestionId = questionId,
                Content = "Test comment",
                CreatedByUserId = userId
            };
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(["Student"]);
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Student");
            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync((LessonMaterialQuestion?)null);

            // Act & Assert
            Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(command, CancellationToken.None));
        }

        [Test]
        public void Handle_ShouldThrowQuestionNotActive_WhenQuestionStatusIsNotActive()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var command = new CreateQuestionCommentCommand
            {
                QuestionId = questionId,
                Content = "Test comment",
                CreatedByUserId = userId
            };
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var question = new LessonMaterialQuestion
            {
                Id = questionId,
                Status = EntityStatus.Inactive,
                LessonMaterialId = Guid.NewGuid()
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(["Student"]);
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Student");
            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(question);

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(command, CancellationToken.None));
            Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.QuestionNotActive));
        }

        #endregion

        #region Lesson Material Validation Tests

        [Test]
        public void Handle_ShouldThrowLessonMaterialNotFound_WhenLessonMaterialDoesNotExist()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var lessonId = Guid.NewGuid();
            var command = new CreateQuestionCommentCommand
            {
                QuestionId = questionId,
                Content = "Test comment",
                CreatedByUserId = userId
            };
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var question = new LessonMaterialQuestion
            {
                Id = questionId,
                Status = EntityStatus.Active,
                LessonMaterialId = lessonId
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(["Student"]);
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Student");
            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(question);
            _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lessonId))
                .ReturnsAsync((LessonMaterial?)null);

            // Act & Assert
            Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(command, CancellationToken.None));
        }

        [Test]
        public void Handle_ShouldThrowLessonMaterialNotActive_WhenLessonStatusIsNotActive()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var lessonId = Guid.NewGuid();
            var command = new CreateQuestionCommentCommand
            {
                QuestionId = questionId,
                Content = "Test comment",
                CreatedByUserId = userId
            };
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var question = new LessonMaterialQuestion
            {
                Id = questionId,
                Status = EntityStatus.Active,
                LessonMaterialId = lessonId
            };
            var lesson = new LessonMaterial
            {
                Id = lessonId,
                Status = EntityStatus.Inactive,
                SchoolId = 1
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(["Student"]);
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Student");
            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(question);
            _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lessonId))
                .ReturnsAsync(lesson);

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(command, CancellationToken.None));
            Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.LessonMaterialNotActive));
        }

        [Test]
        public void Handle_ShouldThrowCannotCreateQuestionForPendingLesson_WhenLessonStatusIsNotApproved()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var lessonId = Guid.NewGuid();
            var command = new CreateQuestionCommentCommand
            {
                QuestionId = questionId,
                Content = "Test comment",
                CreatedByUserId = userId
            };
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var question = new LessonMaterialQuestion
            {
                Id = questionId,
                Status = EntityStatus.Active,
                LessonMaterialId = lessonId
            };
            var lesson = new LessonMaterial
            {
                Id = lessonId,
                Status = EntityStatus.Active,
                LessonStatus = LessonMaterialStatus.Pending,
                SchoolId = 1
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(["Student"]);
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Student");
            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(question);
            _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lessonId))
                .ReturnsAsync(lesson);

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(command, CancellationToken.None));
            Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.CannotCreateQuestionForPendingLesson));
        }

        [Test]
        public void Handle_ShouldThrowUserNotPartOfSchool_WhenUserSchoolIdDoesNotMatchLessonSchoolId()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var lessonId = Guid.NewGuid();
            var command = new CreateQuestionCommentCommand
            {
                QuestionId = questionId,
                Content = "Test comment",
                CreatedByUserId = userId
            };
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var question = new LessonMaterialQuestion
            {
                Id = questionId,
                Status = EntityStatus.Active,
                LessonMaterialId = lessonId
            };
            var lesson = new LessonMaterial
            {
                Id = lessonId,
                Status = EntityStatus.Active,
                LessonStatus = LessonMaterialStatus.Approved,
                SchoolId = 2
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(["Student"]);
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Student");
            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(question);
            _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lessonId))
                .ReturnsAsync(lesson);

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(command, CancellationToken.None));
            Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.UserNotPartOfSchool));
        }

        #endregion

        #region Student Access Validation Tests

        [Test]
        public void Handle_ShouldThrowStudentNotEnrolledInAnyClassForQuestions_WhenStudentNotEnrolledInAnyClass()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var lessonId = Guid.NewGuid();
            var command = new CreateQuestionCommentCommand
            {
                QuestionId = questionId,
                Content = "Test comment",
                CreatedByUserId = userId
            };
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var question = new LessonMaterialQuestion
            {
                Id = questionId,
                Status = EntityStatus.Active,
                LessonMaterialId = lessonId
            };
            var lesson = new LessonMaterial
            {
                Id = lessonId,
                Status = EntityStatus.Active,
                LessonStatus = LessonMaterialStatus.Approved,
                SchoolId = 1
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(["Student"]);
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Student");
            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(question);
            _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lessonId))
                .ReturnsAsync(lesson);
            _studentClassRepositoryMock.Setup(x => x.HasAccessToMaterialAsync(userId, lessonId))
                .ReturnsAsync(false);
            _studentClassRepositoryMock.Setup(x => x.IsEnrolledInAnyClassAsync(userId))
                .ReturnsAsync(false);

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(command, CancellationToken.None));
            Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.StudentNotEnrolledInAnyClassForQuestions));
        }

        [Test]
        public void Handle_ShouldThrowCannotCreateQuestionForLessonNotAccessible_WhenStudentEnrolledButNoAccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var lessonId = Guid.NewGuid();
            var command = new CreateQuestionCommentCommand
            {
                QuestionId = questionId,
                Content = "Test comment",
                CreatedByUserId = userId
            };
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var question = new LessonMaterialQuestion
            {
                Id = questionId,
                Status = EntityStatus.Active,
                LessonMaterialId = lessonId
            };
            var lesson = new LessonMaterial
            {
                Id = lessonId,
                Status = EntityStatus.Active,
                LessonStatus = LessonMaterialStatus.Approved,
                SchoolId = 1
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(["Student"]);
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Student");
            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(question);
            _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lessonId))
                .ReturnsAsync(lesson);
            _studentClassRepositoryMock.Setup(x => x.HasAccessToMaterialAsync(userId, lessonId))
                .ReturnsAsync(false);
            _studentClassRepositoryMock.Setup(x => x.IsEnrolledInAnyClassAsync(userId))
                .ReturnsAsync(true);

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(command, CancellationToken.None));
            Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.CannotCreateQuestionForLessonNotAccessible));
        }

        #endregion

        #region Teacher Access Validation Tests

        [Test]
        public void Handle_ShouldThrowTeacherNotHaveAccessToMaterial_WhenTeacherHasNoAccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var lessonId = Guid.NewGuid();
            var command = new CreateQuestionCommentCommand
            {
                QuestionId = questionId,
                Content = "Test comment",
                CreatedByUserId = userId
            };
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var question = new LessonMaterialQuestion
            {
                Id = questionId,
                Status = EntityStatus.Active,
                LessonMaterialId = lessonId
            };
            var lesson = new LessonMaterial
            {
                Id = lessonId,
                Status = EntityStatus.Active,
                LessonStatus = LessonMaterialStatus.Approved,
                SchoolId = 1
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(["Teacher"]);
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Teacher");
            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(question);
            _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lessonId))
                .ReturnsAsync(lesson);
            _studentClassRepositoryMock.Setup(x => x.TeacherHasAccessToMaterialAsync(userId, lessonId))
                .ReturnsAsync(false);

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(command, CancellationToken.None));
            Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.TeacherNotHaveAccessToMaterial));
        }

        #endregion

        #region Parent Comment Validation Tests

        [Test]
        public void Handle_ShouldThrowParentCommentNotFound_WhenParentCommentDoesNotExist()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var lessonId = Guid.NewGuid();
            var parentCommentId = Guid.NewGuid();
            var command = new CreateQuestionCommentCommand
            {
                QuestionId = questionId,
                Content = "Test reply",
                ParentCommentId = parentCommentId,
                CreatedByUserId = userId
            };
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var question = new LessonMaterialQuestion
            {
                Id = questionId,
                Status = EntityStatus.Active,
                LessonMaterialId = lessonId
            };
            var lesson = new LessonMaterial
            {
                Id = lessonId,
                Status = EntityStatus.Active,
                LessonStatus = LessonMaterialStatus.Approved,
                SchoolId = 1
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(["Student"]);
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Student");
            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(question);
            _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lessonId))
                .ReturnsAsync(lesson);
            _studentClassRepositoryMock.Setup(x => x.HasAccessToMaterialAsync(userId, lessonId))
                .ReturnsAsync(true);
            _commentRepositoryMock.Setup(x => x.GetByIdAsync(parentCommentId))
                .ReturnsAsync((QuestionComment?)null);

            // Act & Assert
            Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(command, CancellationToken.None));
        }

        [Test]
        public void Handle_ShouldThrowParentCommentNotFound_WhenParentCommentQuestionIdDoesNotMatch()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var lessonId = Guid.NewGuid();
            var parentCommentId = Guid.NewGuid();
            var command = new CreateQuestionCommentCommand
            {
                QuestionId = questionId,
                Content = "Test reply",
                ParentCommentId = parentCommentId,
                CreatedByUserId = userId
            };
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var question = new LessonMaterialQuestion
            {
                Id = questionId,
                Status = EntityStatus.Active,
                LessonMaterialId = lessonId
            };
            var lesson = new LessonMaterial
            {
                Id = lessonId,
                Status = EntityStatus.Active,
                LessonStatus = LessonMaterialStatus.Approved,
                SchoolId = 1
            };
            var parentComment = new QuestionComment
            {
                Id = parentCommentId,
                QuestionId = Guid.NewGuid(),
                Status = EntityStatus.Active
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(["Student"]);
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Student");
            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(question);
            _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lessonId))
                .ReturnsAsync(lesson);
            _studentClassRepositoryMock.Setup(x => x.HasAccessToMaterialAsync(userId, lessonId))
                .ReturnsAsync(true);
            _commentRepositoryMock.Setup(x => x.GetByIdAsync(parentCommentId))
                .ReturnsAsync(parentComment);

            // Act & Assert
            Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(command, CancellationToken.None));
        }

        [Test]
        public void Handle_ShouldThrowCommentNotActive_WhenParentCommentStatusIsNotActive()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var lessonId = Guid.NewGuid();
            var parentCommentId = Guid.NewGuid();
            var command = new CreateQuestionCommentCommand
            {
                QuestionId = questionId,
                Content = "Test reply",
                ParentCommentId = parentCommentId,
                CreatedByUserId = userId
            };
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var question = new LessonMaterialQuestion
            {
                Id = questionId,
                Status = EntityStatus.Active,
                LessonMaterialId = lessonId
            };
            var lesson = new LessonMaterial
            {
                Id = lessonId,
                Status = EntityStatus.Active,
                LessonStatus = LessonMaterialStatus.Approved,
                SchoolId = 1
            };
            var parentComment = new QuestionComment
            {
                Id = parentCommentId,
                QuestionId = questionId,
                Status = EntityStatus.Inactive
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(["Student"]);
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Student");
            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(question);
            _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lessonId))
                .ReturnsAsync(lesson);
            _studentClassRepositoryMock.Setup(x => x.HasAccessToMaterialAsync(userId, lessonId))
                .ReturnsAsync(true);
            _commentRepositoryMock.Setup(x => x.GetByIdAsync(parentCommentId))
                .ReturnsAsync(parentComment);

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(command, CancellationToken.None));
            Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.CommentNotActive));
        }

        [Test]
        public async Task Handle_ShouldFlattenReply_WhenReplyingToReply()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var lessonId = Guid.NewGuid();
            var topLevelCommentId = Guid.NewGuid();
            var secondLevelCommentId = Guid.NewGuid();

            var command = new CreateQuestionCommentCommand
            {
                QuestionId = questionId,
                Content = "Reply to a reply",
                ParentCommentId = secondLevelCommentId,
                CreatedByUserId = userId
            };

            var user = new ApplicationUser { Id = userId, SchoolId = 1, FullName = "Test User", AvatarUrl = "avatar.jpg" };
            var question = new LessonMaterialQuestion
            {
                Id = questionId,
                Status = EntityStatus.Active,
                LessonMaterialId = lessonId
            };
            var lesson = new LessonMaterial
            {
                Id = lessonId,
                Status = EntityStatus.Active,
                LessonStatus = LessonMaterialStatus.Approved,
                SchoolId = 1
            };

            var secondLevelComment = new QuestionComment
            {
                Id = secondLevelCommentId,
                QuestionId = questionId,
                Status = EntityStatus.Active,
                ParentCommentId = topLevelCommentId
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Student" });
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>())).Returns("Student");
            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId)).ReturnsAsync(question);
            _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lessonId)).ReturnsAsync(lesson);
            _commentRepositoryMock.Setup(x => x.GetByIdAsync(secondLevelCommentId)).ReturnsAsync(secondLevelComment);

            var studentClassRepoMock = new Mock<IStudentClassRepository>();
            studentClassRepoMock.Setup(x => x.HasAccessToMaterialAsync(userId, lessonId))
                .ReturnsAsync(true);
            studentClassRepoMock.Setup(x => x.IsEnrolledInAnyClassAsync(userId))
                .ReturnsAsync(true);

            _unitOfWorkMock.Setup(x => x.GetCustomRepository<IStudentClassRepository>())
                .Returns(studentClassRepoMock.Object);

            _commentRepositoryMock.Setup(x => x.AddAsync(It.IsAny<QuestionComment>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(x => x.CommitAsync()).ReturnsAsync(1);
            _hubNotificationServiceMock.Setup(x => x.NotifyQuestionCommentedAsync(
                    It.IsAny<QuestionCommentResponse>(),
                    lessonId,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Guid?>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Content, Is.EqualTo("Reply to a reply"));

            _commentRepositoryMock.Verify(x => x.AddAsync(It.Is<QuestionComment>(c =>
                c.ParentCommentId == topLevelCommentId)), Times.Once);
        }

        #endregion

        #region Success Response Tests

        [Test]
        public async Task Handle_ShouldCreateCommentSuccessfully_WhenUserIsStudent()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var lessonId = Guid.NewGuid();
            var command = new CreateQuestionCommentCommand
            {
                QuestionId = questionId,
                Content = "Test comment",
                CreatedByUserId = userId
            };
            var user = new ApplicationUser { Id = userId, SchoolId = 1, FullName = "Test User", AvatarUrl = "avatar.jpg" };
            var question = new LessonMaterialQuestion
            {
                Id = questionId,
                Status = EntityStatus.Active,
                LessonMaterialId = lessonId
            };
            var lesson = new LessonMaterial
            {
                Id = lessonId,
                Status = EntityStatus.Active,
                LessonStatus = LessonMaterialStatus.Approved,
                SchoolId = 1
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(["Student"]);
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Student");
            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(question);
            _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lessonId))
                .ReturnsAsync(lesson);
            _studentClassRepositoryMock.Setup(x => x.HasAccessToMaterialAsync(userId, lessonId))
                .ReturnsAsync(true);
            _commentRepositoryMock.Setup(x => x.AddAsync(It.IsAny<QuestionComment>()))
                .Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(x => x.CommitAsync())
                .ReturnsAsync(1);
            _hubNotificationServiceMock.Setup(x => x.NotifyQuestionCommentedAsync(
                It.IsAny<QuestionCommentResponse>(),
                lessonId,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Guid?>()))
            .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Content, Is.EqualTo("Test comment"));
                Assert.That(result.CreatedByName, Is.EqualTo("Test User"));
                Assert.That(result.CreatedByAvatar, Is.EqualTo("avatar.jpg"));
                Assert.That(result.CreatedByRole, Is.EqualTo("Student"));
                Assert.That(result.CanUpdate, Is.True);
                Assert.That(result.CanDelete, Is.True);
                Assert.That(result.ReplyCount, Is.EqualTo(0));
                Assert.That(result.Replies, Is.Empty);
            });

            _commentRepositoryMock.Verify(x => x.AddAsync(It.IsAny<QuestionComment>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.CommitAsync(), Times.Once);
            _hubNotificationServiceMock.Verify(x => x.NotifyQuestionCommentedAsync(
                It.IsAny<QuestionCommentResponse>(),
                lessonId,
                It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<Guid?>()), Times.Once);
        }

        [Test]
        public async Task Handle_ShouldCreateCommentSuccessfully_WhenUserIsSchoolAdmin()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var lessonId = Guid.NewGuid();
            var command = new CreateQuestionCommentCommand
            {
                QuestionId = questionId,
                Content = "Test comment",
                CreatedByUserId = userId
            };
            var user = new ApplicationUser { Id = userId, SchoolId = 1, FullName = "Admin User", AvatarUrl = "admin.jpg" };
            var question = new LessonMaterialQuestion
            {
                Id = questionId,
                Status = EntityStatus.Active,
                LessonMaterialId = lessonId
            };
            var lesson = new LessonMaterial
            {
                Id = lessonId,
                Status = EntityStatus.Active,
                LessonStatus = LessonMaterialStatus.Approved,
                SchoolId = 1
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(["SchoolAdmin"]);
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("SchoolAdmin");
            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(question);
            _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lessonId))
                .ReturnsAsync(lesson);
            _commentRepositoryMock.Setup(x => x.AddAsync(It.IsAny<QuestionComment>()))
                .Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(x => x.CommitAsync())
                .ReturnsAsync(1);
            _hubNotificationServiceMock.Setup(x => x.NotifyQuestionCommentedAsync(
                 It.IsAny<QuestionCommentResponse>(),
                 lessonId,
                 It.IsAny<string>(),
                 It.IsAny<string>(), It.IsAny<Guid?>()))
             .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.CreatedByRole, Is.EqualTo("SchoolAdmin"));
        }

        [Test]
        public async Task Handle_ShouldCreateReplySuccessfully_WhenParentCommentIsValid()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var lessonId = Guid.NewGuid();
            var parentCommentId = Guid.NewGuid();
            var command = new CreateQuestionCommentCommand
            {
                QuestionId = questionId,
                Content = "Test reply",
                ParentCommentId = parentCommentId,
                CreatedByUserId = userId
            };
            var user = new ApplicationUser { Id = userId, SchoolId = 1, FullName = "Test User", AvatarUrl = "avatar.jpg" };
            var question = new LessonMaterialQuestion
            {
                Id = questionId,
                Status = EntityStatus.Active,
                LessonMaterialId = lessonId
            };
            var lesson = new LessonMaterial
            {
                Id = lessonId,
                Status = EntityStatus.Active,
                LessonStatus = LessonMaterialStatus.Approved,
                SchoolId = 1
            };
            var parentComment = new QuestionComment
            {
                Id = parentCommentId,
                QuestionId = questionId,
                Status = EntityStatus.Active,
                ParentCommentId = null
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(["Student"]);
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Student");
            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(question);
            _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lessonId))
                .ReturnsAsync(lesson);
            _studentClassRepositoryMock.Setup(x => x.HasAccessToMaterialAsync(userId, lessonId))
                .ReturnsAsync(true);
            _commentRepositoryMock.Setup(x => x.GetByIdAsync(parentCommentId))
                .ReturnsAsync(parentComment);
            _commentRepositoryMock.Setup(x => x.AddAsync(It.IsAny<QuestionComment>()))
                .Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(x => x.CommitAsync())
                .ReturnsAsync(1);
            _hubNotificationServiceMock.Setup(x => x.NotifyQuestionCommentedAsync(
                  It.IsAny<QuestionCommentResponse>(),
                  lessonId,
                  It.IsAny<string>(),
                  It.IsAny<string>(), It.IsAny<Guid?>()))
              .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Content, Is.EqualTo("Test reply"));
                Assert.That(result.ParentCommentId, Is.EqualTo(parentCommentId));
            });
        }

        #endregion

        #region Exception Handling Tests

        [Test]
        public void Handle_ShouldThrowInsufficientPermissionToCreateComment_WhenUserRoleIsInvalid()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var lessonId = Guid.NewGuid();
            var command = new CreateQuestionCommentCommand
            {
                QuestionId = questionId,
                Content = "Test comment",
                CreatedByUserId = userId
            };
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var question = new LessonMaterialQuestion
            {
                Id = questionId,
                Status = EntityStatus.Active,
                LessonMaterialId = lessonId
            };
            var lesson = new LessonMaterial
            {
                Id = lessonId,
                Status = EntityStatus.Active,
                LessonStatus = LessonMaterialStatus.Approved,
                SchoolId = 1
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync([]);
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("InvalidRole");
            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(question);
            _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lessonId))
                .ReturnsAsync(lesson);

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(command, CancellationToken.None));
            Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.InsufficientPermissionToCreateComment));
        }

        #endregion

    }
}
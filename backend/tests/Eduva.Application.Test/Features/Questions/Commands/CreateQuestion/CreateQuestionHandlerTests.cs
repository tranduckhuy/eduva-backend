using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.Questions.Commands.CreateQuestion;
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

namespace Eduva.Application.Test.Features.Questions.Commands.CreateQuestion
{
    [TestFixture]
    public class CreateQuestionHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = default!;
        private Mock<UserManager<ApplicationUser>> _userManagerMock = default!;
        private Mock<IHubNotificationService> _hubNotificationServiceMock = default!;
        private Mock<IQuestionPermissionService> _permissionServiceMock = default!;
        private Mock<IGenericRepository<ApplicationUser, Guid>> _userRepositoryMock = default!;
        private Mock<IGenericRepository<LessonMaterial, Guid>> _lessonRepositoryMock = default!;
        private Mock<IGenericRepository<LessonMaterialQuestion, Guid>> _questionRepositoryMock = default!;
        private Mock<IStudentClassRepository> _studentClassRepositoryMock = default!;
        private Mock<IGenericRepository<FolderLessonMaterial, Guid>> _folderLessonRepositoryMock = default!;
        private CreateQuestionHandler _handler = default!;

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
            _lessonRepositoryMock = new Mock<IGenericRepository<LessonMaterial, Guid>>();
            _questionRepositoryMock = new Mock<IGenericRepository<LessonMaterialQuestion, Guid>>();
            _studentClassRepositoryMock = new Mock<IStudentClassRepository>();
            _folderLessonRepositoryMock = new Mock<IGenericRepository<FolderLessonMaterial, Guid>>();

            _unitOfWorkMock.Setup(x => x.GetRepository<ApplicationUser, Guid>())
                .Returns(_userRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.GetRepository<LessonMaterial, Guid>())
                .Returns(_lessonRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.GetRepository<LessonMaterialQuestion, Guid>())
                .Returns(_questionRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.GetRepository<FolderLessonMaterial, Guid>())
                .Returns(_folderLessonRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.GetCustomRepository<IStudentClassRepository>())
                .Returns(_studentClassRepositoryMock.Object);

            _handler = new CreateQuestionHandler(
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
            var userId = Guid.NewGuid();
            var command = new CreateQuestionCommand
            {
                LessonMaterialId = Guid.NewGuid(),
                Title = "Test Title",
                Content = "Test Content",
                CreatedByUserId = userId
            };

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
        public void Handle_ShouldThrowInsufficientPermission_WhenUserRoleIsNotAllowed()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new CreateQuestionCommand
            {
                LessonMaterialId = Guid.NewGuid(),
                Title = "Test Title",
                Content = "Test Content",
                CreatedByUserId = userId
            };
            var user = new ApplicationUser { Id = userId };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "SystemAdmin" });
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("SystemAdmin");

            // Act & Assert
            var exception = Assert.ThrowsAsync<AppException>(() =>
                _handler.Handle(command, CancellationToken.None));

            Assert.That(exception.StatusCode, Is.EqualTo(CustomCode.InsufficientPermissionToCreateQuestion));
        }

        #endregion

        #region Lesson Material Validation Tests

        [Test]
        public void Handle_ShouldThrowLessonMaterialNotFound_WhenLessonMaterialDoesNotExist()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();
            var command = new CreateQuestionCommand
            {
                LessonMaterialId = lessonMaterialId,
                Title = "Test Title",
                Content = "Test Content",
                CreatedByUserId = userId
            };
            var user = new ApplicationUser { Id = userId };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Teacher" });
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Teacher");
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
            var userId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();
            var command = new CreateQuestionCommand
            {
                LessonMaterialId = lessonMaterialId,
                Title = "Test Title",
                Content = "Test Content",
                CreatedByUserId = userId
            };
            var user = new ApplicationUser { Id = userId };
            var lessonMaterial = new LessonMaterial { Id = lessonMaterialId, Status = EntityStatus.Archived };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Teacher" });
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Teacher");
            _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lessonMaterialId))
                .ReturnsAsync(lessonMaterial);

            // Act & Assert
            var exception = Assert.ThrowsAsync<AppException>(() =>
                _handler.Handle(command, CancellationToken.None));

            Assert.That(exception.StatusCode, Is.EqualTo(CustomCode.LessonMaterialNotActive));
        }

        #endregion

        #region Teacher/Content Moderator Permission Tests

        [Test]
        public void Handle_ShouldThrowUserNotPartOfSchool_WhenTeacherHasNoSchoolId()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();
            var command = new CreateQuestionCommand
            {
                LessonMaterialId = lessonMaterialId,
                Title = "Test Title",
                Content = "Test Content",
                CreatedByUserId = userId
            };
            var user = new ApplicationUser { Id = userId, SchoolId = null };
            var lessonMaterial = new LessonMaterial { Id = lessonMaterialId, Status = EntityStatus.Active };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Teacher" });
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Teacher");
            _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lessonMaterialId))
                .ReturnsAsync(lessonMaterial);

            // Act & Assert
            var exception = Assert.ThrowsAsync<AppException>(() =>
                _handler.Handle(command, CancellationToken.None));

            Assert.That(exception.StatusCode, Is.EqualTo(CustomCode.UserNotPartOfSchool));
        }

        [Test]
        public void Handle_ShouldThrowCannotCreateQuestionForLessonNotInYourSchool_WhenTeacherAndLessonMaterialHaveDifferentSchools()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();
            var command = new CreateQuestionCommand
            {
                LessonMaterialId = lessonMaterialId,
                Title = "Test Title",
                Content = "Test Content",
                CreatedByUserId = userId
            };
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var lessonMaterial = new LessonMaterial { Id = lessonMaterialId, Status = EntityStatus.Active, SchoolId = 2 };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Teacher" });
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Teacher");
            _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lessonMaterialId))
                .ReturnsAsync(lessonMaterial);

            // Act & Assert
            var exception = Assert.ThrowsAsync<AppException>(() =>
                _handler.Handle(command, CancellationToken.None));

            Assert.That(exception.StatusCode, Is.EqualTo(CustomCode.CannotCreateQuestionForLessonNotInYourSchool));
        }

        [Test]
        public void Handle_ShouldThrowCannotCreateQuestionForPendingLesson_WhenLessonMaterialIsPending()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();
            var command = new CreateQuestionCommand
            {
                LessonMaterialId = lessonMaterialId,
                Title = "Test Title",
                Content = "Test Content",
                CreatedByUserId = userId
            };
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var lessonMaterial = new LessonMaterial { Id = lessonMaterialId, Status = EntityStatus.Active, SchoolId = 1, LessonStatus = LessonMaterialStatus.Pending };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Teacher" });
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Teacher");
            _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lessonMaterialId))
                .ReturnsAsync(lessonMaterial);

            // Act & Assert
            var exception = Assert.ThrowsAsync<AppException>(() =>
                _handler.Handle(command, CancellationToken.None));

            Assert.That(exception.StatusCode, Is.EqualTo(CustomCode.CannotCreateQuestionForPendingLesson));
        }

        #endregion

        #region Student Permission Tests

        [Test]
        public void Handle_ShouldThrowStudentNotEnrolledInAnyClass_WhenStudentHasNoEnrolledClasses()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();
            var command = new CreateQuestionCommand
            {
                LessonMaterialId = lessonMaterialId,
                Title = "Test Title",
                Content = "Test Content",
                CreatedByUserId = userId
            };
            var user = new ApplicationUser { Id = userId };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Student" });
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Student");
            var lessonMaterial = new LessonMaterial { Id = lessonMaterialId, Status = EntityStatus.Active };
            _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lessonMaterialId))
                .ReturnsAsync(lessonMaterial);
            _studentClassRepositoryMock.Setup(x => x.GetClassesForStudentAsync(userId))
                .ReturnsAsync(new List<Classroom>());

            // Act & Assert
            var exception = Assert.ThrowsAsync<AppException>(() =>
                _handler.Handle(command, CancellationToken.None));

            Assert.That(exception.StatusCode, Is.EqualTo(CustomCode.StudentNotEnrolledInAnyClass));
        }

        [Test]
        public void Handle_ShouldThrowCannotCreateQuestionForLessonNotAccessible_WhenStudentHasNoAccessToLesson()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();
            var command = new CreateQuestionCommand
            {
                LessonMaterialId = lessonMaterialId,
                Title = "Test Title",
                Content = "Test Content",
                CreatedByUserId = userId
            };
            var user = new ApplicationUser { Id = userId };
            var lessonMaterial = new LessonMaterial { Id = lessonMaterialId, Status = EntityStatus.Active };
            var enrolledClass = new Classroom { Id = Guid.NewGuid() };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Student" });
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Student");
            _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lessonMaterialId))
                .ReturnsAsync(lessonMaterial);
            _studentClassRepositoryMock.Setup(x => x.GetClassesForStudentAsync(userId))
                .ReturnsAsync(new List<Classroom> { enrolledClass });
            _folderLessonRepositoryMock.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<FolderLessonMaterial, bool>>>()))
                .ReturnsAsync(false);

            // Act & Assert
            var exception = Assert.ThrowsAsync<AppException>(() =>
                _handler.Handle(command, CancellationToken.None));

            Assert.That(exception.StatusCode, Is.EqualTo(CustomCode.CannotCreateQuestionForLessonNotAccessible));
        }

        #endregion

        #region Success Response Tests

        [Test]
        public async Task Handle_ShouldCreateQuestionSuccessfully_WhenTeacherHasValidPermissions()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();
            var command = new CreateQuestionCommand
            {
                LessonMaterialId = lessonMaterialId,
                Title = "Test Title",
                Content = "Test Content",
                CreatedByUserId = userId
            };
            var user = new ApplicationUser { Id = userId, SchoolId = 1, FullName = "Test User", AvatarUrl = "avatar.jpg" };
            var lessonMaterial = new LessonMaterial { Id = lessonMaterialId, Status = EntityStatus.Active, SchoolId = 1, Title = "Test Lesson", LessonStatus = LessonMaterialStatus.Approved };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Teacher" });
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Teacher");
            _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lessonMaterialId))
                .ReturnsAsync(lessonMaterial);
            _questionRepositoryMock.Setup(x => x.AddAsync(It.IsAny<LessonMaterialQuestion>()))
                .Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(x => x.CommitAsync())
                .ReturnsAsync(1);
            _hubNotificationServiceMock.Setup(x => x.NotifyQuestionCreatedAsync(
                It.IsAny<QuestionResponse>(),
                lessonMaterialId,
                It.IsAny<ApplicationUser?>()))
            .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.LessonMaterialId, Is.EqualTo(lessonMaterialId));
                Assert.That(result.Title, Is.EqualTo("Test Title"));
                Assert.That(result.Content, Is.EqualTo("Test Content"));
                Assert.That(result.CreatedByUserId, Is.EqualTo(userId));
                Assert.That(result.CreatedByName, Is.EqualTo("Test User"));
                Assert.That(result.CreatedByAvatar, Is.EqualTo("avatar.jpg"));
                Assert.That(result.CreatedByRole, Is.EqualTo("Teacher"));
                Assert.That(result.CommentCount, Is.EqualTo(0));
            });
            _questionRepositoryMock.Verify(x => x.AddAsync(It.IsAny<LessonMaterialQuestion>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.CommitAsync(), Times.Once);
            _hubNotificationServiceMock.Verify(x => x.NotifyQuestionCreatedAsync(It.IsAny<QuestionResponse>(), lessonMaterialId, It.IsAny<ApplicationUser?>()), Times.Once);
        }

        [Test]
        public async Task Handle_ShouldCreateQuestionSuccessfully_WhenStudentHasValidPermissions()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();
            var command = new CreateQuestionCommand
            {
                LessonMaterialId = lessonMaterialId,
                Title = "Test Title",
                Content = "Test Content",
                CreatedByUserId = userId
            };
            var user = new ApplicationUser { Id = userId, FullName = "Test Student", AvatarUrl = "avatar.jpg" };
            var lessonMaterial = new LessonMaterial { Id = lessonMaterialId, Status = EntityStatus.Active, Title = "Test Lesson" };
            var enrolledClass = new Classroom { Id = Guid.NewGuid() };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Student" });
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Student");
            _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lessonMaterialId))
                .ReturnsAsync(lessonMaterial);
            _studentClassRepositoryMock.Setup(x => x.GetClassesForStudentAsync(userId))
                .ReturnsAsync(new List<Classroom> { enrolledClass });
            _folderLessonRepositoryMock.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<FolderLessonMaterial, bool>>>()))
                .ReturnsAsync(true);
            _questionRepositoryMock.Setup(x => x.AddAsync(It.IsAny<LessonMaterialQuestion>()))
                .Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(x => x.CommitAsync())
                .ReturnsAsync(1);
            _hubNotificationServiceMock.Setup(x => x.NotifyQuestionCreatedAsync(It.IsAny<QuestionResponse>(), lessonMaterialId, It.IsAny<ApplicationUser?>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.LessonMaterialId, Is.EqualTo(lessonMaterialId));
                Assert.That(result.Title, Is.EqualTo("Test Title"));
                Assert.That(result.Content, Is.EqualTo("Test Content"));
                Assert.That(result.CreatedByUserId, Is.EqualTo(userId));
                Assert.That(result.CreatedByName, Is.EqualTo("Test Student"));
                Assert.That(result.CreatedByAvatar, Is.EqualTo("avatar.jpg"));
                Assert.That(result.CreatedByRole, Is.EqualTo("Student"));
                Assert.That(result.CommentCount, Is.EqualTo(0));
            });
            _questionRepositoryMock.Verify(x => x.AddAsync(It.IsAny<LessonMaterialQuestion>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.CommitAsync(), Times.Once);
            _hubNotificationServiceMock.Verify(x => x.NotifyQuestionCreatedAsync(It.IsAny<QuestionResponse>(), lessonMaterialId, It.IsAny<ApplicationUser?>()), Times.Once);
        }

        #endregion

        #region Exception Handling Tests

        [Test]
        public void Handle_ShouldThrowException_WhenRepositoryThrowsException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new CreateQuestionCommand
            {
                LessonMaterialId = Guid.NewGuid(),
                Title = "Test Title",
                Content = "Test Content",
                CreatedByUserId = userId
            };

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
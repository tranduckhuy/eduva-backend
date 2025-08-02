using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;

namespace Eduva.Infrastructure.Test.Services
{
    [TestFixture]
    public class NotificationServiceTests
    {
        #region Fields and Setup

        private Mock<IUnitOfWork> _unitOfWorkMock;
        private Mock<ILogger<NotificationService>> _loggerMock;
        private Mock<INotificationRepository> _notificationRepoMock;
        private Mock<IUserNotificationRepository> _userNotificationRepoMock;
        private Mock<IGenericRepository<LessonMaterial, Guid>> _lessonRepoMock;
        private Mock<IGenericRepository<FolderLessonMaterial, Guid>> _folderLessonRepoMock;
        private Mock<IGenericRepository<LessonMaterialQuestion, Guid>> _questionRepoMock;
        private Mock<IGenericRepository<QuestionComment, Guid>> _commentRepoMock;
        private Mock<IGenericRepository<Folder, Guid>> _folderRepoMock;
        private Mock<IGenericRepository<Classroom, Guid>> _classRepoMock;
        private Mock<IGenericRepository<StudentClass, Guid>> _studentClassRepoMock;
        private Mock<IStudentClassRepository> _studentClassCustomRepoMock;
        private Mock<IGenericRepository<ApplicationUser, Guid>> _userRepoMock;
        private Mock<UserManager<ApplicationUser>> _userManagerMock;
        private NotificationService _service;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<NotificationService>>();
            _notificationRepoMock = new Mock<INotificationRepository>();
            _userNotificationRepoMock = new Mock<IUserNotificationRepository>();
            _lessonRepoMock = new Mock<IGenericRepository<LessonMaterial, Guid>>();
            _folderLessonRepoMock = new Mock<IGenericRepository<FolderLessonMaterial, Guid>>();
            _questionRepoMock = new Mock<IGenericRepository<LessonMaterialQuestion, Guid>>();
            _commentRepoMock = new Mock<IGenericRepository<QuestionComment, Guid>>();
            _folderRepoMock = new Mock<IGenericRepository<Folder, Guid>>();
            _classRepoMock = new Mock<IGenericRepository<Classroom, Guid>>();
            _studentClassRepoMock = new Mock<IGenericRepository<StudentClass, Guid>>();
            _studentClassCustomRepoMock = new Mock<IStudentClassRepository>();
            _userRepoMock = new Mock<IGenericRepository<ApplicationUser, Guid>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                new Mock<IUserStore<ApplicationUser>>().Object,
                null!, null!, null!, null!, null!, null!, null!, null!);

            SetupRepositoryMocks();
            _service = new NotificationService(_unitOfWorkMock.Object, _loggerMock.Object, _userManagerMock.Object);
        }

        private void SetupRepositoryMocks()
        {
            _unitOfWorkMock.Setup(x => x.GetCustomRepository<INotificationRepository>()).Returns(_notificationRepoMock.Object);
            _unitOfWorkMock.Setup(x => x.GetCustomRepository<IUserNotificationRepository>()).Returns(_userNotificationRepoMock.Object);
            _unitOfWorkMock.Setup(x => x.GetCustomRepository<IStudentClassRepository>()).Returns(_studentClassCustomRepoMock.Object);
            _unitOfWorkMock.Setup(x => x.GetRepository<LessonMaterial, Guid>()).Returns(_lessonRepoMock.Object);
            _unitOfWorkMock.Setup(x => x.GetRepository<FolderLessonMaterial, Guid>()).Returns(_folderLessonRepoMock.Object);
            _unitOfWorkMock.Setup(x => x.GetRepository<LessonMaterialQuestion, Guid>()).Returns(_questionRepoMock.Object);
            _unitOfWorkMock.Setup(x => x.GetRepository<QuestionComment, Guid>()).Returns(_commentRepoMock.Object);
            _unitOfWorkMock.Setup(x => x.GetRepository<Folder, Guid>()).Returns(_folderRepoMock.Object);
            _unitOfWorkMock.Setup(x => x.GetRepository<Classroom, Guid>()).Returns(_classRepoMock.Object);
            _unitOfWorkMock.Setup(x => x.GetRepository<StudentClass, Guid>()).Returns(_studentClassRepoMock.Object);
            _unitOfWorkMock.Setup(x => x.GetRepository<ApplicationUser, Guid>()).Returns(_userRepoMock.Object);
        }

        #endregion

        #region Basic CRUD Tests

        [Test]
        public async Task CreateNotificationAsync_ShouldCreateAndReturnNotification()
        {
            // Arrange
            var type = "test-type";
            var payload = "test-payload";

            // Act
            var result = await _service.CreateNotificationAsync(type, payload);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Type, Is.EqualTo(type));
                Assert.That(result.Payload, Is.EqualTo(payload));
            });
            _notificationRepoMock.Verify(x => x.AddAsync(It.IsAny<Notification>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.CommitAsync(), Times.Once);
        }

        [Test]
        public async Task CreateUserNotificationsAsync_ShouldCreateUserNotifications_WhenHasTargetUsers()
        {
            // Arrange
            var notificationId = Guid.NewGuid();
            var userIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

            // Act
            await _service.CreateUserNotificationsAsync(notificationId, userIds);

            // Assert
            _userNotificationRepoMock.Verify(x => x.AddRangeAsync(It.Is<List<UserNotification>>(un =>
                un.Count == 2 && un.All(u => u.NotificationId == notificationId && !u.IsRead))), Times.Once);
            _unitOfWorkMock.Verify(x => x.CommitAsync(), Times.Once);
        }

        [Test]
        public async Task CreateUserNotificationsAsync_ShouldReturnEarly_WhenNoTargetUsers()
        {
            // Arrange
            var notificationId = Guid.NewGuid();
            var userIds = new List<Guid>();

            // Act
            await _service.CreateUserNotificationsAsync(notificationId, userIds);

            // Assert
            _userNotificationRepoMock.Verify(x => x.AddRangeAsync(It.IsAny<List<UserNotification>>()), Times.Never);
            _unitOfWorkMock.Verify(x => x.CommitAsync(), Times.Never);
        }

        [Test]
        public async Task GetUserNotificationsAsync_ShouldReturnAllNotifications_WhenCalledWithUserIdOnly()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var notifications = new List<UserNotification>
            {
                new UserNotification { Id = Guid.NewGuid(), Notification = new Notification { /* ... */ } },
                new UserNotification { Id = Guid.NewGuid(), Notification = new Notification { /* ... */ } }
            };

            var userNotificationRepoMock = new Mock<IUserNotificationRepository>();
            userNotificationRepoMock
                .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(notifications);

            var unitOfWorkMock = new Mock<IUnitOfWork>();
            unitOfWorkMock
                .Setup(u => u.GetCustomRepository<IUserNotificationRepository>())
                .Returns(userNotificationRepoMock.Object);

            var loggerMock = new Mock<ILogger<NotificationService>>();

            var userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!
            );

            var service = new NotificationService(unitOfWorkMock.Object, loggerMock.Object, userManagerMock.Object);

            // Act
            var result = await service.GetUserNotificationsAsync(userId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Count.EqualTo(notifications.Count));
            userNotificationRepoMock.Verify(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task GetUserNotificationsAsync_ShouldReturnFromRepository()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var skip = 10;
            var take = 20;
            var expected = new List<UserNotification>();
            _userNotificationRepoMock.Setup(x => x.GetByUserIdAsync(userId, skip, take, default)).ReturnsAsync(expected);

            // Act
            var result = await _service.GetUserNotificationsAsync(userId, skip, take);

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public async Task GetUnreadNotificationsAsync_ShouldReturnFromRepository()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expected = new List<UserNotification>();
            _userNotificationRepoMock.Setup(x => x.GetUnreadByUserIdAsync(userId, default)).ReturnsAsync(expected);

            // Act
            var result = await _service.GetUnreadNotificationsAsync(userId);

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public async Task GetUnreadCountAsync_ShouldReturnFromRepository()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expectedCount = 5;
            _userNotificationRepoMock.Setup(x => x.GetUnreadCountByUserIdAsync(userId, default)).ReturnsAsync(expectedCount);

            // Act
            var result = await _service.GetUnreadCountAsync(userId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedCount));
        }

        [Test]
        public async Task GetTotalCountAsync_ShouldReturnFromRepository()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expectedCount = 10;
            _userNotificationRepoMock.Setup(x => x.GetTotalCountByUserIdAsync(userId, default)).ReturnsAsync(expectedCount);

            // Act
            var result = await _service.GetTotalCountAsync(userId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedCount));
        }

        [Test]
        public async Task MarkAsReadAsync_ShouldCallRepositoryAndCommit()
        {
            // Arrange
            var userNotificationId = Guid.NewGuid();

            // Act
            await _service.MarkAsReadAsync(userNotificationId);

            // Assert
            _userNotificationRepoMock.Verify(x => x.MarkAsReadAsync(userNotificationId, default), Times.Once);
            _unitOfWorkMock.Verify(x => x.CommitAsync(), Times.Once);
        }

        [Test]
        public async Task MarkAllAsReadAsync_ShouldCallRepositoryAndCommit()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            await _service.MarkAllAsReadAsync(userId);

            // Assert
            _userNotificationRepoMock.Verify(x => x.MarkAllAsReadAsync(userId, default), Times.Once);
            _unitOfWorkMock.Verify(x => x.CommitAsync(), Times.Once);
        }

        #endregion

        #region GetUsersInLessonAsync - Basic Tests

        [Test]
        public async Task GetUsersInLessonAsync_ShouldReturnEmpty_WhenLessonNotFound()
        {
            // Arrange
            var lessonId = Guid.NewGuid();
            _lessonRepoMock.Setup(x => x.GetByIdAsync(lessonId)).ReturnsAsync((LessonMaterial?)null);

            // Act
            var result = await _service.GetUsersInLessonAsync(lessonId);

            // Assert
            Assert.That(result, Has.Count.EqualTo(0));
        }

        [Test]
        public async Task GetUsersInLessonAsync_ShouldReturnEmpty_WhenExceptionThrown()
        {
            // Arrange
            var lessonId = Guid.NewGuid();
            _lessonRepoMock.Setup(x => x.GetByIdAsync(lessonId)).ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _service.GetUsersInLessonAsync(lessonId);

            // Assert
            Assert.That(result, Has.Count.EqualTo(0));
        }

        [Test]
        public async Task GetUsersInLessonAsync_ShouldIncludeLessonCreator()
        {
            // Arrange
            var lessonId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var lesson = new LessonMaterial { Id = lessonId, CreatedByUserId = creatorId };

            _lessonRepoMock.Setup(x => x.GetByIdAsync(lessonId)).ReturnsAsync(lesson);
            _questionRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync([]);
            _folderLessonRepoMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<FolderLessonMaterial, bool>>>(),
                null,
                It.IsAny<CancellationToken>())).ReturnsAsync((FolderLessonMaterial?)null);

            // Act
            var result = await _service.GetUsersInLessonAsync(lessonId);

            // Assert
            Assert.That(result, Contains.Item(creatorId));
        }

        #endregion

        #region GetUsersInLessonAsync - Interaction Tests

        [Test]
        public async Task GetUsersInLessonAsync_ShouldIncludeUsersWhoAskedQuestions()
        {
            // Arrange
            var lessonId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var questionerId = Guid.NewGuid();
            var lesson = new LessonMaterial { Id = lessonId, CreatedByUserId = creatorId };
            var questions = new List<LessonMaterialQuestion>
            {
                new() { Id = Guid.NewGuid(), LessonMaterialId = lessonId, CreatedByUserId = questionerId }
            };

            _lessonRepoMock.Setup(x => x.GetByIdAsync(lessonId)).ReturnsAsync(lesson);
            _questionRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(questions);
            _commentRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync([]);
            _folderLessonRepoMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<FolderLessonMaterial, bool>>>(),
                null,
                It.IsAny<CancellationToken>())).ReturnsAsync((FolderLessonMaterial?)null);

            // Act
            var result = await _service.GetUsersInLessonAsync(lessonId);

            // Assert
            Assert.That(result, Contains.Item(questionerId));
        }

        [Test]
        public async Task GetUsersInLessonAsync_ShouldIncludeUsersWhoCommented()
        {
            // Arrange
            var lessonId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var commenterId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var lesson = new LessonMaterial { Id = lessonId, CreatedByUserId = creatorId };
            var questions = new List<LessonMaterialQuestion>
            {
                new() { Id = questionId, LessonMaterialId = lessonId, CreatedByUserId = creatorId }
            };
            var comments = new List<QuestionComment>
            {
                new() { Id = Guid.NewGuid(), QuestionId = questionId, CreatedByUserId = commenterId }
            };

            _lessonRepoMock.Setup(x => x.GetByIdAsync(lessonId)).ReturnsAsync(lesson);
            _questionRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(questions);
            _commentRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(comments);
            _folderLessonRepoMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<FolderLessonMaterial, bool>>>(),
                null,
                It.IsAny<CancellationToken>())).ReturnsAsync((FolderLessonMaterial?)null);

            // Act
            var result = await _service.GetUsersInLessonAsync(lessonId);

            // Assert
            Assert.That(result, Contains.Item(commenterId));
        }

        #endregion

        #region GetUsersInLessonAsync - Folder Access Tests

        [Test]
        public async Task GetUsersInLessonAsync_ShouldIncludePersonalFolderOwner()
        {
            // Arrange
            var lessonId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var folderOwnerId = Guid.NewGuid();
            var folderClassId = Guid.NewGuid();

            var lesson = new LessonMaterial { Id = lessonId, CreatedByUserId = creatorId };
            var folderLesson = new FolderLessonMaterial { Id = Guid.NewGuid(), LessonMaterialId = lessonId, FolderId = folderId };
            var folder = new Folder { Id = folderId, UserId = folderOwnerId, ClassId = folderClassId, OwnerType = OwnerType.Personal };

            _lessonRepoMock.Setup(x => x.GetByIdAsync(lessonId)).ReturnsAsync(lesson);
            _questionRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync([]);
            _folderLessonRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<FolderLessonMaterial> { folderLesson });
            _folderRepoMock.Setup(x => x.GetByIdAsync(folderId)).ReturnsAsync(folder);

            // Act
            var result = await _service.GetUsersInLessonAsync(lessonId);

            // Assert
            Assert.That(result, Contains.Item(folderOwnerId));
            Assert.That(result, Does.Not.Contain(Guid.Empty));
        }

        [Test]
        public async Task GetUsersInLessonAsync_ShouldIncludeClassUsers_WhenInClassFolder()
        {
            // Arrange
            var lessonId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var folderOwnerId = Guid.NewGuid();

            var lesson = new LessonMaterial { Id = lessonId, CreatedByUserId = creatorId };
            var folderLesson = new FolderLessonMaterial { Id = Guid.NewGuid(), LessonMaterialId = lessonId, FolderId = folderId };
            var folder = new Folder { Id = folderId, ClassId = classId, UserId = folderOwnerId, OwnerType = OwnerType.Class };

            var classroom = new Classroom { Id = classId, TeacherId = teacherId };
            var studentClasses = new List<StudentClass>
            {
                new StudentClass { Id = Guid.NewGuid(), ClassId = classId, StudentId = studentId }
            };

            _lessonRepoMock.Setup(x => x.GetByIdAsync(lessonId)).ReturnsAsync(lesson);
            _questionRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync([]);
            _folderLessonRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<FolderLessonMaterial> { folderLesson });
            _folderRepoMock.Setup(x => x.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _classRepoMock.Setup(x => x.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _studentClassRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(studentClasses);
            _studentClassCustomRepoMock.Setup(x => x.HasAccessToMaterialAsync(studentId, lessonId)).ReturnsAsync(true);

            // Act
            var result = await _service.GetUsersInLessonAsync(lessonId);

            // Assert
            Assert.That(result, Contains.Item(teacherId));
            Assert.That(result, Contains.Item(studentId));
            Assert.That(result, Does.Not.Contain(Guid.Empty));
        }

        [Test]
        public async Task GetUsersInLessonAsync_ShouldIncludeAllUsers_WhenInClassAndPersonalFolders()
        {
            // Arrange
            var lessonId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var folderClassId = Guid.NewGuid();
            var folderPersonalId = Guid.NewGuid();
            var folderOwnerId = Guid.NewGuid();

            var lesson = new LessonMaterial { Id = lessonId, CreatedByUserId = creatorId };
            var folderLessons = new List<FolderLessonMaterial>
            {
                new FolderLessonMaterial { Id = Guid.NewGuid(), LessonMaterialId = lessonId, FolderId = folderClassId },
                new FolderLessonMaterial { Id = Guid.NewGuid(), LessonMaterialId = lessonId, FolderId = folderPersonalId }
            };
            var folderClass = new Folder { Id = folderClassId, ClassId = classId, UserId = folderPersonalId, OwnerType = OwnerType.Class };
            var folderPersonal = new Folder { Id = folderPersonalId, UserId = folderOwnerId, ClassId = folderClassId, OwnerType = OwnerType.Personal };

            var classroom = new Classroom { Id = classId, TeacherId = teacherId };
            var studentClasses = new List<StudentClass>
            {
                new StudentClass { Id = Guid.NewGuid(), ClassId = classId, StudentId = studentId }
            };

            _lessonRepoMock.Setup(x => x.GetByIdAsync(lessonId)).ReturnsAsync(lesson);
            _questionRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync([]);
            _folderLessonRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(folderLessons);
            _folderRepoMock.Setup(x => x.GetByIdAsync(folderClassId)).ReturnsAsync(folderClass);
            _folderRepoMock.Setup(x => x.GetByIdAsync(folderPersonalId)).ReturnsAsync(folderPersonal);
            _classRepoMock.Setup(x => x.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _studentClassRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(studentClasses);
            _studentClassCustomRepoMock.Setup(x => x.HasAccessToMaterialAsync(studentId, lessonId)).ReturnsAsync(true);

            // Act
            var result = await _service.GetUsersInLessonAsync(lessonId);

            // Assert
            Assert.That(result, Contains.Item(teacherId));
            Assert.That(result, Contains.Item(studentId));
            Assert.That(result, Contains.Item(folderOwnerId));
            Assert.That(result, Does.Not.Contain(Guid.Empty));
        }

        #endregion

        #region GetUsersInLessonAsync - Visibility Tests

        [Test]
        public async Task GetUsersInLessonAsync_ShouldIncludeOnlyEligibleRoles_WhenSchoolVisibility()
        {
            // Arrange
            var lessonId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var schoolId = 1;
            var teacherId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            var moderatorId = Guid.NewGuid();
            var studentId = Guid.NewGuid();

            var lesson = new LessonMaterial
            {
                Id = lessonId,
                CreatedByUserId = creatorId,
                SchoolId = schoolId,
                Visibility = LessonMaterialVisibility.School
            };

            var schoolUsers = new List<ApplicationUser>
            {
                new() { Id = teacherId, SchoolId = schoolId, Status = EntityStatus.Active },
                new() { Id = adminId, SchoolId = schoolId, Status = EntityStatus.Active },
                new() { Id = moderatorId, SchoolId = schoolId, Status = EntityStatus.Active },
                new() { Id = studentId, SchoolId = schoolId, Status = EntityStatus.Active }
            };

            _lessonRepoMock.Setup(x => x.GetByIdAsync(lessonId)).ReturnsAsync(lesson);
            _questionRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<LessonMaterialQuestion>());
            _folderLessonRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<FolderLessonMaterial>());
            _userRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(schoolUsers);

            // Mock roles
            _userManagerMock.Setup(x => x.GetRolesAsync(It.Is<ApplicationUser>(u => u.Id == teacherId)))
                .ReturnsAsync(new List<string> { "Teacher" });
            _userManagerMock.Setup(x => x.GetRolesAsync(It.Is<ApplicationUser>(u => u.Id == adminId)))
                .ReturnsAsync(new List<string> { "SchoolAdmin" });
            _userManagerMock.Setup(x => x.GetRolesAsync(It.Is<ApplicationUser>(u => u.Id == moderatorId)))
                .ReturnsAsync(new List<string> { "ContentModerator" });
            _userManagerMock.Setup(x => x.GetRolesAsync(It.Is<ApplicationUser>(u => u.Id == studentId)))
                .ReturnsAsync(new List<string> { "Student" });

            // Act
            var result = await _service.GetUsersInLessonAsync(lessonId);

            // Assert
            Assert.That(result, Does.Not.Contain(teacherId));
            Assert.That(result, Does.Not.Contain(adminId));
            Assert.That(result, Does.Contain(moderatorId));
            Assert.That(result, Does.Not.Contain(studentId));
        }

        [Test]
        public async Task GetUsersInLessonAsync_ShouldIncludeSchoolUsers_WhenSchoolVisibilityAndNotInFolder()
        {
            // Arrange
            var lessonId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var schoolId = 1;
            var teacherId = Guid.NewGuid();
            var studentId = Guid.NewGuid();

            var lesson = new LessonMaterial
            {
                Id = lessonId,
                CreatedByUserId = creatorId,
                SchoolId = schoolId,
                Visibility = LessonMaterialVisibility.School
            };
            var schoolUsers = new List<ApplicationUser>
            {
                new() { Id = teacherId, SchoolId = schoolId, Status = EntityStatus.Active },
                new() { Id = studentId, SchoolId = schoolId, Status = EntityStatus.Active }
            };

            _lessonRepoMock.Setup(x => x.GetByIdAsync(lessonId)).ReturnsAsync(lesson);
            _questionRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<LessonMaterialQuestion>());
            _folderLessonRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<FolderLessonMaterial>());
            _userRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(schoolUsers);

            // Mock roles
            _userManagerMock.Setup(x => x.GetRolesAsync(It.Is<ApplicationUser>(u => u.Id == teacherId)))
                .ReturnsAsync(new List<string> { "Teacher" });
            _userManagerMock.Setup(x => x.GetRolesAsync(It.Is<ApplicationUser>(u => u.Id == studentId)))
                .ReturnsAsync(new List<string> { "Student" });

            // Act
            var result = await _service.GetUsersInLessonAsync(lessonId);

            // Assert
            Assert.That(result, Does.Not.Contain(teacherId));
            Assert.That(result, Does.Not.Contain(studentId));
        }

        [Test]
        public async Task GetUsersInLessonAsync_ShouldNotIncludeSchoolUsers_WhenPrivateVisibility()
        {
            // Arrange
            var lessonId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var schoolId = 1;
            var schoolUserId = Guid.NewGuid();
            var lesson = new LessonMaterial
            {
                Id = lessonId,
                CreatedByUserId = creatorId,
                SchoolId = schoolId,
                Visibility = LessonMaterialVisibility.Private
            };
            var schoolUsers = new List<ApplicationUser>
            {
                new() { Id = schoolUserId, SchoolId = schoolId, Status = EntityStatus.Active }
            };

            _lessonRepoMock.Setup(x => x.GetByIdAsync(lessonId)).ReturnsAsync(lesson);
            _questionRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync([]);
            _folderLessonRepoMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<FolderLessonMaterial, bool>>>(),
                null,
                It.IsAny<CancellationToken>())).ReturnsAsync((FolderLessonMaterial?)null);
            _userRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(schoolUsers);

            // Act
            var result = await _service.GetUsersInLessonAsync(lessonId);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Does.Not.Contain(schoolUserId));
                Assert.That(result, Contains.Item(creatorId)); // Only creator
            });
        }

        #endregion

        #region GetUsersInLessonAsync - Exception Handling Tests

        [Test]
        public async Task GetUsersInLessonAsync_ShouldHandleExceptionInInteractedUsers()
        {
            // Arrange
            var lessonId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var lesson = new LessonMaterial { Id = lessonId, CreatedByUserId = creatorId };

            _lessonRepoMock.Setup(x => x.GetByIdAsync(lessonId)).ReturnsAsync(lesson);
            _questionRepoMock.Setup(x => x.GetAllAsync()).ThrowsAsync(new Exception("Question repo error"));
            _folderLessonRepoMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<FolderLessonMaterial, bool>>>(),
                null,
                It.IsAny<CancellationToken>())).ReturnsAsync((FolderLessonMaterial?)null);

            // Act
            var result = await _service.GetUsersInLessonAsync(lessonId);

            // Assert
            Assert.That(result, Contains.Item(creatorId)); // Should still include creator
        }

        [Test]
        public async Task GetUsersInLessonAsync_ShouldHandleExceptionInFolderAccess()
        {
            // Arrange
            var lessonId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var lesson = new LessonMaterial { Id = lessonId, CreatedByUserId = creatorId };

            _lessonRepoMock.Setup(x => x.GetByIdAsync(lessonId)).ReturnsAsync(lesson);
            _questionRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync([]);
            _folderLessonRepoMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<FolderLessonMaterial, bool>>>(),
                null,
                It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Folder repo error"));

            // Act
            var result = await _service.GetUsersInLessonAsync(lessonId);

            // Assert
            Assert.That(result, Contains.Item(creatorId)); // Should still include creator
        }

        [Test]
        public async Task GetUsersInLessonAsync_ShouldHandleExceptionInClassUsers()
        {
            // Arrange
            var lessonId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var lesson = new LessonMaterial { Id = lessonId, CreatedByUserId = creatorId };
            var folderLesson = new FolderLessonMaterial { Id = Guid.NewGuid(), LessonMaterialId = lessonId, FolderId = Guid.NewGuid() };
            var folder = new Folder { Id = folderLesson.FolderId, ClassId = classId };

            _lessonRepoMock.Setup(x => x.GetByIdAsync(lessonId)).ReturnsAsync(lesson);
            _questionRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync([]);
            _folderLessonRepoMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<FolderLessonMaterial, bool>>>(),
                null,
                It.IsAny<CancellationToken>())).ReturnsAsync(folderLesson);
            _folderRepoMock.Setup(x => x.GetByIdAsync(folderLesson.FolderId)).ReturnsAsync(folder);
            _classRepoMock.Setup(x => x.GetByIdAsync(classId)).ThrowsAsync(new Exception("Class repo error"));

            // Act
            var result = await _service.GetUsersInLessonAsync(lessonId);

            // Assert
            Assert.That(result, Contains.Item(creatorId)); // Should still include creator
        }

        [Test]
        public async Task GetUsersInLessonAsync_ShouldHandleExceptionInVisibilityUsers()
        {
            // Arrange
            var lessonId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var lesson = new LessonMaterial
            {
                Id = lessonId,
                CreatedByUserId = creatorId,
                Visibility = LessonMaterialVisibility.School
            };

            _lessonRepoMock.Setup(x => x.GetByIdAsync(lessonId)).ReturnsAsync(lesson);
            _questionRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync([]);
            _folderLessonRepoMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<FolderLessonMaterial, bool>>>(),
                null,
                It.IsAny<CancellationToken>())).ReturnsAsync((FolderLessonMaterial?)null);
            _userRepoMock.Setup(x => x.GetAllAsync()).ThrowsAsync(new Exception("User repo error"));

            // Act
            var result = await _service.GetUsersInLessonAsync(lessonId);

            // Assert
            Assert.That(result, Contains.Item(creatorId)); // Should still include creator
        }

        [Test]
        public async Task GetUsersInLessonAsync_ShouldReturnOnlyCreator_WhenFolderNotFound()
        {
            // Arrange
            var lessonId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var lesson = new LessonMaterial { Id = lessonId, CreatedByUserId = creatorId };
            var folderLesson = new FolderLessonMaterial { Id = Guid.NewGuid(), LessonMaterialId = lessonId, FolderId = folderId };

            _lessonRepoMock.Setup(x => x.GetByIdAsync(lessonId)).ReturnsAsync(lesson);
            _questionRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync([]);
            _folderLessonRepoMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<FolderLessonMaterial, bool>>>(),
                null,
                It.IsAny<CancellationToken>())).ReturnsAsync(folderLesson);
            _folderRepoMock.Setup(x => x.GetByIdAsync(folderId)).ReturnsAsync((Folder?)null);

            // Act
            var result = await _service.GetUsersInLessonAsync(lessonId);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Contains.Item(creatorId));
                Assert.That(result, Has.Count.EqualTo(1));
            });
        }

        [Test]
        public async Task GetUsersInLessonAsync_ShouldReturnOnlyCreator_WhenClassNotFound()
        {
            // Arrange
            var lessonId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var lesson = new LessonMaterial { Id = lessonId, CreatedByUserId = creatorId };
            var folderLesson = new FolderLessonMaterial { Id = Guid.NewGuid(), LessonMaterialId = lessonId, FolderId = Guid.NewGuid() };
            var folder = new Folder { Id = folderLesson.FolderId, ClassId = classId };

            _lessonRepoMock.Setup(x => x.GetByIdAsync(lessonId)).ReturnsAsync(lesson);
            _questionRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync([]);
            _folderLessonRepoMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<FolderLessonMaterial, bool>>>(),
                null,
                It.IsAny<CancellationToken>())).ReturnsAsync(folderLesson);
            _folderRepoMock.Setup(x => x.GetByIdAsync(folderLesson.FolderId)).ReturnsAsync(folder);
            _classRepoMock.Setup(x => x.GetByIdAsync(classId)).ReturnsAsync((Classroom?)null);

            // Act
            var result = await _service.GetUsersInLessonAsync(lessonId);

            // Assert
            Assert.That(result, Contains.Item(creatorId));
        }

        #endregion

        #region GetUsersForNewQuestionNotificationAsync Tests

        [Test]
        public async Task GetUsersForNewQuestionNotificationAsync_ShouldReturnTeacherAndAccessUsers_WhenLessonExists()
        {
            // Arrange
            var lessonId = Guid.NewGuid();
            var lessonCreatorId = Guid.NewGuid();
            var classTeacherId = Guid.NewGuid();
            var student1Id = Guid.NewGuid();
            var student2Id = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var classId = Guid.NewGuid();

            var lesson = new LessonMaterial
            {
                Id = lessonId,
                CreatedByUserId = lessonCreatorId,
                Status = EntityStatus.Active,
                Visibility = LessonMaterialVisibility.Private,
                SchoolId = 1,
                Title = "Test Lesson",
                Description = "Test Description",
                ContentType = ContentType.DOCX,
                LessonStatus = LessonMaterialStatus.Approved,
                Duration = 60,
                FileSize = 1024,
                IsAIContent = false,
                SourceUrl = "test.pdf",
                CreatedAt = DateTimeOffset.UtcNow,
                LastModifiedAt = DateTimeOffset.UtcNow
            };

            var folderLesson = new FolderLessonMaterial
            {
                Id = Guid.NewGuid(),
                LessonMaterialId = lessonId,
                FolderId = folderId
            };

            var folder = new Folder
            {
                Id = folderId,
                ClassId = classId,
                UserId = classTeacherId,
                OwnerType = OwnerType.Class,
                Name = "Test Folder",
                Order = 1,
                Status = EntityStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow,
                LastModifiedAt = DateTimeOffset.UtcNow
            };

            var classroom = new Classroom
            {
                Id = classId,
                TeacherId = classTeacherId,
                Name = "Test Class",
                ClassCode = "TEST123",
                SchoolId = 1,
                BackgroundImageUrl = "",
                Status = EntityStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow,
                LastModifiedAt = DateTimeOffset.UtcNow
            };

            var studentClasses = new List<StudentClass>
            {
                new StudentClass
                {
                    Id = Guid.NewGuid(),
                    StudentId = student1Id,
                    ClassId = classId,
                    EnrolledAt = DateTimeOffset.UtcNow
                },
                new StudentClass
                {
                    Id = Guid.NewGuid(),
                    StudentId = student2Id,
                    ClassId = classId,
                    EnrolledAt = DateTimeOffset.UtcNow
                }
            };

            _lessonRepoMock.Setup(x => x.GetByIdAsync(lessonId)).ReturnsAsync(lesson);
            _folderLessonRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<FolderLessonMaterial> { folderLesson });
            _folderRepoMock.Setup(x => x.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _classRepoMock.Setup(x => x.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _studentClassRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(studentClasses);

            _studentClassCustomRepoMock.Setup(x => x.HasAccessToMaterialAsync(student1Id, lessonId))
                .ReturnsAsync(true);
            _studentClassCustomRepoMock.Setup(x => x.HasAccessToMaterialAsync(student2Id, lessonId))
                .ReturnsAsync(true);

            // Act
            var result = await _service.GetUsersForNewQuestionNotificationAsync(lessonId);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Has.Count.EqualTo(4));
                Assert.That(result, Contains.Item(lessonCreatorId));
                Assert.That(result, Contains.Item(classTeacherId));
                Assert.That(result, Contains.Item(student1Id));
                Assert.That(result, Contains.Item(student2Id));
                Assert.That(result, Does.Not.Contain(Guid.Empty));
            });
        }

        [Test]
        public async Task GetUsersForNewQuestionNotificationAsync_ShouldDeduplicateTeacher_WhenSameTeacher()
        {
            // Arrange
            var lessonId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();
            var student1Id = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var classId = Guid.NewGuid();

            var lesson = new LessonMaterial
            {
                Id = lessonId,
                CreatedByUserId = teacherId,
                Status = EntityStatus.Active,
                Visibility = LessonMaterialVisibility.Private,
                SchoolId = 1,
                Title = "Test Lesson",
                Description = "Test Description",
                ContentType = ContentType.DOCX,
                LessonStatus = LessonMaterialStatus.Approved,
                Duration = 60,
                FileSize = 1024,
                IsAIContent = false,
                SourceUrl = "test.pdf",
                CreatedAt = DateTimeOffset.UtcNow,
                LastModifiedAt = DateTimeOffset.UtcNow
            };

            var folderLesson = new FolderLessonMaterial
            {
                Id = Guid.NewGuid(),
                LessonMaterialId = lessonId,
                FolderId = folderId
            };

            var folder = new Folder
            {
                Id = folderId,
                ClassId = classId,
                UserId = teacherId,
                OwnerType = OwnerType.Class,
                Name = "Test Folder",
                Order = 1,
                Status = EntityStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow,
                LastModifiedAt = DateTimeOffset.UtcNow
            };

            var classroom = new Classroom
            {
                Id = classId,
                TeacherId = teacherId,
                Name = "Test Class",
                ClassCode = "TEST123",
                SchoolId = 1,
                BackgroundImageUrl = "",
                Status = EntityStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow,
                LastModifiedAt = DateTimeOffset.UtcNow
            };

            var studentClasses = new List<StudentClass>
            {
                new StudentClass
                {
                    Id = Guid.NewGuid(),
                    StudentId = student1Id,
                    ClassId = classId,
                    EnrolledAt = DateTimeOffset.UtcNow
                }
            };

            _lessonRepoMock.Setup(x => x.GetByIdAsync(lessonId)).ReturnsAsync(lesson);
            _folderLessonRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<FolderLessonMaterial> { folderLesson });
            _folderRepoMock.Setup(x => x.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _classRepoMock.Setup(x => x.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _studentClassRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(studentClasses);

            _studentClassCustomRepoMock.Setup(x => x.HasAccessToMaterialAsync(student1Id, lessonId))
                .ReturnsAsync(true);

            // Act
            var result = await _service.GetUsersForNewQuestionNotificationAsync(lessonId);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Has.Count.EqualTo(2));
                Assert.That(result, Contains.Item(teacherId));
                Assert.That(result, Contains.Item(student1Id));
                Assert.That(result, Does.Not.Contain(Guid.Empty));
            });
        }

        [Test]
        public async Task GetUsersForNewQuestionNotificationAsync_ShouldReturnOnlyTeacher_WhenStudentsHaveNoAccess()
        {
            // Arrange
            var lessonId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();
            var student1Id = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var classId = Guid.NewGuid();

            var lesson = new LessonMaterial
            {
                Id = lessonId,
                CreatedByUserId = teacherId,
                Status = EntityStatus.Active,
                Visibility = LessonMaterialVisibility.Private,
                SchoolId = 1,
                Title = "Test Lesson",
                Description = "Test Description",
                ContentType = ContentType.DOCX,
                LessonStatus = LessonMaterialStatus.Approved,
                Duration = 60,
                FileSize = 1024,
                IsAIContent = false,
                SourceUrl = "test.pdf",
                CreatedAt = DateTimeOffset.UtcNow,
                LastModifiedAt = DateTimeOffset.UtcNow
            };

            var folderLesson = new FolderLessonMaterial
            {
                Id = Guid.NewGuid(),
                LessonMaterialId = lessonId,
                FolderId = folderId
            };

            var folder = new Folder
            {
                Id = folderId,
                ClassId = classId,
                UserId = teacherId,
                OwnerType = OwnerType.Class,
                Name = "Test Folder",
                Order = 1,
                Status = EntityStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow,
                LastModifiedAt = DateTimeOffset.UtcNow
            };

            var classroom = new Classroom
            {
                Id = classId,
                TeacherId = teacherId,
                Name = "Test Class",
                ClassCode = "TEST123",
                SchoolId = 1,
                BackgroundImageUrl = "",
                Status = EntityStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow,
                LastModifiedAt = DateTimeOffset.UtcNow
            };

            var studentClasses = new List<StudentClass>
            {
                new StudentClass
                {
                    Id = Guid.NewGuid(),
                    StudentId = student1Id,
                    ClassId = classId,
                    EnrolledAt = DateTimeOffset.UtcNow
                }
            };

            _lessonRepoMock.Setup(x => x.GetByIdAsync(lessonId)).ReturnsAsync(lesson);
            _folderLessonRepoMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<FolderLessonMaterial, bool>>>(),
                null,
                It.IsAny<CancellationToken>())).ReturnsAsync(folderLesson);
            _folderRepoMock.Setup(x => x.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _classRepoMock.Setup(x => x.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _studentClassRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(studentClasses);

            _studentClassCustomRepoMock.Setup(x => x.HasAccessToMaterialAsync(student1Id, lessonId))
                .ReturnsAsync(false);

            // Act
            var result = await _service.GetUsersForNewQuestionNotificationAsync(lessonId);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Has.Count.EqualTo(1));
                Assert.That(result, Contains.Item(teacherId));
                Assert.That(result, Does.Not.Contain(student1Id));
                Assert.That(result, Does.Not.Contain(Guid.Empty));
            });
        }

        [Test]
        public async Task GetUsersForNewQuestionNotificationAsync_ShouldReturnOnlyLessonCreator_WhenNotInFolder()
        {
            // Arrange
            var lessonId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();

            var lesson = new LessonMaterial
            {
                Id = lessonId,
                CreatedByUserId = teacherId,
                Status = EntityStatus.Active,
                Visibility = LessonMaterialVisibility.Private,
                SchoolId = 1,
                Title = "Test Lesson",
                Description = "Test Description",
                ContentType = ContentType.DOCX,
                LessonStatus = LessonMaterialStatus.Approved,
                Duration = 60,
                FileSize = 1024,
                IsAIContent = false,
                SourceUrl = "test.pdf",
                CreatedAt = DateTimeOffset.UtcNow,
                LastModifiedAt = DateTimeOffset.UtcNow
            };

            _lessonRepoMock.Setup(x => x.GetByIdAsync(lessonId)).ReturnsAsync(lesson);
            _folderLessonRepoMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<FolderLessonMaterial, bool>>>(),
                null,
                It.IsAny<CancellationToken>())).ReturnsAsync((FolderLessonMaterial?)null);

            // Act
            var result = await _service.GetUsersForNewQuestionNotificationAsync(lessonId);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Has.Count.EqualTo(1));
                Assert.That(result, Contains.Item(teacherId));
                Assert.That(result, Does.Not.Contain(Guid.Empty));
            });
        }

        [Test]
        public async Task GetUsersForNewQuestionNotificationAsync_ShouldAddSchoolUsers_WhenNotInFolderAndSchoolVisibility()
        {
            // Arrange
            var lessonId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();
            var user1Id = Guid.NewGuid();
            var user2Id = Guid.NewGuid();

            var lesson = new LessonMaterial
            {
                Id = lessonId,
                CreatedByUserId = teacherId,
                Status = EntityStatus.Active,
                Visibility = LessonMaterialVisibility.School,
                SchoolId = 1,
                Title = "Test Lesson",
                Description = "Test Description",
                ContentType = ContentType.DOCX,
                LessonStatus = LessonMaterialStatus.Approved,
                Duration = 60,
                FileSize = 1024,
                IsAIContent = false,
                SourceUrl = "test.pdf",
                CreatedAt = DateTimeOffset.UtcNow,
                LastModifiedAt = DateTimeOffset.UtcNow
            };

            List<ApplicationUser> schoolUsers = [
                new() { Id = teacherId, SchoolId = 1, Status = EntityStatus.Active, UserName = "teacher@test.com", Email = "teacher@test.com" },
                new() { Id = user1Id, SchoolId = 1, Status = EntityStatus.Active, UserName = "user1@test.com", Email = "user1@test.com" },
                new() { Id = user2Id, SchoolId = 1, Status = EntityStatus.Active, UserName = "user2@test.com", Email = "user2@test.com" },
                new() { Id = Guid.NewGuid(), SchoolId = 2, Status = EntityStatus.Active, UserName = "other@test.com", Email = "other@test.com" },
                new() { Id = Guid.NewGuid(), SchoolId = 1, Status = EntityStatus.Deleted, UserName = "deleted@test.com", Email = "deleted@test.com" }
            ];

            _lessonRepoMock.Setup(x => x.GetByIdAsync(lessonId)).ReturnsAsync(lesson);

            _folderLessonRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<FolderLessonMaterial>());

            _userRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(schoolUsers);

            _userManagerMock.Setup(x => x.GetRolesAsync(It.Is<ApplicationUser>(u => u.Id == teacherId)))
                .ReturnsAsync(new List<string> { "Teacher" });
            _userManagerMock.Setup(x => x.GetRolesAsync(It.Is<ApplicationUser>(u => u.Id == user1Id)))
                .ReturnsAsync(new List<string> { "ContentModerator" });
            _userManagerMock.Setup(x => x.GetRolesAsync(It.Is<ApplicationUser>(u => u.Id == user2Id)))
                .ReturnsAsync(new List<string> { "Student" });

            // Act
            var result = await _service.GetUsersForNewQuestionNotificationAsync(lessonId);

            // Assert
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result, Contains.Item(teacherId));
            Assert.That(result, Contains.Item(user1Id));
            Assert.That(result, Does.Not.Contain(user2Id));
        }

        [Test]
        public async Task GetUsersForNewQuestionNotificationAsync_ShouldReturnEmpty_WhenLessonNotFound()
        {
            // Arrange
            var lessonId = Guid.NewGuid();

            _lessonRepoMock.Setup(x => x.GetByIdAsync(lessonId)).ReturnsAsync((LessonMaterial?)null);

            // Act
            var result = await _service.GetUsersForNewQuestionNotificationAsync(lessonId);

            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetUsersForNewQuestionNotificationAsync_ShouldFallbackToOldLogic_WhenExceptionThrown()
        {
            // Arrange
            var lessonId = Guid.NewGuid();

            _lessonRepoMock.Setup(x => x.GetByIdAsync(lessonId)).ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _service.GetUsersForNewQuestionNotificationAsync(lessonId);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        #endregion

        #region GetUsersForQuestionCommentNotificationAsync Tests

        [Test]
        public async Task GetUsersForQuestionCommentNotificationAsync_ShouldReturnQuestionCreatorAndTeacherAndCommenters()
        {
            // Arrange
            var questionId = Guid.NewGuid();
            var lessonId = Guid.NewGuid();
            var questionCreatorId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();
            var commenter1Id = Guid.NewGuid();
            var commenter2Id = Guid.NewGuid();

            var question = new LessonMaterialQuestion
            {
                Id = questionId,
                LessonMaterialId = lessonId,
                CreatedByUserId = questionCreatorId
            };

            var lesson = new LessonMaterial
            {
                Id = lessonId,
                CreatedByUserId = teacherId
            };

            var comments = new List<QuestionComment>
            {
                new() { Id = Guid.NewGuid(), QuestionId = questionId, CreatedByUserId = commenter1Id, Status = EntityStatus.Active },
                new() { Id = Guid.NewGuid(), QuestionId = questionId, CreatedByUserId = commenter2Id, Status = EntityStatus.Active },
                new() { Id = Guid.NewGuid(), QuestionId = Guid.NewGuid(), CreatedByUserId = Guid.NewGuid(), Status = EntityStatus.Active } // Different question
            };

            _questionRepoMock.Setup(x => x.GetByIdAsync(questionId)).ReturnsAsync(question);
            _lessonRepoMock.Setup(x => x.GetByIdAsync(lessonId)).ReturnsAsync(lesson);
            _commentRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(comments);

            // Act
            var result = await _service.GetUsersForQuestionCommentNotificationAsync(questionId, lessonId);

            // Assert
            Assert.That(result, Has.Count.EqualTo(4));
            Assert.That(result, Contains.Item(questionCreatorId)); // Question creator
            Assert.That(result, Contains.Item(teacherId)); // Lesson creator
            Assert.That(result, Contains.Item(commenter1Id)); // Commenter 1
            Assert.That(result, Contains.Item(commenter2Id)); // Commenter 2
        }

        [Test]
        public async Task GetUsersForQuestionCommentNotificationAsync_ShouldReturnOnlyTeacher_WhenQuestionNotFoundButLessonExists()
        {
            // Arrange
            var questionId = Guid.NewGuid();
            var lessonId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();

            var lesson = new LessonMaterial
            {
                Id = lessonId,
                CreatedByUserId = teacherId
            };

            _questionRepoMock.Setup(x => x.GetByIdAsync(questionId)).ReturnsAsync((LessonMaterialQuestion?)null);
            _lessonRepoMock.Setup(x => x.GetByIdAsync(lessonId)).ReturnsAsync(lesson);
            _commentRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<QuestionComment>());

            // Act
            var result = await _service.GetUsersForQuestionCommentNotificationAsync(questionId, lessonId);

            // Assert
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result, Contains.Item(teacherId));
        }

        [Test]
        public async Task GetUsersForQuestionCommentNotificationAsync_ShouldExcludeInactiveComments()
        {
            // Arrange
            var questionId = Guid.NewGuid();
            var lessonId = Guid.NewGuid();
            var questionCreatorId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();
            var activeCommenterId = Guid.NewGuid();
            var inactiveCommenterId = Guid.NewGuid();

            var question = new LessonMaterialQuestion
            {
                Id = questionId,
                CreatedByUserId = questionCreatorId
            };

            var lesson = new LessonMaterial
            {
                Id = lessonId,
                CreatedByUserId = teacherId
            };

            var comments = new List<QuestionComment>
            {
                new() { Id = Guid.NewGuid(), QuestionId = questionId, CreatedByUserId = activeCommenterId, Status = EntityStatus.Active },
                new() { Id = Guid.NewGuid(), QuestionId = questionId, CreatedByUserId = inactiveCommenterId, Status = EntityStatus.Deleted }
            };

            _questionRepoMock.Setup(x => x.GetByIdAsync(questionId)).ReturnsAsync(question);
            _lessonRepoMock.Setup(x => x.GetByIdAsync(lessonId)).ReturnsAsync(lesson);
            _commentRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(comments);

            // Act
            var result = await _service.GetUsersForQuestionCommentNotificationAsync(questionId, lessonId);

            // Assert
            Assert.That(result, Has.Count.EqualTo(3));
            Assert.That(result, Contains.Item(questionCreatorId));
            Assert.That(result, Contains.Item(teacherId));
            Assert.That(result, Contains.Item(activeCommenterId));
            Assert.That(result, Does.Not.Contain(inactiveCommenterId)); // Should exclude inactive comment
        }

        [Test]
        public async Task GetUsersForQuestionCommentNotificationAsync_ShouldFallbackToOldLogic_WhenExceptionThrown()
        {
            // Arrange
            var questionId = Guid.NewGuid();
            var lessonId = Guid.NewGuid();

            _questionRepoMock.Setup(x => x.GetByIdAsync(questionId)).ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _service.GetUsersForQuestionCommentNotificationAsync(questionId, lessonId);

            // Assert - Should call old logic as fallback
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task GetUsersForQuestionCommentNotificationAsync_ShouldHandleDuplicateUsers()
        {
            // Arrange
            var questionId = Guid.NewGuid();
            var lessonId = Guid.NewGuid();
            var teacherId = Guid.NewGuid(); // Same user is both question creator and lesson creator

            var question = new LessonMaterialQuestion
            {
                Id = questionId,
                CreatedByUserId = teacherId // Teacher created the question
            };

            var lesson = new LessonMaterial
            {
                Id = lessonId,
                CreatedByUserId = teacherId // Same teacher created the lesson
            };

            var comments = new List<QuestionComment>
            {
                new() { Id = Guid.NewGuid(), QuestionId = questionId, CreatedByUserId = teacherId, Status = EntityStatus.Active } // Teacher also commented
            };

            _questionRepoMock.Setup(x => x.GetByIdAsync(questionId)).ReturnsAsync(question);
            _lessonRepoMock.Setup(x => x.GetByIdAsync(lessonId)).ReturnsAsync(lesson);
            _commentRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(comments);

            // Act
            var result = await _service.GetUsersForQuestionCommentNotificationAsync(questionId, lessonId);

            // Assert - Should have only 1 user (deduplication)
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result, Contains.Item(teacherId));
        }

        #endregion

        #region DeleteNotificationsByLessonMaterialIdAsync Tests

        [Test]
        public async Task DeleteNotificationsByLessonMaterialIdAsync_ShouldDeleteRelatedNotifications_WhenFound()
        {
            // Arrange
            var lessonMaterialId = Guid.NewGuid();
            var notificationId1 = Guid.NewGuid();
            var notificationId2 = Guid.NewGuid();
            var userNotificationId1 = Guid.NewGuid();
            var userNotificationId2 = Guid.NewGuid();

            var notifications = new List<Notification>
            {
                new Notification
                {
                    Id = notificationId1,
                    Payload = $"{{\"lessonMaterialId\":\"{lessonMaterialId}\",\"title\":\"Test\"}}"
                },
                new Notification
                {
                    Id = notificationId2,
                    Payload = $"{{\"LessonMaterialId\":\"{lessonMaterialId}\",\"content\":\"Test\"}}"
                }
            };

            var userNotifications = new List<UserNotification>
            {
                new UserNotification { Id = userNotificationId1, NotificationId = notificationId1 },
                new UserNotification { Id = userNotificationId2, NotificationId = notificationId2 }
            };

            var notificationRepoMock = new Mock<IGenericRepository<Notification, Guid>>();
            _unitOfWorkMock.Setup(x => x.GetRepository<Notification, Guid>()).Returns(notificationRepoMock.Object);
            _unitOfWorkMock.Setup(x => x.GetCustomRepository<IUserNotificationRepository>()).Returns(_userNotificationRepoMock.Object);

            notificationRepoMock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Notification, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(notifications);

            _userNotificationRepoMock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<UserNotification, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(userNotifications);

            notificationRepoMock.Setup(x => x.RemoveRange(It.IsAny<IEnumerable<Notification>>()));
            _userNotificationRepoMock.Setup(x => x.RemoveRange(It.IsAny<IEnumerable<UserNotification>>()));

            // Act
            await _service.DeleteNotificationsByLessonMaterialIdAsync(lessonMaterialId);

            // Assert
            notificationRepoMock.Verify(x => x.RemoveRange(It.Is<IEnumerable<Notification>>(n => n.Count() == 2)), Times.Once);
            _userNotificationRepoMock.Verify(x => x.RemoveRange(It.Is<IEnumerable<UserNotification>>(un => un.Count() == 2)), Times.Once);
        }

        [Test]
        public async Task DeleteNotificationsByLessonMaterialIdAsync_ShouldReturnEarly_WhenNoNotificationsFound()
        {
            // Arrange
            var lessonMaterialId = Guid.NewGuid();

            var notificationRepoMock = new Mock<IGenericRepository<Notification, Guid>>();
            _unitOfWorkMock.Setup(x => x.GetRepository<Notification, Guid>()).Returns(notificationRepoMock.Object);
            _unitOfWorkMock.Setup(x => x.GetCustomRepository<IUserNotificationRepository>()).Returns(_userNotificationRepoMock.Object);

            notificationRepoMock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Notification, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Notification>());

            // Act
            await _service.DeleteNotificationsByLessonMaterialIdAsync(lessonMaterialId);

            // Assert
            notificationRepoMock.Verify(x => x.RemoveRange(It.IsAny<IEnumerable<Notification>>()), Times.Never);
            _userNotificationRepoMock.Verify(x => x.RemoveRange(It.IsAny<IEnumerable<UserNotification>>()), Times.Never);
        }

        [Test]
        public async Task DeleteNotificationsByLessonMaterialIdAsync_ShouldHandleEmptyPayload()
        {
            // Arrange
            var lessonMaterialId = Guid.NewGuid();
            var notificationId = Guid.NewGuid();

            var notifications = new List<Notification>
            {
                new Notification
                {
                    Id = notificationId,
                    Payload = "" // Empty payload
                }
            };

            var notificationRepoMock = new Mock<IGenericRepository<Notification, Guid>>();
            _unitOfWorkMock.Setup(x => x.GetRepository<Notification, Guid>()).Returns(notificationRepoMock.Object);
            _unitOfWorkMock.Setup(x => x.GetCustomRepository<IUserNotificationRepository>()).Returns(_userNotificationRepoMock.Object);

            notificationRepoMock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Notification, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(notifications);

            // Act
            await _service.DeleteNotificationsByLessonMaterialIdAsync(lessonMaterialId);

            // Assert
            notificationRepoMock.Verify(x => x.RemoveRange(It.IsAny<IEnumerable<Notification>>()), Times.Never);
            _userNotificationRepoMock.Verify(x => x.RemoveRange(It.IsAny<IEnumerable<UserNotification>>()), Times.Never);
        }

        [Test]
        public async Task DeleteNotificationsByLessonMaterialIdAsync_ShouldHandleNullPayload()
        {
            // Arrange
            var lessonMaterialId = Guid.NewGuid();
            var notificationId = Guid.NewGuid();

            var notifications = new List<Notification>
            {
                new Notification
                {
                    Id = notificationId,
                    Payload = null! // Null payload
                }
            };

            var notificationRepoMock = new Mock<IGenericRepository<Notification, Guid>>();
            _unitOfWorkMock.Setup(x => x.GetRepository<Notification, Guid>()).Returns(notificationRepoMock.Object);
            _unitOfWorkMock.Setup(x => x.GetCustomRepository<IUserNotificationRepository>()).Returns(_userNotificationRepoMock.Object);

            notificationRepoMock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Notification, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(notifications);

            // Act
            await _service.DeleteNotificationsByLessonMaterialIdAsync(lessonMaterialId);

            // Assert
            notificationRepoMock.Verify(x => x.RemoveRange(It.IsAny<IEnumerable<Notification>>()), Times.Never);
            _userNotificationRepoMock.Verify(x => x.RemoveRange(It.IsAny<IEnumerable<UserNotification>>()), Times.Never);
        }

        [Test]
        public async Task DeleteNotificationsByLessonMaterialIdAsync_ShouldHandleInvalidJsonPayload()
        {
            // Arrange
            var lessonMaterialId = Guid.NewGuid();
            var notificationId = Guid.NewGuid();

            var notifications = new List<Notification>
            {
                new Notification
                {
                    Id = notificationId,
                    Payload = "invalid json {"
                }
            };

            var notificationRepoMock = new Mock<IGenericRepository<Notification, Guid>>();
            _unitOfWorkMock.Setup(x => x.GetRepository<Notification, Guid>()).Returns(notificationRepoMock.Object);
            _unitOfWorkMock.Setup(x => x.GetCustomRepository<IUserNotificationRepository>()).Returns(_userNotificationRepoMock.Object);

            notificationRepoMock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Notification, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(notifications);

            // Act
            await _service.DeleteNotificationsByLessonMaterialIdAsync(lessonMaterialId);

            // Assert
            notificationRepoMock.Verify(x => x.RemoveRange(It.IsAny<IEnumerable<Notification>>()), Times.Never);
            _userNotificationRepoMock.Verify(x => x.RemoveRange(It.IsAny<IEnumerable<UserNotification>>()), Times.Never);
        }

        [Test]
        public async Task DeleteNotificationsByLessonMaterialIdAsync_ShouldHandleDifferentPropertyNames()
        {
            // Arrange
            var lessonMaterialId = Guid.NewGuid();
            var notificationId1 = Guid.NewGuid();
            var notificationId2 = Guid.NewGuid();
            var notificationId3 = Guid.NewGuid();

            var notifications = new List<Notification>
            {
                new Notification
                {
                    Id = notificationId1,
                    Payload = $"{{\"lessonmaterialid\":\"{lessonMaterialId}\",\"title\":\"Test\"}}"
                },
                new Notification
                {
                    Id = notificationId2,
                    Payload = $"{{\"lesson_material_id\":\"{lessonMaterialId}\",\"content\":\"Test\"}}"
                },
                new Notification
                {
                    Id = notificationId3,
                    Payload = $"{{\"otherProperty\":\"{lessonMaterialId}\",\"title\":\"Test\"}}"
                }
            };

            var userNotifications = new List<UserNotification>
            {
                new UserNotification { Id = Guid.NewGuid(), NotificationId = notificationId1 },
                new UserNotification { Id = Guid.NewGuid(), NotificationId = notificationId2 }
            };

            var notificationRepoMock = new Mock<IGenericRepository<Notification, Guid>>();
            _unitOfWorkMock.Setup(x => x.GetRepository<Notification, Guid>()).Returns(notificationRepoMock.Object);
            _unitOfWorkMock.Setup(x => x.GetCustomRepository<IUserNotificationRepository>()).Returns(_userNotificationRepoMock.Object);

            notificationRepoMock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Notification, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(notifications);

            _userNotificationRepoMock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<UserNotification, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(userNotifications);

            notificationRepoMock.Setup(x => x.RemoveRange(It.IsAny<IEnumerable<Notification>>()));
            _userNotificationRepoMock.Setup(x => x.RemoveRange(It.IsAny<IEnumerable<UserNotification>>()));

            // Act
            await _service.DeleteNotificationsByLessonMaterialIdAsync(lessonMaterialId);

            // Assert
            notificationRepoMock.Verify(x => x.RemoveRange(It.Is<IEnumerable<Notification>>(n => n.Count() == 2)), Times.Once);
            _userNotificationRepoMock.Verify(x => x.RemoveRange(It.Is<IEnumerable<UserNotification>>(un => un.Count() == 2)), Times.Once);
        }

        [Test]
        public async Task DeleteNotificationsByLessonMaterialIdAsync_ShouldHandleNoUserNotifications()
        {
            // Arrange
            var lessonMaterialId = Guid.NewGuid();
            var notificationId = Guid.NewGuid();

            var notifications = new List<Notification>
            {
                new Notification
                {
                    Id = notificationId,
                    Payload = $"{{\"lessonMaterialId\":\"{lessonMaterialId}\",\"title\":\"Test\"}}"
                }
            };

            var notificationRepoMock = new Mock<IGenericRepository<Notification, Guid>>();
            _unitOfWorkMock.Setup(x => x.GetRepository<Notification, Guid>()).Returns(notificationRepoMock.Object);
            _unitOfWorkMock.Setup(x => x.GetCustomRepository<IUserNotificationRepository>()).Returns(_userNotificationRepoMock.Object);

            notificationRepoMock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Notification, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(notifications);

            _userNotificationRepoMock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<UserNotification, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<UserNotification>());

            notificationRepoMock.Setup(x => x.RemoveRange(It.IsAny<IEnumerable<Notification>>()));

            // Act
            await _service.DeleteNotificationsByLessonMaterialIdAsync(lessonMaterialId);

            // Assert
            notificationRepoMock.Verify(x => x.RemoveRange(It.Is<IEnumerable<Notification>>(n => n.Count() == 1)), Times.Once);
            _userNotificationRepoMock.Verify(x => x.RemoveRange(It.IsAny<IEnumerable<UserNotification>>()), Times.Never);
        }

        [Test]
        public Task DeleteNotificationsByLessonMaterialIdAsync_ShouldHandleException()
        {
            // Arrange
            var lessonMaterialId = Guid.NewGuid();

            var notificationRepoMock = new Mock<IGenericRepository<Notification, Guid>>();
            _unitOfWorkMock.Setup(x => x.GetRepository<Notification, Guid>()).Returns(notificationRepoMock.Object);
            _unitOfWorkMock.Setup(x => x.GetCustomRepository<IUserNotificationRepository>()).Returns(_userNotificationRepoMock.Object);

            notificationRepoMock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Notification, bool>>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            var exception = Assert.ThrowsAsync<Exception>(() =>
                _service.DeleteNotificationsByLessonMaterialIdAsync(lessonMaterialId));

            Assert.That(exception.Message, Is.EqualTo("Database error"));
            return Task.CompletedTask;
        }

        [Test]
        public async Task DeleteNotificationsByLessonMaterialIdAsync_ShouldHandleCaseInsensitiveMatching()
        {
            // Arrange
            var lessonMaterialId = Guid.NewGuid();
            var notificationId = Guid.NewGuid();

            var notifications = new List<Notification>
            {
                new Notification
                {
                    Id = notificationId,
                    Payload = $"{{\"lessonMaterialId\":\"{lessonMaterialId.ToString().ToUpper()}\",\"title\":\"Test\"}}"
                }
            };

            var userNotifications = new List<UserNotification>
            {
                new UserNotification { Id = Guid.NewGuid(), NotificationId = notificationId }
            };

            var notificationRepoMock = new Mock<IGenericRepository<Notification, Guid>>();
            _unitOfWorkMock.Setup(x => x.GetRepository<Notification, Guid>()).Returns(notificationRepoMock.Object);
            _unitOfWorkMock.Setup(x => x.GetCustomRepository<IUserNotificationRepository>()).Returns(_userNotificationRepoMock.Object);

            notificationRepoMock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Notification, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(notifications);

            _userNotificationRepoMock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<UserNotification, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(userNotifications);

            notificationRepoMock.Setup(x => x.RemoveRange(It.IsAny<IEnumerable<Notification>>()));
            _userNotificationRepoMock.Setup(x => x.RemoveRange(It.IsAny<IEnumerable<UserNotification>>()));

            // Act
            await _service.DeleteNotificationsByLessonMaterialIdAsync(lessonMaterialId);

            // Assert
            notificationRepoMock.Verify(x => x.RemoveRange(It.Is<IEnumerable<Notification>>(n => n.Count() == 1)), Times.Once);
            _userNotificationRepoMock.Verify(x => x.RemoveRange(It.Is<IEnumerable<UserNotification>>(un => un.Count() == 1)), Times.Once);
        }

        [Test]
        public async Task DeleteNotificationsByLessonMaterialIdAsync_ShouldHandleLargeNumberOfNotifications()
        {
            // Arrange
            var lessonMaterialId = Guid.NewGuid();
            var notifications = new List<Notification>();
            var userNotifications = new List<UserNotification>();

            for (int i = 0; i < 100; i++)
            {
                var notificationId = Guid.NewGuid();
                notifications.Add(new Notification
                {
                    Id = notificationId,
                    Payload = $"{{\"lessonMaterialId\":\"{lessonMaterialId}\",\"index\":{i}}}"
                });
                userNotifications.Add(new UserNotification { Id = Guid.NewGuid(), NotificationId = notificationId });
            }

            var notificationRepoMock = new Mock<IGenericRepository<Notification, Guid>>();
            _unitOfWorkMock.Setup(x => x.GetRepository<Notification, Guid>()).Returns(notificationRepoMock.Object);
            _unitOfWorkMock.Setup(x => x.GetCustomRepository<IUserNotificationRepository>()).Returns(_userNotificationRepoMock.Object);

            notificationRepoMock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Notification, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(notifications);

            _userNotificationRepoMock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<UserNotification, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(userNotifications);

            notificationRepoMock.Setup(x => x.RemoveRange(It.IsAny<IEnumerable<Notification>>()));
            _userNotificationRepoMock.Setup(x => x.RemoveRange(It.IsAny<IEnumerable<UserNotification>>()));

            // Act
            await _service.DeleteNotificationsByLessonMaterialIdAsync(lessonMaterialId);

            // Assert
            notificationRepoMock.Verify(x => x.RemoveRange(It.Is<IEnumerable<Notification>>(n => n.Count() == 100)), Times.Once);
            _userNotificationRepoMock.Verify(x => x.RemoveRange(It.Is<IEnumerable<UserNotification>>(un => un.Count() == 100)), Times.Once);
        }

        #endregion

    }
}
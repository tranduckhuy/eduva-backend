using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Eduva.Infrastructure.Test.Services
{
    [TestFixture]
    public class QuestionPermissionServiceTests
    {
        #region Setup

        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;
        private Mock<IGenericRepository<ApplicationUser, Guid>> _userRepositoryMock = null!;
        private Mock<IGenericRepository<Classroom, Guid>> _classroomRepositoryMock = null!;
        private Mock<IStudentClassRepository> _studentClassRepositoryMock = null!;
        private QuestionPermissionService _service = null!;

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _userRepositoryMock = new Mock<IGenericRepository<ApplicationUser, Guid>>();
            _classroomRepositoryMock = new Mock<IGenericRepository<Classroom, Guid>>();
            _studentClassRepositoryMock = new Mock<IStudentClassRepository>();

            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            _unitOfWorkMock.Setup(x => x.GetRepository<ApplicationUser, Guid>())
                .Returns(_userRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.GetRepository<Classroom, Guid>())
                .Returns(_classroomRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.GetCustomRepository<IStudentClassRepository>())
                .Returns(_studentClassRepositoryMock.Object);

            _service = new QuestionPermissionService(_unitOfWorkMock.Object, _userManagerMock.Object);
        }

        #endregion

        #region GetHighestPriorityRole Tests

        [Test]
        public void GetHighestPriorityRole_ShouldReturnUnknown_WhenRolesIsNull()
        {
            // Act
            var result = _service.GetHighestPriorityRole(null!);

            // Assert
            Assert.That(result, Is.EqualTo("Unknown"));
        }

        [Test]
        public void GetHighestPriorityRole_ShouldReturnUnknown_WhenRolesIsEmpty()
        {
            // Act
            var result = _service.GetHighestPriorityRole([]);

            // Assert
            Assert.That(result, Is.EqualTo("Unknown"));
        }

        [Test]
        [TestCase("SystemAdmin")]
        [TestCase("SchoolAdmin")]
        [TestCase("ContentModerator")]
        [TestCase("Teacher")]
        [TestCase("Student")]
        public void GetHighestPriorityRole_ShouldReturnHighestPriorityRole(string role)
        {
            // Arrange
            var roles = new List<string> { role };

            // Act
            var result = _service.GetHighestPriorityRole(roles);

            // Assert
            Assert.That(result, Is.EqualTo(role));
        }

        [Test]
        public void GetHighestPriorityRole_ShouldReturnSystemAdmin_WhenMultipleRoles()
        {
            // Arrange
            var roles = new List<string> { "Student", "Teacher", "SystemAdmin" };

            // Act
            var result = _service.GetHighestPriorityRole(roles);

            // Assert
            Assert.That(result, Is.EqualTo("SystemAdmin"));
        }

        [Test]
        public void GetHighestPriorityRole_ShouldReturnUnknown_WhenInvalidRole()
        {
            // Arrange
            var roles = new List<string> { "InvalidRole" };

            // Act
            var result = _service.GetHighestPriorityRole(roles);

            // Assert
            Assert.That(result, Is.EqualTo("Unknown"));
        }

        #endregion

        #region GetUserRoleSafelyAsync Tests

        [Test]
        public async Task GetUserRoleSafelyAsync_ShouldReturnUnknown_WhenUserIsNull()
        {
            // Act
            var result = await _service.GetUserRoleSafelyAsync(null);

            // Assert
            Assert.That(result, Is.EqualTo("Unknown"));
        }

        [Test]
        public async Task GetUserRoleSafelyAsync_ShouldReturnRole_WhenUserIsValid()
        {
            // Arrange
            var user = new ApplicationUser { Id = Guid.NewGuid() };
            var roles = new List<string> { "Teacher" };

            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(roles);

            // Act
            var result = await _service.GetUserRoleSafelyAsync(user);

            // Assert
            Assert.That(result, Is.EqualTo("Teacher"));
        }

        [Test]
        public async Task GetUserRoleSafelyAsync_ShouldReturnUnknown_WhenExceptionThrown()
        {
            // Arrange
            var user = new ApplicationUser { Id = Guid.NewGuid() };

            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _service.GetUserRoleSafelyAsync(user);

            // Assert
            Assert.That(result, Is.EqualTo("Unknown"));
        }

        #endregion

        #region CanUserUpdateQuestion Tests

        [Test]
        public void CanUserUpdateQuestion_ShouldReturnTrue_WhenUserIsSystemAdmin()
        {
            // Arrange
            var question = new LessonMaterialQuestion { CreatedByUserId = Guid.NewGuid() };
            var currentUser = new ApplicationUser { Id = Guid.NewGuid() };
            var userRole = "SystemAdmin";

            // Act
            var result = _service.CanUserUpdateQuestion(question, currentUser, userRole);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void CanUserUpdateQuestion_ShouldReturnTrue_WhenUserIsCreator()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var question = new LessonMaterialQuestion { CreatedByUserId = userId };
            var currentUser = new ApplicationUser { Id = userId };
            var userRole = "Teacher";

            // Act
            var result = _service.CanUserUpdateQuestion(question, currentUser, userRole);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void CanUserUpdateQuestion_ShouldReturnFalse_WhenUserIsNotCreator()
        {
            // Arrange
            var question = new LessonMaterialQuestion { CreatedByUserId = Guid.NewGuid() };
            var currentUser = new ApplicationUser { Id = Guid.NewGuid() };
            var userRole = "Teacher";

            // Act
            var result = _service.CanUserUpdateQuestion(question, currentUser, userRole);

            // Assert
            Assert.That(result, Is.False);
        }

        #endregion

        #region CanUserDeleteQuestionAsync Tests

        [Test]
        public async Task CanUserDeleteQuestionAsync_ShouldReturnTrue_WhenUserIsSystemAdmin()
        {
            // Arrange
            var question = new LessonMaterialQuestion { CreatedByUserId = Guid.NewGuid() };
            var currentUser = new ApplicationUser { Id = Guid.NewGuid() };
            var userRole = "SystemAdmin";

            // Act
            var result = await _service.CanUserDeleteQuestionAsync(question, currentUser, userRole);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task CanUserDeleteQuestionAsync_ShouldReturnFalse_WhenStudentHasComments()
        {
            // Arrange
            var question = new LessonMaterialQuestion
            {
                CreatedByUserId = Guid.NewGuid(),
                Comments = [new QuestionComment()]
            };
            var currentUser = new ApplicationUser { Id = Guid.NewGuid() };
            var userRole = "Student";

            // Act
            var result = await _service.CanUserDeleteQuestionAsync(question, currentUser, userRole);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task CanUserDeleteQuestionAsync_ShouldReturnTrue_WhenUserIsCreator()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var question = new LessonMaterialQuestion { CreatedByUserId = userId };
            var currentUser = new ApplicationUser { Id = userId };
            var userRole = "Teacher";

            // Act
            var result = await _service.CanUserDeleteQuestionAsync(question, currentUser, userRole);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task CanUserDeleteQuestionAsync_ShouldReturnFalse_WhenOriginalCreatorNotFound()
        {
            // Arrange
            var question = new LessonMaterialQuestion { CreatedByUserId = Guid.NewGuid() };
            var currentUser = new ApplicationUser { Id = Guid.NewGuid() };
            var userRole = "SchoolAdmin";

            _userRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _service.CanUserDeleteQuestionAsync(question, currentUser, userRole);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task CanUserDeleteQuestionAsync_ShouldReturnTrue_WhenSchoolAdminSameSchool()
        {
            // Arrange
            var schoolId = 1;
            var question = new LessonMaterialQuestion { CreatedByUserId = Guid.NewGuid() };
            var currentUser = new ApplicationUser { Id = Guid.NewGuid(), SchoolId = schoolId };
            var originalCreator = new ApplicationUser { Id = Guid.NewGuid(), SchoolId = schoolId };
            var userRole = "SchoolAdmin";

            _userRepositoryMock.Setup(x => x.GetByIdAsync(question.CreatedByUserId))
                .ReturnsAsync(originalCreator);
            _userManagerMock.Setup(x => x.GetRolesAsync(originalCreator))
                .ReturnsAsync(new List<string> { "Student" });

            // Act
            var result = await _service.CanUserDeleteQuestionAsync(question, currentUser, userRole);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task CanUserDeleteQuestionAsync_ShouldReturnFalse_WhenSchoolAdminNoSchoolId()
        {
            // Arrange
            var question = new LessonMaterialQuestion { CreatedByUserId = Guid.NewGuid() };
            var currentUser = new ApplicationUser { Id = Guid.NewGuid(), SchoolId = null };
            var originalCreator = new ApplicationUser { Id = Guid.NewGuid(), SchoolId = 1 };
            var userRole = "SchoolAdmin";

            _userRepositoryMock.Setup(x => x.GetByIdAsync(question.CreatedByUserId))
                .ReturnsAsync(originalCreator);
            _userManagerMock.Setup(x => x.GetRolesAsync(originalCreator))
                .ReturnsAsync(new List<string> { "Student" });

            // Act
            var result = await _service.CanUserDeleteQuestionAsync(question, currentUser, userRole);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task CanUserDeleteQuestionAsync_ShouldReturnFalse_WhenSchoolAdminDifferentSchool()
        {
            // Arrange
            var question = new LessonMaterialQuestion { CreatedByUserId = Guid.NewGuid() };
            var currentUser = new ApplicationUser { Id = Guid.NewGuid(), SchoolId = 1 };
            var originalCreator = new ApplicationUser { Id = Guid.NewGuid(), SchoolId = 2 };
            var userRole = "SchoolAdmin";

            _userRepositoryMock.Setup(x => x.GetByIdAsync(question.CreatedByUserId))
                .ReturnsAsync(originalCreator);
            _userManagerMock.Setup(x => x.GetRolesAsync(originalCreator))
                .ReturnsAsync(new List<string> { "Student" });

            // Act
            var result = await _service.CanUserDeleteQuestionAsync(question, currentUser, userRole);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task CanUserDeleteQuestionAsync_ShouldReturnTrue_WhenTeacherWithStudentRelationship()
        {
            // Arrange
            var question = new LessonMaterialQuestion { CreatedByUserId = Guid.NewGuid() };
            var currentUser = new ApplicationUser { Id = Guid.NewGuid(), SchoolId = 1 };
            var originalCreator = new ApplicationUser { Id = Guid.NewGuid(), SchoolId = 1 };
            var userRole = "Teacher";

            _userRepositoryMock.Setup(x => x.GetByIdAsync(question.CreatedByUserId))
                .ReturnsAsync(originalCreator);
            _userManagerMock.Setup(x => x.GetRolesAsync(originalCreator))
                .ReturnsAsync(new List<string> { "Student" });

            // Mock ValidateTeacherStudentRelationshipAsync to return true
            var classId = Guid.NewGuid();
            var classes = new List<Classroom>
            {
                new() { Id = classId, TeacherId = currentUser.Id, Status = EntityStatus.Active }
            };
            var studentClasses = new List<Classroom>
            {
                new() { Id = classId }
            };

            _classroomRepositoryMock.Setup(x => x.GetAllAsync())
                .ReturnsAsync(classes);
            _studentClassRepositoryMock.Setup(x => x.GetClassesForStudentAsync(originalCreator.Id))
                .ReturnsAsync(studentClasses);

            // Act
            var result = await _service.CanUserDeleteQuestionAsync(question, currentUser, userRole);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task CanUserDeleteQuestionAsync_ShouldReturnFalse_WhenTeacherNoStudentRelationship()
        {
            // Arrange
            var question = new LessonMaterialQuestion { CreatedByUserId = Guid.NewGuid() };
            var currentUser = new ApplicationUser { Id = Guid.NewGuid(), SchoolId = 1 };
            var originalCreator = new ApplicationUser { Id = Guid.NewGuid(), SchoolId = 1 };
            var userRole = "Teacher";

            _userRepositoryMock.Setup(x => x.GetByIdAsync(question.CreatedByUserId))
                .ReturnsAsync(originalCreator);
            _userManagerMock.Setup(x => x.GetRolesAsync(originalCreator))
                .ReturnsAsync(new List<string> { "Student" });

            // Mock ValidateTeacherStudentRelationshipAsync to return false
            _classroomRepositoryMock.Setup(x => x.GetAllAsync())
                .ReturnsAsync([]);

            // Act
            var result = await _service.CanUserDeleteQuestionAsync(question, currentUser, userRole);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task CanUserDeleteQuestionAsync_ShouldReturnTrue_WhenContentModeratorWithStudentRelationship()
        {
            // Arrange
            var question = new LessonMaterialQuestion { CreatedByUserId = Guid.NewGuid() };
            var currentUser = new ApplicationUser { Id = Guid.NewGuid(), SchoolId = 1 };
            var originalCreator = new ApplicationUser { Id = Guid.NewGuid(), SchoolId = 1 };
            var userRole = "ContentModerator";

            _userRepositoryMock.Setup(x => x.GetByIdAsync(question.CreatedByUserId))
                .ReturnsAsync(originalCreator);
            _userManagerMock.Setup(x => x.GetRolesAsync(originalCreator))
                .ReturnsAsync(new List<string> { "Student" });

            // Mock ValidateTeacherStudentRelationshipAsync to return true
            var classId = Guid.NewGuid();
            var classes = new List<Classroom>
            {
                new() { Id = classId, TeacherId = currentUser.Id, Status = EntityStatus.Active }
            };
            var studentClasses = new List<Classroom>
            {
                new() { Id = classId }
            };

            _classroomRepositoryMock.Setup(x => x.GetAllAsync())
                .ReturnsAsync(classes);
            _studentClassRepositoryMock.Setup(x => x.GetClassesForStudentAsync(originalCreator.Id))
                .ReturnsAsync(studentClasses);

            // Act
            var result = await _service.CanUserDeleteQuestionAsync(question, currentUser, userRole);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task CanUserDeleteQuestionAsync_ShouldReturnFalse_WhenTeacherNoSchoolId()
        {
            // Arrange
            var question = new LessonMaterialQuestion { CreatedByUserId = Guid.NewGuid() };
            var currentUser = new ApplicationUser { Id = Guid.NewGuid(), SchoolId = null };
            var originalCreator = new ApplicationUser { Id = Guid.NewGuid(), SchoolId = 1 };
            var userRole = "Teacher";

            _userRepositoryMock.Setup(x => x.GetByIdAsync(question.CreatedByUserId))
                .ReturnsAsync(originalCreator);
            _userManagerMock.Setup(x => x.GetRolesAsync(originalCreator))
                .ReturnsAsync(new List<string> { "Student" });

            // Act
            var result = await _service.CanUserDeleteQuestionAsync(question, currentUser, userRole);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task CanUserDeleteQuestionAsync_ShouldReturnFalse_WhenTeacherDifferentSchool()
        {
            // Arrange
            var question = new LessonMaterialQuestion { CreatedByUserId = Guid.NewGuid() };
            var currentUser = new ApplicationUser { Id = Guid.NewGuid(), SchoolId = 1 };
            var originalCreator = new ApplicationUser { Id = Guid.NewGuid(), SchoolId = 2 };
            var userRole = "Teacher";

            _userRepositoryMock.Setup(x => x.GetByIdAsync(question.CreatedByUserId))
                .ReturnsAsync(originalCreator);
            _userManagerMock.Setup(x => x.GetRolesAsync(originalCreator))
                .ReturnsAsync(new List<string> { "Student" });

            // Act
            var result = await _service.CanUserDeleteQuestionAsync(question, currentUser, userRole);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task CanUserDeleteQuestionAsync_ShouldReturnFalse_WhenOriginalCreatorNotStudent()
        {
            // Arrange
            var question = new LessonMaterialQuestion { CreatedByUserId = Guid.NewGuid() };
            var currentUser = new ApplicationUser { Id = Guid.NewGuid(), SchoolId = 1 };
            var originalCreator = new ApplicationUser { Id = Guid.NewGuid(), SchoolId = 1 };
            var userRole = "Teacher";

            _userRepositoryMock.Setup(x => x.GetByIdAsync(question.CreatedByUserId))
                .ReturnsAsync(originalCreator);
            _userManagerMock.Setup(x => x.GetRolesAsync(originalCreator))
                .ReturnsAsync(new List<string> { "Teacher" });

            // Act
            var result = await _service.CanUserDeleteQuestionAsync(question, currentUser, userRole);

            // Assert
            Assert.That(result, Is.False);
        }

        #endregion

        #region CanUserUpdateComment Tests

        [Test]
        public void CanUserUpdateComment_ShouldReturnTrue_WhenUserIsSystemAdmin()
        {
            // Arrange
            var comment = new QuestionComment { CreatedByUserId = Guid.NewGuid() };
            var currentUser = new ApplicationUser { Id = Guid.NewGuid() };
            var userRole = "SystemAdmin";

            // Act
            var result = _service.CanUserUpdateComment(comment, currentUser, userRole);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void CanUserUpdateComment_ShouldReturnTrue_WhenUserIsCreator()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var comment = new QuestionComment { CreatedByUserId = userId };
            var currentUser = new ApplicationUser { Id = userId };
            var userRole = "Teacher";

            // Act
            var result = _service.CanUserUpdateComment(comment, currentUser, userRole);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void CanUserUpdateComment_ShouldReturnFalse_WhenUserIsNotCreator()
        {
            // Arrange
            var comment = new QuestionComment { CreatedByUserId = Guid.NewGuid() };
            var currentUser = new ApplicationUser { Id = Guid.NewGuid() };
            var userRole = "Teacher";

            // Act
            var result = _service.CanUserUpdateComment(comment, currentUser, userRole);

            // Assert
            Assert.That(result, Is.False);
        }

        #endregion

        #region CanUserDeleteCommentAsync Tests

        [Test]
        public async Task CanUserDeleteCommentAsync_ShouldReturnTrue_WhenUserIsSystemAdmin()
        {
            // Arrange
            var comment = new QuestionComment { CreatedByUserId = Guid.NewGuid() };
            var currentUser = new ApplicationUser { Id = Guid.NewGuid() };
            var userRole = "SystemAdmin";

            // Act
            var result = await _service.CanUserDeleteCommentAsync(comment, currentUser, userRole);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task CanUserDeleteCommentAsync_ShouldReturnFalse_WhenStudentHasReplies()
        {
            // Arrange
            var comment = new QuestionComment
            {
                CreatedByUserId = Guid.NewGuid(),
                Replies = [new QuestionComment()]
            };
            var currentUser = new ApplicationUser { Id = Guid.NewGuid() };
            var userRole = "Student";

            // Act
            var result = await _service.CanUserDeleteCommentAsync(comment, currentUser, userRole);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task CanUserDeleteCommentAsync_ShouldReturnTrue_WhenUserIsCreator()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var comment = new QuestionComment { CreatedByUserId = userId };
            var currentUser = new ApplicationUser { Id = userId };
            var userRole = "Teacher";

            // Act
            var result = await _service.CanUserDeleteCommentAsync(comment, currentUser, userRole);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task CanUserDeleteCommentAsync_ShouldReturnFalse_WhenCommentCreatorNotFound()
        {
            // Arrange
            var comment = new QuestionComment { CreatedByUserId = Guid.NewGuid() };
            var currentUser = new ApplicationUser { Id = Guid.NewGuid() };
            var userRole = "SchoolAdmin";

            _userRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _service.CanUserDeleteCommentAsync(comment, currentUser, userRole);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task CanUserDeleteCommentAsync_ShouldReturnTrue_WhenSchoolAdminSameSchool()
        {
            // Arrange
            var schoolId = 1;
            var comment = new QuestionComment { CreatedByUserId = Guid.NewGuid() };
            var currentUser = new ApplicationUser { Id = Guid.NewGuid(), SchoolId = schoolId };
            var commentCreator = new ApplicationUser { Id = Guid.NewGuid(), SchoolId = schoolId };
            var userRole = "SchoolAdmin";

            _userRepositoryMock.Setup(x => x.GetByIdAsync(comment.CreatedByUserId))
                .ReturnsAsync(commentCreator);
            _userManagerMock.Setup(x => x.GetRolesAsync(commentCreator))
                .ReturnsAsync(new List<string> { "Student" });

            // Act
            var result = await _service.CanUserDeleteCommentAsync(comment, currentUser, userRole);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task CanUserDeleteCommentAsync_ShouldReturnFalse_WhenSchoolAdminNoSchoolId()
        {
            // Arrange
            var comment = new QuestionComment { CreatedByUserId = Guid.NewGuid() };
            var currentUser = new ApplicationUser { Id = Guid.NewGuid(), SchoolId = null };
            var commentCreator = new ApplicationUser { Id = Guid.NewGuid(), SchoolId = 1 };
            var userRole = "SchoolAdmin";

            _userRepositoryMock.Setup(x => x.GetByIdAsync(comment.CreatedByUserId))
                .ReturnsAsync(commentCreator);
            _userManagerMock.Setup(x => x.GetRolesAsync(commentCreator))
                .ReturnsAsync(new List<string> { "Student" });

            // Act
            var result = await _service.CanUserDeleteCommentAsync(comment, currentUser, userRole);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task CanUserDeleteCommentAsync_ShouldReturnFalse_WhenSchoolAdminDifferentSchool()
        {
            // Arrange
            var comment = new QuestionComment { CreatedByUserId = Guid.NewGuid() };
            var currentUser = new ApplicationUser { Id = Guid.NewGuid(), SchoolId = 1 };
            var commentCreator = new ApplicationUser { Id = Guid.NewGuid(), SchoolId = 2 };
            var userRole = "SchoolAdmin";

            _userRepositoryMock.Setup(x => x.GetByIdAsync(comment.CreatedByUserId))
                .ReturnsAsync(commentCreator);
            _userManagerMock.Setup(x => x.GetRolesAsync(commentCreator))
                .ReturnsAsync(new List<string> { "Student" });

            // Act
            var result = await _service.CanUserDeleteCommentAsync(comment, currentUser, userRole);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task CanUserDeleteCommentAsync_ShouldReturnTrue_WhenTeacherWithStudentRelationship()
        {
            // Arrange
            var comment = new QuestionComment { CreatedByUserId = Guid.NewGuid() };
            var currentUser = new ApplicationUser { Id = Guid.NewGuid(), SchoolId = 1 };
            var commentCreator = new ApplicationUser { Id = Guid.NewGuid(), SchoolId = 1 };
            var userRole = "Teacher";

            _userRepositoryMock.Setup(x => x.GetByIdAsync(comment.CreatedByUserId))
                .ReturnsAsync(commentCreator);
            _userManagerMock.Setup(x => x.GetRolesAsync(commentCreator))
                .ReturnsAsync(new List<string> { "Student" });

            // Mock ValidateTeacherStudentRelationshipAsync to return true
            var classId = Guid.NewGuid();
            var classes = new List<Classroom>
            {
                new() { Id = classId, TeacherId = currentUser.Id, Status = EntityStatus.Active }
            };
            var studentClasses = new List<Classroom>
            {
                new() { Id = classId }
            };

            _classroomRepositoryMock.Setup(x => x.GetAllAsync())
                .ReturnsAsync(classes);
            _studentClassRepositoryMock.Setup(x => x.GetClassesForStudentAsync(commentCreator.Id))
                .ReturnsAsync(studentClasses);

            // Act
            var result = await _service.CanUserDeleteCommentAsync(comment, currentUser, userRole);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task CanUserDeleteCommentAsync_ShouldReturnFalse_WhenTeacherNoStudentRelationship()
        {
            // Arrange
            var comment = new QuestionComment { CreatedByUserId = Guid.NewGuid() };
            var currentUser = new ApplicationUser { Id = Guid.NewGuid(), SchoolId = 1 };
            var commentCreator = new ApplicationUser { Id = Guid.NewGuid(), SchoolId = 1 };
            var userRole = "Teacher";

            _userRepositoryMock.Setup(x => x.GetByIdAsync(comment.CreatedByUserId))
                .ReturnsAsync(commentCreator);
            _userManagerMock.Setup(x => x.GetRolesAsync(commentCreator))
                .ReturnsAsync(new List<string> { "Student" });

            // Mock ValidateTeacherStudentRelationshipAsync to return false
            _classroomRepositoryMock.Setup(x => x.GetAllAsync())
                .ReturnsAsync([]);

            // Act
            var result = await _service.CanUserDeleteCommentAsync(comment, currentUser, userRole);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task CanUserDeleteCommentAsync_ShouldReturnTrue_WhenContentModeratorWithStudentRelationship()
        {
            // Arrange
            var comment = new QuestionComment { CreatedByUserId = Guid.NewGuid() };
            var currentUser = new ApplicationUser { Id = Guid.NewGuid(), SchoolId = 1 };
            var commentCreator = new ApplicationUser { Id = Guid.NewGuid(), SchoolId = 1 };
            var userRole = "ContentModerator";

            _userRepositoryMock.Setup(x => x.GetByIdAsync(comment.CreatedByUserId))
                .ReturnsAsync(commentCreator);
            _userManagerMock.Setup(x => x.GetRolesAsync(commentCreator))
                .ReturnsAsync(new List<string> { "Student" });

            // Mock ValidateTeacherStudentRelationshipAsync to return true
            var classId = Guid.NewGuid();
            var classes = new List<Classroom>
            {
                new() { Id = classId, TeacherId = currentUser.Id, Status = EntityStatus.Active }
            };
            var studentClasses = new List<Classroom>
            {
                new() { Id = classId }
            };

            _classroomRepositoryMock.Setup(x => x.GetAllAsync())
                .ReturnsAsync(classes);
            _studentClassRepositoryMock.Setup(x => x.GetClassesForStudentAsync(commentCreator.Id))
                .ReturnsAsync(studentClasses);

            // Act
            var result = await _service.CanUserDeleteCommentAsync(comment, currentUser, userRole);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task CanUserDeleteCommentAsync_ShouldReturnFalse_WhenTeacherNoSchoolId()
        {
            // Arrange
            var comment = new QuestionComment { CreatedByUserId = Guid.NewGuid() };
            var currentUser = new ApplicationUser { Id = Guid.NewGuid(), SchoolId = null };
            var commentCreator = new ApplicationUser { Id = Guid.NewGuid(), SchoolId = 1 };
            var userRole = "Teacher";

            _userRepositoryMock.Setup(x => x.GetByIdAsync(comment.CreatedByUserId))
                .ReturnsAsync(commentCreator);
            _userManagerMock.Setup(x => x.GetRolesAsync(commentCreator))
                .ReturnsAsync(new List<string> { "Student" });

            // Act
            var result = await _service.CanUserDeleteCommentAsync(comment, currentUser, userRole);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task CanUserDeleteCommentAsync_ShouldReturnFalse_WhenTeacherDifferentSchool()
        {
            // Arrange
            var comment = new QuestionComment { CreatedByUserId = Guid.NewGuid() };
            var currentUser = new ApplicationUser { Id = Guid.NewGuid(), SchoolId = 1 };
            var commentCreator = new ApplicationUser { Id = Guid.NewGuid(), SchoolId = 2 };
            var userRole = "Teacher";

            _userRepositoryMock.Setup(x => x.GetByIdAsync(comment.CreatedByUserId))
                .ReturnsAsync(commentCreator);
            _userManagerMock.Setup(x => x.GetRolesAsync(commentCreator))
                .ReturnsAsync(new List<string> { "Student" });

            // Act
            var result = await _service.CanUserDeleteCommentAsync(comment, currentUser, userRole);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task CanUserDeleteCommentAsync_ShouldReturnFalse_WhenCommentCreatorNotStudent()
        {
            // Arrange
            var comment = new QuestionComment { CreatedByUserId = Guid.NewGuid() };
            var currentUser = new ApplicationUser { Id = Guid.NewGuid(), SchoolId = 1 };
            var commentCreator = new ApplicationUser { Id = Guid.NewGuid(), SchoolId = 1 };
            var userRole = "Teacher";

            _userRepositoryMock.Setup(x => x.GetByIdAsync(comment.CreatedByUserId))
                .ReturnsAsync(commentCreator);
            _userManagerMock.Setup(x => x.GetRolesAsync(commentCreator))
                .ReturnsAsync(new List<string> { "Teacher" });

            // Act
            var result = await _service.CanUserDeleteCommentAsync(comment, currentUser, userRole);

            // Assert
            Assert.That(result, Is.False);
        }

        #endregion

        #region ValidateTeacherStudentRelationshipAsync Tests

        [Test]
        public async Task ValidateTeacherStudentRelationshipAsync_ShouldReturnFalse_WhenTeacherHasNoClasses()
        {
            // Arrange
            var teacherId = Guid.NewGuid();
            var studentId = Guid.NewGuid();

            _classroomRepositoryMock.Setup(x => x.GetAllAsync())
                .ReturnsAsync([]);

            // Act
            var result = await _service.ValidateTeacherStudentRelationshipAsync(teacherId, studentId);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task ValidateTeacherStudentRelationshipAsync_ShouldReturnFalse_WhenTeacherHasNoActiveClasses()
        {
            // Arrange
            var teacherId = Guid.NewGuid();
            var studentId = Guid.NewGuid();

            var classes = new List<Classroom>
            {
                new() { Id = Guid.NewGuid(), TeacherId = teacherId, Status = EntityStatus.Inactive }
            };

            _classroomRepositoryMock.Setup(x => x.GetAllAsync())
                .ReturnsAsync(classes);

            // Act
            var result = await _service.ValidateTeacherStudentRelationshipAsync(teacherId, studentId);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task ValidateTeacherStudentRelationshipAsync_ShouldReturnTrue_WhenTeacherAndStudentInSameClass()
        {
            // Arrange
            var teacherId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var classId = Guid.NewGuid();

            var classes = new List<Classroom>
            {
                new() { Id = classId, TeacherId = teacherId, Status = EntityStatus.Active }
            };

            var studentClasses = new List<Classroom>
            {
                new() { Id = classId }
            };

            _classroomRepositoryMock.Setup(x => x.GetAllAsync())
                .ReturnsAsync(classes);
            _studentClassRepositoryMock.Setup(x => x.GetClassesForStudentAsync(studentId))
                .ReturnsAsync(studentClasses);

            // Act
            var result = await _service.ValidateTeacherStudentRelationshipAsync(teacherId, studentId);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task ValidateTeacherStudentRelationshipAsync_ShouldReturnFalse_WhenTeacherAndStudentInDifferentClasses()
        {
            // Arrange
            var teacherId = Guid.NewGuid();
            var studentId = Guid.NewGuid();

            var classes = new List<Classroom>
            {
                new() { Id = Guid.NewGuid(), TeacherId = teacherId, Status = EntityStatus.Active }
            };

            var studentClasses = new List<Classroom>
            {
                new() { Id = Guid.NewGuid() }
            };

            _classroomRepositoryMock.Setup(x => x.GetAllAsync())
                .ReturnsAsync(classes);
            _studentClassRepositoryMock.Setup(x => x.GetClassesForStudentAsync(studentId))
                .ReturnsAsync(studentClasses);

            // Act
            var result = await _service.ValidateTeacherStudentRelationshipAsync(teacherId, studentId);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task ValidateTeacherStudentRelationshipAsync_ShouldReturnTrue_WhenMultipleClassesWithOneMatch()
        {
            // Arrange
            var teacherId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var class1Id = Guid.NewGuid();
            var class2Id = Guid.NewGuid();
            var class3Id = Guid.NewGuid();

            var classes = new List<Classroom>
            {
                new() { Id = class1Id, TeacherId = teacherId, Status = EntityStatus.Active },
                new() { Id = class2Id, TeacherId = teacherId, Status = EntityStatus.Active }
            };

            var studentClasses = new List<Classroom>
            {
                new() { Id = class2Id },
                new() { Id = class3Id }
            };

            _classroomRepositoryMock.Setup(x => x.GetAllAsync())
                .ReturnsAsync(classes);
            _studentClassRepositoryMock.Setup(x => x.GetClassesForStudentAsync(studentId))
                .ReturnsAsync(studentClasses);

            // Act
            var result = await _service.ValidateTeacherStudentRelationshipAsync(teacherId, studentId);

            // Assert
            Assert.That(result, Is.True);
        }

        #endregion

        #region CalculateTotalCommentCount Tests

        [Test]
        public void CalculateTotalCommentCount_ShouldReturnZero_WhenCommentsIsNull()
        {
            // Act
            var result = _service.CalculateTotalCommentCount(null);

            // Assert
            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public void CalculateTotalCommentCount_ShouldReturnZero_WhenCommentsIsEmpty()
        {
            // Act
            var result = _service.CalculateTotalCommentCount([]);

            // Assert
            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public void CalculateTotalCommentCount_ShouldReturnCorrectCount_WhenCommentsExist()
        {
            // Arrange
            var comments = new List<QuestionComment>
            {
                new() { Id = Guid.NewGuid(), ParentCommentId = null }, // Top level
                new() { Id = Guid.NewGuid(), ParentCommentId = Guid.NewGuid() }, // Reply
                new() { Id = Guid.NewGuid(), ParentCommentId = null }, // Top level
                new() { Id = Guid.NewGuid(), ParentCommentId = Guid.NewGuid() } // Reply
            };

            // Act
            var result = _service.CalculateTotalCommentCount(comments);

            // Assert
            Assert.That(result, Is.EqualTo(4));
        }

        [Test]
        public void CalculateTotalCommentCount_ShouldReturnCorrectCount_WhenOnlyTopLevelComments()
        {
            // Arrange
            var comments = new List<QuestionComment>
            {
                new() { Id = Guid.NewGuid(), ParentCommentId = null },
                new() { Id = Guid.NewGuid(), ParentCommentId = null },
                new() { Id = Guid.NewGuid(), ParentCommentId = null }
            };

            // Act
            var result = _service.CalculateTotalCommentCount(comments);

            // Assert
            Assert.That(result, Is.EqualTo(3));
        }

        [Test]
        public void CalculateTotalCommentCount_ShouldReturnCorrectCount_WhenOnlyReplies()
        {
            // Arrange
            var parentId = Guid.NewGuid();
            var comments = new List<QuestionComment>
            {
                new() { Id = Guid.NewGuid(), ParentCommentId = parentId },
                new() { Id = Guid.NewGuid(), ParentCommentId = parentId },
                new() { Id = Guid.NewGuid(), ParentCommentId = parentId }
            };

            // Act
            var result = _service.CalculateTotalCommentCount(comments);

            // Assert
            Assert.That(result, Is.EqualTo(3));
        }

        [Test]
        public void CalculateTotalCommentCount_ShouldReturnCorrectCount_WithComplexMix()
        {
            // Arrange
            var parentId1 = Guid.NewGuid();
            var parentId2 = Guid.NewGuid();
            var comments = new List<QuestionComment>
            {
                new() { Id = Guid.NewGuid(), ParentCommentId = null }, // Top level
                new() { Id = Guid.NewGuid(), ParentCommentId = null }, // Top level
                new() { Id = Guid.NewGuid(), ParentCommentId = parentId1 }, // Reply to first
                new() { Id = Guid.NewGuid(), ParentCommentId = parentId1 }, // Reply to first
                new() { Id = Guid.NewGuid(), ParentCommentId = parentId2 }, // Reply to second
                new() { Id = Guid.NewGuid(), ParentCommentId = null }, // Top level
            };

            // Act
            var result = _service.CalculateTotalCommentCount(comments);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.EqualTo(6));
                Assert.That(comments.Count(c => c.ParentCommentId == null), Is.EqualTo(3));
                Assert.That(comments.Count(c => c.ParentCommentId != null), Is.EqualTo(3));
            });
        }

        #endregion
    }
}
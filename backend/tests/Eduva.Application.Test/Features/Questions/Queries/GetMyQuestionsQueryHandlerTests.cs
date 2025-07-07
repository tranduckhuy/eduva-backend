using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.Questions.Queries;
using Eduva.Application.Features.Questions.Specifications;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Eduva.Application.Test.Features.Questions.Queries
{
    [TestFixture]
    public class GetMyQuestionsQueryHandlerTests
    {
        private Mock<ILessonMaterialQuestionRepository> _repositoryMock = null!;
        private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<IQuestionPermissionService> _permissionServiceMock = null!;
        private Mock<IGenericRepository<ApplicationUser, Guid>> _userRepositoryMock = null!;
        private Mock<IStudentClassRepository> _studentClassRepositoryMock = null!;
        private GetMyQuestionsQueryHandler _handler = null!;

        #region Setup

        [SetUp]
        public void Setup()
        {
            _repositoryMock = new Mock<ILessonMaterialQuestionRepository>();
            _userRepositoryMock = new Mock<IGenericRepository<ApplicationUser, Guid>>();
            _studentClassRepositoryMock = new Mock<IStudentClassRepository>();

            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _permissionServiceMock = new Mock<IQuestionPermissionService>();

            _unitOfWorkMock.Setup(x => x.GetRepository<ApplicationUser, Guid>())
                .Returns(_userRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.GetCustomRepository<IStudentClassRepository>())
                .Returns(_studentClassRepositoryMock.Object);

            _handler = new GetMyQuestionsQueryHandler(
                _repositoryMock.Object,
                _userManagerMock.Object,
                _unitOfWorkMock.Object,
                _permissionServiceMock.Object);
        }

        #endregion

        #region Tests

        [Test]
        public void Handle_ShouldThrowUserNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            var param = new MyQuestionsSpecParam();
            var query = new GetMyQuestionsQuery(param, Guid.NewGuid());

            _userRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((ApplicationUser?)null);

            // Act & Assert
            Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(query, CancellationToken.None));
        }

        [Test]
        public void Handle_ShouldThrowUserNotPartOfSchool_WhenUserHasNoSchoolId()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var param = new MyQuestionsSpecParam();
            var query = new GetMyQuestionsQuery(param, userId);
            var user = new ApplicationUser { Id = userId, SchoolId = null };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(query, CancellationToken.None));
            Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.UserNotPartOfSchool));
        }

        [Test]
        public void Handle_ShouldThrowInsufficientPermission_WhenUserRoleIsInvalid()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var param = new MyQuestionsSpecParam();
            var query = new GetMyQuestionsQuery(param, userId);
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync([]);
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("InvalidRole");

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(query, CancellationToken.None));
            Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.InsufficientPermission));
        }

        [Test]
        public async Task Handle_ShouldReturnQuestions_WhenUserIsSchoolAdmin()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var param = new MyQuestionsSpecParam();
            var query = new GetMyQuestionsQuery(param, userId);
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var questions = new List<LessonMaterialQuestion> { new() { Id = Guid.NewGuid() } };
            var pagination = new Pagination<LessonMaterialQuestion> { Data = questions };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(["SchoolAdmin"]);
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("SchoolAdmin");
            _repositoryMock.Setup(x => x.GetWithSpecAsync(It.IsAny<MyQuestionsSpecification>()))
                .ReturnsAsync(pagination);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.That(result.Data, Has.Count.EqualTo(1));
            Assert.That(result.Data.First().CreatedByRole, Is.EqualTo("SchoolAdmin"));
        }

        [Test]
        public void Handle_ShouldThrowStudentNotEnrolled_WhenStudentNotEnrolledInAnyClass()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var param = new MyQuestionsSpecParam();
            var query = new GetMyQuestionsQuery(param, userId);
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(["Student"]);
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Student");
            _studentClassRepositoryMock.Setup(x => x.IsEnrolledInAnyClassAsync(userId))
                .ReturnsAsync(false);

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(query, CancellationToken.None));
            Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.StudentNotEnrolledInAnyClassForQuestions));
        }

        [Test]
        public void Handle_ShouldThrowTeacherMustHaveActiveClass_WhenTeacherHasNoActiveClass()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var param = new MyQuestionsSpecParam();
            var query = new GetMyQuestionsQuery(param, userId);
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(["Teacher"]);
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Teacher");
            _studentClassRepositoryMock.Setup(x => x.TeacherHasActiveClassAsync(userId))
                .ReturnsAsync(false);

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(query, CancellationToken.None));
            Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.TeacherMustHaveActiveClass));
        }

        #endregion

    }
}
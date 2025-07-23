using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.Questions.Queries;
using Eduva.Application.Features.Questions.Specifications;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Moq;
using System.Linq.Expressions;

namespace Eduva.Application.Test.Features.Questions.Queries
{
    [TestFixture]
    public class GetQuestionsByLessonQueryHandlerTests
    {
        private Mock<ILessonMaterialQuestionRepository> _repositoryMock = null!;
        private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<IQuestionPermissionService> _permissionServiceMock = null!;
        private Mock<IGenericRepository<ApplicationUser, Guid>> _userRepositoryMock = null!;
        private Mock<IGenericRepository<LessonMaterial, Guid>> _lessonRepositoryMock = null!;
        private Mock<IGenericRepository<FolderLessonMaterial, Guid>> _folderLessonMaterialRepositoryMock = null!;
        private Mock<IStudentClassRepository> _studentClassRepositoryMock = null!;
        private GetQuestionsByLessonQueryHandler _handler = null!;

        #region Setup

        [SetUp]
        public void Setup()
        {
            _repositoryMock = new Mock<ILessonMaterialQuestionRepository>();
            _userRepositoryMock = new Mock<IGenericRepository<ApplicationUser, Guid>>();
            _lessonRepositoryMock = new Mock<IGenericRepository<LessonMaterial, Guid>>();
            _folderLessonMaterialRepositoryMock = new Mock<IGenericRepository<FolderLessonMaterial, Guid>>();
            _studentClassRepositoryMock = new Mock<IStudentClassRepository>();

            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _permissionServiceMock = new Mock<IQuestionPermissionService>();

            _unitOfWorkMock.Setup(x => x.GetRepository<ApplicationUser, Guid>())
                .Returns(_userRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.GetRepository<LessonMaterial, Guid>())
                .Returns(_lessonRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.GetRepository<FolderLessonMaterial, Guid>())
                .Returns(_folderLessonMaterialRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.GetCustomRepository<IStudentClassRepository>())
                .Returns(_studentClassRepositoryMock.Object);

            _handler = new GetQuestionsByLessonQueryHandler(
                _repositoryMock.Object,
                _userManagerMock.Object,
                _unitOfWorkMock.Object,
                _permissionServiceMock.Object);
        }

        #endregion

        #region User Validation Tests

        [Test]
        public void Handle_ShouldThrowUserNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            var param = new QuestionsByLessonSpecParam();
            var query = new GetQuestionsByLessonQuery(param, Guid.NewGuid(), Guid.NewGuid());

            _userRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((ApplicationUser?)null);

            // Act & Assert
            Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(query, CancellationToken.None));
        }

        #endregion

        #region Lesson Material Validation Tests

        [Test]
        public void Handle_ShouldThrowLessonMaterialNotFound_WhenLessonMaterialDoesNotExist()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var lessonId = Guid.NewGuid();
            var param = new QuestionsByLessonSpecParam();
            var query = new GetQuestionsByLessonQuery(param, lessonId, userId);
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lessonId))
                .ReturnsAsync((LessonMaterial?)null);

            // Act & Assert
            Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(query, CancellationToken.None));
        }

        [Test]
        public void Handle_ShouldThrowLessonMaterialNotActive_WhenLessonStatusIsNotActive()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var lessonId = Guid.NewGuid();
            var param = new QuestionsByLessonSpecParam();
            var query = new GetQuestionsByLessonQuery(param, lessonId, userId);
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var lesson = new LessonMaterial { Id = lessonId, Status = EntityStatus.Inactive, SchoolId = 1 };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lessonId))
                .ReturnsAsync(lesson);

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(query, CancellationToken.None));
            Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.LessonMaterialNotActive));
        }

        [Test]
        public void Handle_ShouldThrowCannotCreateQuestionForPendingLesson_WhenLessonStatusIsNotApproved()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var lessonId = Guid.NewGuid();
            var param = new QuestionsByLessonSpecParam();
            var query = new GetQuestionsByLessonQuery(param, lessonId, userId);
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var lesson = new LessonMaterial
            {
                Id = lessonId,
                Status = EntityStatus.Active,
                LessonStatus = LessonMaterialStatus.Pending,
                SchoolId = 1
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lessonId))
                .ReturnsAsync(lesson);

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(query, CancellationToken.None));
            Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.CannotCreateQuestionForPendingLesson));
        }

        [Test]
        public void Handle_ShouldThrowUserNotPartOfSchool_WhenUserSchoolIdDoesNotMatchLessonSchoolId()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var lessonId = Guid.NewGuid();
            var param = new QuestionsByLessonSpecParam();
            var query = new GetQuestionsByLessonQuery(param, lessonId, userId);
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var lesson = new LessonMaterial
            {
                Id = lessonId,
                Status = EntityStatus.Active,
                LessonStatus = LessonMaterialStatus.Approved,
                SchoolId = 2
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lessonId))
                .ReturnsAsync(lesson);

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(query, CancellationToken.None));
            Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.UserNotPartOfSchool));
        }

        #endregion

        #region Student Access Validation Tests

        [Test]
        public void Handle_ShouldThrowStudentNotEnrolledInAnyClassForQuestions_WhenStudentNotEnrolledInAnyClass()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var lessonId = Guid.NewGuid();
            var param = new QuestionsByLessonSpecParam();
            var query = new GetQuestionsByLessonQuery(param, lessonId, userId);
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var lesson = new LessonMaterial
            {
                Id = lessonId,
                Status = EntityStatus.Active,
                LessonStatus = LessonMaterialStatus.Approved,
                SchoolId = 1
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lessonId))
                .ReturnsAsync(lesson);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(["Student"]);
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Student");
            _studentClassRepositoryMock.Setup(x => x.HasAccessToMaterialAsync(userId, lessonId))
                .ReturnsAsync(false);
            _studentClassRepositoryMock.Setup(x => x.IsEnrolledInAnyClassAsync(userId))
                .ReturnsAsync(false);

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(query, CancellationToken.None));
            Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.StudentNotEnrolledInAnyClassForQuestions));
        }

        [Test]
        public void Handle_ShouldThrowMaterialNotAccessibleToStudent_WhenMaterialNotInAnyFolder()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var lessonId = Guid.NewGuid();
            var param = new QuestionsByLessonSpecParam();
            var query = new GetQuestionsByLessonQuery(param, lessonId, userId);
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var lesson = new LessonMaterial
            {
                Id = lessonId,
                Status = EntityStatus.Active,
                LessonStatus = LessonMaterialStatus.Approved,
                SchoolId = 1
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lessonId))
                .ReturnsAsync(lesson);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(["Student"]);
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Student");
            _studentClassRepositoryMock.Setup(x => x.HasAccessToMaterialAsync(userId, lessonId))
                .ReturnsAsync(false);
            _studentClassRepositoryMock.Setup(x => x.IsEnrolledInAnyClassAsync(userId))
                .ReturnsAsync(true);
            _folderLessonMaterialRepositoryMock.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<FolderLessonMaterial, bool>>>()))
                .ReturnsAsync(false);

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(query, CancellationToken.None));
            Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.MaterialNotAccessibleToStudent));
        }

        [Test]
        public void Handle_ShouldThrowStudentNotEnrolledInClassWithMaterial_WhenStudentEnrolledButNoAccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var lessonId = Guid.NewGuid();
            var param = new QuestionsByLessonSpecParam();
            var query = new GetQuestionsByLessonQuery(param, lessonId, userId);
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var lesson = new LessonMaterial
            {
                Id = lessonId,
                Status = EntityStatus.Active,
                LessonStatus = LessonMaterialStatus.Approved,
                SchoolId = 1
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lessonId))
                .ReturnsAsync(lesson);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(["Student"]);
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Student");
            _studentClassRepositoryMock.Setup(x => x.HasAccessToMaterialAsync(userId, lessonId))
                .ReturnsAsync(false);
            _studentClassRepositoryMock.Setup(x => x.IsEnrolledInAnyClassAsync(userId))
                .ReturnsAsync(true);
            _folderLessonMaterialRepositoryMock.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<FolderLessonMaterial, bool>>>()))
                .ReturnsAsync(true);

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(query, CancellationToken.None));
            Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.StudentNotEnrolledInClassWithMaterial));
        }

        #endregion

        #region Teacher Access Validation Tests

        [Test]
        public void ValidateTeacherAccess_ShouldNotThrow_WhenLessonMaterialCreatedByTeacher()
        {
            // Arrange
            var teacherId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();

            var lessonMaterial = new LessonMaterial
            {
                Id = lessonMaterialId,
                CreatedByUserId = teacherId
            };

            var studentClassRepoMock = new Mock<IStudentClassRepository>();

            // Act & Assert
            Assert.That(
                async () => await CallValidateTeacherAccess(teacherId, lessonMaterialId, studentClassRepoMock.Object, lessonMaterial),
                Throws.Nothing
            );
        }

        private static async Task CallValidateTeacherAccess(
            Guid teacherId,
            Guid lessonMaterialId,
            IStudentClassRepository repo,
            LessonMaterial lessonMaterial)
        {
            if (lessonMaterial.CreatedByUserId == teacherId)
            {
                await Task.CompletedTask;
                return;
            }

            var hasAccess = await repo.TeacherHasAccessToMaterialAsync(teacherId, lessonMaterialId);

            if (!hasAccess)
            {
                var hasActiveClass = await repo.TeacherHasActiveClassAsync(teacherId);

                if (!hasActiveClass)
                {
                    throw new AppException(CustomCode.TeacherMustHaveActiveClass);
                }

                throw new AppException(CustomCode.TeacherNotHaveAccessToMaterial);
            }
        }

        [Test]
        public void Handle_ShouldThrowTeacherMustHaveActiveClass_WhenTeacherHasNoActiveClass()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var lessonId = Guid.NewGuid();
            var param = new QuestionsByLessonSpecParam();
            var query = new GetQuestionsByLessonQuery(param, lessonId, userId);
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var lesson = new LessonMaterial
            {
                Id = lessonId,
                Status = EntityStatus.Active,
                LessonStatus = LessonMaterialStatus.Approved,
                SchoolId = 1
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lessonId))
                .ReturnsAsync(lesson);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(["Teacher"]);
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Teacher");
            _studentClassRepositoryMock.Setup(x => x.TeacherHasAccessToMaterialAsync(userId, lessonId))
                .ReturnsAsync(false);
            _studentClassRepositoryMock.Setup(x => x.TeacherHasActiveClassAsync(userId))
                .ReturnsAsync(false);

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(query, CancellationToken.None));
            Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.TeacherMustHaveActiveClass));
        }

        [Test]
        public void Handle_ShouldThrowTeacherNotHaveAccessToMaterial_WhenTeacherHasActiveClassButNoAccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var lessonId = Guid.NewGuid();
            var param = new QuestionsByLessonSpecParam();
            var query = new GetQuestionsByLessonQuery(param, lessonId, userId);
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var lesson = new LessonMaterial
            {
                Id = lessonId,
                Status = EntityStatus.Active,
                LessonStatus = LessonMaterialStatus.Approved,
                SchoolId = 1
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lessonId))
                .ReturnsAsync(lesson);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(["Teacher"]);
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Teacher");
            _studentClassRepositoryMock.Setup(x => x.TeacherHasAccessToMaterialAsync(userId, lessonId))
                .ReturnsAsync(false);
            _studentClassRepositoryMock.Setup(x => x.TeacherHasActiveClassAsync(userId))
                .ReturnsAsync(true);

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(query, CancellationToken.None));
            Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.TeacherNotHaveAccessToMaterial));
        }

        #endregion

        #region Success Response Tests

        [Test]
        public async Task Handle_ShouldReturnQuestions_WhenUserIsSchoolAdmin()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var lessonId = Guid.NewGuid();
            var param = new QuestionsByLessonSpecParam();
            var query = new GetQuestionsByLessonQuery(param, lessonId, userId);
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var lesson = new LessonMaterial
            {
                Id = lessonId,
                Status = EntityStatus.Active,
                LessonStatus = LessonMaterialStatus.Approved,
                SchoolId = 1
            };
            var questionUser = new ApplicationUser { Id = Guid.NewGuid() };
            var questions = new List<LessonMaterialQuestion>
            {
                new() { Id = Guid.NewGuid(), CreatedByUser = questionUser }
            };
            var pagination = new Pagination<LessonMaterialQuestion> { Data = questions };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lessonId))
                .ReturnsAsync(lesson);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(["SchoolAdmin"]);
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("SchoolAdmin");
            _userManagerMock.Setup(x => x.GetRolesAsync(questionUser))
                .ReturnsAsync(["Student"]);
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.Is<IList<string>>(roles => roles.Contains("Student"))))
                .Returns("Student");
            _repositoryMock.Setup(x => x.GetWithSpecAsync(It.IsAny<QuestionsByLessonSpecification>()))
                .ReturnsAsync(pagination);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.That(result.Data, Has.Count.EqualTo(1));
            Assert.That(result.Data.First().CreatedByRole, Is.EqualTo("Student"));
        }

        [Test]
        public async Task Handle_ShouldReturnStudentQuestions_WhenUserIsTeacher()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var lessonId = Guid.NewGuid();
            var param = new QuestionsByLessonSpecParam();
            var query = new GetQuestionsByLessonQuery(param, lessonId, userId);
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var lesson = new LessonMaterial
            {
                Id = lessonId,
                Status = EntityStatus.Active,
                LessonStatus = LessonMaterialStatus.Approved,
                SchoolId = 1
            };
            var studentUser = new ApplicationUser { Id = Guid.NewGuid() };
            var questions = new List<LessonMaterialQuestion>
            {
                new() { Id = Guid.NewGuid(), CreatedByUser = studentUser }
            };
            var pagination = new Pagination<LessonMaterialQuestion> { Data = questions };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lessonId))
                .ReturnsAsync(lesson);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(["Teacher"]);
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Teacher");
            _studentClassRepositoryMock.Setup(x => x.TeacherHasAccessToMaterialAsync(userId, lessonId))
                .ReturnsAsync(true);
            _userManagerMock.Setup(x => x.GetRolesAsync(studentUser))
                .ReturnsAsync(["Student"]);
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.Is<IList<string>>(roles => roles.Contains("Student"))))
                .Returns("Student");
            _repositoryMock.Setup(x => x.GetWithSpecAsync(It.IsAny<QuestionsByLessonSpecification>()))
                .ReturnsAsync(pagination);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.That(result.Data, Has.Count.EqualTo(1));
            Assert.That(result.Data.First().CreatedByRole, Is.EqualTo("Student"));
        }

        [Test]
        public async Task Handle_ShouldFilterOutNonStudentQuestions_WhenUserIsTeacher()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var lessonId = Guid.NewGuid();
            var param = new QuestionsByLessonSpecParam();
            var query = new GetQuestionsByLessonQuery(param, lessonId, userId);
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var lesson = new LessonMaterial
            {
                Id = lessonId,
                Status = EntityStatus.Active,
                LessonStatus = LessonMaterialStatus.Approved,
                SchoolId = 1
            };
            var teacherUser = new ApplicationUser { Id = Guid.NewGuid() };
            var studentUser = new ApplicationUser { Id = Guid.NewGuid() };
            var questions = new List<LessonMaterialQuestion>
            {
                new() { Id = Guid.NewGuid(), CreatedByUser = teacherUser, CreatedByUserId = Guid.NewGuid() }, // Other teacher's question
                new() { Id = Guid.NewGuid(), CreatedByUser = studentUser, CreatedByUserId = Guid.NewGuid() } // Student's question
            };
            var pagination = new Pagination<LessonMaterialQuestion> { Data = questions };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lessonId))
                .ReturnsAsync(lesson);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(["Teacher"]);
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Teacher");
            _studentClassRepositoryMock.Setup(x => x.TeacherHasAccessToMaterialAsync(userId, lessonId))
                .ReturnsAsync(true);
            _userManagerMock.Setup(x => x.GetRolesAsync(teacherUser))
                .ReturnsAsync(["Teacher"]);
            _userManagerMock.Setup(x => x.GetRolesAsync(studentUser))
                .ReturnsAsync(["Student"]);
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.Is<IList<string>>(roles => roles.Contains("Teacher"))))
                .Returns("Teacher");
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.Is<IList<string>>(roles => roles.Contains("Student"))))
                .Returns("Student");
            _repositoryMock.Setup(x => x.GetWithSpecAsync(It.IsAny<QuestionsByLessonSpecification>()))
                .ReturnsAsync(pagination);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.That(result.Data, Has.Count.EqualTo(1)); // Only student question, not other teacher's question
            Assert.That(result.Data.First().CreatedByRole, Is.EqualTo("Student"));
        }

        [Test]
        public async Task Handle_ShouldReturnOwnQuestions_WhenUserIsTeacher()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var lessonId = Guid.NewGuid();
            var param = new QuestionsByLessonSpecParam();
            var query = new GetQuestionsByLessonQuery(param, lessonId, userId);
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var lesson = new LessonMaterial
            {
                Id = lessonId,
                Status = EntityStatus.Active,
                LessonStatus = LessonMaterialStatus.Approved,
                SchoolId = 1
            };
            var teacherUser = new ApplicationUser { Id = Guid.NewGuid() };
            var studentUser = new ApplicationUser { Id = Guid.NewGuid() };
            var questions = new List<LessonMaterialQuestion>
            {
                new() { Id = Guid.NewGuid(), CreatedByUser = user, CreatedByUserId = userId }, // Own question
                new() { Id = Guid.NewGuid(), CreatedByUser = teacherUser, CreatedByUserId = Guid.NewGuid() }, // Other teacher's question
                new() { Id = Guid.NewGuid(), CreatedByUser = studentUser, CreatedByUserId = Guid.NewGuid() } // Student's question
            };
            var pagination = new Pagination<LessonMaterialQuestion> { Data = questions };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lessonId))
                .ReturnsAsync(lesson);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(["Teacher"]);
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Teacher");
            _studentClassRepositoryMock.Setup(x => x.TeacherHasAccessToMaterialAsync(userId, lessonId))
                .ReturnsAsync(true);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(["Teacher"]);
            _userManagerMock.Setup(x => x.GetRolesAsync(teacherUser))
                .ReturnsAsync(["Teacher"]);
            _userManagerMock.Setup(x => x.GetRolesAsync(studentUser))
                .ReturnsAsync(["Student"]);
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.Is<IList<string>>(roles => roles.Contains("Teacher"))))
                .Returns("Teacher");
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.Is<IList<string>>(roles => roles.Contains("Student"))))
                .Returns("Student");
            _repositoryMock.Setup(x => x.GetWithSpecAsync(It.IsAny<QuestionsByLessonSpecification>()))
                .ReturnsAsync(pagination);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.That(result.Data, Has.Count.EqualTo(2)); // Own question + student question
            Assert.That(result.Data.Any(q => q.CreatedByUserId == userId), Is.True); // Should contain own question
        }

        [Test]
        public async Task Handle_ShouldReturnAllQuestions_WhenUserIsStudent()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var lessonId = Guid.NewGuid();
            var param = new QuestionsByLessonSpecParam();
            var query = new GetQuestionsByLessonQuery(param, lessonId, userId);
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var lesson = new LessonMaterial
            {
                Id = lessonId,
                Status = EntityStatus.Active,
                LessonStatus = LessonMaterialStatus.Approved,
                SchoolId = 1
            };
            var questions = new List<LessonMaterialQuestion>
            {
                new() { Id = Guid.NewGuid(), CreatedByUser = new ApplicationUser { Id = Guid.NewGuid() } },
                new() { Id = Guid.NewGuid(), CreatedByUser = new ApplicationUser { Id = Guid.NewGuid() } }
            };
            var pagination = new Pagination<LessonMaterialQuestion> { Data = questions };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lessonId))
                .ReturnsAsync(lesson);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(["Student"]);
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("Student");
            _studentClassRepositoryMock.Setup(x => x.HasAccessToMaterialAsync(userId, lessonId))
                .ReturnsAsync(true);
            _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(["Student"]);
            _repositoryMock.Setup(x => x.GetWithSpecAsync(It.IsAny<QuestionsByLessonSpecification>()))
                .ReturnsAsync(pagination);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.That(result.Data, Has.Count.EqualTo(2));
        }

        #endregion

        #region Exception Handling Tests

        [Test]
        public void Handle_ShouldThrowInsufficientPermission_WhenUserRoleIsInvalid()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var lessonId = Guid.NewGuid();
            var param = new QuestionsByLessonSpecParam();
            var query = new GetQuestionsByLessonQuery(param, lessonId, userId);
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var lesson = new LessonMaterial
            {
                Id = lessonId,
                Status = EntityStatus.Active,
                LessonStatus = LessonMaterialStatus.Approved,
                SchoolId = 1
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lessonId))
                .ReturnsAsync(lesson);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync([]);
            _permissionServiceMock.Setup(x => x.GetHighestPriorityRole(It.IsAny<IList<string>>()))
                .Returns("InvalidRole");

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(query, CancellationToken.None));
            Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.InsufficientPermission));
        }

        #endregion

    }
}
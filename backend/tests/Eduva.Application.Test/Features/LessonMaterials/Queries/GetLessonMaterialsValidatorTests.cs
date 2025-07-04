using Eduva.Application.Features.LessonMaterials.Queries;
using Eduva.Application.Features.LessonMaterials.Specifications;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Eduva.Application.Test.Features.LessonMaterials.Queries
{
    [TestFixture]
    public class GetLessonMaterialsValidatorTests
    {
        private Mock<IUnitOfWork> _mockUnitOfWork = default!;
        private Mock<UserManager<ApplicationUser>> _mockUserManager = default!;
        private Mock<IGenericRepository<School, int>> _mockSchoolRepo = default!;
        private Mock<IGenericRepository<Classroom, Guid>> _mockClassRepo = default!;
        private Mock<IGenericRepository<StudentClass, Guid>> _mockStudentClassRepo = default!;
        private Mock<IGenericRepository<Folder, Guid>> _mockFolderRepo = default!;
        private GetLessonMaterialsValidator _validator = default!;

        [SetUp]
        public void Setup()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockUserManager = MockUserManager();
            _mockSchoolRepo = new Mock<IGenericRepository<School, int>>();
            _mockClassRepo = new Mock<IGenericRepository<Classroom, Guid>>();
            _mockStudentClassRepo = new Mock<IGenericRepository<StudentClass, Guid>>();
            _mockFolderRepo = new Mock<IGenericRepository<Folder, Guid>>();

            // Setup repository returns
            _mockUnitOfWork.Setup(x => x.GetRepository<School, int>()).Returns(_mockSchoolRepo.Object);
            _mockUnitOfWork.Setup(x => x.GetRepository<Classroom, Guid>()).Returns(_mockClassRepo.Object);
            _mockUnitOfWork.Setup(x => x.GetRepository<StudentClass, Guid>()).Returns(_mockStudentClassRepo.Object);
            _mockUnitOfWork.Setup(x => x.GetRepository<Folder, Guid>()).Returns(_mockFolderRepo.Object);

            _validator = new GetLessonMaterialsValidator(_mockUnitOfWork.Object, _mockUserManager.Object);
        }

        [Test]
        public async Task Should_Have_Error_When_SearchTerm_Too_Long()
        {
            // Arrange
            var query = new GetLessonMaterialsQuery(
                new LessonMaterialSpecParam { SearchTerm = new string('a', 256) },
                Guid.NewGuid(),
                null);

            // Act & Assert
            var result = await _validator.TestValidateAsync(query);
            result.ShouldHaveValidationErrorFor(x => x.LessonMaterialSpecParam.SearchTerm);
        }

        [Test]
        public async Task Should_Have_Error_When_Tag_Too_Long()
        {
            // Arrange
            var query = new GetLessonMaterialsQuery(
                new LessonMaterialSpecParam { Tag = new string('a', 101) },
                Guid.NewGuid(),
                null);

            // Act & Assert
            var result = await _validator.TestValidateAsync(query);
            result.ShouldHaveValidationErrorFor(x => x.LessonMaterialSpecParam.Tag);
        }

        [Test]
        public async Task Should_Pass_When_SystemAdmin_Access_Any_Content()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new ApplicationUser { Id = userId };
            var roles = new List<string> { nameof(Role.SystemAdmin) };

            SetupUserManagerMocks(user, roles);

            // Setup existence checks for the specified IDs
            _mockSchoolRepo.Setup(x => x.ExistsAsync(1)).ReturnsAsync(true);
            _mockClassRepo.Setup(x => x.ExistsAsync(It.IsAny<Guid>())).ReturnsAsync(true);
            _mockFolderRepo.Setup(x => x.ExistsAsync(It.IsAny<Guid>())).ReturnsAsync(true);

            var query = new GetLessonMaterialsQuery(
                new LessonMaterialSpecParam
                {
                    ClassId = Guid.NewGuid(),
                    FolderId = Guid.NewGuid()
                },
                userId,
                1);

            // Act & Assert
            var result = await _validator.TestValidateAsync(query);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public async Task Should_Have_Error_When_SchoolAdmin_Access_Different_School()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var roles = new List<string> { nameof(Role.SchoolAdmin) };

            SetupUserManagerMocks(user, roles);

            var query = new GetLessonMaterialsQuery(
                new LessonMaterialSpecParam(),
                userId,
                2); // Different school

            // Act & Assert
            var result = await _validator.TestValidateAsync(query);
            result.ShouldHaveValidationErrorFor(x => x);
        }

        [Test]
        public async Task Should_Pass_When_SchoolAdmin_Access_Own_School()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var roles = new List<string> { nameof(Role.SchoolAdmin) };

            SetupUserManagerMocks(user, roles);
            _mockSchoolRepo.Setup(x => x.ExistsAsync(1)).ReturnsAsync(true);

            var query = new GetLessonMaterialsQuery(
                new LessonMaterialSpecParam(),
                userId,
                1);

            // Act & Assert
            var result = await _validator.TestValidateAsync(query);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public async Task Should_Have_Error_When_Student_Access_Class_Not_Enrolled()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var roles = new List<string> { nameof(Role.Student) };
            var classroom = new Classroom { Id = classId, SchoolId = 1, TeacherId = Guid.NewGuid() };

            SetupUserManagerMocks(user, roles);
            _mockClassRepo.Setup(x => x.ExistsAsync(classId)).ReturnsAsync(true);
            _mockClassRepo.Setup(x => x.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _mockStudentClassRepo.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<StudentClass, bool>>>(),
                It.IsAny<Func<IQueryable<StudentClass>, IQueryable<StudentClass>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((StudentClass?)null); // Not enrolled

            var query = new GetLessonMaterialsQuery(
                new LessonMaterialSpecParam { ClassId = classId },
                userId,
                1);

            // Act & Assert
            var result = await _validator.TestValidateAsync(query);
            result.ShouldHaveValidationErrorFor(x => x);
        }

        [Test]
        public async Task Should_Pass_When_Teacher_Access_Own_Class()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var roles = new List<string> { nameof(Role.Teacher) };
            var classroom = new Classroom { Id = classId, SchoolId = 1, TeacherId = userId };

            SetupUserManagerMocks(user, roles);
            _mockSchoolRepo.Setup(x => x.ExistsAsync(1)).ReturnsAsync(true);
            _mockClassRepo.Setup(x => x.ExistsAsync(classId)).ReturnsAsync(true);
            _mockClassRepo.Setup(x => x.GetByIdAsync(classId)).ReturnsAsync(classroom);

            var query = new GetLessonMaterialsQuery(
                new LessonMaterialSpecParam { ClassId = classId },
                userId,
                1);

            // Act & Assert
            var result = await _validator.TestValidateAsync(query);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public async Task Should_Have_Error_When_Teacher_Access_Other_Class()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var roles = new List<string> { nameof(Role.Teacher) };
            var classroom = new Classroom { Id = classId, SchoolId = 1, TeacherId = Guid.NewGuid() }; // Different teacher

            SetupUserManagerMocks(user, roles);
            _mockSchoolRepo.Setup(x => x.ExistsAsync(1)).ReturnsAsync(true);
            _mockClassRepo.Setup(x => x.ExistsAsync(classId)).ReturnsAsync(true);
            _mockClassRepo.Setup(x => x.GetByIdAsync(classId)).ReturnsAsync(classroom);

            var query = new GetLessonMaterialsQuery(
                new LessonMaterialSpecParam { ClassId = classId },
                userId,
                1);

            // Act & Assert
            var result = await _validator.TestValidateAsync(query);
            result.ShouldHaveValidationErrorFor(x => x);
        }

        #region Setup Methods

        private void SetupUserManagerMocks(ApplicationUser user, IList<string> roles)
        {
            _mockUserManager.Setup(x => x.FindByIdAsync(user.Id.ToString()))
                .ReturnsAsync(user);
            _mockUserManager.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(roles);
        }

        private static Mock<UserManager<ApplicationUser>> MockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(
                store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        }

        #endregion
    }
}

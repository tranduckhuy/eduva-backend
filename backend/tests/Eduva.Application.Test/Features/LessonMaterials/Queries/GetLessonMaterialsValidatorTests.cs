using Eduva.Application.Features.LessonMaterials.Queries;
using Eduva.Application.Features.LessonMaterials.Specifications;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Identity;
using Moq;
using NUnit.Framework;

namespace Eduva.Application.Test.Features.LessonMaterials.Queries
{
    [TestFixture]
    public class GetLessonMaterialsValidatorTests
    {
        private Mock<IUnitOfWork> _mockUnitOfWork = default!;
        private Mock<UserManager<ApplicationUser>> _mockUserManager = default!;
        private GetLessonMaterialsValidator _validator = default!;

        [SetUp]
        public void Setup()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockUserManager = MockUserManager();
            _validator = new GetLessonMaterialsValidator(_mockUnitOfWork.Object, _mockUserManager.Object);
        }

        [Test]
        public async Task Should_Have_Error_When_SearchTerm_Too_Long()
        {
            // Arrange
            var query = new GetLessonMaterialsQuery(
                new LessonMaterialSpecParam { SearchTerm = new string('a', 256) },
                Guid.NewGuid());

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
                Guid.NewGuid());

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

            var query = new GetLessonMaterialsQuery(
                new LessonMaterialSpecParam 
                { 
                    SchoolId = 1,
                    ClassId = Guid.NewGuid(),
                    FolderId = Guid.NewGuid()
                },
                userId);

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
                new LessonMaterialSpecParam { SchoolId = 2 }, // Different school
                userId);

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
            SetupSchoolExists(1, true);

            var query = new GetLessonMaterialsQuery(
                new LessonMaterialSpecParam { SchoolId = 1 },
                userId);

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
            SetupClassExists(classId, true);
            SetupGetClassById(classId, classroom);
            SetupStudentEnrollment(userId, classId, false); // Not enrolled

            var query = new GetLessonMaterialsQuery(
                new LessonMaterialSpecParam { ClassId = classId },
                userId);

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
            SetupClassExists(classId, true);
            SetupGetClassById(classId, classroom);

            var query = new GetLessonMaterialsQuery(
                new LessonMaterialSpecParam { ClassId = classId },
                userId);

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
            SetupClassExists(classId, true);
            SetupGetClassById(classId, classroom);

            var query = new GetLessonMaterialsQuery(
                new LessonMaterialSpecParam { ClassId = classId },
                userId);

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

        private void SetupSchoolExists(int schoolId, bool exists)
        {
            var mockSchoolRepo = new Mock<IGenericRepository<School, int>>();
            mockSchoolRepo.Setup(x => x.ExistsAsync(schoolId))
                .ReturnsAsync(exists);
            _mockUnitOfWork.Setup(x => x.GetRepository<School, int>())
                .Returns(mockSchoolRepo.Object);
        }

        private void SetupClassExists(Guid classId, bool exists)
        {
            var mockClassRepo = new Mock<IGenericRepository<Classroom, Guid>>();
            mockClassRepo.Setup(x => x.ExistsAsync(classId))
                .ReturnsAsync(exists);
            _mockUnitOfWork.Setup(x => x.GetRepository<Classroom, Guid>())
                .Returns(mockClassRepo.Object);
        }

        private void SetupGetClassById(Guid classId, Classroom classroom)
        {
            var mockClassRepo = new Mock<IGenericRepository<Classroom, Guid>>();
            mockClassRepo.Setup(x => x.GetByIdAsync(classId))
                .ReturnsAsync(classroom);
            _mockUnitOfWork.Setup(x => x.GetRepository<Classroom, Guid>())
                .Returns(mockClassRepo.Object);
        }

        private void SetupStudentEnrollment(Guid studentId, Guid classId, bool isEnrolled)
        {
            var mockStudentClassRepo = new Mock<IGenericRepository<StudentClass, Guid>>();
            var enrollment = isEnrolled ? new StudentClass { StudentId = studentId, ClassId = classId } : null;
            
            mockStudentClassRepo.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<StudentClass, bool>>>(),
                It.IsAny<Func<IQueryable<StudentClass>, IQueryable<StudentClass>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(enrollment);
            
            _mockUnitOfWork.Setup(x => x.GetRepository<StudentClass, Guid>())
                .Returns(mockStudentClassRepo.Object);
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

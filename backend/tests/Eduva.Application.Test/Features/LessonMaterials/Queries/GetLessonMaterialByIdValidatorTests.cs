using Eduva.Application.Features.LessonMaterials.Queries;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Eduva.Application.Test.Features.LessonMaterials.Queries
{
    [TestFixture]
    public class GetLessonMaterialByIdValidatorTests
    {
        private Mock<IUnitOfWork> _mockUnitOfWork;
        private Mock<UserManager<ApplicationUser>> _mockUserManager;
        private Mock<ISchoolRepository> _mockSchoolRepository;
        private GetLessonMaterialByIdValidator _validator;

        [SetUp]
        public void Setup()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockSchoolRepository = new Mock<ISchoolRepository>();
            
            // Mock UserManager
            var store = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(store.Object, 
                It.IsAny<IOptions<IdentityOptions>>(), 
                It.IsAny<IPasswordHasher<ApplicationUser>>(), 
                It.IsAny<IEnumerable<IUserValidator<ApplicationUser>>>(), 
                It.IsAny<IEnumerable<IPasswordValidator<ApplicationUser>>>(), 
                It.IsAny<ILookupNormalizer>(), 
                It.IsAny<IdentityErrorDescriber>(), 
                It.IsAny<IServiceProvider>(), 
                It.IsAny<ILogger<UserManager<ApplicationUser>>>());

            _mockUnitOfWork.Setup(x => x.GetCustomRepository<ISchoolRepository>())
                .Returns(_mockSchoolRepository.Object);

            _validator = new GetLessonMaterialByIdValidator(_mockUnitOfWork.Object, _mockUserManager.Object);
        }

        #region Id Validation Tests

        [Test]
        public async Task Should_Have_Error_When_Id_Is_Empty()
        {
            // Arrange
            var query = new GetLessonMaterialByIdQuery
            {
                Id = Guid.Empty,
                UserId = Guid.NewGuid(),
                SchoolId = 1
            };

            // Act & Assert
            var result = await _validator.TestValidateAsync(query);
            result.ShouldHaveValidationErrorFor(x => x.Id)
                .WithErrorMessage("Lesson material ID must not be an empty GUID.");
        }

        [Test]
        public async Task Should_Not_Have_Error_When_Id_Is_Valid()
        {
            // Arrange
            var validId = Guid.NewGuid();
            var query = new GetLessonMaterialByIdQuery
            {
                Id = validId,
                UserId = Guid.NewGuid(),
                SchoolId = null
            };

            // Act & Assert
            var result = await _validator.TestValidateAsync(query);
            result.ShouldNotHaveValidationErrorFor(x => x.Id);
        }

        #endregion

        #region SchoolId Validation Tests

        [Test]
        public async Task Should_Not_Have_Error_When_SchoolId_Is_Null()
        {
            // Arrange
            var query = new GetLessonMaterialByIdQuery
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                SchoolId = null
            };

            // Act & Assert
            var result = await _validator.TestValidateAsync(query);
            result.ShouldNotHaveValidationErrorFor(x => x.SchoolId);
        }

        [Test]
        public async Task Should_Have_Error_When_SchoolId_Does_Not_Exist()
        {
            // Arrange
            var schoolId = 999;
            _mockSchoolRepository.Setup(x => x.GetByIdAsync(schoolId))
                .ReturnsAsync((School?)null);

            var query = new GetLessonMaterialByIdQuery
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                SchoolId = schoolId
            };

            // Act & Assert
            var result = await _validator.TestValidateAsync(query);
            result.ShouldHaveValidationErrorFor(x => x.SchoolId)
                .WithErrorMessage("The specified school does not exist.");
        }

        [Test]
        public async Task Should_Not_Have_Error_When_SchoolId_Exists()
        {
            // Arrange
            var schoolId = 1;
            var school = new School { Id = schoolId, Name = "Test School" };
            
            _mockSchoolRepository.Setup(x => x.GetByIdAsync(schoolId))
                .ReturnsAsync(school);

            var query = new GetLessonMaterialByIdQuery
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                SchoolId = schoolId
            };

            // Act & Assert
            var result = await _validator.TestValidateAsync(query);
            result.ShouldNotHaveValidationErrorFor(x => x.SchoolId);
        }

        [Test]
        public async Task Should_Not_Have_Error_When_SchoolId_Is_Zero()
        {
            // Arrange - SchoolId = 0 should be treated as null
            var query = new GetLessonMaterialByIdQuery
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                SchoolId = 0
            };

            _mockSchoolRepository.Setup(x => x.GetByIdAsync(0))
                .ReturnsAsync((School?)null);

            // Act & Assert
            var result = await _validator.TestValidateAsync(query);
            result.ShouldHaveValidationErrorFor(x => x.SchoolId)
                .WithErrorMessage("The specified school does not exist.");
        }

        #endregion

        #region Complete Validation Tests

        [Test]
        public async Task Should_Not_Have_Any_Error_When_All_Fields_Are_Valid()
        {
            // Arrange
            var schoolId = 1;
            var school = new School { Id = schoolId, Name = "Test School" };
            
            _mockSchoolRepository.Setup(x => x.GetByIdAsync(schoolId))
                .ReturnsAsync(school);

            var query = new GetLessonMaterialByIdQuery
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                SchoolId = schoolId
            };

            // Act & Assert
            var result = await _validator.TestValidateAsync(query);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public async Task Should_Have_Multiple_Errors_When_Multiple_Fields_Are_Invalid()
        {
            // Arrange
            var schoolId = 999;
            _mockSchoolRepository.Setup(x => x.GetByIdAsync(schoolId))
                .ReturnsAsync((School?)null);

            var query = new GetLessonMaterialByIdQuery
            {
                Id = Guid.Empty, // Invalid
                UserId = Guid.NewGuid(),
                SchoolId = schoolId // Invalid - doesn't exist
            };

            // Act & Assert
            var result = await _validator.TestValidateAsync(query);
            result.ShouldHaveValidationErrorFor(x => x.Id);
            result.ShouldHaveValidationErrorFor(x => x.SchoolId);
        }

        [Test]
        public async Task Should_Call_School_Repository_When_SchoolId_Has_Value()
        {
            // Arrange
            var schoolId = 1;
            var school = new School { Id = schoolId, Name = "Test School" };
            
            _mockSchoolRepository.Setup(x => x.GetByIdAsync(schoolId))
                .ReturnsAsync(school);

            var query = new GetLessonMaterialByIdQuery
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                SchoolId = schoolId
            };

            // Act
            await _validator.TestValidateAsync(query);

            // Assert
            _mockSchoolRepository.Verify(x => x.GetByIdAsync(schoolId), Times.Once);
        }

        [Test]
        public async Task Should_Not_Call_School_Repository_When_SchoolId_Is_Null()
        {
            // Arrange
            var query = new GetLessonMaterialByIdQuery
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                SchoolId = null
            };

            // Act
            await _validator.TestValidateAsync(query);

            // Assert
            _mockSchoolRepository.Verify(x => x.GetByIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Test]
        public void Should_Handle_Exception_From_Repository()
        {
            // Arrange
            var schoolId = 1;
            _mockSchoolRepository.Setup(x => x.GetByIdAsync(schoolId))
                .ThrowsAsync(new Exception("Database error"));

            var query = new GetLessonMaterialByIdQuery
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                SchoolId = schoolId
            };

            // Act & Assert
            Assert.ThrowsAsync<Exception>(async () => await _validator.TestValidateAsync(query));
        }

        #endregion
    }
}

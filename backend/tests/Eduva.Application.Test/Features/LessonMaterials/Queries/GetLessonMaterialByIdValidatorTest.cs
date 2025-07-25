using Eduva.Application.Features.LessonMaterials.Queries.GetLessonMaterialById;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Eduva.Application.Test.Features.LessonMaterials.Queries
{
    [TestFixture]
    public class GetLessonMaterialByIdValidatorTest
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<ISchoolRepository> _schoolRepoMock = null!;
        private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;
        private GetLessonMaterialByIdValidator _validator = null!;

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _schoolRepoMock = new Mock<ISchoolRepository>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);

            _unitOfWorkMock.Setup(u => u.GetCustomRepository<ISchoolRepository>())
                .Returns(_schoolRepoMock.Object);

            _validator = new GetLessonMaterialByIdValidator(_unitOfWorkMock.Object, _userManagerMock.Object);
        }

        [Test]
        public void Should_Have_Error_When_Id_Is_Empty()
        {
            var query = new GetLessonMaterialByIdQuery { Id = Guid.Empty };
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor(q => q.Id);
        }

        [Test]
        public void Should_Have_Error_When_Id_Is_EmptyGuid()
        {
            var query = new GetLessonMaterialByIdQuery { Id = Guid.Empty, SchoolId = null };
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor(q => q.Id);
            result.ShouldNotHaveValidationErrorFor(q => q.SchoolId);
        }

        [Test]
        public async Task Should_Have_Error_When_SchoolId_Not_Exists()
        {
            var query = new GetLessonMaterialByIdQuery { Id = Guid.NewGuid(), SchoolId = 123 };
            _schoolRepoMock.Setup(r => r.GetByIdAsync(123)).ReturnsAsync((School?)null);

            var result = await _validator.TestValidateAsync(query);
            result.ShouldHaveValidationErrorFor(q => q.SchoolId);
        }

        [Test]
        public async Task Should_Not_Have_Error_When_SchoolId_Exists()
        {
            var query = new GetLessonMaterialByIdQuery { Id = Guid.NewGuid(), SchoolId = 123 };
            _schoolRepoMock.Setup(r => r.GetByIdAsync(123)).ReturnsAsync(new School { Id = 123 });

            var result = await _validator.TestValidateAsync(query);
            result.ShouldNotHaveValidationErrorFor(q => q.SchoolId);
        }

        [Test]
        public async Task Should_Not_Have_Error_When_SchoolId_Is_Null()
        {
            var query = new GetLessonMaterialByIdQuery { Id = Guid.NewGuid(), SchoolId = null };
            var result = await _validator.TestValidateAsync(query);
            result.ShouldNotHaveValidationErrorFor(q => q.SchoolId);
        }

        [Test]
        public async Task Should_Not_Have_Error_For_Valid_Id_And_SchoolId()
        {
            var query = new GetLessonMaterialByIdQuery { Id = Guid.NewGuid(), SchoolId = 123 };
            _schoolRepoMock.Setup(r => r.GetByIdAsync(123)).ReturnsAsync(new School { Id = 123 });

            var result = await _validator.TestValidateAsync(query);
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
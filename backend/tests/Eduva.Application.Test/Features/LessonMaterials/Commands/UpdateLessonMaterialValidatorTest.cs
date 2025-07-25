using Eduva.Application.Features.LessonMaterials.Commands.UpdateLessonMaterial;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using FluentValidation.TestHelper;
using Moq;

namespace Eduva.Application.Test.Features.LessonMaterials.Commands
{
    [TestFixture]
    public class UpdateLessonMaterialValidatorTest
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<IGenericRepository<LessonMaterial, Guid>> _lessonMaterialRepoMock = null!;
        private UpdateLessonMaterialValidator _validator = null!;

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _lessonMaterialRepoMock = new Mock<IGenericRepository<LessonMaterial, Guid>>();
            _unitOfWorkMock.Setup(u => u.GetRepository<LessonMaterial, Guid>())
                .Returns(_lessonMaterialRepoMock.Object);

            _validator = new UpdateLessonMaterialValidator(_unitOfWorkMock.Object);
        }

        [Test]
        public async Task Should_Have_Error_When_Id_Is_Empty()
        {
            var command = new UpdateLessonMaterialCommand { Id = Guid.Empty };
            var result = await _validator.TestValidateAsync(command);
            result.ShouldHaveValidationErrorFor(c => c.Id);
        }

        [Test]
        public async Task Should_Have_Error_When_LessonMaterial_Not_Exists()
        {
            var id = Guid.NewGuid();
            _lessonMaterialRepoMock.Setup(r => r.ExistsAsync(id)).ReturnsAsync(false);

            var command = new UpdateLessonMaterialCommand { Id = id };
            var result = await _validator.TestValidateAsync(command);
            result.ShouldHaveValidationErrorFor(c => c.Id);
        }

        [Test]
        public async Task Should_Not_Have_Error_When_LessonMaterial_Exists()
        {
            var id = Guid.NewGuid();
            _lessonMaterialRepoMock.Setup(r => r.ExistsAsync(id)).ReturnsAsync(true);

            var command = new UpdateLessonMaterialCommand { Id = id };
            var result = await _validator.TestValidateAsync(command);
            result.ShouldNotHaveValidationErrorFor(c => c.Id);
        }

        [Test]
        public async Task Should_Have_Error_When_Title_Too_Long()
        {
            var id = Guid.NewGuid();
            _lessonMaterialRepoMock.Setup(r => r.ExistsAsync(id)).ReturnsAsync(true);

            var command = new UpdateLessonMaterialCommand
            {
                Id = id,
                Title = new string('a', 256)
            };
            var result = await _validator.TestValidateAsync(command);
            result.ShouldHaveValidationErrorFor(c => c.Title);
        }

        [Test]
        public async Task Should_Not_Have_Error_When_Title_Is_Valid()
        {
            var id = Guid.NewGuid();
            _lessonMaterialRepoMock.Setup(r => r.ExistsAsync(id)).ReturnsAsync(true);

            var command = new UpdateLessonMaterialCommand
            {
                Id = id,
                Title = "Valid Title"
            };
            var result = await _validator.TestValidateAsync(command);
            result.ShouldNotHaveValidationErrorFor(c => c.Title);
        }

        [Test]
        public async Task Should_Have_Error_When_Duration_Is_Negative()
        {
            var id = Guid.NewGuid();
            _lessonMaterialRepoMock.Setup(r => r.ExistsAsync(id)).ReturnsAsync(true);

            var command = new UpdateLessonMaterialCommand
            {
                Id = id,
                Duration = -1
            };
            var result = await _validator.TestValidateAsync(command);
            result.ShouldHaveValidationErrorFor(c => c.Duration);
        }

        [Test]
        public async Task Should_Not_Have_Error_When_Duration_Is_Zero_Or_Positive()
        {
            var id = Guid.NewGuid();
            _lessonMaterialRepoMock.Setup(r => r.ExistsAsync(id)).ReturnsAsync(true);

            var command = new UpdateLessonMaterialCommand
            {
                Id = id,
                Duration = 0
            };
            var result = await _validator.TestValidateAsync(command);
            result.ShouldNotHaveValidationErrorFor(c => c.Duration);

            command.Duration = 10;
            result = await _validator.TestValidateAsync(command);
            result.ShouldNotHaveValidationErrorFor(c => c.Duration);
        }

        [Test]
        public async Task Should_Have_Error_When_Visibility_Is_Invalid()
        {
            var id = Guid.NewGuid();
            _lessonMaterialRepoMock.Setup(r => r.ExistsAsync(id)).ReturnsAsync(true);

            var command = new UpdateLessonMaterialCommand
            {
                Id = id,
                Visibility = (LessonMaterialVisibility)999
            };
            var result = await _validator.TestValidateAsync(command);
            result.ShouldHaveValidationErrorFor(c => c.Visibility);
        }

        [Test]
        public async Task Should_Not_Have_Error_When_Visibility_Is_Valid()
        {
            var id = Guid.NewGuid();
            _lessonMaterialRepoMock.Setup(r => r.ExistsAsync(id)).ReturnsAsync(true);

            var command = new UpdateLessonMaterialCommand
            {
                Id = id,
                Visibility = LessonMaterialVisibility.Private
            };
            var result = await _validator.TestValidateAsync(command);
            result.ShouldNotHaveValidationErrorFor(c => c.Visibility);

            command.Visibility = LessonMaterialVisibility.School;
            result = await _validator.TestValidateAsync(command);
            result.ShouldNotHaveValidationErrorFor(c => c.Visibility);
        }

        [Test]
        public async Task Should_Not_Have_Error_When_Title_Is_Null_Or_Empty()
        {
            var id = Guid.NewGuid();
            _lessonMaterialRepoMock.Setup(r => r.ExistsAsync(id)).ReturnsAsync(true);

            var command = new UpdateLessonMaterialCommand { Id = id, Title = null };
            var result = await _validator.TestValidateAsync(command);
            result.ShouldNotHaveValidationErrorFor(c => c.Title);

            command.Title = "";
            result = await _validator.TestValidateAsync(command);
            result.ShouldNotHaveValidationErrorFor(c => c.Title);
        }

        [Test]
        public async Task Should_Not_Have_Error_When_Duration_Or_Visibility_Is_Null()
        {
            var id = Guid.NewGuid();
            _lessonMaterialRepoMock.Setup(r => r.ExistsAsync(id)).ReturnsAsync(true);

            var command = new UpdateLessonMaterialCommand { Id = id, Duration = null, Visibility = null };
            var result = await _validator.TestValidateAsync(command);
            result.ShouldNotHaveValidationErrorFor(c => c.Duration);
            result.ShouldNotHaveValidationErrorFor(c => c.Visibility);
        }
    }
}
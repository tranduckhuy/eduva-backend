using Eduva.Application.Features.Classes.Commands.CreateClass;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using FluentValidation.TestHelper;
using Moq;

namespace Eduva.Application.Test.Features.Classes.Commands.CreateClass
{
    [TestFixture]
    public class CreateClassValidatorTest
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<IClassroomRepository> _classroomRepoMock = null!;
        private Mock<IGenericRepository<School, int>> _schoolRepoMock = null!;
        private CreateClassValidator _validator = null!;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _classroomRepoMock = new Mock<IClassroomRepository>();
            _schoolRepoMock = new Mock<IGenericRepository<School, int>>();

            _unitOfWorkMock.Setup(u => u.GetCustomRepository<IClassroomRepository>())
                .Returns(_classroomRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<School, int>())
                .Returns(_schoolRepoMock.Object);

            _validator = new CreateClassValidator(_unitOfWorkMock.Object);
        }

        [Test]
        public async Task Should_Have_Error_When_Name_Is_Empty()
        {
            var command = new CreateClassCommand
            {
                Name = "",
                TeacherId = Guid.NewGuid(),
                SchoolId = 1
            };

            var result = await _validator.TestValidateAsync(command);
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("Class name is required");
        }

        [Test]
        public async Task Should_Have_Error_When_Name_Too_Long()
        {
            var command = new CreateClassCommand
            {
                Name = new string('a', 101),
                TeacherId = Guid.NewGuid(),
                SchoolId = 1
            };

            var result = await _validator.TestValidateAsync(command);
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("Class name must not exceed 100 characters");
        }

        [Test]
        public async Task Should_Have_Error_When_Name_Not_Unique_For_Teacher()
        {
            var teacherId = Guid.NewGuid();
            var command = new CreateClassCommand
            {
                Name = "Math",
                TeacherId = teacherId,
                SchoolId = 1
            };

            _classroomRepoMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Classroom, bool>>>()))
                .ReturnsAsync(true);

            var result = await _validator.TestValidateAsync(command);
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("You already have a class with this name");
        }

        [Test]
        public async Task Should_Have_Error_When_SchoolId_Is_Empty()
        {
            var command = new CreateClassCommand
            {
                Name = "Math",
                TeacherId = Guid.NewGuid(),
                SchoolId = 0
            };

            var result = await _validator.TestValidateAsync(command);
            result.ShouldHaveValidationErrorFor(x => x.SchoolId)
                .WithErrorMessage("School ID is required");
        }

        [Test]
        public async Task Should_Have_Error_When_School_Not_Exist()
        {
            var command = new CreateClassCommand
            {
                Name = "Math",
                TeacherId = Guid.NewGuid(),
                SchoolId = 123
            };

            _schoolRepoMock.Setup(r => r.ExistsAsync(command.SchoolId)).ReturnsAsync(false);

            var result = await _validator.TestValidateAsync(command);
            result.ShouldHaveValidationErrorFor(x => x.SchoolId)
                .WithErrorMessage("School with specified ID does not exist");
        }

        [Test]
        public async Task Should_Not_Have_Error_When_Valid()
        {
            var teacherId = Guid.NewGuid();
            var command = new CreateClassCommand
            {
                Name = "Math",
                TeacherId = teacherId,
                SchoolId = 123
            };

            _classroomRepoMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Classroom, bool>>>()))
                .ReturnsAsync(false);
            _schoolRepoMock.Setup(r => r.ExistsAsync(command.SchoolId)).ReturnsAsync(true);

            var result = await _validator.TestValidateAsync(command);
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
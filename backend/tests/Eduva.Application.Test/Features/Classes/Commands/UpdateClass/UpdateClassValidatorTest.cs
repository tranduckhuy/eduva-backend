using Eduva.Application.Features.Classes.Commands.UpdateClass;
using FluentValidation.TestHelper;

namespace Eduva.Application.Test.Features.Classes.Commands.UpdateClass
{
    [TestFixture]
    public class UpdateClassValidatorTest
    {
        private UpdateClassValidator _validator = null!;

        [SetUp]
        public void SetUp()
        {
            _validator = new UpdateClassValidator();
        }

        [Test]
        public void Should_Have_Error_When_Id_Is_Empty()
        {
            var command = new UpdateClassCommand { Id = Guid.Empty, Name = "Math" };
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.Id)
                .WithErrorMessage("Class ID is required.");
        }

        [Test]
        public void Should_Have_Error_When_Name_Is_Empty()
        {
            var command = new UpdateClassCommand { Id = Guid.NewGuid(), Name = "" };
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("Class name is required.");
        }

        [Test]
        public void Should_Have_Error_When_Name_Too_Long()
        {
            var command = new UpdateClassCommand { Id = Guid.NewGuid(), Name = new string('a', 101) };
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("Class name must not exceed 100 characters.");
        }

        [Test]
        public void Should_Not_Have_Error_When_Valid()
        {
            var command = new UpdateClassCommand { Id = Guid.NewGuid(), Name = "Math" };
            var result = _validator.TestValidate(command);
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
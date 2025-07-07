using Eduva.Application.Features.Classes.Commands.RestoreClass;
using FluentValidation.TestHelper;

namespace Eduva.Application.Test.Features.Classes.Commands.RestoreClass
{
    [TestFixture]
    public class RestoreClassValidatorTest
    {
        private RestoreClassValidator _validator = null!;

        [SetUp]
        public void SetUp()
        {
            _validator = new RestoreClassValidator();
        }

        [Test]
        public void Should_Have_Error_When_Id_Is_Empty()
        {
            var command = new RestoreClassCommand { Id = Guid.Empty };
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.Id)
                .WithErrorMessage("Class ID is required.");
        }

        [Test]
        public void Should_Not_Have_Error_When_Id_Is_Valid()
        {
            var command = new RestoreClassCommand { Id = Guid.NewGuid() };
            var result = _validator.TestValidate(command);
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
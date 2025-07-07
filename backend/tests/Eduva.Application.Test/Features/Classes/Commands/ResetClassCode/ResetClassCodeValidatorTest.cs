using Eduva.Application.Features.Classes.Commands.ResetClassCode;
using FluentValidation.TestHelper;

namespace Eduva.Application.Test.Features.Classes.Commands.ResetClassCode
{
    [TestFixture]
    public class ResetClassCodeValidatorTest
    {
        private ResetClassCodeValidator _validator = null!;

        [SetUp]
        public void SetUp()
        {
            _validator = new ResetClassCodeValidator();
        }

        [Test]
        public void Should_Have_Error_When_Id_Is_Empty()
        {
            var command = new ResetClassCodeCommand { Id = Guid.Empty };
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.Id)
                .WithErrorMessage("Class ID is required.");
        }

        [Test]
        public void Should_Not_Have_Error_When_Id_Is_Valid()
        {
            var command = new ResetClassCodeCommand { Id = Guid.NewGuid() };
            var result = _validator.TestValidate(command);
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
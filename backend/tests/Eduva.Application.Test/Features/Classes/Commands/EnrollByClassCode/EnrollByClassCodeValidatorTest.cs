using Eduva.Application.Features.Classes.Commands.EnrollByClassCode;
using FluentValidation.TestHelper;

namespace Eduva.Application.Test.Features.Classes.Commands.EnrollByClassCode
{
    [TestFixture]
    public class EnrollByClassCodeValidatorTest
    {
        private EnrollByClassCodeValidator _validator = null!;

        [SetUp]
        public void SetUp()
        {
            _validator = new EnrollByClassCodeValidator();
        }

        [Test]
        public void Should_Have_Error_When_ClassCode_Is_Empty()
        {
            var command = new EnrollByClassCodeCommand { ClassCode = "" };
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.ClassCode)
                .WithErrorMessage("Class code is required");
        }

        [Test]
        public void Should_Have_Error_When_ClassCode_Too_Long()
        {
            var command = new EnrollByClassCodeCommand { ClassCode = new string('A', 21) };
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.ClassCode)
                .WithErrorMessage("Class code cannot exceed 20 characters");
        }

        [Test]
        public void Should_Not_Have_Error_When_ClassCode_Valid()
        {
            var command = new EnrollByClassCodeCommand { ClassCode = "ABC123" };
            var result = _validator.TestValidate(command);
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
using Eduva.Application.Features.Classes.Commands.ArchiveClass;
using FluentValidation.TestHelper;

namespace Eduva.Application.Test.Features.Classes.Commands.ArchiveClass
{
    [TestFixture]
    public class ArchiveClassValidatorTest
    {
        private ArchiveClassValidator _validator = null!;

        [SetUp]
        public void SetUp()
        {
            _validator = new ArchiveClassValidator();
        }

        [Test]
        public void Should_Have_Error_When_Id_Is_Empty()
        {
            var command = new ArchiveClassCommand { Id = Guid.Empty };
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.Id)
                .WithErrorMessage("Class ID is required.");
        }

        [Test]
        public void Should_Not_Have_Error_When_Id_Is_Valid()
        {
            var command = new ArchiveClassCommand { Id = Guid.NewGuid() };
            var result = _validator.TestValidate(command);
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
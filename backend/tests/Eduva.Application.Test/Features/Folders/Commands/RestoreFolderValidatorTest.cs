using Eduva.Application.Features.Folders.Commands;
using FluentValidation.TestHelper;

namespace Eduva.Application.Test.Features.Folders.Commands
{
    [TestFixture]
    public class RestoreFolderValidatorTest
    {
        private RestoreFolderValidator _validator = null!;

        [SetUp]
        public void SetUp()
        {
            _validator = new RestoreFolderValidator();
        }

        [Test]
        public void Should_Have_Error_When_Id_Is_Empty()
        {
            var command = new RestoreFolderCommand
            {
                Id = Guid.Empty,
                CurrentUserId = Guid.NewGuid()
            };
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.Id)
                .WithErrorMessage("Folder Id is required");
        }

        [Test]
        public void Should_Have_Error_When_CurrentUserId_Is_Empty()
        {
            var command = new RestoreFolderCommand
            {
                Id = Guid.NewGuid(),
                CurrentUserId = Guid.Empty
            };
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.CurrentUserId)
                .WithErrorMessage("Current user Id is required");
        }

        [Test]
        public void Should_Not_Have_Error_When_Valid()
        {
            var command = new RestoreFolderCommand
            {
                Id = Guid.NewGuid(),
                CurrentUserId = Guid.NewGuid()
            };
            var result = _validator.TestValidate(command);
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
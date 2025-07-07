using Eduva.Application.Features.Folders.Commands;
using FluentValidation.TestHelper;

namespace Eduva.Application.Test.Features.Folders.Commands
{
    [TestFixture]
    public class ArchiveFolderValidatorTest
    {
        private ArchiveFolderValidator _validator = null!;

        [SetUp]
        public void SetUp()
        {
            _validator = new ArchiveFolderValidator();
        }

        [Test]
        public void Should_Have_Error_When_Id_Is_Empty()
        {
            var command = new ArchiveFolderCommand
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
            var command = new ArchiveFolderCommand
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
            var command = new ArchiveFolderCommand
            {
                Id = Guid.NewGuid(),
                CurrentUserId = Guid.NewGuid()
            };
            var result = _validator.TestValidate(command);
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
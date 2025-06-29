using Eduva.Application.Features.Folders.Commands;
using FluentValidation.TestHelper;

namespace Eduva.Application.Test.Features.Folders.Commands
{
    [TestFixture]
    public class RenameFolderValidatorTest
    {
        private RenameFolderValidator _validator = default!;

        #region RenameFolderValidator Setup
        [SetUp]
        public void Setup()
        {
            _validator = new RenameFolderValidator();
        }
        #endregion

        #region RenameFolderValidator Tests

        [Test]
        public void Should_Have_Error_When_Id_Is_Empty()
        {
            var model = new RenameFolderCommand { Id = Guid.Empty, Name = "Valid" };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Id);
        }

        [Test]
        public void Should_Have_Error_When_Name_Is_Empty()
        {
            var model = new RenameFolderCommand { Id = Guid.NewGuid(), Name = string.Empty };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Test]
        public void Should_Have_Error_When_Name_Too_Long()
        {
            var model = new RenameFolderCommand { Id = Guid.NewGuid(), Name = new string('a', 101) };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Test]
        public void Should_Not_Have_Error_When_Valid()
        {
            var model = new RenameFolderCommand { Id = Guid.NewGuid(), Name = "Valid Name" };
            var result = _validator.TestValidate(model);
            result.ShouldNotHaveValidationErrorFor(x => x.Id);
            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }

        #endregion
    }
}

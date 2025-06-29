using Eduva.Application.Features.Folders.Commands;
using FluentValidation.TestHelper;

namespace Eduva.Application.Test.Features.Folders.Commands
{
    [TestFixture]
    public class CreateFolderValidatorTest
    {
        private CreateFolderValidator _validator = default!;

        #region CreateFolderValidator Setup
        [SetUp]
        public void Setup()
        {
            _validator = new CreateFolderValidator();
        }
        #endregion

        #region CreateFolderValidator Tests

        [Test]
        public void Constructor_ShouldInitialize()
        {
            var validator = new CreateFolderValidator();
            Assert.That(validator, Is.Not.Null);
        }

        [Test]
        public void Should_Have_Error_When_Name_Is_Empty()
        {
            var model = new CreateFolderCommand { Name = string.Empty };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Test]
        public void Should_Have_Error_When_Name_Too_Long()
        {
            var model = new CreateFolderCommand { Name = new string('a', 101) };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Test]
        public void Should_Not_Have_Error_When_Name_Is_Valid()
        {
            var model = new CreateFolderCommand { Name = "Valid Name" };
            var result = _validator.TestValidate(model);
            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }

        #endregion
    }
}

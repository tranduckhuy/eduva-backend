using Eduva.Application.Features.Classes.Queries.GetClasses;
using Eduva.Application.Features.Classes.Specifications;
using FluentValidation.TestHelper;

namespace Eduva.Application.Test.Features.Classes.Queries.GetClasses
{
    [TestFixture]
    public class GetClassesValidatorTests
    {
        private GetClassesValidator _validator = null!;

        [SetUp]
        public void SetUp()
        {
            _validator = new GetClassesValidator();
        }

        [Test]
        public void Should_Have_Error_When_ClassSpecParam_Is_Null()
        {
            // Arrange
            var query = new GetClassesQuery(null!, Guid.NewGuid());

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.ClassSpecParam)
                .WithErrorMessage("Class specification parameters are required");
        }

        [Test]
        public void Should_Have_Error_When_UserId_Is_Empty()
        {
            // Arrange
            var query = new GetClassesQuery(new ClassSpecParam(), Guid.Empty);

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.UserId)
                .WithErrorMessage("User ID is required");
        }

        [Test]
        public void Should_Not_Have_Error_When_All_Properties_Are_Valid()
        {
            // Arrange
            var query = new GetClassesQuery(new ClassSpecParam(), Guid.NewGuid());

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Should_Not_Have_Error_When_ClassSpecParam_Has_Valid_Properties()
        {
            // Arrange
            var classSpecParam = new ClassSpecParam
            {
                PageIndex = 1,
                PageSize = 10,
                SearchTerm = "Test"
            };
            var query = new GetClassesQuery(classSpecParam, Guid.NewGuid());

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
using Eduva.Application.Features.Classes.Queries.GetClassById;
using FluentValidation.TestHelper;

namespace Eduva.Application.Test.Features.Classes.Queries.GetClassById
{
    [TestFixture]
    public class GetClassByIdValidatorTests
    {
        private GetClassByIdValidator _validator;

        [SetUp]
        public void SetUp()
        {
            _validator = new GetClassByIdValidator();
        }

        [Test]
        public void Should_Have_Error_When_Id_Is_Empty()
        {
            // Arrange
            var query = new GetClassByIdQuery(Guid.Empty, Guid.NewGuid());

            // Act & Assert
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor(x => x.Id)
                .WithErrorMessage("Class ID is required");
        }

        [Test]
        public void Should_Have_Error_When_UserId_Is_Empty()
        {
            // Arrange
            var query = new GetClassByIdQuery(Guid.NewGuid(), Guid.Empty);

            // Act & Assert
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor(x => x.UserId)
                .WithErrorMessage("User ID is required");
        }

        [Test]
        public void Should_Not_Have_Error_When_All_Properties_Are_Valid()
        {
            // Arrange
            var query = new GetClassByIdQuery(Guid.NewGuid(), Guid.NewGuid());

            // Act & Assert
            var result = _validator.TestValidate(query);
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
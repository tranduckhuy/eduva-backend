using Eduva.Application.Features.Questions.Commands.DeleteQuestion;
using FluentValidation.TestHelper;

namespace Eduva.Application.Test.Features.Questions.Commands.DeleteQuestion
{
    [TestFixture]
    public class DeleteQuestionValidatorTests
    {
        private DeleteQuestionValidator _validator = default!;

        #region Setup

        [SetUp]
        public void Setup()
        {
            _validator = new DeleteQuestionValidator();
        }

        #endregion

        #region Id Validation Tests

        [Test]
        public void Should_HaveError_When_IdIsEmpty()
        {
            // Arrange
            var model = new DeleteQuestionCommand
            {
                Id = Guid.Empty,
                DeletedByUserId = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(model);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Id);
        }

        #endregion

        #region Valid Request Tests

        [Test]
        public void Should_NotHaveAnyErrors_When_RequestIsValid()
        {
            // Arrange
            var model = new DeleteQuestionCommand
            {
                Id = Guid.NewGuid(),
                DeletedByUserId = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(model);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        #endregion

    }
}
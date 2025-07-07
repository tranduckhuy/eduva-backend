using Eduva.Application.Features.Questions.Commands.DeleteQuestionComment;
using FluentValidation.TestHelper;

namespace Eduva.Application.Test.Features.Questions.Commands.DeleteQuestionComment
{
    [TestFixture]
    public class DeleteQuestionCommentValidatorTests
    {
        private DeleteQuestionCommentValidator _validator = default!;

        #region Setup

        [SetUp]
        public void Setup()
        {
            _validator = new DeleteQuestionCommentValidator();
        }

        #endregion

        #region Id Validation Tests

        [Test]
        public void ShouldPass_WhenIdIsValid()
        {
            // Arrange
            var command = new DeleteQuestionCommentCommand { Id = Guid.NewGuid(), DeletedByUserId = Guid.NewGuid() };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Id);
        }

        [Test]
        public void ShouldFail_WhenIdIsEmpty()
        {
            // Arrange
            var command = new DeleteQuestionCommentCommand { Id = Guid.Empty, DeletedByUserId = Guid.NewGuid() };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Id)
                .WithErrorMessage("Comment ID is required.");
        }

        #endregion

        #region DeletedByUserId Validation Tests

        [Test]
        public void ShouldPass_WhenDeletedByUserIdIsValid()
        {
            // Arrange
            var command = new DeleteQuestionCommentCommand { Id = Guid.NewGuid(), DeletedByUserId = Guid.NewGuid() };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.DeletedByUserId);
        }

        [Test]
        public void ShouldFail_WhenDeletedByUserIdIsEmpty()
        {
            // Arrange
            var command = new DeleteQuestionCommentCommand { Id = Guid.NewGuid(), DeletedByUserId = Guid.Empty };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.DeletedByUserId)
                .WithErrorMessage("User ID is required.");
        }

        #endregion
    }
}
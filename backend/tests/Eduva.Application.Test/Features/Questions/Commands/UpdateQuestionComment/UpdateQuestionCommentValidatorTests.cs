using Eduva.Application.Features.Questions.Commands.UpdateQuestionComment;
using FluentValidation.TestHelper;

namespace Eduva.Application.Test.Features.Questions.Commands.UpdateQuestionComment
{
    [TestFixture]
    public class UpdateQuestionCommentValidatorTests
    {
        private UpdateQuestionCommentValidator _validator = default!;

        #region Setup

        [SetUp]
        public void Setup()
        {
            _validator = new UpdateQuestionCommentValidator();
        }

        #endregion

        #region Id Validation Tests

        [Test]
        public void Should_HaveError_When_IdIsEmpty()
        {
            // Arrange
            var model = new UpdateQuestionCommentCommand
            {
                Id = Guid.Empty,
                Content = "Valid content",
                UpdatedByUserId = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(model);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Id);
        }

        #endregion

        #region Content Validation Tests

        [Test]
        public void Should_HaveError_When_ContentIsEmpty()
        {
            // Arrange
            var model = new UpdateQuestionCommentCommand
            {
                Id = Guid.NewGuid(),
                Content = "",
                UpdatedByUserId = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(model);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Content);
        }

        [Test]
        public void Should_HaveError_When_ContentIsNull()
        {
            // Arrange
            var model = new UpdateQuestionCommentCommand
            {
                Id = Guid.NewGuid(),
                Content = null!,
                UpdatedByUserId = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(model);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Content);
        }

        [Test]
        public void Should_HaveError_When_ContentIsWhitespace()
        {
            // Arrange
            var model = new UpdateQuestionCommentCommand
            {
                Id = Guid.NewGuid(),
                Content = "   ",
                UpdatedByUserId = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(model);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Content);
        }

        #endregion

        #region UpdatedByUserId Validation Tests

        [Test]
        public void Should_HaveError_When_UpdatedByUserIdIsEmpty()
        {
            // Arrange
            var model = new UpdateQuestionCommentCommand
            {
                Id = Guid.NewGuid(),
                Content = "Valid content",
                UpdatedByUserId = Guid.Empty
            };

            // Act
            var result = _validator.TestValidate(model);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.UpdatedByUserId);
        }

        #endregion

        #region Valid Request Tests

        [Test]
        public void Should_NotHaveAnyErrors_When_RequestIsValid()
        {
            // Arrange
            var model = new UpdateQuestionCommentCommand
            {
                Id = Guid.NewGuid(),
                Content = "Valid updated content",
                UpdatedByUserId = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(model);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        #endregion

    }
}
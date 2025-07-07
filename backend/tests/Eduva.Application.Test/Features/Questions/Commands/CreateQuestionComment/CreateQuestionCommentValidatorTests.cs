using Eduva.Application.Features.Questions.Commands.CreateQuestionComment;
using FluentValidation.TestHelper;

namespace Eduva.Application.Test.Features.Questions.Commands.CreateQuestionComment
{
    [TestFixture]
    public class CreateQuestionCommentValidatorTests
    {
        private CreateQuestionCommentValidator _validator = default!;

        #region Setup

        [SetUp]
        public void Setup()
        {
            _validator = new CreateQuestionCommentValidator();
        }

        #endregion

        #region QuestionId Validation Tests

        [Test]
        public void Should_HaveError_When_QuestionIdIsEmpty()
        {
            // Arrange
            var model = new CreateQuestionCommentCommand
            {
                QuestionId = Guid.Empty,
                Content = "Valid content",
                CreatedByUserId = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(model);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.QuestionId);
        }

        #endregion

        #region Content Validation Tests

        [Test]
        public void Should_HaveError_When_ContentIsEmpty()
        {
            // Arrange
            var model = new CreateQuestionCommentCommand
            {
                QuestionId = Guid.NewGuid(),
                Content = "",
                CreatedByUserId = Guid.NewGuid()
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
            var model = new CreateQuestionCommentCommand
            {
                QuestionId = Guid.NewGuid(),
                Content = null!,
                CreatedByUserId = Guid.NewGuid()
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
            var model = new CreateQuestionCommentCommand
            {
                QuestionId = Guid.NewGuid(),
                Content = "   ",
                CreatedByUserId = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(model);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Content);
        }

        #endregion

        #region CreatedByUserId Validation Tests

        [Test]
        public void Should_HaveError_When_CreatedByUserIdIsEmpty()
        {
            // Arrange
            var model = new CreateQuestionCommentCommand
            {
                QuestionId = Guid.NewGuid(),
                Content = "Valid content",
                CreatedByUserId = Guid.Empty
            };

            // Act
            var result = _validator.TestValidate(model);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.CreatedByUserId);
        }

        #endregion

        #region Valid Request Tests

        [Test]
        public void Should_NotHaveAnyErrors_When_RequestIsValid()
        {
            // Arrange
            var model = new CreateQuestionCommentCommand
            {
                QuestionId = Guid.NewGuid(),
                Content = "Valid comment content",
                CreatedByUserId = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(model);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Should_NotHaveAnyErrors_When_RequestIsValidWithParentComment()
        {
            // Arrange
            var model = new CreateQuestionCommentCommand
            {
                QuestionId = Guid.NewGuid(),
                Content = "Valid reply content",
                ParentCommentId = Guid.NewGuid(),
                CreatedByUserId = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(model);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        #endregion

    }
}
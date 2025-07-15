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
            result.ShouldHaveValidationErrorFor(x => x.QuestionId)
                  .WithErrorMessage("Invalid Question ID format");
        }

        [Test]
        public void Should_NotHaveError_When_QuestionIdIsValid()
        {
            // Arrange
            var model = new CreateQuestionCommentCommand
            {
                QuestionId = Guid.NewGuid(),
                Content = "Valid content",
                CreatedByUserId = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(model);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.QuestionId);
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
                Content = string.Empty,
                CreatedByUserId = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(model);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Content)
                  .WithErrorMessage("Content is required");
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
            result.ShouldHaveValidationErrorFor(x => x.Content)
                  .WithErrorMessage("Content is required");
        }

        [Test]
        public void Should_NotHaveError_When_ContentIsValid()
        {
            // Arrange
            var model = new CreateQuestionCommentCommand
            {
                QuestionId = Guid.NewGuid(),
                Content = "Valid content",
                CreatedByUserId = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(model);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Content);
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
            result.ShouldHaveValidationErrorFor(x => x.CreatedByUserId)
                  .WithErrorMessage("Invalid User ID format");
        }

        [Test]
        public void Should_NotHaveError_When_CreatedByUserIdIsValid()
        {
            // Arrange
            var model = new CreateQuestionCommentCommand
            {
                QuestionId = Guid.NewGuid(),
                Content = "Valid content",
                CreatedByUserId = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(model);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.CreatedByUserId);
        }

        #endregion

        #region ParentCommentId Validation Tests

        [Test]
        public void Should_HaveError_When_ParentCommentIdIsEmpty()
        {
            // Arrange
            var model = new CreateQuestionCommentCommand
            {
                QuestionId = Guid.NewGuid(),
                Content = "Valid content",
                CreatedByUserId = Guid.NewGuid(),
                ParentCommentId = Guid.Empty
            };

            // Act
            var result = _validator.TestValidate(model);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.ParentCommentId)
                  .WithErrorMessage("Parent Comment ID must be valid when provided");
        }

        [Test]
        public void Should_NotHaveError_When_ParentCommentIdIsNull()
        {
            // Arrange
            var model = new CreateQuestionCommentCommand
            {
                QuestionId = Guid.NewGuid(),
                Content = "Valid content",
                CreatedByUserId = Guid.NewGuid(),
                ParentCommentId = null
            };

            // Act
            var result = _validator.TestValidate(model);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.ParentCommentId);
        }

        [Test]
        public void Should_NotHaveError_When_ParentCommentIdIsValid()
        {
            // Arrange
            var model = new CreateQuestionCommentCommand
            {
                QuestionId = Guid.NewGuid(),
                Content = "Valid content",
                CreatedByUserId = Guid.NewGuid(),
                ParentCommentId = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(model);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.ParentCommentId);
        }

        #endregion

        #region Multiple Validation Errors Tests

        [Test]
        public void Should_HaveMultipleErrors_When_AllFieldsAreInvalid()
        {
            // Arrange
            var model = new CreateQuestionCommentCommand
            {
                QuestionId = Guid.Empty,
                Content = string.Empty,
                CreatedByUserId = Guid.Empty,
                ParentCommentId = Guid.Empty
            };

            // Act
            var result = _validator.TestValidate(model);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.QuestionId);
            result.ShouldHaveValidationErrorFor(x => x.Content);
            result.ShouldHaveValidationErrorFor(x => x.CreatedByUserId);
            result.ShouldHaveValidationErrorFor(x => x.ParentCommentId);
        }

        #endregion
    }
}
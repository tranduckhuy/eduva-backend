using Eduva.Application.Features.Questions.Commands.UpdateQuestion;
using FluentValidation.TestHelper;

namespace Eduva.Application.Test.Features.Questions.Commands.UpdateQuestion
{
    [TestFixture]
    public class UpdateQuestionValidatorTests
    {
        private UpdateQuestionValidator _validator = default!;

        #region Setup

        [SetUp]
        public void Setup()
        {
            _validator = new UpdateQuestionValidator();
        }

        #endregion

        #region Id Validation Tests

        [Test]
        public void ShouldPass_WhenIdIsValid()
        {
            // Arrange
            var command = new UpdateQuestionCommand
            {
                Id = Guid.NewGuid(),
                Title = "Test Title",
                Content = "Test Content"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Id);
        }

        [Test]
        public void ShouldFail_WhenIdIsEmpty()
        {
            // Arrange
            var command = new UpdateQuestionCommand
            {
                Id = Guid.Empty,
                Title = "Test Title",
                Content = "Test Content"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Id)
                .WithErrorMessage("Question Id is required");
        }

        #endregion

        #region Title Validation Tests

        [Test]
        public void ShouldPass_WhenTitleIsValid()
        {
            // Arrange
            var command = new UpdateQuestionCommand
            {
                Id = Guid.NewGuid(),
                Title = "Test Title",
                Content = "Test Content"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Title);
        }

        [Test]
        public void ShouldFail_WhenTitleIsEmpty()
        {
            // Arrange
            var command = new UpdateQuestionCommand
            {
                Id = Guid.NewGuid(),
                Title = "",
                Content = "Test Content"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title)
                .WithErrorMessage("Title is required");
        }

        [Test]
        public void ShouldFail_WhenTitleIsNull()
        {
            // Arrange
            var command = new UpdateQuestionCommand
            {
                Id = Guid.NewGuid(),
                Title = null!,
                Content = "Test Content"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title)
                .WithErrorMessage("Title is required");
        }

        [Test]
        public void ShouldFail_WhenTitleExceeds255Characters()
        {
            // Arrange
            var command = new UpdateQuestionCommand
            {
                Id = Guid.NewGuid(),
                Title = new string('A', 256),
                Content = "Test Content"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title)
                .WithErrorMessage("Title must not exceed 255 characters");
        }

        [Test]
        public void ShouldPass_WhenTitleIsExactly255Characters()
        {
            // Arrange
            var command = new UpdateQuestionCommand
            {
                Id = Guid.NewGuid(),
                Title = new string('A', 255),
                Content = "Test Content"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Title);
        }

        #endregion

        #region Content Validation Tests

        [Test]
        public void ShouldPass_WhenContentIsValid()
        {
            // Arrange
            var command = new UpdateQuestionCommand
            {
                Id = Guid.NewGuid(),
                Title = "Test Title",
                Content = "Test Content"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Content);
        }

        [Test]
        public void ShouldFail_WhenContentIsEmpty()
        {
            // Arrange
            var command = new UpdateQuestionCommand
            {
                Id = Guid.NewGuid(),
                Title = "Test Title",
                Content = ""
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Content)
                .WithErrorMessage("Content is required");
        }

        [Test]
        public void ShouldFail_WhenContentIsNull()
        {
            // Arrange
            var command = new UpdateQuestionCommand
            {
                Id = Guid.NewGuid(),
                Title = "Test Title",
                Content = null!
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Content)
                .WithErrorMessage("Content is required");
        }

        #endregion
    }
}
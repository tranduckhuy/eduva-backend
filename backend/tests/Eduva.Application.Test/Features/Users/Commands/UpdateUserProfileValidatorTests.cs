using Eduva.Application.Features.Users.Commands;
using FluentValidation.TestHelper;

namespace Eduva.Application.Test.Features.Users.Commands
{
    [TestFixture]
    public class UpdateUserProfileValidatorTests
    {
        private UpdateUserProfileValidator _validator = default!;

        [SetUp]
        public void Setup()
        {
            _validator = new UpdateUserProfileValidator();
        }

        [Test]
        public void Validate_ShouldNotHaveAnyErrors_WhenAllFieldsAreEmpty()
        {
            // Arrange
            var command = new UpdateUserProfileCommand
            {
                FullName = string.Empty,
                PhoneNumber = string.Empty,
                AvatarUrl = string.Empty
            };

            // Act & Assert
            var result = _validator.TestValidate(command);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Validate_ShouldNotHaveAnyErrors_WhenAllFieldsAreValid()
        {
            // Arrange
            var command = new UpdateUserProfileCommand
            {
                FullName = "John Doe",
                PhoneNumber = "1234567890",
                AvatarUrl = "https://example.com/avatar.jpg"
            };

            // Act & Assert
            var result = _validator.TestValidate(command);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Validate_ShouldHaveError_WhenFullNameExceedsMaxLength()
        {
            // Arrange
            var command = new UpdateUserProfileCommand
            {
                FullName = new string('A', 101)
            };

            // Act & Assert
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.FullName)
                .WithErrorMessage("Full name must not exceed 100 characters.");
        }

        [Test]
        public void Validate_ShouldHaveError_WhenPhoneNumberIsInvalid()
        {
            // Arrange
            var command = new UpdateUserProfileCommand
            {
                PhoneNumber = "123" // Too short
            };

            // Act & Assert
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.PhoneNumber)
                .WithErrorMessage("Phone number must be between 10 and 15 digits.");
        }

        [Test]
        public void Validate_ShouldHaveError_WhenAvatarUrlIsInvalid()
        {
            // Arrange
            var command = new UpdateUserProfileCommand
            {
                AvatarUrl = "invalid-url"
            };

            // Act & Assert
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.AvatarUrl)
                .WithErrorMessage("Avatar URL must be a valid absolute URL.");
        }

        [Test]
        public void Validate_ShouldHaveMultipleErrors_WhenMultipleFieldsAreInvalid()
        {
            // Arrange
            var command = new UpdateUserProfileCommand
            {
                FullName = new string('A', 101),
                PhoneNumber = "123",
                AvatarUrl = "invalid-url"
            };

            // Act & Assert
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.FullName);
            result.ShouldHaveValidationErrorFor(x => x.PhoneNumber);
            result.ShouldHaveValidationErrorFor(x => x.AvatarUrl);
        }
    }
}

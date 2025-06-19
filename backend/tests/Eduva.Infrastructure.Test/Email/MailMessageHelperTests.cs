using Eduva.Domain.Entities;
using Eduva.Infrastructure.Email;

namespace Eduva.Infrastructure.Test.Email
{
    [TestFixture]
    public class MailMessageHelperTests
    {

        #region MailMessageHelper Tests

        // Verifies that the CreateMessage method builds an EmailMessage with the correct properties.
        [Test]
        public void CreateMessage_ShouldBuildCorrectEmailMessage()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Email = "user@example.com",
                FullName = "Test User"
            };
            var token = "abc123";
            var clientUrl = "https://example.com/confirm";
            var subject = "Confirm Your Account";
            var content = "confirm your account";

            // Act
            var message = MailMessageHelper.CreateMessage(user, token, clientUrl, subject, content);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(message, Is.Not.Null);
                Assert.That(message.To, Has.Count.EqualTo(1));
                Assert.That(message.To[0].Email, Is.EqualTo(user.Email));
                Assert.That(message.To[0].DisplayName, Is.EqualTo(user.FullName));
                Assert.That(message.Subject, Is.EqualTo(subject));
                Assert.That(message.Content, Does.Contain("https://example.com/confirm"));
                Assert.That(message.Content, Does.Contain("token=abc123"));
                Assert.That(message.Content, Does.Contain("email=user@example.com"));
                Assert.That(message.Content, Does.Contain("Please confirm your account by <a href='"));
            });
        }

        // Verifies that the CreateMessage method falls back to email when FullName is null.
        [Test]
        public void CreateMessage_ShouldFallbackToEmail_WhenFullNameIsNull()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Email = "user@example.com",
                FullName = null
            };
            var token = "abc123";
            var clientUrl = "https://example.com/reset";
            var subject = "Reset Password";
            var content = "reset your password";

            // Act
            var message = MailMessageHelper.CreateMessage(user, token, clientUrl, subject, content);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(message.To[0].DisplayName, Is.EqualTo(user.Email));
                Assert.That(message.Subject, Is.EqualTo(subject));
                Assert.That(message.Content, Does.Contain("token=abc123"));
                Assert.That(message.Content, Does.Contain("email=user@example.com"));
                Assert.That(message.Content, Does.Contain("Please reset your password by <a href='"));
            });
        }

        // Verifies that the CreateMessage method correctly appends query parameters to the client URL.
        [Test]
        public void CreateMessage_ShouldHandleUrlWithExistingQueryParameters()
        {
            // Arrange
            var user = new ApplicationUser { Email = "abc@x.com", FullName = "ABC" };
            var token = "t123";
            var clientUrl = "https://app.com/action?existing=1";
            var subject = "Join Class";
            var content = "join the class";

            // Act
            var message = MailMessageHelper.CreateMessage(user, token, clientUrl, subject, content);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(message.Content, Does.Contain("existing=1"));
                Assert.That(message.Content, Does.Contain("token=t123"));
                Assert.That(message.Content, Does.Contain("email=abc@x.com"));
            });
        }

        #endregion

    }
}
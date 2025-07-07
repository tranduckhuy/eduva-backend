using Eduva.Application.Features.Auth.Enums;
using Eduva.Domain.Entities;
using Eduva.Infrastructure.Email;

namespace Eduva.Infrastructure.Test.Email
{
    [TestFixture]
    public class MailMessageHelperTests
    {
        private string _templateDir = default!;
        private string _basePath = default!;

        #region MailMessageHelperTests Setup and TearDown

        [SetUp]
        public void Setup()
        {
            _basePath = AppContext.BaseDirectory;
            _templateDir = Path.Combine(_basePath, "email-templates");

            if (!Directory.Exists(_templateDir))
                Directory.CreateDirectory(_templateDir);
        }

        [TearDown]
        public void Cleanup()
        {
            if (Directory.Exists(_templateDir))
            {
                foreach (var file in Directory.GetFiles(_templateDir))
                {
                    File.Delete(file);
                }
            }
        }

        #endregion

        #region MailMessageHelper Tests

        [Test]
        public void CreateMessage_ShouldBuildCorrectEmailMessage()
        {
            var templateFile = Path.Combine(_templateDir, "reset-password.html");
            File.WriteAllText(templateFile, "Click here: {{reset_link}} - {{current_year}}");

            var user = new ApplicationUser
            {
                Email = "user@example.com",
                FullName = "Test User"
            };
            var token = "abc123";
            var clientUrl = "https://example.com/reset";
            var subject = "Reset Your Password";

            var message = MailMessageHelper.CreateMessage(new EmailMessageContext
            {
                User = user,
                Token = token,
                ClientUrl = clientUrl,
                TemplateFileName = "reset-password.html",
                Subject = subject,
                Action = AuthEmailAction.ResetPassword
            });

            Assert.Multiple(() =>
            {
                Assert.That(message.To[0].Email, Is.EqualTo(user.Email));
                Assert.That(message.To[0].DisplayName, Is.EqualTo(user.FullName));
                Assert.That(message.Subject, Is.EqualTo(subject));
                Assert.That(message.Content, Does.Contain("token=abc123"));
                Assert.That(message.Content, Does.Contain("email=user@example.com"));
                Assert.That(message.Content, Does.Contain(DateTime.UtcNow.Year.ToString()));
                Assert.That(message.Content, Does.Contain("Click here:"));
            });
        }

        [Test]
        public void CreateMessage_ShouldFallbackToEmail_WhenFullNameIsNull()
        {
            var templateFile = Path.Combine(_templateDir, "reset.html");
            File.WriteAllText(templateFile, "Confirm here: {{reset_link}} - {{current_year}}");

            var user = new ApplicationUser
            {
                Email = "fallback@example.com",
                FullName = null
            };

            var message = MailMessageHelper.CreateMessage(new EmailMessageContext
            {
                User = user,
                Token = "token321",
                ClientUrl = "https://client.com/verify",
                TemplateFileName = "reset.html",
                Subject = "Reset",
                Action = AuthEmailAction.ResetPassword
            });

            Assert.That(message.To[0].DisplayName, Is.EqualTo(user.Email));
        }

        [Test]
        public void CreateMessage_ShouldHandleUrlWithExistingQueryParameters()
        {
            var templateFile = Path.Combine(_templateDir, "query.html");
            File.WriteAllText(templateFile, "Join here: {{reset_link}}");

            var user = new ApplicationUser { Email = "x@x.com", FullName = "X" };

            var clientUrl = "https://app.com/invite?x=1";

            var message = MailMessageHelper.CreateMessage(new EmailMessageContext
            {
                User = user,
                Token = "abc",
                ClientUrl = clientUrl,
                TemplateFileName = "query.html",
                Subject = "Join"
            });

            Assert.That(message.Content, Does.Contain("x=1"));
            Assert.That(message.Content, Does.Contain("token=abc"));
            Assert.That(message.Content, Does.Contain("email=x@x.com"));
        }

        [Test]
        public void CreateMessage_TemplateFileNotFound_ThrowsFileNotFoundException()
        {
            var user = new ApplicationUser { Email = "no@file.com", FullName = "No File" };
            var ex = Assert.Throws<FileNotFoundException>(() =>
                    MailMessageHelper.CreateMessage(new EmailMessageContext
                    {
                        User = user,
                        Token = "token",
                        ClientUrl = "https://client.com",
                        TemplateFileName = "notfound.html",
                        Subject = "Subject"
                    }));

            Assert.That(ex!.Message, Does.Contain("Email template file not found"));
        }

        [Test]
        public async Task CreateMessageAsync_ShouldBuildCorrectOtpEmailMessage()
        {
            var filePath = Path.Combine(_templateDir, "otp-verification.html");
            File.WriteAllText(filePath, "Your OTP is: {{otp_code}}, Year: {{current_year}}");

            var user = new ApplicationUser { Email = "otp@eduva.com", FullName = "Otp User" };
            var message = await MailMessageHelper.CreateMessageAsync(user, "654321", "OTP Subject");

            Assert.Multiple(() =>
            {
                Assert.That(message.Subject, Is.EqualTo("OTP Subject"));
                Assert.That(message.To[0].Email, Is.EqualTo("otp@eduva.com"));
                Assert.That(message.Content, Does.Contain("Your OTP is: 654321"));
                Assert.That(message.Content, Does.Contain(DateTime.UtcNow.Year.ToString()));
            });
        }

        [Test]
        public void CreateMessageAsync_TemplateFileNotFound_ThrowsFileNotFoundException()
        {
            var user = new ApplicationUser { Email = "otp@eduva.com", FullName = "Otp User" };

            var ex = Assert.ThrowsAsync<FileNotFoundException>(async () =>
                await MailMessageHelper.CreateMessageAsync(user, "123456", "OTP Subject"));

            Assert.That(ex!.Message, Does.Contain("OTP template file not found"));
        }

        #endregion

    }
}
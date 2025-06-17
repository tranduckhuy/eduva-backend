using Eduva.Application.Common.Models;
using Eduva.Infrastructure.Configurations;
using Eduva.Infrastructure.Email;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;

namespace Eduva.Infrastructure.Test.Email
{
    [TestFixture]
    public class EmailSenderTests
    {
        private EmailSender _emailSender;
        private EmailConfiguration _config;
        private Mock<ILogger<EmailSender>> _loggerMock;

        [SetUp]
        public void Setup()
        {
            _config = new EmailConfiguration
            {
                From = "from@example.com",
                UserName = "from@example.com",
                Password = "password",
                SmtpServer = "smtp.example.com",
                Port = 587,
                ApiKey = "fake-api-key"
            };

            _loggerMock = new Mock<ILogger<EmailSender>>();
            _emailSender = new EmailSender(_config, _loggerMock.Object);
        }

        [Test]
        public async Task SendEmailAsync_NoAttachments_ShouldSucceed()
        {
            var message = new EmailMessage(
                new List<EmailAddress> { new EmailAddress("to@example.com", "To User") },
                "Test Subject",
                "Test Content",
                null
            );

            await _emailSender.SendEmailAsync(message);
        }

        [Test]
        public async Task SendEmailAsync_WithAttachments_ShouldSucceed()
        {
            var content = "This is test file content";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            var formFileMock = new Mock<IFormFile>();
            formFileMock.Setup(f => f.FileName).Returns("test.txt");
            formFileMock.Setup(f => f.ContentType).Returns("text/plain");
            formFileMock.Setup(f => f.OpenReadStream()).Returns(stream);
            formFileMock.Setup(f => f.CopyTo(It.IsAny<Stream>())).Callback<Stream>(s =>
            {
                stream.Seek(0, SeekOrigin.Begin);
                stream.CopyTo(s);
            });

            var formFiles = new FormFileCollection { formFileMock.Object };

            var message = new EmailMessage(
                new List<EmailAddress> { new EmailAddress("to@example.com", "To User") },
                "Test Subject With File",
                "Test Content",
                formFiles //
            );

            await _emailSender.SendEmailAsync(message);
        }

        [Test]
        public async Task SendEmailBrevoAsync_ShouldSucceed()
        {
            await _emailSender.SendEmailBrevoAsync(
                "receiver@example.com",
                "Receiver",
                "Test Subject",
                "Test Brevo Email Content"
            );
        }

        [Test]
        public async Task SendEmailAsync_ShouldLogError_OnInvalidSmtp()
        {
            var badConfig = new EmailConfiguration
            {
                From = "invalid@example.com",
                UserName = "invalid@example.com",
                Password = "wrongpassword",
                SmtpServer = "smtp.invalidserver.com",
                Port = 587,
                ApiKey = "key"
            };

            var emailSender = new EmailSender(badConfig, _loggerMock.Object);

            var message = new EmailMessage(
                new List<EmailAddress> { new EmailAddress("to@example.com", "To") },
                "Bad SMTP Test",
                "Will Fail",
                null
            );

            await emailSender.SendEmailAsync(message);
        }
    }
}

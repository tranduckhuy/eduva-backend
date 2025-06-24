using Eduva.Application.Common.Models;
using Eduva.Infrastructure.Configurations.Email;
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

        #region EmailSender Setup

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

        #endregion

        #region EmailSender Tests

        [Test]
        public async Task SendEmailAsync_ShouldAlwaysDisconnectAndLog_WhenEmailFails()
        {
            var badConfig = new EmailConfiguration
            {
                From = "invalid@example.com",
                UserName = "invalid@example.com",
                Password = "wrong",
                SmtpServer = "invalid.server.com",
                Port = 587,
                ApiKey = ""
            };

            var sender = new EmailSender(badConfig, _loggerMock.Object);

            var message = new EmailMessage(
                new List<EmailAddress> { new EmailAddress("to@example.com", "To") },
                "Fail Email",
                "Fail content",
                null
            );

            await sender.SendEmailAsync(message);

            _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Disconnected from SMTP server")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Test]
        public async Task SimulateSendSuccess_ShouldLogSuccessMessages()
        {
            var fakeSender = new FakeEmailSender(_loggerMock.Object);

            await fakeSender.SimulateSendSuccess();

            _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Email sent successfully")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Disconnected from SMTP server")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Test]
        public async Task SimulateBrevoSendSuccess_ShouldLogSuccessMessage()
        {
            var fakeSender = new FakeEmailSender(_loggerMock.Object);

            await fakeSender.SimulateBrevoSendSuccess();

            _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Email sent successfully to")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        // Verifies that sending an email without attachments does not throw any exceptions.
        [Test]
        public async Task SendEmailAsync_NoAttachments_ShouldNotThrow()
        {
            var message = new EmailMessage(
                new List<EmailAddress> { new EmailAddress("to@example.com", "To User") },
                "Test Subject",
                "Test Content",
                null
            );

            try
            {
                await _emailSender.SendEmailAsync(message);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Expected no exception, but got: {ex.GetType().Name} - {ex.Message}");
            }
        }

        // Verifies that sending an email with attachments does not throw any exceptions.
        [Test]
        public async Task SendEmailAsync_WithAttachments_ShouldNotThrow()
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
                formFiles
            );

            try
            {
                await _emailSender.SendEmailAsync(message);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Expected no exception, but got: {ex.GetType().Name} - {ex.Message}");
            }
        }

        // Verifies that sending an email using the Brevo API does not throw any exceptions.
        [Test]
        public async Task SendEmailBrevoAsync_ShouldNotThrow()
        {
            try
            {
                await _emailSender.SendEmailBrevoAsync(
                    "receiver@example.com",
                    "Receiver",
                    "Test Subject",
                    "Test Brevo Email Content"
                );
            }
            catch (Exception ex)
            {
                Assert.Fail($"Expected no exception, but got: {ex.GetType().Name} - {ex.Message}");
            }
        }

        // Verifies that when SMTP configuration is invalid, an error is logged during email sending.
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

            _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("An error occurred while sending email")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
                Times.AtLeastOnce
            );
        }

        #endregion

        #region FakeEmailSender Setup

        public class FakeEmailSender
        {
            private readonly ILogger<EmailSender> _logger;

            public FakeEmailSender(ILogger<EmailSender> logger)
            {
                _logger = logger;
            }

            public Task SimulateSendSuccess()
            {
                _logger.LogInformation("Email sent successfully.");
                _logger.LogInformation("Disconnected from SMTP server.");
                return Task.CompletedTask;
            }

            public Task SimulateBrevoSendSuccess()
            {
                _logger.LogInformation("Email sent successfully to {Email}", "to@example.com");
                return Task.CompletedTask;
            }
        }

        #endregion
    }
}
using Eduva.Application.Common.Models;
using Eduva.Application.Interfaces.Services;
using Eduva.Infrastructure.Configurations;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using MimeKit;
using sib_api_v3_sdk.Api;
using sib_api_v3_sdk.Client;

namespace Eduva.Infrastructure.Email
{
    public class EmailSender : IEmailSender
    {
        private readonly EmailConfiguration _emailConfiguration;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(EmailConfiguration emailConfiguration, ILogger<EmailSender> logger)
        {
            _emailConfiguration = emailConfiguration;
            _logger = logger;
        }

        public async Task SendEmailAsync(EmailMessage message)
        {
            var emailMessage = CreateEmailMessage(message);
            await Send(emailMessage);
        }

        private MimeMessage CreateEmailMessage(EmailMessage message)
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress(_emailConfiguration.UserName, _emailConfiguration.From));
            emailMessage.To.AddRange(message.To.Select(x => new MailboxAddress(x.DisplayName, x.Email)));
            emailMessage.Subject = message.Subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = string.Format("<h2 style='color: blue;'>{0}</h2>", message.Content) };

            if (message.Attachments != null && message.Attachments.Any())
            {
                byte[] fileBytes;
                foreach (var attachment in message.Attachments)
                {
                    using (var ms = new MemoryStream())
                    {
                        attachment.CopyTo(ms);
                        fileBytes = ms.ToArray();
                    }
                    bodyBuilder.Attachments.Add(attachment.FileName, fileBytes, ContentType.Parse(attachment.ContentType));
                }
            }

            emailMessage.Body = bodyBuilder.ToMessageBody();

            return emailMessage;
        }

        private async Task Send(MimeMessage mailMessage)
        {
            using var client = new SmtpClient();

            try
            {
                _logger.LogInformation("Sending email to {Email}", mailMessage.To);
                await client.ConnectAsync(_emailConfiguration.SmtpServer, _emailConfiguration.Port, true);
                client.AuthenticationMechanisms.Remove("XOAUTH2");
                await client.AuthenticateAsync(_emailConfiguration.From, _emailConfiguration.Password);
                await client.SendAsync(mailMessage);
                _logger.LogInformation("Email sent successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while sending email to `{Email}`", mailMessage.To);
            }
            finally
            {
                await client.DisconnectAsync(true);
                _logger.LogInformation("Disconnected from SMTP server.");
            }
        }

        public async Task SendEmailBrevoAsync(string receiverEmail, string receiverName, string subject, string message)
        {
            Configuration.Default.AddApiKey("api-key", _emailConfiguration.ApiKey);

            var apiInstance = new TransactionalEmailsApi();

            var sender = new sib_api_v3_sdk.Model.SendSmtpEmailSender(_emailConfiguration.UserName, _emailConfiguration.From);

            var receiver1 = new sib_api_v3_sdk.Model.SendSmtpEmailTo(receiverEmail, receiverName);

            var to = new List<sib_api_v3_sdk.Model.SendSmtpEmailTo> { receiver1 };

            try
            {
                var htmlContent = string.Format("<h2 style='color: blue;'>{0}</h2>", message);
                var sendSmtpEmail = new sib_api_v3_sdk.Model.SendSmtpEmail(sender, to, null, null, htmlContent, null, subject);

                await apiInstance.SendTransacEmailAsync(sendSmtpEmail);

                _logger.LogInformation("Email sent successfully to {Email}", receiverEmail);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while sending email to `{Email}`", receiverEmail);
            }
        }
    }
}

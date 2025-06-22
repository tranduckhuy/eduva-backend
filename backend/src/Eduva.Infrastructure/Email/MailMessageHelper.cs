using Eduva.Application.Common.Models;
using Eduva.Domain.Entities;
using Microsoft.AspNetCore.WebUtilities;

namespace Eduva.Infrastructure.Email
{
    public static class MailMessageHelper
    {
        public static EmailMessage CreateMessage(ApplicationUser user, string token, string clientUrl, string templateFileName, string subject)
        {
            var queryParams = new Dictionary<string, string?>
        {
            { "token", token },
            { "email", user.Email }
        };

            var link = QueryHelpers.AddQueryString(clientUrl, queryParams!);

            var basePath = AppContext.BaseDirectory;
            var templatePath = Path.Combine(basePath, "email-templates", templateFileName);

            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException("Email template file not found", templatePath);
            }

            var htmlContent = File.ReadAllText(templatePath)
                .Replace("{{reset_link}}", link)
                .Replace("{{current_year}}", DateTime.UtcNow.Year.ToString());

            return new EmailMessage(
                [new EmailAddress(user.Email!, user.FullName ?? user.Email!)],
                subject,
                htmlContent,
                null
            );
        }

        public static async Task<EmailMessage> CreateMessageAsync(ApplicationUser user, string otpCode, string subject)
        {
            var basePath = AppContext.BaseDirectory;
            var templatePath = Path.Combine(basePath, "email-templates", "otp-verification.html");

            if (!File.Exists(templatePath))
                throw new FileNotFoundException("OTP template file not found", templatePath);

            var template = await File.ReadAllTextAsync(templatePath);

            var htmlBody = template
                .Replace("{{otp_code}}", otpCode)
                .Replace("{{current_year}}", DateTime.UtcNow.Year.ToString());

            return new EmailMessage(
                [new EmailAddress(user.Email!, user.FullName ?? user.Email!)],
                subject,
                htmlBody,
                null
            );
        }
    }
}
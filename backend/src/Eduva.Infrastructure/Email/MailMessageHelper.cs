using Eduva.Application.Common.Models;
using Eduva.Domain.Entities;
using Microsoft.AspNetCore.WebUtilities;

namespace Eduva.Infrastructure.Email
{
    public static class MailMessageHelper
    {
        public static EmailMessage CreateMessage(EmailMessageContext context)
        {
            var queryParams = new Dictionary<string, string?>
            {
                { "token", context.Token },
                { "email", context.User.Email },
            };

            if (context.Action.HasValue)
            {
                queryParams.Add("action", context.Action.Value.ToString().ToLowerInvariant());
            }

            var link = QueryHelpers.AddQueryString(context.ClientUrl, queryParams!);

            var basePath = AppContext.BaseDirectory;
            var templatePath = Path.Combine(basePath, "email-templates", context.TemplateFileName);

            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException("Email template file not found", templatePath);
            }

            var htmlContent = File.ReadAllText(templatePath)
                .Replace("{{reset_link}}", link)
                .Replace("{{current_year}}", DateTime.UtcNow.Year.ToString());

            return new EmailMessage(
                [new EmailAddress(context.User.Email!, context.User.FullName ?? context.User.Email!)],
                context.Subject,
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
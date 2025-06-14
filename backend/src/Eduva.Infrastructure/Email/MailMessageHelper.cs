using Eduva.Application.Common.Models;
using Eduva.Domain.Entities;
using Microsoft.AspNetCore.WebUtilities;

namespace Eduva.Infrastructure.Email
{
    public static class MailMessageHelper
    {
        public static EmailMessage CreateMessage(ApplicationUser user, string token, string clientUrl, string subject, string content)
        {

            var param = new Dictionary<string, string>
            {
                { "token", token },
                { "email", user.Email! }
            };

            var callbackUrl = QueryHelpers.AddQueryString(clientUrl, param!);

            var message = new EmailMessage(
                [new EmailAddress(user.Email!, user.FullName ?? user.Email)],
                subject,
                $"Please {content} by <a href='{callbackUrl}'>clicking here</a>.",
                null
            );

            return message;
        }
    }
}

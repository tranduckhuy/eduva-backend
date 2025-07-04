using Eduva.Application.Common.Models;

namespace Eduva.Application.Interfaces.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(EmailMessage message);
        Task SendEmailBrevoHtmlAsync(string receiverEmail, string receiverName, string subject, string htmlContent);
    }
}

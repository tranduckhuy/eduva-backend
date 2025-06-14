using Eduva.Application.Common.Models;

namespace Eduva.Application.Common.Interfaces
{
    public interface IEmailSender
    {
        Task SendEmailAsync(EmailMessage message);
        Task SendEmailBrevoAsync(string receiverEmail, string receiverName, string subject, string message);
    }
}

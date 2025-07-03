using Microsoft.AspNetCore.Http;

namespace Eduva.Application.Common.Models
{
    public class EmailMessage
    {
        public List<EmailAddress> To { get; set; }
        public string Subject { get; set; }
        public string Content { get; set; }
        public IFormFileCollection? Attachments { get; set; }


        public EmailMessage(IEnumerable<EmailAddress> to, string subject, string content, IFormFileCollection? attachments)
        {
            To = to.ToList();
            Subject = subject;
            Content = content;

            if (attachments is not null)
            {
                Attachments = attachments;
            }
        }
    }
}
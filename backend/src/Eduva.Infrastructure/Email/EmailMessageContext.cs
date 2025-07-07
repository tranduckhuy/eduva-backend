using Eduva.Application.Features.Auth.Enums;
using Eduva.Domain.Entities;

namespace Eduva.Infrastructure.Email
{
    public class EmailMessageContext
    {
        public ApplicationUser User { get; set; } = default!;
        public string Token { get; set; } = default!;
        public string ClientUrl { get; set; } = default!;
        public string TemplateFileName { get; set; } = default!;
        public string Subject { get; set; } = default!;
        public AuthEmailAction? Action { get; set; }
    }
}

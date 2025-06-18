using Eduva.Domain.Enums;

namespace Eduva.Application.Features.Schools.Reponses
{
    public class SchoolResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? WebsiteUrl { get; set; }
        public EntityStatus Status { get; set; }
    }
}
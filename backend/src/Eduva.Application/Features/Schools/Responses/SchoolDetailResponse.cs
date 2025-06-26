using Eduva.Domain.Enums;

namespace Eduva.Application.Features.Schools.Responses
{
    public class SchoolDetailResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? WebsiteUrl { get; set; }
        public EntityStatus Status { get; set; }

        public Guid? SchoolAdminId { get; set; }
        public string? SchoolAdminFullName { get; set; }
        public string? SchoolAdminEmail { get; set; }
    }
}